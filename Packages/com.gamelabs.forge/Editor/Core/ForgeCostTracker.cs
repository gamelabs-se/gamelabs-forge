using System;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Tracks API costs per session and provides budget warnings.
    /// </summary>
    [Serializable]
    public class ForgeCostTracker
    {
        [SerializeField] private float _sessionCost = 0f;
        [SerializeField] private int _sessionGenerations = 0;
        [SerializeField] private int _sessionItemsGenerated = 0;
        [SerializeField] private DateTime _sessionStartTime;
        
        private const string PREFS_KEY = "GameLabs.Forge.CostTracker";
        private static ForgeCostTracker _instance;
        
        public static ForgeCostTracker Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }
        
        public float SessionCost => _sessionCost;
        public int SessionGenerations => _sessionGenerations;
        public int SessionItemsGenerated => _sessionItemsGenerated;
        public DateTime SessionStartTime => _sessionStartTime;
        
        private ForgeCostTracker()
        {
            _sessionStartTime = DateTime.Now;
        }
        
        public void RecordGeneration(int itemCount, float cost)
        {
            _sessionCost += cost;
            _sessionGenerations++;
            _sessionItemsGenerated += itemCount;
            Save();
        }
        
        public void ResetSession()
        {
            _sessionCost = 0f;
            _sessionGenerations = 0;
            _sessionItemsGenerated = 0;
            _sessionStartTime = DateTime.Now;
            Save();
        }
        
        public string GetSessionSummary()
        {
            var duration = DateTime.Now - _sessionStartTime;
            return $"Session: {_sessionGenerations} generations, {_sessionItemsGenerated} items, ${_sessionCost:F4} ({duration.TotalMinutes:F0}m)";
        }
        
        private void Save()
        {
            var json = JsonUtility.ToJson(this);
            UnityEditor.EditorPrefs.SetString(PREFS_KEY, json);
        }
        
        private static ForgeCostTracker Load()
        {
            var json = UnityEditor.EditorPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<ForgeCostTracker>(json);
                }
                catch
                {
                    // Corrupted data, create new
                }
            }
            return new ForgeCostTracker();
        }
    }
}
