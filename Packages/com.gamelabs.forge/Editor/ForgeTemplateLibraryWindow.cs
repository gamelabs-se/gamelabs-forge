#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Template library browser with favorites, recents, and search.
    /// </summary>
    public class ForgeTemplateLibraryWindow : EditorWindow
    {
        private Vector2 _scrollPos;
        private string _searchQuery = "";
        private bool _showFavorites = true;
        private bool _showRecents = true;
        private bool _showAll = true;
        
        private Action<ScriptableObject> _onSelect;
        private List<ScriptableObject> _allTemplates;
        
        public static void Open(Action<ScriptableObject> onSelect)
        {
            var window = GetWindow<ForgeTemplateLibraryWindow>("Template Library");
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(600, 800);
            window._onSelect = onSelect;
            window.RefreshTemplates();
            
            // Load preferences
            window._showFavorites = EditorPrefs.GetBool("GameLabs.Forge.TemplateLib.ShowFavorites", true);
            window._showRecents = EditorPrefs.GetBool("GameLabs.Forge.TemplateLib.ShowRecents", true);
            window._showAll = EditorPrefs.GetBool("GameLabs.Forge.TemplateLib.ShowAll", true);
        }
        
        private void RefreshTemplates()
        {
            // Find all ScriptableObject assets in project
            var guids = AssetDatabase.FindAssets("t:ScriptableObject");
            _allTemplates = new List<ScriptableObject>();
            
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null)
                {
                    _allTemplates.Add(asset);
                }
            }
        }
        
        private void OnGUI()
        {
            DrawHeader();
            DrawSearch();
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            var library = ForgeTemplateLibrary.Instance;
            var favorites = library.GetFavorites();
            var recents = library.GetRecents();
            
            // Apply search filter
            var filteredFavorites = ApplySearch(favorites);
            var filteredRecents = ApplySearch(recents);
            var filteredAll = ApplySearch(_allTemplates);
            
            // Favorites section
            if (_showFavorites)
            {
                DrawSection("Favorites", filteredFavorites, ref _showFavorites, true);
            }
            else
            {
                DrawCollapsedSection("Favorites", filteredFavorites.Count, ref _showFavorites);
            }
            
            EditorGUILayout.Space(5);
            
            // Recent section
            if (_showRecents)
            {
                DrawSection("Recent", filteredRecents, ref _showRecents, false);
            }
            else
            {
                DrawCollapsedSection("Recent", filteredRecents.Count, ref _showRecents);
            }
            
            EditorGUILayout.Space(5);
            
            // All templates section
            if (_showAll)
            {
                DrawSection("All Templates", filteredAll, ref _showAll, false);
            }
            else
            {
                DrawCollapsedSection("All Templates", filteredAll.Count, ref _showAll);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Template Library", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
        }
        
        private void DrawSearch()
        {
            EditorGUILayout.BeginHorizontal();
            GUI.SetNextControlName("SearchField");
            _searchQuery = EditorGUILayout.TextField(_searchQuery, EditorStyles.toolbarSearchField);
            
            if (GUILayout.Button("", GUI.skin.FindStyle("SearchCancelButton"), GUILayout.Width(18)))
            {
                _searchQuery = "";
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
        }
        
        private List<ScriptableObject> ApplySearch(List<ScriptableObject> templates)
        {
            if (string.IsNullOrEmpty(_searchQuery))
                return templates;
            
            return templates.Where(t => 
                t.name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.GetType().Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }
        
        private void DrawCollapsedSection(string title, int count, ref bool expanded)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button($"> {title} ({count})", EditorStyles.toolbarButton))
            {
                expanded = !expanded;
                SavePreference(title, expanded);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSection(string title, List<ScriptableObject> templates, ref bool expanded, bool showFavButton)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button($"v {title} ({templates.Count})", EditorStyles.toolbarButton))
            {
                expanded = !expanded;
                SavePreference(title, expanded);
            }
            EditorGUILayout.EndHorizontal();
            
            if (templates.Count == 0)
            {
                EditorGUILayout.LabelField("  No templates", EditorStyles.centeredGreyMiniLabel);
                return;
            }
            
            var library = ForgeTemplateLibrary.Instance;
            
            foreach (var template in templates)
            {
                EditorGUILayout.BeginHorizontal();
                
                // Favorite toggle (only if not in favorites section)
                if (!showFavButton)
                {
                    bool isFav = library.IsFavorite(template);
                    string icon = isFav ? "Star" : "StarEmpty";
                    if (GUILayout.Button(EditorGUIUtility.IconContent(icon), GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        if (isFav)
                            library.RemoveFromFavorites(template);
                        else
                            library.AddToFavorites(template);
                    }
                }
                else
                {
                    GUILayout.Space(20);
                }
                
                // Template button
                string displayName = $"{template.name} ({template.GetType().Name})";
                if (GUILayout.Button(displayName, EditorStyles.label, GUILayout.Height(20)))
                {
                    _onSelect?.Invoke(template);
                    Close();
                }
                
                GUILayout.FlexibleSpace();
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private void SavePreference(string section, bool value)
        {
            if (section == "Favorites")
                EditorPrefs.SetBool("GameLabs.Forge.TemplateLib.ShowFavorites", value);
            else if (section == "Recent")
                EditorPrefs.SetBool("GameLabs.Forge.TemplateLib.ShowRecents", value);
            else if (section == "All Templates")
                EditorPrefs.SetBool("GameLabs.Forge.TemplateLib.ShowAll", value);
        }
    }
}
#endif
