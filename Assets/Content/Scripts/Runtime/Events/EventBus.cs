using System;
using System.Collections.Generic;

namespace LittleHeroJourney
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new Dictionary<Type, List<Delegate>>();
        private static readonly object _lock = new object();

        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            lock (_lock)
            {
                var t = typeof(T);
                if (!_handlers.TryGetValue(t, out var list))
                {
                    list = new List<Delegate>();
                    _handlers[t] = list;
                }
                list.Add(handler);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null) return;
            lock (_lock)
            {
                var t = typeof(T);
                if (_handlers.TryGetValue(t, out var list))
                    list.Remove(handler);
            }
        }

        public static void Publish<T>(T data)
        {
            List<Delegate> copy;
            lock (_lock)
            {
                var t = typeof(T);
                if (!_handlers.TryGetValue(t, out var list) || list.Count == 0)
                    return;
                copy = new List<Delegate>(list);
            }
            var action = (Action<T>)null;
            foreach (var d in copy)
            {
                try
                {
                    action = (Action<T>)d;
                    action?.Invoke(data);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogException(e);
                }
            }
        }

        public static void Clear<T>()
        {
            lock (_lock)
            {
                var t = typeof(T);
                if (_handlers.TryGetValue(t, out var list))
                    list.Clear();
            }
        }

        public static void ClearAll()
        {
            lock (_lock)
                _handlers.Clear();
        }
    }
}
