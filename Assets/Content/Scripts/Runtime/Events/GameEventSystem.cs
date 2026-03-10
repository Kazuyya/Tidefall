using System;
using System.Collections.Generic;

namespace LittleHeroJourney
{
    public struct UIActionEvent
    {
        public string ActionId;
        public UIActionEvent(string actionId) => ActionId = actionId;
    }

    public static class GameEventSystem
    {
        private static readonly Dictionary<string, List<Action>> _actionHandlers = new Dictionary<string, List<Action>>();
        private static readonly object _lock = new object();

        public static void SubscribeAction(string actionId, Action handler)
        {
            if (string.IsNullOrEmpty(actionId) || handler == null) return;
            lock (_lock)
            {
                if (!_actionHandlers.TryGetValue(actionId, out var list))
                {
                    list = new List<Action>();
                    _actionHandlers[actionId] = list;
                }
                list.Add(handler);
            }
        }

        public static void UnsubscribeAction(string actionId, Action handler)
        {
            if (string.IsNullOrEmpty(actionId) || handler == null) return;
            lock (_lock)
            {
                if (_actionHandlers.TryGetValue(actionId, out var list))
                    list.Remove(handler);
            }
        }

        public static void Publish(UIActionEvent e)
        {
            if (string.IsNullOrEmpty(e.ActionId)) return;
            List<Action> actionCopy;
            lock (_lock)
            {
                if (!_actionHandlers.TryGetValue(e.ActionId, out var list) || list.Count == 0) return;
                actionCopy = new List<Action>(list);
            }
            foreach (var a in actionCopy)
            {
                try { a?.Invoke(); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }

        public static void ClearAll()
        {
            lock (_lock)
                _actionHandlers.Clear();
        }
    }
}