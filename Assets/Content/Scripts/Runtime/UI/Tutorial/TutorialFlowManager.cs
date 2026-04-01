using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney.UI
{
    public class TutorialFlowManager : MonoBehaviour
    {
        private const bool ForceTutorialTrace = true;
        private static TutorialFlowManager _activeInstance;
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
        private Action<string> _canvasOpenStartPayloadHandler;
        private Action<string> _canvasOpenCompletePayloadHandler;
        private TutorialSequenceSO _pendingSequence;
        private string _pendingTutorialId;

        private void OnEnable()
        {
            if (_activeInstance != null && _activeInstance != this)
            {
                if (showDebugLog) Debug.LogWarning("[TutorialFlowManager] Duplicate instance detected, disabling this component.");
                enabled = false;
                return;
            }
            _activeInstance = this;
            _openTutorialPayloadHandler = HandleOpenTutorial;
            _closeTutorialHandler = HandleCloseTutorial;
            _canvasOpenStartPayloadHandler = HandleCanvasOpenStart;
            _canvasOpenCompletePayloadHandler = HandleCanvasOpenComplete;

            if (!string.IsNullOrEmpty(OpenActionId))
                GameEventSystem.SubscribeActionWithPayload(OpenActionId, _openTutorialPayloadHandler);
            if (!string.IsNullOrEmpty(CloseActionId))
                GameEventSystem.SubscribeAction(CloseActionId, _closeTutorialHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenStart", _canvasOpenStartPayloadHandler);
            GameEventSystem.SubscribeActionWithPayload("CanvasOpenComplete", _canvasOpenCompletePayloadHandler);
        }

        private void OnDisable()
        {
            if (_activeInstance == this) _activeInstance = null;
            if (!string.IsNullOrEmpty(OpenActionId) && _openTutorialPayloadHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload(OpenActionId, _openTutorialPayloadHandler);
            if (!string.IsNullOrEmpty(CloseActionId) && _closeTutorialHandler != null)
                GameEventSystem.UnsubscribeAction(CloseActionId, _closeTutorialHandler);
            if (_canvasOpenStartPayloadHandler != null)
                GameEventSystem.UnsubscribeActionWithPayload("CanvasOpenStart", _canvasOpenStartPayloadHandler);
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
            _pendingTutorialId = tutorialId;
            TraceTutorial("HandleOpenTutorial id=" + tutorialId + " stepCount=" + sequence.StepCount);
            PreloadSequence();
            if (GameManager.Instance != null && GameManager.Instance.CanvasManager != null)
                GameManager.Instance.CanvasManager.SwitchCanvas(TutorialCanvasId);
            else
                ApplySequenceNow();
        }

        private void HandleCanvasOpenStart(string canvasId)
        {
            if (_pendingSequence == null) return;
            if (!string.Equals(canvasId, TutorialCanvasId, StringComparison.OrdinalIgnoreCase)) return;
            PreloadSequence();
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
            var tutorialId = _pendingTutorialId;
            _pendingSequence = null;
            _pendingTutorialId = null;
            if (sequence == null) return;

            if (tutorialManager == null)
                tutorialManager = FindObjectOfType<TutorialManager>(true);
            if (tutorialManager == null)
            {
                if (showDebugLog) Debug.LogWarning("[TutorialFlowManager] TutorialManager not found.");
                return;
            }

            tutorialManager.SetSequence(sequence, true, tutorialId);
            TraceTutorial("ApplySequenceNow id=" + tutorialId + " stepCount=" + sequence.StepCount + " managerActive=" + tutorialManager.gameObject.activeSelf);
        }

        private void PreloadSequence()
        {
            if (_pendingSequence == null) return;
            if (tutorialManager == null)
                tutorialManager = FindObjectOfType<TutorialManager>(true);
            if (tutorialManager == null) return;
            tutorialManager.SetSequence(_pendingSequence, true, _pendingTutorialId);
            TraceTutorial("PreloadSequence id=" + _pendingTutorialId + " stepCount=" + _pendingSequence.StepCount);
        }

        private void HandleCloseTutorial()
        {
            if (tutorialManager == null)
                tutorialManager = FindObjectOfType<TutorialManager>(true);
            if (tutorialManager != null)
                tutorialManager.CloseTutorial();
        }

        private void TraceTutorial(string msg)
        {
            if (!ForceTutorialTrace && !showDebugLog) return;
            Debug.Log("[TutorialFlowManager] " + msg);
        }
    }
}
