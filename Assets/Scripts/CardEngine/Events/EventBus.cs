using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _listeners = new();

        public void Subscribe<T>(Action<T> callback) where T : IGameEvent
        {
            var type = typeof(T);

            if (!_listeners.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _listeners[type] = list;
            }

            if (!list.Contains(callback))
                list.Add(callback);
        }

        public void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
        {
            var type = typeof(T);
            if (!_listeners.TryGetValue(type, out var list))
                return;

            list.Remove(callback);
            if (list.Count == 0)
                _listeners.Remove(type);
        }

        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);

            if (!_listeners.TryGetValue(type, out var list))
                return;

            // Snapshot to avoid issues if listeners mutate subscriptions while handling events.
            var snapshot = list.ToArray();
            foreach (var del in snapshot)
                ((Action<T>)del).Invoke(gameEvent);
        }
    }
}