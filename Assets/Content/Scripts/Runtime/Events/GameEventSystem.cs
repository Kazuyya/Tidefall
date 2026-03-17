using System;
using System.Collections.Generic;

namespace LittleHeroJourney
{
    public struct UIActionEvent
    {
        public string ActionId;
        public string Payload;
        public UIActionEvent(string actionId, string payload = null)
        {
            ActionId = actionId;
            Payload = payload;
        }
    }

    public static class GameEventSystem
    {
        private static readonly Dictionary<string, List<Action>> _actionHandlers = new Dictionary<string, List<Action>>();
        private static readonly Dictionary<string, List<Action<string>>> _actionPayloadHandlers = new Dictionary<string, List<Action<string>>>();
        private static readonly Dictionary<string, List<Action<float, float>>> _healthHandlers = new Dictionary<string, List<Action<float, float>>>();
        private static readonly Dictionary<string, List<Action>> _healthDeathHandlers = new Dictionary<string, List<Action>>();
        private static readonly Dictionary<string, (float current, float max)> _lastHealthPerId = new Dictionary<string, (float, float)>();
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

        public static void SubscribeActionWithPayload(string actionId, Action<string> handler)
        {
            if (string.IsNullOrEmpty(actionId) || handler == null) return;
            lock (_lock)
            {
                if (!_actionPayloadHandlers.TryGetValue(actionId, out var list))
                {
                    list = new List<Action<string>>();
                    _actionPayloadHandlers[actionId] = list;
                }
                list.Add(handler);
            }
        }

        public static void UnsubscribeActionWithPayload(string actionId, Action<string> handler)
        {
            if (string.IsNullOrEmpty(actionId) || handler == null) return;
            lock (_lock)
            {
                if (_actionPayloadHandlers.TryGetValue(actionId, out var list))
                    list.Remove(handler);
            }
        }

        public static void Publish(UIActionEvent e)
        {
            if (string.IsNullOrEmpty(e.ActionId)) return;
            List<Action> actionCopy;
            List<Action<string>> payloadCopy = null;
            lock (_lock)
            {
                if (_actionHandlers.TryGetValue(e.ActionId, out var list) && list.Count > 0)
                    actionCopy = new List<Action>(list);
                else
                    actionCopy = new List<Action>();
                if (!string.IsNullOrEmpty(e.Payload) && _actionPayloadHandlers.TryGetValue(e.ActionId, out var payloadList) && payloadList.Count > 0)
                    payloadCopy = new List<Action<string>>(payloadList);
            }
            foreach (var a in actionCopy)
            {
                try { a?.Invoke(); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
            if (payloadCopy != null)
            {
                foreach (var p in payloadCopy)
                {
                    try { p?.Invoke(e.Payload); }
                    catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
                }
            }
        }

        #region Health events (by ID: set/update health bar, and death for bar hide)

        public static void SubscribeHealth(string healthId, Action<float, float> handler)
        {
            if (string.IsNullOrEmpty(healthId) || handler == null) return;
            lock (_lock)
            {
                if (!_healthHandlers.TryGetValue(healthId, out var list))
                {
                    list = new List<Action<float, float>>();
                    _healthHandlers[healthId] = list;
                }
                list.Add(handler);
                if (_lastHealthPerId.TryGetValue(healthId, out var last))
                {
                    try { handler.Invoke(last.current, last.max); }
                    catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
                }
            }
        }

        public static void UnsubscribeHealth(string healthId, Action<float, float> handler)
        {
            if (string.IsNullOrEmpty(healthId) || handler == null) return;
            lock (_lock)
            {
                if (_healthHandlers.TryGetValue(healthId, out var list))
                    list.Remove(handler);
            }
        }

        public static void PublishHealth(string healthId, float current, float max)
        {
            if (string.IsNullOrEmpty(healthId)) return;
            List<Action<float, float>> copy;
            lock (_lock)
            {
                _lastHealthPerId[healthId] = (current, max);
                if (!_healthHandlers.TryGetValue(healthId, out var list) || list.Count == 0) return;
                copy = new List<Action<float, float>>(list);
            }
            foreach (var h in copy)
            {
                try { h?.Invoke(current, max); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }

        public static void SubscribeHealthDeath(string healthId, Action handler)
        {
            if (string.IsNullOrEmpty(healthId) || handler == null) return;
            lock (_lock)
            {
                if (!_healthDeathHandlers.TryGetValue(healthId, out var list))
                {
                    list = new List<Action>();
                    _healthDeathHandlers[healthId] = list;
                }
                list.Add(handler);
            }
        }

        public static void UnsubscribeHealthDeath(string healthId, Action handler)
        {
            if (string.IsNullOrEmpty(healthId) || handler == null) return;
            lock (_lock)
            {
                if (_healthDeathHandlers.TryGetValue(healthId, out var list))
                    list.Remove(handler);
            }
        }

        public static void PublishHealthDeath(string healthId)
        {
            if (string.IsNullOrEmpty(healthId)) return;
            List<Action> copy;
            lock (_lock)
            {
                if (!_healthDeathHandlers.TryGetValue(healthId, out var list) || list.Count == 0) return;
                copy = new List<Action>(list);
            }
            foreach (var h in copy)
            {
                try { h?.Invoke(); }
                catch (Exception ex) { UnityEngine.Debug.LogException(ex); }
            }
        }

        #endregion

        public static void ClearAll()
        {
            lock (_lock)
            {
                _actionHandlers.Clear();
                _actionPayloadHandlers.Clear();
                _healthHandlers.Clear();
                _healthDeathHandlers.Clear();
                _lastHealthPerId.Clear();
            }
        }
    }
}