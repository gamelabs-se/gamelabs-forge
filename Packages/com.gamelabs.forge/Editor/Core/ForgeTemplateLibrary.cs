using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Manages template favorites and recent templates.
    /// </summary>
    [Serializable]
    public class ForgeTemplateLibrary
    {
        [SerializeField] private List<string> _favoriteGuids = new List<string>();
        [SerializeField] private List<string> _recentGuids = new List<string>();
        
        private const string PREFS_KEY = "GameLabs.Forge.TemplateLibrary";
        private const int MAX_RECENTS = 10;
        private static ForgeTemplateLibrary _instance;
        
        public static ForgeTemplateLibrary Instance
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
        
        public void AddToFavorites(ScriptableObject template)
        {
            if (template == null) return;
            string path = AssetDatabase.GetAssetPath(template);
            string guid = AssetDatabase.AssetPathToGUID(path);
            
            if (!_favoriteGuids.Contains(guid))
            {
                _favoriteGuids.Add(guid);
                Save();
            }
        }
        
        public void RemoveFromFavorites(ScriptableObject template)
        {
            if (template == null) return;
            string path = AssetDatabase.GetAssetPath(template);
            string guid = AssetDatabase.AssetPathToGUID(path);
            
            _favoriteGuids.Remove(guid);
            Save();
        }
        
        public bool IsFavorite(ScriptableObject template)
        {
            if (template == null) return false;
            string path = AssetDatabase.GetAssetPath(template);
            string guid = AssetDatabase.AssetPathToGUID(path);
            return _favoriteGuids.Contains(guid);
        }
        
        public void RecordUsage(ScriptableObject template)
        {
            if (template == null) return;
            string path = AssetDatabase.GetAssetPath(template);
            string guid = AssetDatabase.AssetPathToGUID(path);
            
            // Remove if already in list
            _recentGuids.Remove(guid);
            
            // Add to front
            _recentGuids.Insert(0, guid);
            
            // Trim to max size
            if (_recentGuids.Count > MAX_RECENTS)
            {
                _recentGuids.RemoveRange(MAX_RECENTS, _recentGuids.Count - MAX_RECENTS);
            }
            
            Save();
        }
        
        public List<ScriptableObject> GetFavorites()
        {
            var result = new List<ScriptableObject>();
            foreach (var guid in _favoriteGuids.ToList()) // ToList to avoid collection modified exception
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    // Asset was deleted, remove from favorites
                    _favoriteGuids.Remove(guid);
                    continue;
                }
                
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null)
                {
                    result.Add(asset);
                }
                else
                {
                    // Asset was deleted, remove from favorites
                    _favoriteGuids.Remove(guid);
                }
            }
            Save(); // Save if we cleaned up any deleted assets
            return result;
        }
        
        public List<ScriptableObject> GetRecents()
        {
            var result = new List<ScriptableObject>();
            foreach (var guid in _recentGuids.ToList()) // ToList to avoid collection modified exception
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path))
                {
                    // Asset was deleted, remove from recents
                    _recentGuids.Remove(guid);
                    continue;
                }
                
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null)
                {
                    result.Add(asset);
                }
                else
                {
                    // Asset was deleted, remove from recents
                    _recentGuids.Remove(guid);
                }
            }
            Save(); // Save if we cleaned up any deleted assets
            return result;
        }
        
        private void Save()
        {
            var json = JsonUtility.ToJson(this);
            EditorPrefs.SetString(PREFS_KEY, json);
        }
        
        private static ForgeTemplateLibrary Load()
        {
            var json = EditorPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonUtility.FromJson<ForgeTemplateLibrary>(json);
                }
                catch
                {
                    // Corrupted data, create new
                }
            }
            return new ForgeTemplateLibrary();
        }
    }
}
