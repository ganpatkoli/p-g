using System;
using PoolGame.Client.CameraRig;
using PoolGame.Client.Core;
using PoolGame.Client.Gameplay;
using PoolGame.Client.Input;
using PoolGame.Client.Physics;
using PoolGame.Client.UI;
using PoolGame.Core.Physics;
using PoolGame.Core.Rules;
using UnityEngine;

namespace PoolGame.Client.Bootstrap
{
    /// <summary>
    /// Composition root. Only wires objects together; holds no game logic.
    /// Press Play in any empty scene and a local practice table appears (until real scenes exist).
    /// Optional overrides: Resources/PhysicsConfig and Resources/TableConfig assets (Create > PoolGame).
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public GameType Game = GameType.EightBall;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (FindFirstObjectByType<GameBootstrap>() != null) return;
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
        }

        void Start()
        {
            var physicsAsset = Resources.Load<PhysicsConfigAsset>("PhysicsConfig");
            var tableAsset = Resources.Load<TableConfigAsset>("TableConfig");
            var cfg = physicsAsset != null ? physicsAsset.ToConfig() : new PhysicsConfig();
            var table = tableAsset != null ? tableAsset.ToConfig() : new TableConfig();
            cfg.Validate();

            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<UnityEngine.Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.07f);
            cam.nearClipPlane = 0.05f;
            if (!cam.TryGetComponent<AimCameraRig>(out var rig)) rig = cam.gameObject.AddComponent<AimCameraRig>();

            if (FindFirstObjectByType<Light>() == null)
            {
                var l = new GameObject("Light").AddComponent<Light>();
                l.type = LightType.Directional;
                l.transform.rotation = Quaternion.Euler(60, 30, 0);
            }

            TableView.Build(table);

            var rack = Game == GameType.EightBall ? RackType.EightBall : RackType.NineBall;
            var controller = new GameObject("ShotController").AddComponent<ShotController>();
            controller.Init(cfg, table, new LocalShotResolver(cfg, table), RuleSets.Create(Game), rack,
                            new EventBus(), new AimInput(), rig, (ulong)DateTime.UtcNow.Ticks);

            new GameObject("Hud").AddComponent<DebugHud>().Controller = controller;
        }
    }
}
