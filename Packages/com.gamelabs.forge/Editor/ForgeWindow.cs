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
    /// Forge - AI-powered item generator for Unity.
    /// </summary>
    public class ForgeWindow : EditorWindow
    {
        // ========= UI State =========
        private Vector2 _scroll;
        private ForgeBlueprint _blueprint;
        private ScriptableObject _template;
        private int _itemCount = 20;
        private string _customFolderName = "";
        private bool _useCustomFolder = false;
        private bool _autoSaveAsAsset = true;
        private bool _showAdvanced = false; // Collapse advanced options by default

        // ========= Blueprint & Window-Level Settings =========
        private string _blueprintInstructions = "";
        private ForgeDuplicateStrategy _blueprintStrategy = ForgeDuplicateStrategy.Ignore;
        private string _blueprintDiscoveryPath = "";
        private bool _blueprintDirty = false;
        // Window-level settings (used when no blueprint selected)
        private string _windowInstructions = "";
        private ForgeDuplicateStrategy _windowStrategy = ForgeDuplicateStrategy.Ignore;
        private string _windowDiscoveryPath = "";

        private int _foundCount = 0;
        private List<string> _foundJson = new();
        
        // ========= Discovery State =========
        private string _lastDiscoveredTemplateName = "";
        private string _lastDiscoveredPath = "";

        private bool _isGenerating = false;
        private string _status = "";
        private MessageType _statusType = MessageType.None;

        private readonly List<ScriptableObject> _lastGenerated = new();
        private readonly Dictionary<ScriptableObject, bool> _itemSavedState = new(); // track saved/unsaved

        private const float LABEL_W = 120f; // unified label width
        private const float CONTENT_PADDING = 16f; // canonical horizontal padding everywhere

        [MenuItem("GameLabs/Forge/FORGE", priority = 0)]
        public static void OpenWindow()
        {
            var w = GetWindow<ForgeWindow>();
            // Use hammer/tool icon for the tab
            var icon = EditorGUIUtility.IconContent("d_SceneViewTools").image;
            w.titleContent = new GUIContent("GameLabs | FORGE", icon);
            w.minSize = new Vector2(560, 660);
            w.maxSize = new Vector2(1200, 1400);
        }

        private void OnEnable()
        {
            _showAdvanced = EditorPrefs.GetBool("GameLabs.Forge.ShowAdvanced", false);
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
            public static Texture2D Eye => (Texture2D)EditorGUIUtility.IconContent("d_Folder Icon").image;

            public static void Init()
            {
                if (Title != null) return;

                Title = new GUIStyle(EditorStyles.boldLabel) 
                { 
                    fontSize = 14, 
                    alignment = TextAnchor.MiddleLeft
                };

                ToolbarBtn = new GUIStyle(EditorStyles.toolbarButton) { fixedHeight = 22 };

                Section = new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 };

                Header = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.75f) : new Color(0, 0, 0, 0.75f) }
                };

                Card = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(12, 12, 8, 8),     // Standard Unity padding
                    margin = new RectOffset(0, 0, 0, 0)
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

            DrawTopBar();
            DrawToolbar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawTemplateSection();      // #1 - Template first
            GUILayout.Space(4);          // Reduced spacing between sections
            DrawGenerateOptions();      // #2 - How many to generate
            GUILayout.Space(4);
            DrawSaveOptions();          // #3 - Where to save
            GUILayout.Space(4);
            DrawAdvancedSection();      // #4 - Collapsed advanced options
            
            GUILayout.Space(8);          // Space before primary action
            DrawPrimaryButton();        // #5 - Big generate button
            DrawStatus();
            DrawResults();

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        // ========= Bars =========
        private void DrawTopBar()
        {
            // Top divider
            GUILayout.Space(4);
            var topDivider = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(topDivider, UI.Line);
            
            // Title and buttons - vertically centered between dividers
            GUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            
            // FORGE title
            GUILayout.Label("GameLabs | FORGE", UI.Title);
            
            GUILayout.FlexibleSpace();
            
            // Settings button
            if (GUILayout.Button(new GUIContent(UI.Gear, "Settings"), GUILayout.Width(24), GUILayout.Height(24)))
            {
                ForgeSettingsWindow.Open();
            }
            
            GUILayout.Space(4);
            
            // Statistics button
            if (GUILayout.Button(new GUIContent("📊", "Statistics"), GUILayout.Width(24), GUILayout.Height(24)))
            {
                ForgeStatisticsWindow.Open();
            }
            
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(8);
            
            // Bottom divider
            var bottomDivider = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(bottomDivider, UI.Line);
        }

        private void DrawToolbar()
        {
            // Removed - tabs don't represent real navigation modes
            // Actions moved to context-appropriate locations (results section, etc.)
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
                    new GUIContent("Blueprint", "Saves template, instructions, and duplicate strategy"),
                    _blueprint,
                    typeof(ForgeBlueprint),
                    false);

                // Trigger refresh if blueprint changed
                if (_blueprint != oldBlueprint)
                {
                    // Clear count to trigger auto-find in next frame, but preserve if we haven't changed actual template
                    if (oldBlueprint == null || oldBlueprint.Template != (_blueprint?.Template))
                    {
                        _foundCount = 0;
                        _foundJson.Clear();
                    }
                    // Load blueprint values into editor fields
                    if (_blueprint != null)
                    {
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintDirty = false;
                    }
                    // NOTE: Window settings are preserved even if blueprint is removed
                }

                if (GUILayout.Button(new GUIContent(UI.Search, "Create New Blueprint"), GUILayout.Width(32), GUILayout.Height(18)))
                {
                    CreateNewBlueprint();
                }

                EditorGUILayout.EndHorizontal();

                if (_blueprint != null)
                {
                    EditorGUILayout.Space(6);
                    
                    // Editable blueprint settings
                    EditorGUILayout.LabelField("Template", _blueprint.Template?.name ?? "None", UI.Hint);
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Strategy");
                    _blueprintStrategy = (ForgeDuplicateStrategy)EditorGUILayout.EnumPopup(_blueprintStrategy);
                    if (_blueprintStrategy != _blueprint.DuplicateStrategy)
                        _blueprintDirty = true;

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Instructions (Optional)");
                    var newInstructions = EditorGUILayout.TextArea(_blueprintInstructions, UI.Code, GUILayout.MinHeight(50));
                    if (newInstructions != _blueprintInstructions)
                    {
                        _blueprintInstructions = newInstructions;
                        _blueprintDirty = true;
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Discovery Path", GUILayout.Width(LABEL_W), GUILayout.Height(18));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(string.IsNullOrEmpty(_blueprintDiscoveryPath) ? "Assets (default)" : _blueprintDiscoveryPath, GUILayout.Height(18));
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(new GUIContent(UI.Folder, "Browse for folder"), GUILayout.Width(32), GUILayout.Height(18)))
                    {
                        string initialPath = string.IsNullOrEmpty(_blueprintDiscoveryPath) ? "Assets" : _blueprintDiscoveryPath;
                        string selected = EditorUtility.OpenFolderPanel("Select Discovery Path", initialPath, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            // Convert absolute path to relative if it's within project
                            if (selected.StartsWith(Application.dataPath))
                            {
                                _blueprintDiscoveryPath = "Assets" + selected.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                _blueprintDiscoveryPath = selected;
                            }
                            _blueprintDirty = true;
                        }
                    }
                    if (!string.IsNullOrEmpty(_blueprintDiscoveryPath) && GUILayout.Button(new GUIContent("✕", "Clear override"), GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        _blueprintDiscoveryPath = "";
                        _blueprintDirty = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Refresh and discovery info right below path
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent(UI.Refresh, "Refresh discovery"), GUILayout.Width(32), GUILayout.Height(22)))
                        FindExistingItems();
                    GUILayout.Space(8);
                    string countText = _foundCount > 0 ? $"Discovered: {_foundCount}" : "No items found";
                    EditorGUILayout.LabelField(countText, UI.Header);
                    GUILayout.FlexibleSpace();
                    if (_foundCount > 0 && GUILayout.Button("View", GUILayout.Width(60), GUILayout.Height(22)))
                        ShowFoundPopup();
                    EditorGUILayout.EndHorizontal();
                    var s = ForgeConfig.GetGeneratorSettings();
                    string effectivePath = _blueprint.GetEffectiveDiscoveryPath();
                    GUILayout.Label($"Search path: {effectivePath}", UI.Hint);

                    EditorGUILayout.Space(6);
                    
                    // Save/Discard buttons
                    EditorGUILayout.BeginHorizontal();
                    
                    using (new EditorGUI.DisabledScope(!_blueprintDirty))
                    {
                        if (GUILayout.Button(new GUIContent(UI.Save, "Save changes to blueprint"), GUILayout.Height(24)))
                        {
                            _blueprint.Instructions = _blueprintInstructions;
                            _blueprint.DuplicateStrategy = _blueprintStrategy;
                            _blueprint.DiscoveryPathOverride = _blueprintDiscoveryPath;
                            EditorUtility.SetDirty(_blueprint);
                            AssetDatabase.SaveAssets();
                            _blueprintDirty = false;
                            ForgeLogger.DebugLog($"Blueprint '{_blueprint.DisplayName}' saved.");
                        }
                    }
                    
                    if (GUILayout.Button("Discard", GUILayout.Height(24)))
                    {
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintDirty = false;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Select or create a Blueprint to save generation settings.",
                        MessageType.Info);
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField("Strategy");
                    _windowStrategy = (ForgeDuplicateStrategy)EditorGUILayout.EnumPopup(_windowStrategy);
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Instructions (Optional)");
                    _windowInstructions = EditorGUILayout.TextArea(_windowInstructions, UI.Code, GUILayout.MinHeight(50));
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Discovery Path", GUILayout.Width(LABEL_W), GUILayout.Height(18));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(string.IsNullOrEmpty(_windowDiscoveryPath) ? "Assets (default)" : _windowDiscoveryPath, GUILayout.Height(18));
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(new GUIContent(UI.Folder, "Browse for folder"), GUILayout.Width(32), GUILayout.Height(18)))
                    {
                        string initialPath = string.IsNullOrEmpty(_windowDiscoveryPath) ? "Assets" : _windowDiscoveryPath;
                        string selected = EditorUtility.OpenFolderPanel("Select Discovery Path", initialPath, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            // Convert absolute path to relative if it's within project
                            if (selected.StartsWith(Application.dataPath))
                            {
                                _windowDiscoveryPath = "Assets" + selected.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                _windowDiscoveryPath = selected;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(_windowDiscoveryPath) && GUILayout.Button(new GUIContent("✕", "Clear override"), GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        _windowDiscoveryPath = "";
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Refresh and discovery info right below path
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent(UI.Refresh, "Refresh discovery"), GUILayout.Width(32), GUILayout.Height(22)))
                        FindExistingItems();
                    GUILayout.Space(8);
                    string countText = _foundCount > 0 ? $"Discovered: {_foundCount}" : "No items found";
                    EditorGUILayout.LabelField(countText, UI.Header);
                    GUILayout.FlexibleSpace();
                    if (_foundCount > 0 && GUILayout.Button("View", GUILayout.Width(60), GUILayout.Height(22)))
                        ShowFoundPopup();
                    EditorGUILayout.EndHorizontal();
                    var settings = ForgeConfig.GetGeneratorSettings();
                    string effectivePath = string.IsNullOrEmpty(_windowDiscoveryPath) ? (settings?.existingAssetsSearchPath ?? "Assets") : _windowDiscoveryPath;
                    GUILayout.Label($"Search path: {effectivePath}", UI.Hint);
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
            
            // Initialize with current template if available
            if (_template != null)
            {
                blueprint.Template = _template;
            }
            
            AssetDatabase.CreateAsset(blueprint, path);
            AssetDatabase.SaveAssets();

            _blueprint = blueprint;
            _blueprintInstructions = blueprint.Instructions;
            _blueprintStrategy = blueprint.DuplicateStrategy;
            _blueprintDiscoveryPath = blueprint.DiscoveryPathOverride;
            _blueprintDirty = false;
            
            ForgeLogger.DebugLog($"Created new blueprint: {blueprint.DisplayName}");
        }

        private void DrawTemplateSection()
        {
            DrawSectionHeader("1. Select Template");

            bool hasTemplate = _template != null;
            
            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                // Ready indicator when template is set (compact badge)
                if (hasTemplate)
                {
                    var readyRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.DrawRect(readyRect, new Color(0.2f, 0.75f, 0.35f, 0.15f));
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel) 
                    { 
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10
                    };
                    EditorGUI.LabelField(readyRect, "✓ Template Detected", labelStyle);
                    GUILayout.Space(6);
                }
                
                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                var oldTemplate = _template;
                _template = (ScriptableObject)EditorGUILayout.ObjectField(
                    new GUIContent("Template", "Any existing ScriptableObject can be used as a template"),
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
                    GUILayout.Label($"Type: {schema.typeName}", UI.Header, GUILayout.Height(22));
                    GUILayout.Space(8);
                    // green count pill
                    var pillRect = GUILayoutUtility.GetRect(100, 22, GUILayout.Width(100));
                    EditorGUI.DrawRect(pillRect, new Color(0.2f, 0.75f, 0.35f, 0.18f));
                    GUI.Label(pillRect, $"Fields: {schema.fields.Count}", UI.Pill);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Preview Schema", "View field structure"), GUILayout.Height(22), GUILayout.Width(120)))
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
                        "Any existing ScriptableObject can be used as a template.",
                        MessageType.Info);
                }

                EditorGUIUtility.labelWidth = old;
            }
        }

        private void DrawExistingSection()
        {
            // Auto-find when template or discovery path changes
            var currentTemplate = _blueprint != null ? _blueprint.Template : _template;
            string currentPath = "Assets";
            if (_blueprint != null)
            {
                currentPath = _blueprint.GetEffectiveDiscoveryPath();
            }
            else
            {
                var settings = ForgeConfig.GetGeneratorSettings();
                currentPath = settings?.existingAssetsSearchPath ?? "Assets";
            }

            string currentTemplateName = currentTemplate?.GetType().Name ?? "";
            
            // Check if template or path changed
            bool templateOrPathChanged = (currentTemplateName != _lastDiscoveredTemplateName) || (currentPath != _lastDiscoveredPath);
            
            if (!string.IsNullOrEmpty(currentTemplateName) && templateOrPathChanged)
            {
                _foundCount = 0;
                _foundJson.Clear();
                FindExistingItems();
            }
        }

        private void DrawGenerateOptions()
        {
            DrawSectionHeader("2. Generation Options");

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

                EditorGUIUtility.labelWidth = old;
            }
        }

        private void DrawSaveOptions()
        {
            DrawSectionHeader("3. Save Options");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                _autoSaveAsAsset = EditorGUILayout.ToggleLeft(new GUIContent("Auto-Save Assets", "Automatically create assets after generation"), _autoSaveAsAsset);
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
                        GUILayout.Label($"Will save to: Generated/{_template.GetType().Name}/", UI.Hint);
                    }

                    GUILayout.Space(4);
                    string basePath = ForgeAssetExporter.GetGeneratedBasePath();
                    GUILayout.Label(new GUIContent("Base Path", "Configured in settings"), UI.Hint);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.SelectableLabel(basePath, EditorStyles.textField, GUILayout.Height(18));
                    if (GUILayout.Button(new GUIContent("", UI.Copy, "Copy path"), GUILayout.Width(24), GUILayout.Height(18)))
                        EditorGUIUtility.systemCopyBuffer = basePath;
                    EditorGUILayout.EndHorizontal();

                    EditorGUIUtility.labelWidth = old;
                }
            }
        }

        private void DrawAdvancedSection()
        {
            // Collapsible header
            EditorGUILayout.BeginHorizontal();
            string arrow = _showAdvanced ? "▼" : "▸";
            if (GUILayout.Button($"{arrow} Advanced Options (optional)", EditorStyles.boldLabel, GUILayout.Height(20)))
            {
                _showAdvanced = !_showAdvanced;
                EditorPrefs.SetBool("GameLabs.Forge.ShowAdvanced", _showAdvanced);
            }
            EditorGUILayout.EndHorizontal();

            if (!_showAdvanced) return;

            // Blueprint section
            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                GUILayout.Label("Blueprints", EditorStyles.boldLabel);
                GUILayout.Label("Blueprints let you reuse generation settings across sessions.", UI.Hint);
                GUILayout.Space(4);
                
                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                EditorGUILayout.BeginHorizontal();
                
                var oldBlueprint = _blueprint;
                _blueprint = (ForgeBlueprint)EditorGUILayout.ObjectField(
                    new GUIContent("Blueprint", "Saves template, instructions, and duplicate strategy"),
                    _blueprint,
                    typeof(ForgeBlueprint),
                    false);

                // Trigger refresh if blueprint changed
                if (_blueprint != oldBlueprint)
                {
                    if (oldBlueprint == null || oldBlueprint.Template != (_blueprint?.Template))
                    {
                        _foundCount = 0;
                        _foundJson.Clear();
                    }
                    if (_blueprint != null)
                    {
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintDirty = false;
                    }
                }

                if (GUILayout.Button("Create Blueprint", GUILayout.Width(120), GUILayout.Height(18)))
                {
                    CreateNewBlueprint();
                }

                EditorGUILayout.EndHorizontal();

                if (_blueprint != null)
                {
                    EditorGUILayout.Space(6);
                    
                    EditorGUILayout.LabelField("Template", _blueprint.Template?.name ?? "None", UI.Hint);
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Generation Strategy");
                    _blueprintStrategy = (ForgeDuplicateStrategy)EditorGUILayout.EnumPopup(_blueprintStrategy);
                    if (_blueprintStrategy != _blueprint.DuplicateStrategy)
                        _blueprintDirty = true;

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Additional Instructions (optional)");
                    var newInstructions = EditorGUILayout.TextArea(_blueprintInstructions, UI.Code, GUILayout.MinHeight(50));
                    if (newInstructions != _blueprintInstructions)
                    {
                        _blueprintInstructions = newInstructions;
                        _blueprintDirty = true;
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Search Existing Assets In", GUILayout.Width(LABEL_W), GUILayout.Height(18));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(string.IsNullOrEmpty(_blueprintDiscoveryPath) ? "Assets (default)" : _blueprintDiscoveryPath, GUILayout.Height(18));
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(new GUIContent(UI.Folder, "Browse for folder"), GUILayout.Width(32), GUILayout.Height(18)))
                    {
                        string initialPath = string.IsNullOrEmpty(_blueprintDiscoveryPath) ? "Assets" : _blueprintDiscoveryPath;
                        string selected = EditorUtility.OpenFolderPanel("Select Discovery Path", initialPath, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            if (selected.StartsWith(Application.dataPath))
                            {
                                _blueprintDiscoveryPath = "Assets" + selected.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                _blueprintDiscoveryPath = selected;
                            }
                            _blueprintDirty = true;
                        }
                    }
                    if (!string.IsNullOrEmpty(_blueprintDiscoveryPath) && GUILayout.Button(new GUIContent("✕", "Clear override"), GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        _blueprintDiscoveryPath = "";
                        _blueprintDirty = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent(UI.Refresh, "Refresh discovery"), GUILayout.Width(32), GUILayout.Height(22)))
                        FindExistingItems();
                    GUILayout.Space(8);
                    string countText = _foundCount > 0 ? $"Discovered: {_foundCount} existing items" : "Used to scan existing assets to reduce duplicates. Can be ignored.";
                    EditorGUILayout.LabelField(countText, UI.Hint);
                    GUILayout.FlexibleSpace();
                    if (_foundCount > 0 && GUILayout.Button("View", GUILayout.Width(60), GUILayout.Height(22)))
                        ShowFoundPopup();
                    EditorGUILayout.EndHorizontal();
                    var s = ForgeConfig.GetGeneratorSettings();
                    string effectivePath = _blueprint.GetEffectiveDiscoveryPath();
                    GUILayout.Label($"Search path: {effectivePath}", UI.Hint);

                    EditorGUILayout.Space(6);
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    using (new EditorGUI.DisabledScope(!_blueprintDirty))
                    {
                        if (GUILayout.Button(new GUIContent(UI.Save, "Save changes to blueprint"), GUILayout.Height(24)))
                        {
                            _blueprint.Instructions = _blueprintInstructions;
                            _blueprint.DuplicateStrategy = _blueprintStrategy;
                            _blueprint.DiscoveryPathOverride = _blueprintDiscoveryPath;
                            EditorUtility.SetDirty(_blueprint);
                            AssetDatabase.SaveAssets();
                            _blueprintDirty = false;
                            ForgeLogger.DebugLog($"Blueprint '{_blueprint.DisplayName}' saved.");
                        }
                    }
                    
                    if (GUILayout.Button("Discard", GUILayout.Height(24)))
                    {
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintDirty = false;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField("Generation Strategy");
                    _windowStrategy = (ForgeDuplicateStrategy)EditorGUILayout.EnumPopup(_windowStrategy);
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Additional Instructions (optional)");
                    _windowInstructions = EditorGUILayout.TextArea(_windowInstructions, UI.Code, GUILayout.MinHeight(50));
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Search Existing Assets In", GUILayout.Width(LABEL_W), GUILayout.Height(18));
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(string.IsNullOrEmpty(_windowDiscoveryPath) ? "Assets (default)" : _windowDiscoveryPath, GUILayout.Height(18));
                    EditorGUI.EndDisabledGroup();
                    if (GUILayout.Button(new GUIContent(UI.Folder, "Browse for folder"), GUILayout.Width(32), GUILayout.Height(18)))
                    {
                        string initialPath = string.IsNullOrEmpty(_windowDiscoveryPath) ? "Assets" : _windowDiscoveryPath;
                        string selected = EditorUtility.OpenFolderPanel("Select Discovery Path", initialPath, "");
                        if (!string.IsNullOrEmpty(selected))
                        {
                            if (selected.StartsWith(Application.dataPath))
                            {
                                _windowDiscoveryPath = "Assets" + selected.Substring(Application.dataPath.Length);
                            }
                            else
                            {
                                _windowDiscoveryPath = selected;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(_windowDiscoveryPath) && GUILayout.Button(new GUIContent("✕", "Clear override"), GUILayout.Width(24), GUILayout.Height(18)))
                    {
                        _windowDiscoveryPath = "";
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(2);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent(UI.Refresh, "Refresh discovery"), GUILayout.Width(32), GUILayout.Height(22)))
                        FindExistingItems();
                    GUILayout.Space(8);
                    string countText = _foundCount > 0 ? $"Discovered: {_foundCount} existing items" : "Used to scan existing assets to reduce duplicates. Can be ignored.";
                    EditorGUILayout.LabelField(countText, UI.Hint);
                    GUILayout.FlexibleSpace();
                    if (_foundCount > 0 && GUILayout.Button("View", GUILayout.Width(60), GUILayout.Height(22)))
                        ShowFoundPopup();
                    EditorGUILayout.EndHorizontal();
                    var s = ForgeConfig.GetGeneratorSettings();
                    string effectivePath = string.IsNullOrEmpty(_windowDiscoveryPath) ? 
                        (s?.existingAssetsSearchPath ?? "Assets") : _windowDiscoveryPath;
                    GUILayout.Label($"Search path: {effectivePath}", UI.Hint);
                }

                EditorGUIUtility.labelWidth = old;
            }
            
            // Auto-trigger discovery
            DrawExistingSection();
        }

        // ========= Primary Generate Button =========
        private void DrawPrimaryButton()
        {
            bool hasTemplateOrBlueprint = _template != null || (_blueprint != null && _blueprint.Template != null);
            
            EditorGUI.BeginDisabledGroup(_isGenerating || !hasTemplateOrBlueprint);

            // Aligned to content bounds, not full width
            var r = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));

            // Clean background with proper corners (no 1px gaps)
            var bgRect = new Rect(r.x, r.y, r.width, r.height);
            if (hasTemplateOrBlueprint)
            {
                EditorGUI.DrawRect(bgRect, UI.Accent);
            }
            else
            {
                EditorGUI.DrawRect(bgRect, new Color(0, 0, 0, 0.12f));
            }
            
            // Hover effect
            if (r.Contains(Event.current.mousePosition) && !_isGenerating && hasTemplateOrBlueprint)
                EditorGUI.DrawRect(bgRect, new Color(1, 1, 1, 0.08f));

            // Click area
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                GenerateItems();

            // Text only (no icon - clean and clear)
            string text;
            if (!hasTemplateOrBlueprint)
            {
                text = "Select a template to generate items";
            }
            else if (_isGenerating)
            {
                text = "Generating...";
            }
            else
            {
                text = $"Generate {_itemCount} Items";
            }
            
            var textStyle = new GUIStyle(UI.PrimaryBtnText);
            textStyle.normal.textColor = hasTemplateOrBlueprint ? Color.white : 
                (EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.5f) : new Color(0, 0, 0, 0.5f));
            
            EditorGUI.LabelField(r, text, textStyle);

            EditorGUI.EndDisabledGroup();
        }

        // ========= Status & Results =========
        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_status)) return;
            GUILayout.Space(6);
            EditorGUILayout.HelpBox(_status, _statusType);
        }

        private void DrawResults()
        {
            if (_lastGenerated.Count == 0) return;

            // Success banner at top
            int savedCount = _lastGenerated.Count(x => x != null && _itemSavedState.ContainsKey(x) && _itemSavedState[x]);
            if (savedCount > 0)
            {
                GUILayout.Space(6);
                var successRect = EditorGUILayout.GetControlRect(GUILayout.Height(32));
                EditorGUI.DrawRect(successRect, new Color(0.2f, 0.75f, 0.35f, 0.2f));
                
                var labelRect = new Rect(successRect.x + 12, successRect.y, successRect.width - 12, successRect.height);
                string savePath = _template != null ? ForgeAssetExporter.GetSavePathFor(_template.GetType(), _useCustomFolder ? _customFolderName : null) : "";
                EditorGUI.LabelField(labelRect, $"✓ Generated {savedCount} assets in {savePath}", EditorStyles.boldLabel);
            }
            
            // Action buttons below success banner
            if (savedCount > 0)
            {
                GUILayout.Space(4);
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                
                if (GUILayout.Button(new GUIContent(" Clear Results", UI.Trash), GUILayout.Height(24), GUILayout.Width(120)))
                {
                    _lastGenerated.Clear();
                    _itemSavedState.Clear();
                    _status = "";
                    _statusType = MessageType.None;
                }
                
                GUILayout.Space(4);
                
                using (new EditorGUI.DisabledScope(!(_template != null)))
                {
                    if (GUILayout.Button(new GUIContent(" Open Folder", UI.Folder), GUILayout.Height(24), GUILayout.Width(120)))
                        OpenGeneratedFolder();
                }
                
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            DrawSectionHeader("Generated Items");
            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                // Draw each generated item with action buttons
                for (int i = 0; i < _lastGenerated.Count; i++)
                {
                    var item = _lastGenerated[i];
                    if (item == null) continue;

                    bool isSaved = _itemSavedState.ContainsKey(item) && _itemSavedState[item];
                    
                    EditorGUILayout.BeginHorizontal();
                    
                    // Item name with saved/unsaved indicator
                    string indicator = isSaved ? "✓ " : "○ ";
                    EditorGUILayout.LabelField(indicator + item.name, GUILayout.ExpandWidth(true));
                    
                    // Action buttons
                    if (GUILayout.Button("View", GUILayout.Width(50)))
                    {
                        EditorGUIUtility.PingObject(item);
                        Selection.activeObject = item;
                    }
                    
                    if (!isSaved && GUILayout.Button("Save", GUILayout.Width(50)))
                    {
                        SaveSingleItem(item, i);
                    }
                    
                    // Softer "Remove" instead of "Discard"
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        _lastGenerated.RemoveAt(i);
                        _itemSavedState.Remove(item);
                        DestroyImmediate(item);
                        i--;
                    }
                    GUI.backgroundColor = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }

                GUILayout.Space(10);
                
                // Bulk action buttons
                EditorGUILayout.BeginHorizontal();
                
                // Save All button (enabled only if there are unsaved items)
                bool hasUnsaved = _lastGenerated.Any(x => x != null && (!_itemSavedState.ContainsKey(x) || !_itemSavedState[x]));
                using (new EditorGUI.DisabledScope(!hasUnsaved || _template == null))
                {
                    if (GUILayout.Button(new GUIContent(" Save All", UI.Save), GUILayout.Height(24)))
                    {
                        SaveAllUnsavedItems();
                    }
                }
                
                // Remove All button with confirmation
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button(new GUIContent(" Remove All", UI.Trash), GUILayout.Height(24)))
                {
                    if (EditorUtility.DisplayDialog("Remove All Items?", 
                        "This will remove all generated items. Saved assets will not be deleted.", 
                        "Remove All", "Cancel"))
                    {
                        _lastGenerated.Clear();
                        _itemSavedState.Clear();
                        _status = "";
                        _statusType = MessageType.None;
                    }
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFooter()
        {
            GUILayout.Space(8);
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, UI.Line);
            GUILayout.Space(6);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            GUILayout.Label("GameLabs | FORGE", UI.Hint);
            GUILayout.FlexibleSpace();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(6);
        }

        // ========= Section header helper =========
        private void DrawSectionHeader(string title)
        {
            GUILayout.Space(8);
            var rect = EditorGUILayout.GetControlRect(false, 20);
            var line = new Rect(rect.x, rect.y + rect.height - 2, rect.width, 1);
            EditorGUI.DrawRect(line, UI.Line);
            EditorGUI.LabelField(rect, title, UI.Section);
            GUILayout.Space(4);
        }

        // ========= Logic =========
        private void GenerateItems()
        {
            // Support both blueprint-based and window-level generation
            if (_blueprint != null && _blueprint.Template != null)
            {
                // Blueprint-based generation
                _isGenerating = true;
                _status = "Generating items…";
                _statusType = MessageType.Info;
                _lastGenerated.Clear();
                Repaint();

                var generator = ForgeTemplateGenerator.Instance;
                if (generator == null)
                {
                    _isGenerating = false;
                    _status = "Error: Failed to initialize generator.";
                    _statusType = MessageType.Error;
                    ForgeLogger.Error("ForgeTemplateGenerator.Instance returned null");
                    return;
                }
                generator.GenerateFromBlueprint(_blueprint, _itemCount, OnGenerationComplete);
            }
            else if (_template != null)
            {
                // Window-level generation (no blueprint)
                _isGenerating = true;
                _status = "Generating items…";
                _statusType = MessageType.Info;
                _lastGenerated.Clear();
                Repaint();

                var generator = ForgeTemplateGenerator.Instance;
                if (generator == null)
                {
                    _isGenerating = false;
                    _status = "Error: Failed to initialize generator.";
                    _statusType = MessageType.Error;
                    ForgeLogger.Error("ForgeTemplateGenerator.Instance returned null");
                    return;
                }
                generator.GenerateFromTemplate(_template, _itemCount, OnGenerationComplete, _windowInstructions);
            }
            else
            {
                EditorUtility.DisplayDialog("FORGE", "Select a template or blueprint to generate items.", "OK");
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
            
            // Record statistics
            var settings = ForgeConfig.GetGeneratorSettings();
            var model = settings?.model ?? ForgeAIModel.GPT5Mini;
            ForgeStatistics.Instance.RecordGeneration(
                _itemCount, 
                result.items.Count, 
                result.promptTokens, 
                result.completionTokens, 
                result.estimatedCost,
                model
            );
            
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
                    ForgeLogger.DebugLog($"Saved asset: {full}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ForgeLogger.DebugLog($"Batch save completed: {saved} assets saved to {folderPath}");
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
                ForgeLogger.DebugLog($"Created folder: {path}");
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
                ForgeLogger.DebugLog($"Saved asset: {full}");
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
                    ForgeLogger.DebugLog($"Saved asset: {full}");
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
            ForgeLogger.DebugLog($"Batch save completed: {saved} assets saved to {folderPath}");
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
                EditorUtility.DisplayDialog("FORGE", $"Folder not found:\n{path}\n\nIt will be created on first save.", "OK");
            }
        }

        private void FindExistingItems()
        {
            if (_template == null)
            {
                _status = "Please select a template first.";
                _statusType = MessageType.Error;
                return;
            }

            var itemType = _template.GetType();
            
            // Get discovery path from blueprint override, or global default
            string searchPath = "Assets";
            if (_blueprint != null)
            {
                searchPath = _blueprint.GetEffectiveDiscoveryPath();
            }
            else
            {
                var settings = ForgeConfig.GetGeneratorSettings();
                searchPath = settings?.existingAssetsSearchPath ?? "Assets";
            }

            var method = typeof(ForgeAssetDiscovery).GetMethod(nameof(ForgeAssetDiscovery.DiscoverAssetsAsJson),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var generic = method.MakeGenericMethod(itemType);
            var result = generic.Invoke(null, new object[] { searchPath }) as List<string>;

            _foundJson = result ?? new List<string>();
            _foundCount = _foundJson.Count;
            
            // Track what we discovered
            _lastDiscoveredTemplateName = itemType.Name;
            _lastDiscoveredPath = searchPath;

            if (_foundCount == 0)
            {
                ForgeLogger.Warn($"No existing {itemType.Name} items found in '{searchPath}'.");
            }
            else
            {
                ForgeLogger.DebugLog($"Discovered {_foundCount} existing {itemType.Name} items in '{searchPath}'");
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
            EditorGUILayout.HelpBox("Existing items used to avoid duplicates and maintain consistency.", MessageType.Info);

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
