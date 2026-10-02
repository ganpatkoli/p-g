using System.Collections.Generic;
using System.Linq;
using PoolGame.Client.CameraRig;
using PoolGame.Client.Core;
using PoolGame.Client.Input;
using PoolGame.Client.Physics;
using PoolGame.Core.Geometry;
using PoolGame.Core.Physics;
using PoolGame.Core.Rules;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PoolGame.Client.Gameplay
{
    /// <summary>
    /// Drives one table: place cue ball -> aim -> submit shot -> replay authoritative result -> apply rules.
    /// It never decides outcomes itself: physics comes from an IShotResolver, outcomes from an IRuleSet.
    /// </summary>
    public sealed class ShotController : MonoBehaviour
    {
        public enum Phase { Placing, Aiming, Playing, GameOver }

        PhysicsConfig _cfg; TableConfig _table;
        IShotResolver _resolver; IRuleSet _rules; EventBus _bus;
        AimInput _input; AimCameraRig _camRig;

        RulesState _state;
        List<BallState> _balls;
        readonly Dictionary<int, BallView> _views = new Dictionary<int, BallView>();
        CueView _cue; AimGuideView _guide;

        Phase _phase;
        double _angle;
        Vec2 _placePos;

        ResolvedShot _resolved; TrajectoryPlayer _player; BallState[] _frame; List<ShotEvent> _pending; double _t;

        // read-only view for HUD
        public Phase CurrentPhase => _phase;
        public RulesState State => _state;
        public float Power => _input.Power;
        public Vector2 Spin => _input.Spin;
        public string Message { get; private set; } = "";

        public void Init(PhysicsConfig cfg, TableConfig table, IShotResolver resolver, IRuleSet rules, RackType rack,
                         EventBus bus, AimInput input, AimCameraRig camRig, ulong seed)
        {
            _cfg = cfg; _table = table; _resolver = resolver; _rules = rules; _bus = bus; _input = input; _camRig = camRig;

            _balls = Rack.Build(rack, cfg, table, seed);
            _state = rules.CreateInitialState(0);
            float r = (float)cfg.BallRadius;
            foreach (var b in _balls) _views[b.Id] = BallView.Create(transform, b.Id, r);
            _cue = CueView.Create(transform);
            _guide = AimGuideView.Create(transform, r);
            _camRig.Target = _views[BallState.CueBallId].transform;
            SyncViews();
            EnterPlacing($"Player {_state.CurrentPlayer + 1}: place the cue ball behind the head string and click");
        }

        BallState Cue => _balls.First(b => b.Id == BallState.CueBallId);

        void Update()
        {
            if (_input == null) return;
            _input.Tick(Time.deltaTime);
            var kb = Keyboard.current;
            if (kb != null && kb.cKey.wasPressedThisFrame) _camRig.Toggle();

            switch (_phase)
            {
                case Phase.Placing: UpdatePlacing(); break;
                case Phase.Aiming: UpdateAiming(); break;
                case Phase.Playing: UpdatePlayback(); break;
            }
        }

        // ---------------- placing ----------------
        void EnterPlacing(string msg)
        {
            _phase = Phase.Placing; Message = msg;
            _cue.Show(false); _guide.Show(false);
            _camRig.CurrentMode = AimCameraRig.Mode.Overview;
            _placePos = Cue.Position;
        }

        void UpdatePlacing()
        {
            var hit = _input.HasPointer ? _camRig.ScreenToTable(_input.PointerScreen) : null;
            if (hit.HasValue)
            {
                var p = ViewMath.ToCore(hit.Value);
                if (BallPlacement.IsLegal(p, _balls, _cfg, _table, _state.IsBreak))
                {
                    _placePos = p;
                    _views[BallState.CueBallId].transform.position = ViewMath.ToWorld(p, (float)_cfg.BallRadius);
                    if (_input.PointerPressed) ConfirmPlacement();
                }
            }
        }

        void ConfirmPlacement()
        {
            Cue.Position = _placePos;
            _camRig.CurrentMode = AimCameraRig.Mode.Aim;
            _phase = Phase.Aiming;
            Message = $"Player {_state.CurrentPlayer + 1}: aim, set power, click to shoot";
        }

        // ---------------- aiming ----------------
        void UpdateAiming()
        {
            var cue = Cue;
            Vector3 cueWorld = ViewMath.ToWorld(cue.Position, (float)_cfg.BallRadius);
            if (_input.HasPointer)
            {
                var hit = _camRig.ScreenToTable(_input.PointerScreen);
                if (hit.HasValue)
                {
                    var d = ViewMath.ToCore(hit.Value) - cue.Position;
                    if (d.SqrLength > 1e-6) _angle = System.Math.Atan2(d.Y, d.X);
                }
            }
            _camRig.SetAimDirection((float)_angle);
            _bus.Publish(new AimChanged(_angle));

            var prediction = AimPredictor.Predict(_balls, _angle, _cfg, _table);
            _guide.Show(true); _cue.Show(true);
            _guide.UpdateGuide(prediction, cueWorld, (float)_cfg.BallRadius);
            _cue.Place(cueWorld, (float)_angle, _input.Power, _input.Spin, (float)_cfg.BallRadius);

            if (_input.ShootPressed) Fire();
        }

        // ---------------- firing / replay ----------------
        void Fire()
        {
            var shot = new ShotParameters(_angle, _input.Power, _input.Spin.x, _input.Spin.y);
            if (ShotValidator.Validate(shot) != ShotRejection.None) return;

            var resolved = _resolver.Resolve(_balls, shot);
            if (resolved.Result.Rejection != ShotRejection.None) { Message = "Shot rejected: " + resolved.Result.Rejection; return; }

            _resolved = resolved;
            _player = new TrajectoryPlayer(resolved.Trajectory);
            _frame = new BallState[_player.BallCount];
            _pending = resolved.Result.Events.OrderBy(e => e.Time).ToList();
            _t = 0;
            _phase = Phase.Playing; Message = "";
            _cue.Show(false); _guide.Show(false);
            _camRig.CurrentMode = AimCameraRig.Mode.Overview;
            _bus.Publish(new ShotFired(shot, _state.CurrentPlayer));
        }

        void UpdatePlayback()
        {
            float dt = Time.deltaTime;
            _t += dt;
            _player.Sample(_t, _frame);
            float r = (float)_cfg.BallRadius;
            foreach (var s in _frame)
                if (_views.TryGetValue(s.Id, out var v)) v.Apply(s, r, dt);

            while (_pending.Count > 0 && _pending[0].Time <= _t)
            {
                _bus.Publish(new BallEvent(_pending[0]));
                _pending.RemoveAt(0);
            }
            if (_t >= _player.Duration) Settle();
        }

        void Settle()
        {
            var result = _resolved.Result;
            var outcome = _rules.Resolve(_state, ShotSummary.From(result.Events));

            _balls = result.FinalBalls.Select(b => b.Clone()).ToList();
            foreach (var b in _balls) { b.Velocity = Vec2.Zero; b.Wx = b.Wy = b.Wz = 0; }

            foreach (int id in outcome.RespotBallIds)
            {
                var ball = _balls.First(b => b.Id == id);
                var others = _balls.Where(b => b.Id != id).ToList();
                ball.Position = BallPlacement.FindFreeSpot(_table.FootSpot, others, _cfg, _table);
                ball.OnTable = true;
            }
            var cue = Cue;
            if (!cue.OnTable)
            {
                cue.OnTable = true;
                cue.Position = BallPlacement.FindFreeSpot(_table.HeadSpot, _balls.Where(b => b.Id != 0).ToList(), _cfg, _table);
            }

            _state = outcome.NewState;
            SyncViews();
            _bus.Publish(new ShotSettled(outcome));

            if (_state.IsOver)
            {
                _phase = Phase.GameOver;
                Message = $"Player {_state.Winner + 1} wins ({_state.WinReason})";
                _bus.Publish(new GameFinished(_state.Winner, _state.WinReason));
                return;
            }

            string foul = outcome.IsFoul ? "Foul: " + string.Join(", ", outcome.Fouls) + ". " : "";
            if (_state.BallInHand) EnterPlacing($"{foul}Player {_state.CurrentPlayer + 1} has ball in hand");
            else
            {
                _phase = Phase.Aiming;
                _camRig.CurrentMode = AimCameraRig.Mode.Aim;
                Message = $"{foul}Player {_state.CurrentPlayer + 1} to shoot";
            }
        }

        void SyncViews()
        {
            float r = (float)_cfg.BallRadius;
            foreach (var b in _balls) _views[b.Id].Apply(b, r, 0f);
        }
    }
}
