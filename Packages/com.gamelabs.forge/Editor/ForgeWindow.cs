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
        // ========= Generation Mode =========
        private enum GenerationMode
        {
            New,      // Generate new items from a template type
            Variants  // Generate variants of an existing item
        }
        private GenerationMode _mode = GenerationMode.New;
        
        // ========= UI State =========
        private Vector2 _scroll;
        private ForgeBlueprint _blueprint;
        private MonoScript _templateScript;  // The .cs file containing the ScriptableObject class
        private Type _templateType;          // Cached type extracted from the script
        private ScriptableObject _sourceItem; // For variant mode: the item to create variants of
        private int _itemCount = 20;
        private string _customFolderName = "";
        private bool _useCustomFolder = false;
        private bool _autoSaveAsAsset = true;
        private bool _showAdvanced = false; // Collapse advanced options by default
        private bool _interactiveMode = false; // Interactive review mode

        // ========= Blueprint & Window-Level Settings =========
        private string _blueprintInstructions = "";
        private bool _blueprintOverrideStrategy = false;
        private ForgeDuplicateStrategy _blueprintStrategy = ForgeDuplicateStrategy.Ignore;
        private string _blueprintDiscoveryPath = "";
        private bool _blueprintOverrideModel = false;
        private ForgeAIModel _blueprintModel = ForgeAIModel.GPT5Mini;
        private bool _blueprintDirty = false;
        
        // Session-level instructions (not persisted, cleared on window close)
        // Separate instructions per mode so they don't carry over
        private string _newItemsInstructions = "";
        private string _variantsInstructions = "";
        
        // Interactive mode state
        private ForgeReviewWindow _reviewWindow;
        private List<string> _interactiveFeedback = new(); // Accumulated feedback from interactive review

        private bool _isGenerating = false;
        private string _status = "";
        private MessageType _statusType = MessageType.None;

        private readonly List<ScriptableObject> _lastGenerated = new();
        private readonly Dictionary<ScriptableObject, bool> _itemSavedState = new(); // track saved/unsaved
        private bool _showPreview = false; // Show preview panel when generation completes

        private const float LABEL_W = 120f; // unified label width
        private const float CONTENT_PADDING = 16f; // canonical horizontal padding everywhere

        [MenuItem("GameLabs/Forge/Forge Window", priority = 0)]
        public static void OpenWindow()
        {
            var w = GetWindow<ForgeWindow>();
            // Use a more reliable icon
            var icon = EditorGUIUtility.IconContent("_Popup").image;
            if (icon == null) icon = EditorGUIUtility.IconContent("Settings").image;
            w.titleContent = new GUIContent("Forge", icon);
            w.minSize = new Vector2(560, 660);
            w.maxSize = new Vector2(1200, 1400);
        }

        private void OnEnable()
        {
            _showAdvanced = EditorPrefs.GetBool("GameLabs.Forge.ShowAdvanced", false);
        }
        
        /// <summary>
        /// Sets the template type from a MonoScript (C# file).
        /// Returns true if the script contains a valid ScriptableObject type.
        /// </summary>
        private bool SetTemplateFromScript(MonoScript script)
        {
            if (script == null)
            {
                _templateScript = null;
                _templateType = null;
                return false;
            }
            
            var type = script.GetClass();
            if (type == null)
            {
                ForgeLogger.Warn($"Could not get type from script: {script.name}");
                return false;
            }
            
            if (!typeof(ScriptableObject).IsAssignableFrom(type))
            {
                ForgeLogger.Warn($"Type {type.Name} is not a ScriptableObject");
                return false;
            }
            
            if (type.IsAbstract)
            {
                ForgeLogger.Warn($"Type {type.Name} is abstract and cannot be instantiated");
                return false;
            }
            
            _templateScript = script;
            _templateType = type;
            ForgeLogger.DebugLog($"Template set to type: {type.FullName}");
            return true;
        }
        
        /// <summary>
        /// Gets the effective template type (from script or blueprint).
        /// </summary>
        private Type GetEffectiveTemplateType()
        {
            if (_templateType != null)
                return _templateType;
            
            if (_blueprint?.Template != null)
                return _blueprint.Template.GetType();
            
            return null;
        }
        
        /// <summary>
        /// Returns true if we have a valid template type to generate from.
        /// </summary>
        private bool HasValidTemplate => GetEffectiveTemplateType() != null;

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
            DrawModeSelector();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (_mode == GenerationMode.New)
            {
                DrawTemplateSection();      // #1 - Template type
                GUILayout.Space(4);
                DrawGenerateOptions();      // #2 - How many to generate
                GUILayout.Space(4);
                DrawInstructionsSection();  // #3 - Optional context
                GUILayout.Space(4);
                DrawSaveOptions();          // #4 - Where to save
                GUILayout.Space(4);
                DrawAdvancedSection();      // #5 - Collapsed advanced options
            }
            else // Variants mode
            {
                DrawSourceItemSection();    // #1 - Source item to create variants of
                GUILayout.Space(4);
                DrawGenerateOptions();      // #2 - How many variants
                GUILayout.Space(4);
                DrawVariantInstructions();  // #3 - What kind of variants
                GUILayout.Space(4);
                DrawSaveOptions();          // #4 - Where to save
            }

            GUILayout.Space(8);
            DrawPrimaryButton();
            DrawStatus();
            DrawResults();

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }
        
        private void DrawModeSelector()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(CONTENT_PADDING);
            
            var newStyle = new GUIStyle(EditorStyles.miniButtonLeft);
            var variantStyle = new GUIStyle(EditorStyles.miniButtonRight);
            
            // Highlight active mode
            if (_mode == GenerationMode.New)
            {
                newStyle.fontStyle = FontStyle.Bold;
                GUI.backgroundColor = UI.Accent;
            }
            
            if (GUILayout.Button(new GUIContent("New Items", "Generate new items from a template class"), newStyle, GUILayout.Height(24)))
            {
                _mode = GenerationMode.New;
            }
            
            GUI.backgroundColor = Color.white;
            
            if (_mode == GenerationMode.Variants)
            {
                variantStyle.fontStyle = FontStyle.Bold;
                GUI.backgroundColor = UI.Accent;
            }
            
            if (GUILayout.Button(new GUIContent("Variants", "Generate variants of an existing item"), variantStyle, GUILayout.Height(24)))
            {
                _mode = GenerationMode.Variants;
            }
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(CONTENT_PADDING);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(8);
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
                    ForgeLogger.DebugLog($"Blueprint changed from {oldBlueprint?.name} to {_blueprint?.name}");
                    
                    // ALWAYS load blueprint's template - even if null
                    if (_blueprint != null && _blueprint.Template != null)
                    {
                        // Blueprint uses an instance - extract its type
                        _templateType = _blueprint.Template.GetType();
                        _templateScript = null; // Clear script reference since we're using blueprint
                        ForgeLogger.DebugLog($"Loaded template from blueprint: {_templateType?.Name ?? "NULL"}");
                    }

                    // Load blueprint values into editor fields
                    if (_blueprint != null)
                    {
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintOverrideStrategy = _blueprint.OverrideDuplicateStrategy;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintOverrideModel = _blueprint.OverrideModel;
                        _blueprintModel = _blueprint.Model;
                        _blueprintDirty = false;
                        
                        ForgeLogger.DebugLog($"Loaded blueprint settings: override={_blueprintOverrideStrategy}, strategy={_blueprintStrategy}, modelOverride={_blueprintOverrideModel}");
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

                    // AI Model override
                    EditorGUILayout.LabelField("AI Model");
                    var newModel = (ForgeAIModel)EditorGUILayout.EnumPopup(_blueprintModel);
                    if (newModel != _blueprintModel)
                    {
                        _blueprintModel = newModel;
                        _blueprint.Model = newModel;
                        _blueprint.OverrideModel = true;
                        _blueprintOverrideModel = true;
                        EditorUtility.SetDirty(_blueprint);
                        AssetDatabase.SaveAssets();
                        _blueprintDirty = false;
                        ForgeLogger.DebugLog($"Model changed to {newModel}, override=true, SAVED TO DISK");
                    }

                    var globalModel = ForgeConfig.GetModel();
                    if (_blueprintModel == globalModel)
                    {
                        EditorGUILayout.LabelField("(Same as global - no override)", UI.Hint);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"(Overriding global: {globalModel})", UI.Hint);
                    }

                    EditorGUILayout.Space(4);

                    // Editable blueprint settings
                    EditorGUILayout.LabelField("Duplicate Strategy");
                    var newStrategy = (ForgeDuplicateStrategy)EditorGUILayout.EnumPopup(_blueprintStrategy);
                    if (newStrategy != _blueprintStrategy)
                    {
                        _blueprintStrategy = newStrategy;
                        _blueprint.DuplicateStrategy = newStrategy;
                        _blueprint.OverrideDuplicateStrategy = true;
                        _blueprintOverrideStrategy = true;
                        EditorUtility.SetDirty(_blueprint);
                        AssetDatabase.SaveAssets(); // FORCE SAVE IMMEDIATELY
                        _blueprintDirty = false;
                        ForgeLogger.DebugLog($"Strategy changed to {newStrategy}, override=true, SAVED TO DISK");
                    }

                    var globalSettings = ForgeConfig.GetGeneratorSettings();
                    var globalStrategy = globalSettings?.duplicateStrategy ?? ForgeDuplicateStrategy.Ignore;

                    if (_blueprintStrategy == globalStrategy)
                    {
                        EditorGUILayout.LabelField("(Same as global - no override)", UI.Hint);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"(Overriding global: {globalStrategy})", UI.Hint);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(new GUIContent("Default Context", "Saved with blueprint. Applied to every generation."));
                    var newInstructions = EditorGUILayout.TextArea(_blueprintInstructions, UI.Code, GUILayout.MinHeight(50));
                    if (newInstructions != _blueprintInstructions)
                    {
                        _blueprintInstructions = newInstructions;
                        _blueprint.Instructions = newInstructions;
                        EditorUtility.SetDirty(_blueprint);
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

                    string effectivePath = _blueprint.GetEffectiveDiscoveryPath();
                    GUILayout.Label($"Discovery path: {effectivePath} (auto-discovery on generate)", UI.Hint);

                    EditorGUILayout.Space(6);

                    EditorGUILayout.LabelField("Changes are applied immediately. Save persists to disk.", UI.Hint);

                    // Save/Discard buttons
                    EditorGUILayout.BeginHorizontal();

                    using (new EditorGUI.DisabledScope(!_blueprintDirty))
                    {
                        if (GUILayout.Button(new GUIContent(UI.Save, "Save changes to disk"), GUILayout.Height(24)))
                        {
                            AssetDatabase.SaveAssets();
                            _blueprintDirty = false;
                            ForgeLogger.DebugLog($"Blueprint '{_blueprint.DisplayName}' saved to disk.");
                        }
                    }

                    if (GUILayout.Button("Revert", GUILayout.Height(24)))
                    {
                        AssetDatabase.Refresh();
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintOverrideStrategy = _blueprint.OverrideDuplicateStrategy;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintOverrideModel = _blueprint.OverrideModel;
                        _blueprintModel = _blueprint.Model;
                        _blueprintDirty = false;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.Space(6);

                    var globalSettings = ForgeConfig.GetGeneratorSettings();
                    var globalModel = ForgeConfig.GetModel();
                    var globalStrategy = globalSettings?.duplicateStrategy ?? ForgeDuplicateStrategy.Ignore;
                    
                    EditorGUILayout.LabelField("AI Model", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Using global: {globalModel}", UI.Hint);
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Duplicate Strategy", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Using global: {globalStrategy}", UI.Hint);
                    
                    EditorGUILayout.HelpBox("Create a Blueprint to override model and strategy.", MessageType.Info);

                    var settings = ForgeConfig.GetGeneratorSettings();
                    string effectivePath = settings?.existingAssetsSearchPath ?? "Assets";
                    GUILayout.Label($"Discovery path: {effectivePath}", UI.Hint);
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

            // Initialize with current template type if available
            // Blueprint needs an instance, so create a temporary one from the type
            var templateType = GetEffectiveTemplateType();
            if (templateType != null)
            {
                var tempInstance = ScriptableObject.CreateInstance(templateType);
                blueprint.Template = tempInstance;
            }

            AssetDatabase.CreateAsset(blueprint, path);
            AssetDatabase.SaveAssets();

            _blueprint = blueprint;
            _blueprintInstructions = blueprint.Instructions;
            _blueprintOverrideStrategy = blueprint.OverrideDuplicateStrategy;
            _blueprintStrategy = blueprint.DuplicateStrategy;
            _blueprintDiscoveryPath = blueprint.DiscoveryPathOverride;
            _blueprintOverrideModel = blueprint.OverrideModel;
            _blueprintModel = blueprint.Model;
            _blueprintDirty = false;

            ForgeLogger.DebugLog($"Created new blueprint: {blueprint.DisplayName}");
        }

        private void DrawTemplateSection()
        {
            DrawSectionHeader("1. Select Template Type");

            bool hasTemplate = HasValidTemplate;

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
                    EditorGUI.LabelField(readyRect, "✓ Template Type Selected", labelStyle);
                    GUILayout.Space(6);
                }

                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                var oldScript = _templateScript;
                
                EditorGUILayout.BeginHorizontal();
                
                // Template field label
                EditorGUILayout.LabelField("Template Class", GUILayout.Width(LABEL_W));
                
                // MonoScript field - allows selecting .cs files
                var newScript = (MonoScript)EditorGUILayout.ObjectField(
                    _templateScript, 
                    typeof(MonoScript), 
                    false,
                    GUILayout.Height(18));
                
                EditorGUILayout.EndHorizontal();
                
                // Handle script change
                if (newScript != oldScript)
                {
                    if (newScript != null)
                    {
                        if (!SetTemplateFromScript(newScript))
                        {
                            // Invalid script - show error
                            EditorUtility.DisplayDialog("Invalid Template", 
                                "The selected script must be a non-abstract class that inherits from ScriptableObject.", 
                                "OK");
                        }
                    }
                    else
                    {
                        _templateScript = null;
                        _templateType = null;
                    }
                }

                var templateType = GetEffectiveTemplateType();
                if (templateType != null)
                {
                    var schema = ForgeSchemaExtractor.ExtractSchema(templateType);

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
                        "Select a C# script file (.cs) that defines a ScriptableObject class.\n" +
                        "The class will be used as the template for generation.",
                        MessageType.Info);
                }

                EditorGUIUtility.labelWidth = old;
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

                EditorGUI.LabelField(label, _mode == GenerationMode.Variants ? "Variant Count" : "Item Count");
                _itemCount = Mathf.RoundToInt(GUI.HorizontalSlider(slider, _itemCount, 1, 50));
                // badge
                EditorGUI.DrawRect(badge, UI.Accent);
                var bc = GUI.color; GUI.color = Color.white;
                GUI.Label(badge, _itemCount.ToString(), UI.Pill);
                GUI.color = bc;
                
                GUILayout.Space(8);
                
                // Interactive mode toggle
                EditorGUILayout.BeginHorizontal();
                _interactiveMode = EditorGUILayout.Toggle(_interactiveMode, GUILayout.Width(16));
                EditorGUILayout.LabelField(
                    new GUIContent("Interactive Review", 
                        "Review each generated item before saving. Accept, discard, or provide feedback for better results."),
                    GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();
                
                if (_interactiveMode)
                {
                    EditorGUILayout.LabelField("Review items one-by-one and provide feedback for continuous improvement", UI.Hint);
                }

                EditorGUIUtility.labelWidth = old;
            }
        }

        private void DrawInstructionsSection()
        {
            DrawSectionHeader("3. Context (Optional)");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                _newItemsInstructions = EditorGUILayout.TextArea(_newItemsInstructions, UI.Code, GUILayout.MinHeight(44));
                EditorGUILayout.LabelField("Guide the AI: theme, style, balance, or specific requirements", UI.Hint);
            }
        }
        
        private void DrawSourceItemSection()
        {
            DrawSectionHeader("1. Select Source Item");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                if (_sourceItem != null)
                {
                    var readyRect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
                    EditorGUI.DrawRect(readyRect, new Color(0.2f, 0.75f, 0.35f, 0.15f));
                    var labelStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10
                    };
                    EditorGUI.LabelField(readyRect, "✓ Source Item Selected", labelStyle);
                    GUILayout.Space(6);
                }

                var old = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LABEL_W;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Source Item", GUILayout.Width(LABEL_W));
                _sourceItem = (ScriptableObject)EditorGUILayout.ObjectField(
                    _sourceItem,
                    typeof(ScriptableObject),
                    false,
                    GUILayout.Height(18));
                EditorGUILayout.EndHorizontal();

                if (_sourceItem != null)
                {
                    var itemType = _sourceItem.GetType();
                    var schema = ForgeSchemaExtractor.ExtractSchema(itemType);

                    EditorGUILayout.Space(4);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Type: {itemType.Name}", UI.Header, GUILayout.Height(22));
                    GUILayout.Space(8);
                    
                    // Item name pill
                    string itemName = _sourceItem.name;
                    var nameField = itemType.GetField("name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly);
                    if (nameField != null)
                    {
                        var val = nameField.GetValue(_sourceItem);
                        if (val != null && !string.IsNullOrEmpty(val.ToString()))
                            itemName = val.ToString();
                    }
                    
                    var pillRect = GUILayoutUtility.GetRect(150, 22, GUILayout.Width(150));
                    EditorGUI.DrawRect(pillRect, new Color(0.2f, 0.75f, 0.35f, 0.18f));
                    GUI.Label(pillRect, $"\"{itemName}\"", UI.Pill);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    GUILayout.Space(2);
                    GUILayout.Label($"Variants will inherit structure and modify values based on your instructions.", UI.Hint);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Drag an existing ScriptableObject asset to create variants of it.\n" +
                        "Variants share the same structure but with modified values.",
                        MessageType.Info);
                }

                EditorGUIUtility.labelWidth = old;
            }
        }
        
        private void DrawVariantInstructions()
        {
            DrawSectionHeader("3. Variant Instructions");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                EditorGUILayout.LabelField(new GUIContent("How should variants differ?", "Describe what makes each variant unique"), EditorStyles.boldLabel);
                _variantsInstructions = EditorGUILayout.TextArea(_variantsInstructions, UI.Code, GUILayout.MinHeight(60));
                
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Examples:", UI.Hint);
                EditorGUILayout.LabelField("• \"Different elemental types: fire, ice, lightning\"", UI.Hint);
                EditorGUILayout.LabelField("• \"Varying rarity tiers from common to legendary\"", UI.Hint);
                EditorGUILayout.LabelField("• \"Same stats but different themes/names\"", UI.Hint);
            }
        }

        private void DrawSaveOptions()
        {
            DrawSectionHeader(_mode == GenerationMode.New ? "4. Save Options" : "4. Save Options");

            using (new EditorGUILayout.VerticalScope(UI.Card))
            {
                _autoSaveAsAsset = EditorGUILayout.ToggleLeft(new GUIContent("Auto-Save Assets", "Automatically create assets after generation"), _autoSaveAsAsset);
                
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Tip: Turn off Auto-Save to preview items before saving", UI.Hint);
                
                using (new EditorGUI.DisabledScope(!_autoSaveAsAsset))
                {
                    var old = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = LABEL_W;

                    _useCustomFolder = EditorGUILayout.ToggleLeft(new GUIContent("Use Custom Folder Name"), _useCustomFolder);
                    if (_useCustomFolder)
                    {
                        _customFolderName = EditorGUILayout.TextField(new GUIContent("Folder Name"), _customFolderName);
                    }
                    else if (HasValidTemplate)
                    {
                        GUILayout.Label($"Will save to: Generated/{GetEffectiveTemplateType()?.Name ?? "Unknown"}/", UI.Hint);
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
                    ForgeLogger.DebugLog($"Blueprint changed (advanced) from {oldBlueprint?.name} to {_blueprint?.name}");
                    
                    // ALWAYS load blueprint's template type
                    if (_blueprint != null && _blueprint.Template != null)
                    {
                        _templateType = _blueprint.Template.GetType();
                        _templateScript = null;
                        ForgeLogger.DebugLog($"Loaded template from blueprint (advanced): {_templateType?.Name ?? "NULL"}");
                        
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintOverrideStrategy = _blueprint.OverrideDuplicateStrategy;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintOverrideModel = _blueprint.OverrideModel;
                        _blueprintModel = _blueprint.Model;
                        _blueprintDirty = false;
                        
                        ForgeLogger.DebugLog($"Loaded blueprint settings (advanced): override={_blueprintOverrideStrategy}, strategy={_blueprintStrategy}, modelOverride={_blueprintOverrideModel}");
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

                    // AI Model override
                    EditorGUILayout.LabelField("AI Model");
                    var newModel = (ForgeAIModel)EditorGUILayout.EnumPopup(_blueprintModel);
                    if (newModel != _blueprintModel)
                    {
                        _blueprintModel = newModel;
                        _blueprint.Model = newModel;
                        _blueprint.OverrideModel = true;
                        _blueprintOverrideModel = true;
                        EditorUtility.SetDirty(_blueprint);
                        AssetDatabase.SaveAssets();
                        _blueprintDirty = false;
                        ForgeLogger.DebugLog($"Model changed to {newModel}, override=true, SAVED TO DISK");
                    }

                    var globalModel = ForgeConfig.GetModel();
                    if (_blueprintModel == globalModel)
                    {
                        EditorGUILayout.LabelField("(Same as global - no override)", UI.Hint);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"(Overriding global: {globalModel})", UI.Hint);
                    }

                    EditorGUILayout.Space(4);

                    EditorGUILayout.LabelField("Duplicate Strategy");
                    var newStrat = (ForgeDuplicateStrategy)EditorGUILayout.EnumPopup(_blueprintStrategy);
                    if (newStrat != _blueprintStrategy)
                    {
                        _blueprintStrategy = newStrat;
                        _blueprint.DuplicateStrategy = newStrat;
                        _blueprint.OverrideDuplicateStrategy = true;
                        _blueprintOverrideStrategy = true;
                        EditorUtility.SetDirty(_blueprint);
                        AssetDatabase.SaveAssets(); // FORCE SAVE IMMEDIATELY
                        _blueprintDirty = false;
                        ForgeLogger.DebugLog($"Strategy changed to {newStrat}, override=true, SAVED TO DISK");
                    }

                    var globalSettings = ForgeConfig.GetGeneratorSettings();
                    var globalStrategy = globalSettings?.duplicateStrategy ?? ForgeDuplicateStrategy.Ignore;
                    if (_blueprintStrategy == globalStrategy)
                    {
                        EditorGUILayout.LabelField("(Same as global - no override)", UI.Hint);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"(Overriding global: {globalStrategy})", UI.Hint);
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField(new GUIContent("Default Context", "Saved with blueprint. Applied to every generation."));
                    var newInstructions = EditorGUILayout.TextArea(_blueprintInstructions, UI.Code, GUILayout.MinHeight(50));
                    if (newInstructions != _blueprintInstructions)
                    {
                        _blueprintInstructions = newInstructions;
                        _blueprint.Instructions = newInstructions;
                        EditorUtility.SetDirty(_blueprint);
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

                    string effectivePath = _blueprint.GetEffectiveDiscoveryPath();
                    GUILayout.Label($"Discovery path: {effectivePath} (auto-discovery on generate)", UI.Hint);

                    EditorGUILayout.Space(6);

                    EditorGUILayout.LabelField("Changes are applied immediately. Save persists to disk.", UI.Hint);

                    EditorGUILayout.BeginHorizontal();

                    using (new EditorGUI.DisabledScope(!_blueprintDirty))
                    {
                        if (GUILayout.Button(new GUIContent(UI.Save, "Save changes to disk"), GUILayout.Height(24)))
                        {
                            AssetDatabase.SaveAssets();
                            _blueprintDirty = false;
                            ForgeLogger.DebugLog($"Blueprint '{_blueprint.DisplayName}' saved to disk.");
                        }
                    }

                    if (GUILayout.Button("Revert", GUILayout.Height(24)))
                    {
                        AssetDatabase.Refresh();
                        _blueprintInstructions = _blueprint.Instructions;
                        _blueprintOverrideStrategy = _blueprint.OverrideDuplicateStrategy;
                        _blueprintStrategy = _blueprint.DuplicateStrategy;
                        _blueprintDiscoveryPath = _blueprint.DiscoveryPathOverride;
                        _blueprintOverrideModel = _blueprint.OverrideModel;
                        _blueprintModel = _blueprint.Model;
                        _blueprintDirty = false;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.Space(6);

                    var globalSettings = ForgeConfig.GetGeneratorSettings();
                    var globalModel = ForgeConfig.GetModel();
                    var globalStrategy = globalSettings?.duplicateStrategy ?? ForgeDuplicateStrategy.Ignore;
                    
                    EditorGUILayout.LabelField("AI Model", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Using global: {globalModel}", UI.Hint);
                    
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Duplicate Strategy", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Using global: {globalStrategy}", UI.Hint);
                    
                    EditorGUILayout.HelpBox("Create a Blueprint to override model and strategy.", MessageType.Info);
                }

                EditorGUIUtility.labelWidth = old;
            }
        }

        // ========= Primary Generate Button =========
        private void DrawPrimaryButton()
        {
            bool canGenerate;
            string disabledText;
            string enabledText;
            
            if (_mode == GenerationMode.New)
            {
                canGenerate = HasValidTemplate || (_blueprint != null && _blueprint.Template != null);
                disabledText = "Select a template to generate items";
                enabledText = $"Generate {_itemCount} Items";
            }
            else // Variants
            {
                canGenerate = _sourceItem != null;
                disabledText = "Select a source item to create variants";
                enabledText = $"Generate {_itemCount} Variants";
            }

            EditorGUI.BeginDisabledGroup(_isGenerating || !canGenerate);

            // Aligned to content bounds, not full width
            var r = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));

            // Clean background with proper corners (no 1px gaps)
            var bgRect = new Rect(r.x, r.y, r.width, r.height);
            if (canGenerate)
            {
                EditorGUI.DrawRect(bgRect, UI.Accent);
            }
            else
            {
                EditorGUI.DrawRect(bgRect, new Color(0, 0, 0, 0.12f));
            }

            // Hover effect
            if (r.Contains(Event.current.mousePosition) && !_isGenerating && canGenerate)
                EditorGUI.DrawRect(bgRect, new Color(1, 1, 1, 0.08f));

            // Click area
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                if (_mode == GenerationMode.New)
                    GenerateItems();
                else
                    GenerateVariants();
            }

            // Text only (no icon - clean and clear)
            string text;
            if (!canGenerate)
            {
                text = disabledText;
            }
            else if (_isGenerating)
            {
                text = "Generating...";
            }
            else
            {
                text = enabledText;
            }

            var textStyle = new GUIStyle(UI.PrimaryBtnText);
            textStyle.normal.textColor = canGenerate ? Color.white :
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
                string savePath = HasValidTemplate ? ForgeAssetExporter.GetSavePathFor(GetEffectiveTemplateType(), _useCustomFolder ? _customFolderName : null) : "";
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

                using (new EditorGUI.DisabledScope(!(HasValidTemplate)))
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
                using (new EditorGUI.DisabledScope(!hasUnsaved || !HasValidTemplate))
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
            
            // Session cost and token tracking (clickable to open stats)
            bool showTokenTracking = EditorPrefs.GetBool("GameLabs.Forge.ShowTokenTracking", true);
            if (showTokenTracking)
            {
                var costTracker = ForgeCostTracker.Instance;
                if (costTracker.SessionGenerations > 0)
                {
                    string costText = $"Tokens: {costTracker.SessionTokens} (out: {costTracker.SessionCompletionTokens}, in: {costTracker.SessionPromptTokens}) | Approx. cost ${costTracker.SessionCost:F4}";
                    
                    if (GUILayout.Button(costText, UI.Hint))
                    {
                        ForgeStatisticsWindow.Open();
                    }
                }
            }
            
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

                // Populate blueprint's existing items from discovered JSON
                var effectiveStrategy = _blueprint.GetEffectiveDuplicateStrategy();

                ForgeLogger.DebugLog($"Blueprint mode: Effective strategy = {effectiveStrategy}");

                generator.GenerateFromBlueprint(_blueprint, _itemCount, OnGenerationComplete, _newItemsInstructions);
            }
            else if (HasValidTemplate)
            {
                // Window-level generation (no blueprint) - create temporary blueprint
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

                // Create temporary blueprint with window settings
                var tempBlueprint = ScriptableObject.CreateInstance<ForgeBlueprint>();
                var templateType = GetEffectiveTemplateType();
                if (templateType != null)
                {
                    tempBlueprint.Template = ScriptableObject.CreateInstance(templateType);
                }
                tempBlueprint.Instructions = ""; // No blueprint instructions in window mode
                tempBlueprint.DiscoveryPathOverride = "";

                ForgeLogger.DebugLog($"Window mode: Using global strategy");

                generator.GenerateFromBlueprint(tempBlueprint, _itemCount, OnGenerationComplete, _newItemsInstructions);
            }
            else
            {
                EditorUtility.DisplayDialog("FORGE", "Select a template or blueprint to generate items.", "OK");
            }
        }
        
        private void GenerateVariants()
        {
            if (_sourceItem == null)
            {
                EditorUtility.DisplayDialog("FORGE", "Select a source item to create variants of.", "OK");
                return;
            }
            
            _isGenerating = true;
            _status = "Generating variants…";
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

            generator.GenerateVariants(_sourceItem, _itemCount, _variantsInstructions, OnGenerationComplete);
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

            // Record statistics - use effective model from blueprint if available
            ForgeAIModel effectiveModel;
            if (_blueprint != null)
            {
                effectiveModel = _blueprint.GetEffectiveModel();
            }
            else
            {
                var settings = ForgeConfig.GetGeneratorSettings();
                effectiveModel = settings?.model ?? ForgeAIModel.GPT5Mini;
            }
            
            ForgeStatistics.Instance.RecordGeneration(
                _itemCount,
                result.items.Count,
                result.promptTokens,
                result.completionTokens,
                result.estimatedCost,
                effectiveModel
            );
            
            // Record cost tracking
            ForgeCostTracker.Instance.RecordGeneration(result.items.Count, result.estimatedCost, result.promptTokens, result.completionTokens);

            // Interactive mode: open review window
            if (_interactiveMode && result.items.Count > 0)
            {
                // If review window is already open, add items to it
                if (_reviewWindow != null)
                {
                    _reviewWindow.AddItems(result.items);
                    _status = $"Added {result.items.Count} item(s) to review queue";
                }
                else
                {
                    // Open new review window
                    string currentInstructions = _mode == GenerationMode.Variants 
                        ? _variantsInstructions 
                        : _newItemsInstructions;
                    
                    _reviewWindow = ForgeReviewWindow.Open(
                        result.items,
                        OnInteractiveReviewComplete,
                        OnGenerateMoreFromReview,
                        _itemCount,
                        currentInstructions
                    );
                    
                    _status = $"Generated {result.items.Count} item(s) - Review in progress...";
                }
                
                _statusType = MessageType.Info;
                Repaint();
                return;
            }

            // Non-interactive mode: normal flow
            _lastGenerated.Clear();
            _itemSavedState.Clear();
            _lastGenerated.AddRange(result.items);

            // Mark all as unsaved initially
            foreach (var item in result.items)
                _itemSavedState[item] = false;

            List<string> savedPaths = new List<string>();
            if (_autoSaveAsAsset && HasValidTemplate)
            {
                string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                    ? _customFolderName
                    : GetEffectiveTemplateType()?.Name ?? "Unknown";

                var saved = SaveGeneratedAssets(result.items, folder, savedPaths);

                // Mark saved items
                for (int i = 0; i < saved && i < result.items.Count; i++)
                    _itemSavedState[result.items[i]] = true;

                _status = $"✓ Generated {result.items.Count} item(s) and saved {saved} asset(s)\n" +
                          $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)";
            }
            else
            {
                _status = $"Preview Mode: {result.items.Count} item(s) generated\n" +
                          $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)\n" +
                          $"Review below and click 'Save' or 'Save All' when ready";
            }
            
            // Record to generation history
            RecordGenerationHistory(result, savedPaths, effectiveModel);

            _statusType = MessageType.Info;
            Repaint();
        }
        
        /// <summary>
        /// Records a generation to the history system.
        /// </summary>
        private void RecordGenerationHistory(ForgeTemplateGenerationResult result, List<string> savedPaths, ForgeAIModel model)
        {
            string blueprintName = _blueprint != null ? _blueprint.DisplayName : null;
            string templateTypeName = GetEffectiveTemplateType()?.Name;
            string instructions = _mode == GenerationMode.Variants ? _variantsInstructions : _newItemsInstructions;
            
            var itemNames = result.items.Select(i => i.name).ToList();
            
            ForgeGenerationHistory.Instance.AddRecord(
                blueprintName: blueprintName,
                templateTypeName: templateTypeName,
                isVariantMode: result.isVariantMode,
                model: model,
                itemsRequested: _itemCount,
                itemsGenerated: result.items.Count,
                promptTokens: result.promptTokens,
                completionTokens: result.completionTokens,
                durationSeconds: result.durationSeconds,
                userInstructions: instructions,
                assetPaths: savedPaths,
                itemNames: itemNames,
                hadValidationErrors: result.hadValidationErrors,
                retryCount: result.retryCount,
                sourceAssetPath: result.sourceAssetPath,
                wasSuccessful: result.success,
                errorMessage: result.errorMessage
            );
        }
        
        /// <summary>
        /// Called when the interactive review window is closed.
        /// </summary>
        private void OnInteractiveReviewComplete(List<ScriptableObject> acceptedItems, List<string> feedback)
        {
            _reviewWindow = null;
            _interactiveFeedback = feedback;
            
            if (acceptedItems.Count == 0)
            {
                _status = "Review complete - no items accepted";
                _statusType = MessageType.Info;
                Repaint();
                return;
            }
            
            // Save accepted items
            _lastGenerated.Clear();
            _itemSavedState.Clear();
            _lastGenerated.AddRange(acceptedItems);
            
            foreach (var item in acceptedItems)
                _itemSavedState[item] = false;
            
            if (_autoSaveAsAsset && HasValidTemplate)
            {
                string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                    ? _customFolderName
                    : GetEffectiveTemplateType()?.Name ?? "Unknown";

                var saved = SaveGeneratedAssets(acceptedItems, folder);

                for (int i = 0; i < saved && i < acceptedItems.Count; i++)
                    _itemSavedState[acceptedItems[i]] = true;

                _status = $"✓ Review complete: saved {saved} accepted item(s)";
                
                if (feedback.Count > 0)
                {
                    _status += $"\n{feedback.Count} feedback message(s) collected for future generations";
                }
            }
            else
            {
                _status = $"Review complete: {acceptedItems.Count} item(s) accepted (not auto-saved)";
            }
            
            _statusType = MessageType.Info;
            Repaint();
        }
        
        /// <summary>
        /// Called from the review window to generate more items.
        /// </summary>
        private void OnGenerateMoreFromReview()
        {
            if (_isGenerating) return;
            
            // Build combined instructions with feedback
            string instructions = _mode == GenerationMode.Variants 
                ? _variantsInstructions 
                : _newItemsInstructions;
            
            // Append accumulated feedback from review
            if (_reviewWindow != null)
            {
                var feedback = _reviewWindow.GetAccumulatedFeedback();
                if (feedback.Count > 0)
                {
                    string feedbackText = "\n\nUser feedback from previous generations (IMPORTANT - address these issues):\n";
                    for (int i = 0; i < feedback.Count; i++)
                    {
                        feedbackText += $"- {feedback[i]}\n";
                    }
                    instructions += feedbackText;
                }
            }
            
            _isGenerating = true;
            _status = "Generating more items...";
            Repaint();
            
            var generator = ForgeTemplateGenerator.Instance;
            if (generator == null)
            {
                _isGenerating = false;
                _status = "Error: Failed to initialize generator";
                _statusType = MessageType.Error;
                return;
            }
            
            if (_mode == GenerationMode.Variants && _sourceItem != null)
            {
                generator.GenerateVariants(_sourceItem, _itemCount, instructions, OnGenerationComplete);
            }
            else if (HasValidTemplate)
            {
                generator.Generate(GetEffectiveTemplateType(), _itemCount, instructions, _blueprint, OnGenerationComplete);
            }
        }

        private int SaveGeneratedAssets(List<ScriptableObject> items, string folder, List<string> outPaths = null)
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
                        ? itm.GetType().Name
                        : itm.name;

                    string unique = UniqueName(folderPath, baseName);
                    string full = Path.Combine(folderPath, unique + ".asset");

                    AssetDatabase.CreateAsset(itm, full);
                    saved++;
                    outPaths?.Add(full);
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
            if (item == null || !HasValidTemplate) return;

            string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                ? _customFolderName
                : GetEffectiveTemplateType()?.Name ?? "Unknown";

            string folderPath = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            EnsureDir(folderPath);

            string baseName = string.IsNullOrEmpty(item.name)
                ? item.GetType().Name
                : item.name;

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
            if (!HasValidTemplate) return;

            string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                ? _customFolderName
                : GetEffectiveTemplateType()?.Name ?? "Unknown";

            string folderPath = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            EnsureDir(folderPath);

            int saved = 0;

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
                        ? item.GetType().Name
                        : item.name;

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
            if (!HasValidTemplate) return;

            string folder = _useCustomFolder && !string.IsNullOrEmpty(_customFolderName)
                ? _customFolderName
                : GetEffectiveTemplateType()?.Name ?? "Unknown";

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

    }
}
#endif
