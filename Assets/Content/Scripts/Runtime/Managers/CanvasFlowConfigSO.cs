using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleHeroJourney
{
    [CreateAssetMenu(fileName = "CanvasFlowConfig", menuName = "Canvas/Canvas Flow Config")]
    public class CanvasFlowConfigSO : ScriptableObject
    {
        public enum TransitionMode
        {
            [Tooltip("Show canvas baru tanpa close yang lama (overlay). Contoh: Gameplay -> Pause")]
            Direct,
            
            [Tooltip("Close previous instant, show new instant (no animation). Fast switch.")]
            SnapSwitch,
            
            [Tooltip("Close previous instant (no animation), then play IN animation on new canvas.")]
            SnapOutThenIn,
            
            [Tooltip("Wait previous OUT animation, then new IN animation. Smooth transition.")]
            WaitOutThenIn,
            
            [Tooltip("Wait previous OUT animation, then instant show new. Hybrid.")]
            WaitOutThenSnapIn,
            
            [Tooltip("Play IN baru dan OUT lama secara bersamaan, keduanya pakai animasi.")]
            ParallelInOut,
            
            [Tooltip("Snap IN canvas baru instan, sementara canvas lama tetap OUT animasi (bersamaan).")]
            ParallelOutSnapIn,
        }
        
        [Serializable]
        public class TransitionFromRule
        {
            [Tooltip("Canvas asal. Kosongkan atau isi '*'/'any' untuk wildcard (berlaku untuk semua asal)")]
            public string fromCanvasId;
            public TransitionMode mode = TransitionMode.WaitOutThenIn;
            [Tooltip("Close previous canvas setelah IN animation complete?")]
            public bool closeAfterInComplete = true;
        }
        
        [Serializable]
        public class TransitionToEntry
        {
            [Tooltip("Canvas tujuan. Kosongkan atau isi '*'/'any' untuk wildcard (berlaku untuk semua tujuan)")]
            public string toCanvasId;
            [Tooltip("Daftar rule berdasarkan From Id untuk tujuan ini")]
            public List<TransitionFromRule> fromRules = new List<TransitionFromRule>();
        }
        
        [Header("Transition Rules")]
        [Tooltip("Rules untuk transisi antar canvas. Rule pertama yang match akan digunakan.")]
        public List<TransitionToEntry> transitionRules = new List<TransitionToEntry>();
    }
}
