#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Forge – AI Item Generator (polished IMGUI for Unity 6.3).
    /// Pure UI overhaul: alignment, spacing, button styling. Logic intact.
    /// </summary>
    public class ForgeTemplateWindow : EditorWindow
    {
        // ========= UI State =========
        private Vector2 _scroll;
        private ForgeBlueprint _blueprint;
        private ScriptableObject _template;
        private int _itemCount = 20;
        private string _additionalContext = "";
        private string _customFolderName = "";
        private bool _useCustomFolder = false;
        private bool _autoSaveAsAsset = true;

        private int _foundCount = 0;
        private List<string> _foundJson = new();

        private bool _isGenerating = false;
        private string _status = "";
        private MessageType _statusType = MessageType.None;

        private readonly List<ScriptableObject> _lastGenerated = new();
        private readonly Dictionary<ScriptableObject, bool> _itemSavedState = new(); // track saved/unsaved

        private const float LABEL_W = 120f; // unified label width

        [MenuItem("GameLabs/Forge/FORGE Window", priority = 0)]
        public static void OpenWindow()
        {
            var w = GetWindow<ForgeTemplateWindow>();
            w.titleContent = new GUIContent("Forge – AI Item Generator", EditorGUIUtility.IconContent("d_PlayButton On").image);
            w.minSize = new Vector2(560, 660);
            w.maxSize = new Vector2(1200, 1400);
        }

        // ========= Styles =========
        private static class UI
        {
            public static GUIStyle Title;
            public static GUIStyle ToolbarBtn;
            public static GUIStyle Section;
            public static GUIStyle Header;
            public static GUIStyle Card;
            public static GUIStyle Pill;
            public static GUIStyle Hint;
            public static GUIStyle Code;
            public static GUIStyle PrimaryBtnText;
            public static Color Accent => EditorGUIUtility.isProSkin ? new Color(0.24f, 0.56f, 1f, 1f) : new Color(0.1f, 0.4f, 0.95f, 1f);
            public static Color AccentDim => EditorGUIUtility.isProSkin ? new Color(0.24f, 0.56f, 1f, 0.10f) : new Color(0.1f, 0.4f, 0.95f, 0.12f);
            public static Color Line => EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0.08f);

            public static Texture2D Play => (Texture2D)EditorGUIUtility.IconContent("d_PlayButton On").image;
            public static Texture2D Gear => (Texture2D)EditorGUIUtility.IconContent("d_SettingsIcon").image;
            public static Texture2D Search => (Texture2D)EditorGUIUtility.IconContent("d_Search Icon").image;
            public static Texture2D Folder => (Texture2D)EditorGUIUtility.IconContent("d_Folder Icon").image;
            public static Texture2D Trash => (Texture2D)EditorGUIUtility.IconContent("TreeEditor.Trash").image;
            public static Texture2D Refresh => (Texture2D)EditorGUIUtility.IconContent("d_Refresh").image;
            public static Texture2D Copy => (Texture2D)EditorGUIUtility.IconContent("Clipboard").image;
            public static Texture2D Save => (Texture2D)EditorGUIUtility.IconContent("SaveFromPlay").image;
            public static Texture2D BarChart => (Texture2D)EditorGUIUtility.IconContent("d_Refresh").image;

            public static void Init()
            {
                if (Title != null) return;

                Title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleLeft, fixedHeight = 24, contentOffset = Vector2.zero };

                ToolbarBtn = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 22 };

                Section = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

                Header = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.75f) : new Color(0, 0, 0, 0.75f) }
                };

                Card = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 6, 6)
                };

                Pill = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    padding = new RectOffset(8, 8, 2, 2),
                };

                Hint = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.6f) : new Color(0, 0, 0, 0.6f) }
                };

                Code = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    fontSize = 12,
                    padding = new RectOffset(6, 6, 6, 6)
                };

                PrimaryBtnText = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleCenter
                };
            }
        }

        private void OnGUI()
        {
            UI.Init();

            DrawTopBar();        // no extra vertical padding
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawBlueprintSection();
            DrawTemplateSection();
            DrawExistingSection();
            DrawGenerateOptions();
            DrawSaveOptions();
            DrawPrimaryButton();
            DrawStatus();

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        // ========= Bars =========
        private void DrawTopBar()
        {
            // Create icon button style for consistent sizing and appearance
            var iconBtnStyle = new GUIStyle(EditorStyles.iconButton)
            {
                fixedWidth = 32,
                fixedHeight = 32,
                margin = new RectOffset(2, 2, 6, 6),
                padding = new RectOffset(4, 4, 4, 4)
            };

            // Draw background bar
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(36));
            EditorGUI.DrawRect(new Rect(0, rect.y, position.width, 36), UI.AccentDim);
            
            // Content: icon + title on left, buttons on right
            EditorGUILayout.BeginHorizontal(GUILayout.Height(36));
            GUILayout.Space(12);
            
            GUILayout.Label(UI.Play, GUILayout.Width(20), GUILayout.Height(20));
            GUILayout.Space(8);
            GUILayout.Label("Forge – AI Item Generator", UI.Title, GUILayout.Height(24));
            
            GUILayout.FlexibleSpace();
            
            // Settings button
            if (GUILayout.Button(new GUIContent(UI.Gear, "Settings"), iconBtnStyle))
            {
                ForgeSettingsWindow.Open();
            }
            
            // Statistics button
            if (GUILayout.Button(new GUIContent(UI.BarChart, "Statistics"), iconBtnStyle))
            {
                ForgeStatisticsWindow.Open();
            }
            
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Quick Actions", GUILayout.Width(90));

            using (new EditorGUI.DisabledScope(_template == null))
            {
                if (GUILayout.Button(new GUIContent(" Find Items", UI.Search), UI.ToolbarBtn))
                    FindExistingItems();

                if (GUILayout.Button(new GUIContent(" Open Folder", UI.Folder), UI.ToolbarBtn))
                    OpenGeneratedFolder();
            }

            if (GUILayout.Button(new GUIContent(" Clear Results", UI.Trash), UI.ToolbarBtn))
            {
                _lastGenerated.Clear();
                _status = "";
                _statusType = MessageType.None;
            }

            GUILayout.FlexibleSpace();

            if (_foundCount > 0)
            {
                GUILayout.Label($"Discovered: {_foundCount}", UI.Header);
                if (GUILayout.Button("View", UI.ToolbarBtn, GUILayout.Width(60)))
                    ShowFoundPopup();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ========= Sections =========
        private void DrawBlueprintSection()
        {
            DrawSectionHeader("Blueprint (Optional)");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                EditorGUILayout.BeginHorizontal();
                
                var oldBlueprint = _blueprint;
                _blueprint = (ForgeBlueprint)EditorGUILayout.ObjectField(
                    new GUIContent("Blueprint", "A ForgeBlueprint saves template, instructions, and duplicate strategy."),
                    _blueprint,
                    typeof(ForgeBlueprint),
                    false);

                // Trigger refresh if blueprint changed
                if (_blueprint != oldBlueprint)
                {
                    _foundCount = 0;
                    _foundJson.Clear();
                }

                if (GUILayout.Button(new GUIContent(UI.Search, "Create New Blueprint"), GUILayout.Width(32), GUILayout.Height(18)))
                {
                    CreateNewBlueprint();
                }

                EditorGUILayout.EndHorizontal();

                if (_blueprint != null)
                {
                    EditorGUILayout.Space(6);
                    
                    // Show blueprint settings
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    GUILayout.Label($"Name: {_blueprint.DisplayName}", UI.Header);
                    
                    if (_blueprint.Template != null)
                    {
                        var templateType = _blueprint.Template.GetType();
                        var schema = ForgeSchemaExtractor.ExtractSchema(templateType);
                        GUILayout.Label($"Template: {schema.typeName}", UI.Hint);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Blueprint has no template assigned.", MessageType.Warning);
                    }

                    GUILayout.Label($"Duplicate Strategy: {_blueprint.DuplicateStrategy}", UI.Hint);
                    GUILayout.Label($"Saved Items: {_blueprint.ExistingItems.Count}", UI.Hint);

                    if (!string.IsNullOrEmpty(_blueprint.Instructions))
                    {
                        EditorGUILayout.LabelField("Instructions:", _blueprint.Instructions, UI.Hint);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.Space(4);

                    // Button to edit blueprint
                    if (GUILayout.Button("Edit Blueprint Settings", GUILayout.Height(22)))
                    {
                        Selection.activeObject = _blueprint;
                        EditorGUIUtility.PingObject(_blueprint);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Optionally select or create a ForgeBlueprint to:\n" +
                        "• Save generation settings and instructions\n" +
                        "• Manage duplicate prevention strategy\n" +
                        "• Create profiles for different item types (weapons, armor, etc.)",
                        MessageType.Info);
                }

                EditorGUIUtility.labelWidth = old;
            }
        }

        private void CreateNewBlueprint()
        {
            var path = EditorUtility.SaveFilePanelInProject("Save New Blueprint", "New Blueprint", "asset", "");
            if (string.IsNullOrEmpty(path))
                return;

            var blueprint = ScriptableObject.CreateInstance<ForgeBlueprint>();
            blueprint.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(blueprint, path);
            AssetDatabase.SaveAssets();

            _blueprint = blueprint;
            ForgeLogger.Log($"Created new blueprint: {blueprint.DisplayName}");
        }

        private void DrawTemplateSection()
        {
            DrawSectionHeader("Template");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                var oldTemplate = _template;
                _template = (ScriptableObject)EditorGUILayout.ObjectField(
                    new GUIContent("ScriptableObject Template", "Pick the ScriptableObject that defines your item structure."),
                    _template,
                    typeof(ScriptableObject),
                    false);

                // Trigger refresh if template changed
                if (_template != oldTemplate)
                {
                    _foundCount = 0;
                    _foundJson.Clear();
                }

                if (_template != null)
                {
                    var t = _template.GetType();
                    var schema = ForgeSchemaExtractor.ExtractSchema(t);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Type: {schema.typeName}", UI.Header);
                    GUILayout.Space(8);
                    // green count pill
                    var pillRect = GUILayoutUtility.GetRect(80, 20, GUILayout.Width(80));
                    EditorGUI.DrawRect(pillRect, new Color(0.2f, 0.75f, 0.35f, 0.18f));
                    GUI.Label(pillRect, $"Fields: {schema.fields.Count}", UI.Pill);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Preview Schema", UI.Refresh), GUILayout.Height(22), GUILayout.Width(140)))
                    {
                        var desc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);
                        EditorUtility.DisplayDialog("Schema Preview", desc, "OK");
                    }
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(2);
                    GUILayout.Label(schema.description, UI.Hint);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Select a ScriptableObject template to define the structure:\n" +
                        "• Fields & types  • [Range] constraints  • [Tooltip] descriptions  • Enum options",
                        MessageType.Info);
                }

                EditorGUIUtility.labelWidth = old;
            }
        }

        private void DrawExistingSection()
        {
            // Auto-find when template or blueprint changes
            bool shouldAutoFind = false;
            var currentTemplate = _blueprint != null ? _blueprint.Template : _template;
            if (currentTemplate != null && !string.IsNullOrEmpty(currentTemplate.GetType().Name))
            {
                shouldAutoFind = true;
            }

            if (shouldAutoFind && _foundCount == 0 && (currentTemplate != null))
            {
                FindExistingItems();
            }

            DrawSectionHeader("Existing Items");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                EditorGUILayout.BeginHorizontal();
                
                // Refresh button
                if (GUILayout.Button(new GUIContent(UI.Refresh, "Refresh discovery"), GUILayout.Width(32), GUILayout.Height(22)))
                    FindExistingItems();

                GUILayout.Space(8);

                // Count display (always shown, aligned consistently)
                string countText = _foundCount > 0 ? $"Discovered: {_foundCount}" : "No items found";
                EditorGUILayout.LabelField(countText, UI.Header);

                GUILayout.FlexibleSpace();

                // View button (only if items found)
                if (_foundCount > 0 && GUILayout.Button("View", GUILayout.Width(60), GUILayout.Height(22)))
                    ShowFoundPopup();
                
                EditorGUILayout.EndHorizontal();

                var s = ForgeConfig.GetGeneratorSettings();
                GUILayout.Space(3);
                GUILayout.Label($"Search path: {s?.existingAssetsSearchPath ?? "Resources"}", UI.Hint);
            }
        }

        private void DrawGenerateOptions()
        {
            DrawSectionHeader("Generation Options");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                // Row: Item count slider + right badge (aligned)
                Rect row = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                Rect label = new Rect(row.x, row.y, LABEL_W, row.height);
                Rect slider = new Rect(label.xMax + 4, row.y, row.width - LABEL_W - 60, row.height);
                Rect badge = new Rect(slider.xMax + 6, row.y, 40, row.height);

                EditorGUI.LabelField(label, "Item Count");
                _itemCount = Mathf.RoundToInt(GUI.HorizontalSlider(slider, _itemCount, 1, 50));
                // badge
                EditorGUI.DrawRect(badge, UI.Accent);
                var bc = GUI.color; GUI.color = Color.white;
                GUI.Label(badge, _itemCount.ToString(), UI.Pill);
                GUI.color = bc;

                GUILayout.Space(6);
                EditorGUILayout.LabelField("Additional Context (Optional)");
                _additionalContext = EditorGUILayout.TextArea(_additionalContext, UI.Code, GUILayout.MinHeight(64));
                GUILayout.Space(2);
                GUILayout.Label("Tip: e.g. “Fire-themed items”, “Cyberpunk names”, or “For level 50+”.", UI.Hint);

                EditorGUIUtility.labelWidth = old;
            }
        }

        private void DrawSaveOptions()
        {
            DrawSectionHeader("Save Options");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                _autoSaveAsAsset = EditorGUILayout.ToggleLeft(new GUIContent("Auto-Save as Asset", "Create assets immediately after generation."), _autoSaveAsAsset);
                using (new EditorGUI.DisabledScope(!_autoSaveAsAsset))
                {
                    var old = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = LABEL_W;

                    _useCustomFolder = EditorGUILayout.ToggleLeft(new GUIContent("Use Custom Folder Name"), _useCustomFolder);
                    if (_useCustomFolder)
                    {
                        _customFolderName = EditorGUILayout.TextField(new GUIContent("Folder Name"), _customFolderName);
                    }
                    else if (_template != null)
                    {
                        GUILayout.Label($"Save to: Generated/{_template.GetType().Name}/", UI.Hint);
                    }

                    GUILayout.Space(4);
                    string basePath = ForgeAssetExporter.GetGeneratedBasePath();
                    GUILayout.Label(new GUIContent("Base Path", "Configured in ForgeGeneratorSettings or config file."), UI.Hint);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(basePath, EditorStyles.textField, GUILayout.Height(18));
                    if (GUILayout.Button(new GUIContent("", UI.Copy, "Copy path"), GUILayout.Width(24), GUILayout.Height(18)))
                        EditorGUIUtility.systemCopyBuffer = basePath;
                    EditorGUILayout.EndHorizontal();

                    EditorGUIUtility.labelWidth = old;
                }
            }
        }

        // ========= Primary Generate Button =========
        private void DrawPrimaryButton()
        {
            bool hasTemplateOrBlueprint = _template != null || (_blueprint != null && _blueprint.Template != null);
            EditorGUI.BeginDisabledGroup(_isGenerating || !hasTemplateOrBlueprint);

            var r = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));

            // background
            EditorGUI.DrawRect(r, new Color(0, 0, 0, 0.08f));
            // hover tint
            if (r.Contains(Event.current.mousePosition) && !_isGenerating && hasTemplateOrBlueprint)
                EditorGUI.DrawRect(r, UI.AccentDim);

            // click area
            if (GUI.Button(r, GUIContent.none))
                GenerateItems();

            // icon
            var iconRect = new Rect(r.x + 12, r.y + (r.height - 20) / 2f, 20, 20);
            GUI.DrawTexture(iconRect, UI.Play, ScaleMode.ScaleToFit, true);

            // label (centered)
            string text = _isGenerating ? "Generating…" : $"Generate {_itemCount} Item(s)";
            EditorGUI.LabelField(r, text, UI.PrimaryBtnText);

            EditorGUI.EndDisabledGroup();
        }

        // ========= Status & Results =========
        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_status)) return;
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(_status, _statusType);
        }

        private void DrawFooter()
        {
            var r = EditorGUILayout.GetControlRect(false, 22);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), UI.Line);
            GUILayout.Space(2);
            GUILayout.Label("Forge • GameLabs — Generate faster. Keep control.", UI.Hint);
            GUILayout.Space(2);
        }

        // ========= Section header helper =========
        private void DrawSectionHeader(string title)
        {
            GUILayout.Space(6);
            var rect = EditorGUILayout.GetControlRect(false, 22);
            var line = new Rect(rect.x, rect.y + rect.height - 3, rect.width, 2);
            EditorGUI.DrawRect(line, UI.Line);
            EditorGUI.LabelField(rect, title, UI.Section);
        }

        // ========= Logic (unchanged) =========
        private void GenerateItems()
        {
            // Prefer blueprint if available, otherwise use template
            if (_blueprint == null && _template == null) return;

            _isGenerating = true;
            _status = "Generating items…";
            _statusType = MessageType.Info;
            _lastGenerated.Clear();
            Repaint();

            var generator = ForgeTemplateGenerator.Instance;

            if (_blueprint != null && _blueprint.Template != null)
            {
                // Use blueprint-based generation
                generator.GenerateFromBlueprint(_blueprint, _itemCount, OnGenerationComplete);
            }
            else
            {
                // Use template-based generation (legacy path)
                if (_foundJson != null && _foundJson.Count > 0)
                {
                    generator.Settings.existingItemsJson.Clear();
                    foreach (var j in _foundJson)
                        if (!string.IsNullOrEmpty(j))
                            generator.Settings.existingItemsJson.Add(j);
                    ForgeLogger.Log($"Added {_foundJson.Count} existing items to generation context");
                }

                generator.GenerateFromTemplate(_template, _itemCount, OnGenerationComplete, _additionalContext);
            }
        }

        private void OnGenerationComplete(ForgeTemplateGenerationResult result)
        {
            _isGenerating = false;

            if (!result.success)
            {
                _status = $"Generation failed: {result.errorMessage}";
                _statusType = MessageType.Error;
                Repaint();
                return;
            }

            _lastGenerated.Clear();
            _itemSavedState.Clear();
            _lastGenerated.AddRange(result.items);
            
            // Mark all as unsaved initially
            foreach (var item in result.items)
                _itemSavedState[item] = false;

            if (_autoSaveAsAsset && _template != null)
            {
                string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                    ? _customFolderName
                    : _template.GetType().Name;

                var saved = SaveGeneratedAssets(result.items, folder);
                
                // Mark saved items
                for (int i = 0; i < saved && i < result.items.Count; i++)
                    _itemSavedState[result.items[i]] = true;

                _status = $"✓ Generated {result.items.Count} item(s) and saved {saved} asset(s)\n" +
                          $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)";
            }
            else
            {
                _status = $"✓ Generated {result.items.Count} item(s)\n" +
                          $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)";
            }

            _statusType = MessageType.Info;
            Repaint();
        }

        private int SaveGeneratedAssets(List<ScriptableObject> items, string folder)
        {
            if (items == null || items.Count == 0) return 0;

            string folderPath = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            EnsureDir(folderPath);

            int saved = 0;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var itm = items[i];
                    if (itm == null) continue;

                    string baseName = string.IsNullOrEmpty(itm.name)
                        ? $"{itm.GetType().Name}_{stamp}_{i + 1}"
                        : $"{itm.name}_{stamp}_{i + 1}";

                    string unique = UniqueName(folderPath, baseName);
                    string full = Path.Combine(folderPath, unique + ".asset");

                    AssetDatabase.CreateAsset(itm, full);
                    saved++;
                    ForgeLogger.Log($"Saved asset: {full}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ForgeLogger.Log($"Batch save completed: {saved} assets saved to {folderPath}");
            return saved;
        }

        private void EnsureDir(string path)
        {
            if (Directory.Exists(path)) return;

            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent))
                EnsureDir(parent);

            string parentFolder = Path.GetDirectoryName(path);
            string newFolder = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(newFolder))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolder);
                ForgeLogger.Log($"Created folder: {path}");
            }
        }

        private void SaveSingleItem(ScriptableObject item, int index)
        {
            if (item == null || _template == null) return;

            string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                ? _customFolderName
                : _template.GetType().Name;

            string folderPath = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            EnsureDir(folderPath);

            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string baseName = string.IsNullOrEmpty(item.name)
                ? $"{item.GetType().Name}_{stamp}_{index + 1}"
                : $"{item.name}_{stamp}_{index + 1}";

            string unique = UniqueName(folderPath, baseName);
            string full = Path.Combine(folderPath, unique + ".asset");

            AssetDatabase.StartAssetEditing();
            try
            {
                AssetDatabase.CreateAsset(item, full);
                _itemSavedState[item] = true;
                ForgeLogger.Log($"Saved asset: {full}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Repaint();
        }

        private void SaveAllUnsavedItems()
        {
            if (_template == null) return;

            string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                ? _customFolderName
                : _template.GetType().Name;

            string folderPath = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            EnsureDir(folderPath);

            int saved = 0;
            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < _lastGenerated.Count; i++)
                {
                    var item = _lastGenerated[i];
                    if (item == null) continue;
                    
                    // Skip already saved items
                    if (_itemSavedState.ContainsKey(item) && _itemSavedState[item])
                        continue;

                    string baseName = string.IsNullOrEmpty(item.name)
                        ? $"{item.GetType().Name}_{stamp}_{i + 1}"
                        : $"{item.name}_{stamp}_{i + 1}";

                    string unique = UniqueName(folderPath, baseName);
                    string full = Path.Combine(folderPath, unique + ".asset");

                    AssetDatabase.CreateAsset(item, full);
                    _itemSavedState[item] = true;
                    saved++;
                    ForgeLogger.Log($"Saved asset: {full}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            _status = $"Saved {saved} asset(s) to {folder}";
            _statusType = MessageType.Info;
            ForgeLogger.Log($"Batch save completed: {saved} assets saved to {folderPath}");
            Repaint();
        }

        private string UniqueName(string folderPath, string baseName)
        {
            if (string.IsNullOrEmpty(baseName)) baseName = "Item";
            string file = baseName;
            string full = Path.Combine(folderPath, file + ".asset");
            int n = 1;
            while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(full) != null)
            {
                file = $"{baseName}_{n}";
                full = Path.Combine(folderPath, file + ".asset");
                n++;
            }
            return file;
        }

        private void OpenGeneratedFolder()
        {
            if (_template == null) return;

            string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                ? _customFolderName
                : _template.GetType().Name;

            var path = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
            else
            {
                EditorUtility.DisplayDialog("Folder Not Found", $"No folder at:\n{path}\nIt will be created on first save.", "OK");
            }
        }

        private void FindExistingItems()
        {
            if (_template == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a template first.", "OK");
                return;
            }

            var itemType = _template.GetType();
            var settings = ForgeConfig.GetGeneratorSettings();
            string searchPath = settings?.existingAssetsSearchPath ?? "Resources";

            var method = typeof(ForgeAssetDiscovery).GetMethod(nameof(ForgeAssetDiscovery.DiscoverAssetsAsJson),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var generic = method.MakeGenericMethod(itemType);
            var result = generic.Invoke(null, new object[] { searchPath }) as List<string>;

            _foundJson = result ?? new List<string>();
            _foundCount = _foundJson.Count;

            if (_foundCount == 0)
            {
                EditorUtility.DisplayDialog("Existing Items",
                    $"No existing {itemType.Name} items found in '{searchPath}'.\n\n" +
                    "Make sure you have ScriptableObject assets of this type in the search path.",
                    "OK");
            }
            else
            {
                ForgeLogger.Log($"Discovered {_foundCount} existing {itemType.Name} items");
            }

            Repaint();
        }

        private void ShowFoundPopup()
        {
            var p = ScriptableObject.CreateInstance<ExistingItemsPopup>();
            p.titleContent = new GUIContent($"Existing Items ({_foundCount})");
            p.itemsJson = new List<string>(_foundJson);
            p.minSize = new Vector2(520, 520);
            p.maxSize = new Vector2(820, 1000);
            p.ShowUtility();
        }
    }

    /// <summary>Popup to display discovered existing items (visual only).</summary>
    public class ExistingItemsPopup : EditorWindow
    {
        public List<string> itemsJson = new();
        private Vector2 _scroll;

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Discovered Items", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These items guide generation to avoid duplicates and keep naming/style consistent.", MessageType.Info);

            GUILayout.Space(6);
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(1, 1, 1, 0.07f));

            GUILayout.Space(8);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var json in itemsJson)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.TextArea(json, GUILayout.MinHeight(60));
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Close", GUILayout.Height(26))) Close();
            GUILayout.Space(6);
        }
    }
}
#endif
