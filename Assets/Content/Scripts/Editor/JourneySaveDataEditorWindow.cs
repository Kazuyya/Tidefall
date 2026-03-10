using UnityEditor;
using UnityEngine;
using LittleHeroJourney;

namespace LittleHeroJourney.Editor
{
    public class JourneySaveDataEditorWindow : EditorWindow
    {
        private const string SaveKey = "GameState";
        private const string DefaultJourneysDataPath = "Assets/Content/Data/Story Data/JourneysData.asset";

        [SerializeField] private JourneysDataSO journeysData;
        [SerializeField] private int stageCountOverride = 3;

        private bool _hasSave;
        private JourneySaveData _cachedData;
        private Vector2 _scroll;

        [MenuItem("Little Hero Journey/Debug - Save Data (ES3)")]
        public static void Open()
        {
            var w = GetWindow<JourneySaveDataEditorWindow>("Save Data");
            w.minSize = new Vector2(380, 280);
        }

        private void OnEnable()
        {
            if (journeysData == null)
                journeysData = AssetDatabase.LoadAssetAtPath<JourneysDataSO>(DefaultJourneysDataPath);
            RefreshStatus();
        }

        private void OnFocus()
        {
            RefreshStatus();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Save Data (ES3)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Debug saja. Simulator/build pakai save sendiri.", MessageType.Info);

            int journeyCount = GetJourneyCount();
            if (journeysData == null)
            {
                EditorGUILayout.Space(2);
                journeysData = (JourneysDataSO)EditorGUILayout.ObjectField("Journeys Data SO", journeysData, typeof(JourneysDataSO), false);
                stageCountOverride = EditorGUILayout.IntField("Jumlah journey", Mathf.Max(1, stageCountOverride));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status", _hasSave ? "Save ada" : "Tidak ada save");
            EditorGUILayout.Space(4);

            // Reset / Hapus semua
            if (GUILayout.Button("Reset Save Data (hapus semua)", GUILayout.Height(24)))
            {
                if (ES3.KeyExists(SaveKey))
                {
                    ES3.DeleteKey(SaveKey);
                    _cachedData = null;
                    RefreshStatus();
                    Debug.Log("[Save Data Editor] Save dihapus.");
                }
                else
                    Debug.Log("[Save Data Editor] Tidak ada save.");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            EditorGUILayout.LabelField("Per journey", EditorStyles.boldLabel);
            for (int n = 1; n <= journeyCount; n++)
            {
                string journeyId = GetJourneyId(n);
                bool tamat = GetStageCompleted(n);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(journeyId, EditorStyles.boldLabel, GUILayout.Width(120));
                EditorGUILayout.LabelField(tamat ? "Tamat" : "Belum tamat", GUILayout.Width(80));
                if (GUILayout.Button("Reset", GUILayout.Width(52)))
                {
                    SetStageCompleted(n, false);
                    Debug.Log($"[Save Data Editor] {journeyId} di-reset (belum tamat).");
                }
                if (GUILayout.Button("Set Tamat", GUILayout.Width(70)))
                {
                    SetStageCompleted(n, true);
                    Debug.Log($"[Save Data Editor] {journeyId} set tamat.");
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(6);

            // Set tamat semua
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (GUILayout.Button("Set Tamat Semua", GUILayout.Height(26)))
            {
                var data = new JourneySaveData { currentPlayerHealth = 100, lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks };
                for (int i = 1; i <= journeyCount; i++)
                    data.stages.Add(new JourneySaveData.StageStateData { stageNumber = i, isUnlocked = true, bestScore = 0, isCompleted = true });
                ES3.Save(SaveKey, data);
                _cachedData = data;
                RefreshStatus();
                Debug.Log($"[Save Data Editor] Semua ({journeyCount} journey) set tamat.");
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);
            EditorGUILayout.EndScrollView();
        }

        private int GetJourneyCount()
        {
            if (journeysData != null && journeysData.JourneyCount > 0)
                return journeysData.JourneyCount;
            return Mathf.Max(1, stageCountOverride);
        }

        private string GetJourneyId(int stageNumber)
        {
            var j = journeysData?.GetJourneyByNumber(stageNumber);
            if (j != null && !string.IsNullOrEmpty(j.JourneyId))
                return j.JourneyId;
            return $"Journey_{stageNumber}";
        }

        private bool GetStageCompleted(int stageNumber)
        {
            if (_cachedData?.stages == null) return false;
            foreach (var s in _cachedData.stages)
            {
                if (s.stageNumber == stageNumber) return s.isCompleted;
            }
            return false;
        }

        private void SetStageCompleted(int stageNumber, bool completed)
        {
            var data = LoadOrCreateSaveData();
            if (data.stages == null) data.stages = new System.Collections.Generic.List<JourneySaveData.StageStateData>();

            JourneySaveData.StageStateData stage = null;
            foreach (var s in data.stages)
            {
                if (s.stageNumber == stageNumber) { stage = s; break; }
            }
            if (stage == null)
            {
                bool unlocked = stageNumber == 1;
                foreach (var prev in data.stages)
                {
                    if (prev.stageNumber == stageNumber - 1 && prev.isCompleted) { unlocked = true; break; }
                }
                stage = new JourneySaveData.StageStateData { stageNumber = stageNumber, isUnlocked = unlocked, bestScore = 0, isCompleted = false };
                data.stages.Add(stage);
            }
            stage.isCompleted = completed;
            data.lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks;
            ES3.Save(SaveKey, data);
            _cachedData = data;
            RefreshStatus();
        }

        private JourneySaveData LoadOrCreateSaveData()
        {
            if (ES3.KeyExists(SaveKey))
            {
                try { return ES3.Load<JourneySaveData>(SaveKey); }
                catch { }
            }
            var data = new JourneySaveData { currentPlayerHealth = 100, lastSavedTimestampUtc = System.DateTime.UtcNow.Ticks };
            int count = GetJourneyCount();
            for (int i = 1; i <= count; i++)
                data.stages.Add(new JourneySaveData.StageStateData { stageNumber = i, isUnlocked = i == 1, bestScore = 0, isCompleted = false });
            return data;
        }

        private void RefreshStatus()
        {
            _hasSave = ES3.KeyExists(SaveKey);
            if (_hasSave)
            {
                try { _cachedData = ES3.Load<JourneySaveData>(SaveKey); }
                catch { _cachedData = null; }
            }
            else
                _cachedData = null;
        }
    }
}
