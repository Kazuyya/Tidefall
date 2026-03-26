using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace LittleHeroJourney
{
    public enum AnimationPlayMode { Sequential, Parallel }

    [System.Serializable]
    public class AnimationPhaseConfig
    {
        public List<DOTweenAnimation> animations = new List<DOTweenAnimation>();
        public AnimationPlayMode playMode = AnimationPlayMode.Parallel;
        public bool useCustomDuration = false;
        public float customDuration = 1f;
    }

    [System.Serializable]
    public class WaitPhaseConfig
    {
        public float duration = 1f;
    }

    [System.Serializable]
    public class SplashCanvasStep
    {
        public GameObject targetGameObject;
        public AnimationPhaseConfig inPhase = new AnimationPhaseConfig();
        public WaitPhaseConfig waitPhase = new WaitPhaseConfig();
        public AnimationPhaseConfig outPhase = new AnimationPhaseConfig();
    }

    public class SplashScreenManager : MonoBehaviour
    {
        [SerializeField] private List<SplashCanvasStep> sequence = new List<SplashCanvasStep>();
        [SerializeField] private bool showDebugLog;

        private Sequence _seq;
        private bool _sequenceCompleted;

        private void Start()
        {
            if (GameManager.Instance?.SceneManager == null) return;
            var cfg = GameManager.Instance.SceneManager.Config;
            if (cfg == null || !cfg.useSplash) return;
            PlaySequence();
        }

        private void PlaySequence()
        {
            _sequenceCompleted = false;
            foreach (var step in sequence)
            {
                if (step?.targetGameObject != null)
                    step.targetGameObject.SetActive(false);
            }

            _seq = DOTween.Sequence();
            foreach (var step in sequence)
            {
                if (step == null || step.targetGameObject == null) continue;
                var go = step.targetGameObject;
                var s = step;

                _seq.AppendCallback(() => go.SetActive(true));
                _seq.AppendCallback(() => SetFadeTargetsToZero(s.inPhase?.animations));
                AppendPhase(_seq, s.inPhase);
                if (s.waitPhase != null && s.waitPhase.duration > 0f)
                    _seq.AppendInterval(s.waitPhase.duration);
                AppendPhase(_seq, s.outPhase);
                _seq.AppendCallback(() => go.SetActive(false));
            }
            _seq.AppendCallback(OnSequenceComplete);
            _seq.Play();
        }

        private static void SetFadeTargetsToZero(List<DOTweenAnimation> list)
        {
            if (list == null) return;
            foreach (var anim in list)
            {
                if (anim == null || anim.target == null || anim.animationType != DOTweenAnimation.AnimationType.Fade) continue;
                var cg = anim.target as CanvasGroup;
                if (cg != null) { cg.alpha = 0f; continue; }
                var g = anim.target as Graphic;
                if (g != null) { var c = g.color; c.a = 0f; g.color = c; }
            }
        }

        private void AppendPhase(Sequence seq, AnimationPhaseConfig phase)
        {
            if (phase == null || phase.animations == null || phase.animations.Count == 0) return;

            if (phase.playMode == AnimationPlayMode.Sequential)
            {
                foreach (var anim in phase.animations)
                {
                    if (anim == null) continue;
                    float dur = Helper.GetTweenEffectiveDuration(anim, phase.useCustomDuration, phase.customDuration);
                    var a = anim;
                    seq.AppendCallback(() => { if (a != null) a.RewindThenRecreateTweenAndPlay(); });
                    if (dur > 0f) seq.AppendInterval(dur);
                }
            }
            else
            {
                float maxDur = Helper.GetSequenceTotalDuration(phase.animations, false, phase.useCustomDuration, phase.customDuration);
                seq.AppendCallback(() =>
                {
                    foreach (var anim in phase.animations)
                        if (anim != null) anim.RewindThenRecreateTweenAndPlay();
                });
                if (maxDur > 0f) seq.AppendInterval(maxDur);
            }
        }

        private void OnSequenceComplete()
        {
            if (_sequenceCompleted) return;
            _sequenceCompleted = true;
            if (GameManager.Instance?.SceneManager == null) return;
            var mainMenuId = GameManager.Instance.SceneManager.Config?.mainMenuId;
            if (!string.IsNullOrEmpty(mainMenuId))
                GameManager.Instance.SceneManager.GoToId(mainMenuId);
        }
    }
}
