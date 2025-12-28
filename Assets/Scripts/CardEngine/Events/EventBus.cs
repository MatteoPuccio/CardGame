using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public class EventBus
    {
        private readonly Dictionary<Type, List<Action<IGameEvent>>> Listeners = new();

        public void Subscribe<T>(Action<T> callback) where T : IGameEvent
        {
            var type = typeof(T);
            if (!Listeners.ContainsKey(type))
                Listeners[type] = new List<Action<IGameEvent>>();

            Listeners[type].Add(e => callback((T)e));
        }

        public void Publish<T>(T gameEvent) where T : IGameEvent
        {
            var type = typeof(T);
            if (Listeners.TryGetValue(type, out var actions))
            {
                foreach (var action in actions)
                    action.Invoke(gameEvent);
            }
        }
    }
}