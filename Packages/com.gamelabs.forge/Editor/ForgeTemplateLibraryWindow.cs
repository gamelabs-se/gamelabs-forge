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
            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), new Color(0.22f, 0.22f, 0.22f));
            
            DrawHeader();
            DrawSearch();
            
            EditorGUILayout.Space(5);
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
            
            GUILayout.Space(5);
            
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
            
            EditorGUILayout.Space(3);
            
            // Recent section
            if (_showRecents)
            {
                DrawSection("Recent", filteredRecents, ref _showRecents, false);
            }
            else
            {
                DrawCollapsedSection("Recent", filteredRecents.Count, ref _showRecents);
            }
            
            EditorGUILayout.Space(3);
            
            // All templates section
            if (_showAll)
            {
                DrawSection("All Templates", filteredAll, ref _showAll, false);
            }
            else
            {
                DrawCollapsedSection("All Templates", filteredAll.Count, ref _showAll);
            }
            
            GUILayout.Space(10);
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            GUILayout.Space(12);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Template Library", headerStyle);
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Refresh", GUILayout.Width(60), GUILayout.Height(20)))
            {
                RefreshTemplates();
            }
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(8);
        }
        
        private void DrawSearch()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUI.SetNextControlName("SearchField");
            
            var searchStyle = new GUIStyle(EditorStyles.toolbarSearchField);
            searchStyle.fixedHeight = 20;
            
            _searchQuery = GUILayout.TextField(_searchQuery, searchStyle);
            
            if (GUILayout.Button("", GUI.skin.FindStyle("SearchCancelButton"), GUILayout.Width(18), GUILayout.Height(20)))
            {
                _searchQuery = "";
                GUI.FocusControl(null);
            }
            
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
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
            var bgRect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(bgRect, new Color(0.25f, 0.25f, 0.25f));
            
            var arrowRect = new Rect(bgRect.x + 10, bgRect.y + 4, 16, 16);
            var labelRect = new Rect(bgRect.x + 30, bgRect.y, bgRect.width - 30, bgRect.height);
            
            if (GUI.Button(bgRect, "", GUIStyle.none))
            {
                expanded = !expanded;
                SavePreference(title, expanded);
            }
            
            GUI.Label(arrowRect, ">", EditorStyles.boldLabel);
            GUI.Label(labelRect, $"{title} ({count})", EditorStyles.label);
        }
        
        private void DrawSection(string title, List<ScriptableObject> templates, ref bool expanded, bool showFavButton)
        {
            var bgRect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(bgRect, new Color(0.3f, 0.3f, 0.3f));
            
            var arrowRect = new Rect(bgRect.x + 10, bgRect.y + 4, 16, 16);
            var labelRect = new Rect(bgRect.x + 30, bgRect.y, bgRect.width - 30, bgRect.height);
            
            if (GUI.Button(bgRect, "", GUIStyle.none))
            {
                expanded = !expanded;
                SavePreference(title, expanded);
            }
            
            GUI.Label(arrowRect, "v", EditorStyles.boldLabel);
            
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.normal.textColor = Color.white;
            GUI.Label(labelRect, $"{title} ({templates.Count})", labelStyle);
            
            if (templates.Count == 0)
            {
                GUILayout.Space(5);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(30);
                EditorGUILayout.LabelField("No templates", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(5);
                return;
            }
            
            GUILayout.Space(2);
            
            var library = ForgeTemplateLibrary.Instance;
            
            foreach (var template in templates)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                
                // Favorite toggle
                if (!showFavButton)
                {
                    bool isFav = library.IsFavorite(template);
                    string starIcon = isFav ? "⭐" : "☆";
                    if (GUILayout.Button(starIcon, GUILayout.Width(24), GUILayout.Height(22)))
                    {
                        if (isFav)
                            library.RemoveFromFavorites(template);
                        else
                            library.AddToFavorites(template);
                    }
                }
                else
                {
                    GUILayout.Space(24);
                }
                
                GUILayout.Space(6);
                
                // Template button with hover
                string displayName = $"{template.name}";
                string typeName = $"{template.GetType().Name}";
                
                var rect = GUILayoutUtility.GetRect(new GUIContent(displayName), EditorStyles.label, GUILayout.Height(22));
                
                bool isHovered = rect.Contains(Event.current.mousePosition);
                
                if (Event.current.type == EventType.Repaint)
                {
                    if (isHovered)
                    {
                        EditorGUI.DrawRect(rect, new Color(0.3f, 0.5f, 0.8f, 0.4f));
                    }
                }
                
                var nameStyle = new GUIStyle(EditorStyles.label);
                nameStyle.padding = new RectOffset(4, 4, 4, 4);
                nameStyle.normal.textColor = isHovered ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                
                var nameRect = new Rect(rect.x, rect.y, rect.width - 150, rect.height);
                var typeRect = new Rect(rect.x + rect.width - 145, rect.y, 145, rect.height);
                
                GUI.Label(nameRect, displayName, nameStyle);
                
                var typeStyle = new GUIStyle(EditorStyles.miniLabel);
                typeStyle.alignment = TextAnchor.MiddleRight;
                typeStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
                typeStyle.padding = new RectOffset(0, 8, 4, 4);
                GUI.Label(typeRect, typeName, typeStyle);
                
                if (GUI.Button(rect, "", GUIStyle.none))
                {
                    _onSelect?.Invoke(template);
                    Close();
                }
                
                GUILayout.Space(10);
                EditorGUILayout.EndHorizontal();
            }
            
            GUILayout.Space(2);
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
