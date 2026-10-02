using System;
using System.Collections.Generic;

namespace PoolGame.Client.Core
{
    /// <summary>Tiny typed publish/subscribe bus. Systems talk through events instead of referencing each other.</summary>
    public sealed class EventBus
    {
        readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public void Subscribe<T>(Action<T> handler)
        {
            _handlers.TryGetValue(typeof(T), out var existing);
            _handlers[typeof(T)] = Delegate.Combine(existing, handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            if (!_handlers.TryGetValue(typeof(T), out var existing)) return;
            var next = Delegate.Remove(existing, handler);
            if (next == null) _handlers.Remove(typeof(T)); else _handlers[typeof(T)] = next;
        }

        public void Publish<T>(T evt)
        {
            if (_handlers.TryGetValue(typeof(T), out var d)) ((Action<T>)d)?.Invoke(evt);
        }
    }
}
