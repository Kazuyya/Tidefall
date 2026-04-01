using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney.UI
{
    public class TutorialFlowManager : MonoBehaviour
    {
        [SerializeField] private TutorialManager tutorialManager;
        [SerializeField] private string tutorialCanvasId = "Tutorial";
        [SerializeField] private string openTutorialActionId = "OpenTutorial";
        [SerializeField] private string closeTutorialActionId = "CloseTutorial";
        [SerializeField] private List<TutorialBinding> bindings = new List<TutorialBinding>();
        [SerializeField] private bool showDebugLog = false;

        [Serializable]
        public class TutorialBinding
        {
            public string tutorialId;
            public TutorialSequenceSO sequence;
        }

        private Action<string> _openTutorialPayloadHandler;
        private Action _closeTutorialHandler;
        private Action<string> _canvasOpenCompletePayloadHandler;
        private TutorialSequenceSO _pendingSequence;

        private void OnEnable()
        {
            _openTutorialPayloadHandler = HandleOpenTutorial;
            _closeTutorialHandler = HandleCloseTutorial;
            _canvasOpenCompletePayloadHandler = HandleCanvasOpenComplete;

            if (!string.IsNullOrEmpty(OpenActionId))
                GameEventSystem.SubscribeActionWithPayload(OpenActionId, _openTutorialPayloadHandler);
            if (!string.IsNullOrEmpty(CloseActionId))
                GameEventSystem.SubscribeAction(CloseActionId, _closeTutorialHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompletePayloadHandler);
        }

        private void OnDisable()
        {
            if (!string.IsNullOrEmpty(OpenActionId) && _openTutorialPayloadHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload(OpenActionId, _openTutorialPayloadHandler);
            if (!string.IsNullOrEmpty(CloseActionId) && _closeTutorialHandler != null)
                GameEventSystem.UnsubscribeAction(CloseActionId, _closeTutorialHandler);
            if (_canvasOpenCompletePayloadHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompletePayloadHandler);
        }

        private string TutorialCanvasId => !string.IsNullOrEmpty(tutorialCanvasId) ? tutorialCanvasId : "Tutorial";
        private string OpenActionId => !string.IsNullOrEmpty(openTutorialActionId) ? openTutorialActionId : "OpenTutorial";
        private string CloseActionId => !string.IsNullOrEmpty(closeTutorialActionId) ? closeTutorialActionId : "CloseTutorial";

        private TutorialSequenceSO GetSequence(string tutorialId)
        {
            if (bindings == null || bindings.Count == 0) return null;
            for (int i = 0; i < bindings.Count; i++)
            {
                var b = bindings[i];
                if (b == null || string.IsNullOrEmpty(b.tutorialId)) continue;
                if (string.Equals(b.tutorialId, tutorialId, StringComparison.OrdinalIgnoreCase))
                    return b.sequence;
            }
            return null;
        }

        private void HandleOpenTutorial(string tutorialId)
        {
            var sequence = GetSequence(tutorialId);
            if (sequence == null)
            {
                if (showDebugLog) Debug.Log("[TutorialFlowManager] Sequence not found for tutorialId=" + tutorialId);
                return;
            }

            _pendingSequence = sequence;
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null)
                GameManager.Instance.CanvasManager.SwitchCanvas(TutorialCanvasId);
            else
                ApplySequenceNow();
        }

        private void HandleCanvasOpenComplete(string canvasId)
        {
            if (_pendingSequence == null) return;
            if (!string.Equals(canvasId, TutorialCanvasId, StringComparison.OrdinalIgnoreCase)) return;
            ApplySequenceNow();
        }

        private void ApplySequenceNow()
        {
            var sequence = _pendingSequence;
            _pendingSequence = null;
            if (sequence == null) return;

            if (tutorialManager == null)
                tutorialManager = FindObjectOfType<TutorialManager>(true);
            if (tutorialManager == null)
            {
                if (showDebugLog) Debug.LogWarning("[TutorialFlowManager] TutorialManager not found.");
                return;
            }

            if (!tutorialManager.gameObject.activeSelf)
                tutorialManager.gameObject.SetActive(true);

            tutorialManager.SetSequence(sequence, true);
        }

        private void HandleCloseTutorial()
        {
            if (tutorialManager == null)
                tutorialManager = FindObjectOfType<TutorialManager>(true);
            if (tutorialManager != null)
                tutorialManager.CloseTutorial();
        }
    }
}
