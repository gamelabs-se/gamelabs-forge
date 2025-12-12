#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Visual-polished template-based generator window (Unity 6.3).
    /// Logic unchanged. UI only.
    /// </summary>
    public class ForgeTemplateWindow : EditorWindow
    {
        // ====== UI State ======
        private Vector2 scrollPos;
        private ScriptableObject template;
        private int itemCount = 1;
        private string additionalContext = "";
        private string customFolderName = "";
        private bool useCustomFolder = false;
        private bool autoSaveAsAsset = true;

        // Existing items discovery
        private int discoveredItemsCount = 0;
        private List<string> discoveredItemsJson = new List<string>();

        // Generation state
        private bool isGenerating = false;
        private string statusMessage = "";
        private MessageType statusType = MessageType.None;

        // Results
        private List<ScriptableObject> lastGeneratedItems = new List<ScriptableObject>();

        // ====== Menu ======
        [MenuItem("GameLabs/Forge/AI Item Generator", priority = 5)]
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeTemplateWindow>("Forge – AI Item Generator");
            window.minSize = new Vector2(560, 680);
            window.maxSize = new Vector2(960, 1200);
        }

        // ====== Styles ======
        private static class UI
        {
            public static GUIStyle Title;
            public static GUIStyle SubTitle;
            public static GUIStyle SectionHeader;
            public static GUIStyle Card;
            public static GUIStyle Pill;
            public static GUIStyle FooterHelp;
            public static GUIStyle PrimaryButton;
            public static GUIStyle ToolbarBtn;
            public static GUIStyle MiniMuted;
            public static Color Accent => EditorGUIUtility.isProSkin ? new Color(0.25f, 0.55f, 1f, 1f) : new Color(0.1f, 0.4f, 0.95f, 1f);
            public static Color AccentDim => EditorGUIUtility.isProSkin ? new Color(0.25f, 0.55f, 1f, 0.12f) : new Color(0.1f, 0.4f, 0.95f, 0.12f);
            public static Color Divider => EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0.08f);

            public static Texture SettingsIcon => EditorGUIUtility.IconContent("d_SettingsIcon").image;
            public static Texture SearchIcon => EditorGUIUtility.IconContent("d_Search Icon").image;
            public static Texture FolderIcon => EditorGUIUtility.IconContent("d_Folder Icon").image;
            public static Texture RefreshIcon => EditorGUIUtility.IconContent("d_Refresh").image;
            public static Texture PlayIcon => EditorGUIUtility.IconContent("d_PlayButton On").image;
            public static Texture ClearIcon => EditorGUIUtility.IconContent("TreeEditor.Trash").image;

            public static void Init()
            {
                if (Title != null) return;

                Title = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleLeft
                };

                SubTitle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 11,
                    alignment = TextAnchor.MiddleLeft
                };

                SectionHeader = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12
                };

                Card = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 6, 6)
                };

                Pill = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    padding = new RectOffset(8, 8, 2, 2)
                };

                FooterHelp = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.LowerLeft,
                    wordWrap = true
                };

                PrimaryButton = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    fixedHeight = 40
                };

                ToolbarBtn = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedHeight = 22
                };

                MiniMuted = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.6f) : new Color(0, 0, 0, 0.6f) }
                };
            }
        }

        private void OnGUI()
        {
            UI.Init();
            DrawTitleBar();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DrawTopToolbar();
            DrawSpacer(6);

            DrawTemplateSelection();
            DrawSectionDivider();

            DrawExistingItemsSection();
            DrawSectionDivider();

            DrawGenerationOptions();
            DrawSectionDivider();

            DrawSaveOptions();
            DrawSpacer(10);

            DrawGenerateButton();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                DrawSpacer(8);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }

            DrawSpacer(10);
            DrawResults();

            EditorGUILayout.EndScrollView();

            DrawFooterNote();
        }

        // ====== Visual helpers ======
        private void DrawTitleBar()
        {
            var rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, UI.AccentDim);
            GUILayout.BeginArea(rect);
            GUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(UI.PlayIcon, GUILayout.Width(24), GUILayout.Height(24));
            GUILayout.Space(4);

            GUILayout.BeginVertical();
            GUILayout.Label("Forge – AI Item Generator", UI.Title);
            GUILayout.Label("Prompt-driven content generation from ScriptableObject templates", UI.SubTitle);
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("", UI.SettingsIcon, "Open Forge Settings"), GUILayout.Width(26), GUILayout.Height(24)))
            {
                // Replace with your settings window if present
                // ForgeSettingsWindow.OpenWindow();
                EditorGUIUtility.PingObject(this);
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawSectionHeader(string title, string tooltip = null)
        {
            EditorGUILayout.Space(2);
            var r = EditorGUILayout.GetControlRect(false, 22);
            var line = new Rect(r.x, r.y + r.height - 3, r.width, 2);
            EditorGUI.DrawRect(line, UI.Divider);
            var label = new GUIContent(title, tooltip);
            EditorGUI.LabelField(r, label, UI.SectionHeader);
        }

        private void DrawSectionDivider()
        {
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, UI.Divider);
            EditorGUILayout.Space(6);
        }

        private void DrawSpacer(float px) => GUILayout.Space(px);

        private void DrawPill(string text, Color color)
        {
            var bg = new GUIStyle(UI.Pill);
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Label(text, bg, GUILayout.ExpandWidth(false));
            GUI.color = c;
        }

        // ====== Top toolbar ======
        private void DrawTopToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Quick Actions", GUILayout.Width(90));

            using (new EditorGUI.DisabledScope(template == null))
            {
                if (GUILayout.Button(new GUIContent(" Find Items", UI.SearchIcon), UI.ToolbarBtn, GUILayout.Width(110)))
                    FindExistingItems();

                if (GUILayout.Button(new GUIContent(" Open Folder", UI.FolderIcon), UI.ToolbarBtn, GUILayout.Width(110)))
                    TryOpenGeneratedFolder();
            }

            if (GUILayout.Button(new GUIContent(" Clear Results", UI.ClearIcon), UI.ToolbarBtn, GUILayout.Width(110)))
            {
                lastGeneratedItems.Clear();
                statusMessage = "";
            }

            GUILayout.FlexibleSpace();

            // Subtle count/status on the right
            if (discoveredItemsCount > 0)
            {
                GUILayout.Label($"Discovered: {discoveredItemsCount}", UI.MiniMuted);
                if (GUILayout.Button("View", UI.ToolbarBtn, GUILayout.Width(60)))
                    ShowExistingItemsPopup();
            }

            EditorGUILayout.EndHorizontal();
        }

        // ====== Template selection ======
        private void DrawTemplateSelection()
        {
            DrawSectionHeader("Template");

            EditorGUILayout.BeginVertical(UI.Card);
            template = (ScriptableObject)EditorGUILayout.ObjectField(
                new GUIContent("ScriptableObject Template", "Pick a ScriptableObject that represents your item definition."),
                template,
                typeof(ScriptableObject),
                false);

            if (template != null)
            {
                var templateType = template.GetType();
                // You already had this extractor in your codebase:
                var schema = ForgeSchemaExtractor.ExtractSchema(templateType);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                DrawPill($"Type: {schema.typeName}", UI.Accent);
                GUILayout.Space(4);
                DrawPill($"Fields: {schema.fields.Count}", new Color(0.2f, 0.7f, 0.35f, 1f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Preview Schema", UI.RefreshIcon), GUILayout.Height(24), GUILayout.Width(140)))
                {
                    var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);
                    EditorUtility.DisplayDialog("Schema Preview", schemaDesc, "OK");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(schema.description, UI.FooterHelp);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a ScriptableObject template to define the structure.\n" +
                    "• Fields & types\n• [Range] constraints\n• [Tooltip] descriptions\n• Enum options",
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        // ====== Generation options ======
        private void DrawGenerationOptions()
        {
            DrawSectionHeader("Generation Options");

            EditorGUILayout.BeginVertical(UI.Card);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Item Count", GUILayout.Width(100));
                var prev = itemCount;
                itemCount = EditorGUILayout.IntSlider(prev, 1, 50);
                if (itemCount != prev) Repaint();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Additional Context (Optional)", EditorStyles.miniBoldLabel);
            var ctxRect = GUILayoutUtility.GetRect(0, 64, GUILayout.ExpandWidth(true));
            GUI.Box(ctxRect, GUIContent.none);
            additionalContext = EditorGUI.TextArea(new Rect(ctxRect.x + 4, ctxRect.y + 4, ctxRect.width - 8, ctxRect.height - 8), additionalContext);

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Tip: e.g. “Fire-themed items”, “Cyberpunk names”, or “For level 50+”.", UI.MiniMuted);
            EditorGUILayout.EndVertical();
        }

        // ====== Save options ======
        private void DrawSaveOptions()
        {
            DrawSectionHeader("Save Options");

            EditorGUILayout.BeginVertical(UI.Card);

            autoSaveAsAsset = EditorGUILayout.ToggleLeft(new GUIContent("Auto-Save as Asset", "Create assets immediately after generation."), autoSaveAsAsset);
            using (new EditorGUI.DisabledScope(!autoSaveAsAsset))
            {
                EditorGUI.indentLevel++;
                useCustomFolder = EditorGUILayout.ToggleLeft(new GUIContent("Use Custom Folder Name"), useCustomFolder);
                if (useCustomFolder)
                {
                    customFolderName = EditorGUILayout.TextField(new GUIContent("Folder Name"), customFolderName);
                }
                else if (template != null)
                {
                    EditorGUILayout.LabelField($"Save to: Generated/{template.GetType().Name}/", UI.MiniMuted);
                }

                EditorGUILayout.Space(3);
                string basePath = ForgeAssetExporter.GetGeneratedBasePath();
                EditorGUILayout.LabelField(new GUIContent("Base Path", "Configured in ForgeGeneratorSettings or config file."), UI.MiniMuted);
                EditorGUILayout.SelectableLabel(basePath, EditorStyles.textField, GUILayout.Height(18));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        // ====== Generate ======
        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(isGenerating || template == null);

            var c = GUI.color;
            GUI.color = isGenerating ? new Color(1f, 1f, 1f, 0.7f) : Color.white;

            var btnText = isGenerating ? "Generating..." : $"Generate {itemCount} Item(s)";
            var icon = UI.PlayIcon;
            var r = GUILayoutUtility.GetRect(0, 44, GUILayout.ExpandWidth(true));
            if (GUI.Button(r, GUIContent.none))
            {
                GenerateItems();
            }

            // Fancy button contents (icon + label)
            var pad = 8f;
            var iconRect = new Rect(r.x + pad, r.y + (r.height - 20) / 2f, 20, 20);
            var labelRect = new Rect(iconRect.xMax + 6, r.y, r.width - (iconRect.width + pad * 2 + 6), r.height);
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            EditorGUI.LabelField(labelRect, btnText, UI.PrimaryButton);

            GUI.color = c;
            EditorGUI.EndDisabledGroup();
        }

        // ====== Results ======
        private void DrawResults()
        {
            if (lastGeneratedItems.Count == 0) return;

            DrawSectionHeader("Last Generated Items");

            EditorGUILayout.BeginVertical(UI.Card);
            foreach (var item in lastGeneratedItems)
            {
                if (item == null) continue;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("• " + item.name, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Ping", GUILayout.Width(60)))
                {
                    EditorGUIUtility.PingObject(item);
                    Selection.activeObject = item;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(" Clear Results", UI.ClearIcon), GUILayout.Height(24)))
            {
                lastGeneratedItems.Clear();
                statusMessage = "";
            }
            using (new EditorGUI.DisabledScope(!(autoSaveAsAsset && template != null)))
            {
                if (GUILayout.Button(new GUIContent(" Open Folder", UI.FolderIcon), GUILayout.Height(24)))
                {
                    TryOpenGeneratedFolder();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawFooterNote()
        {
            GUILayout.FlexibleSpace();
            var r = EditorGUILayout.GetControlRect(false, 24);
            var tint = UI.Divider;
            tint.a *= 2f;
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), tint);
            GUILayout.Space(2);
            EditorGUILayout.LabelField("Forge • GameLabs — Generate faster. Keep control.", UI.FooterHelp);
            GUILayout.Space(2);
        }

        // ====== Existing Items Section ======
        private void DrawExistingItemsSection()
        {
            DrawSectionHeader("Existing Items Context");

            EditorGUILayout.BeginVertical(UI.Card);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent(" Find Existing Items", UI.SearchIcon), GUILayout.Height(28)))
                {
                    FindExistingItems();
                }

                if (discoveredItemsCount > 0)
                {
                    DrawPill($"Found: {discoveredItemsCount}", new Color(0.2f, 0.7f, 0.35f, 1f));
                    GUILayout.Space(6);
                    if (GUILayout.Button("View", GUILayout.Width(80), GUILayout.Height(28)))
                    {
                        ShowExistingItemsPopup();
                    }
                }

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space(3);
            var settings = ForgeConfig.GetGeneratorSettings(); // your existing accessor
            EditorGUILayout.LabelField(
                $"Search path: {settings?.existingAssetsSearchPath ?? "Resources"} — Discovered items inform naming & uniqueness.",
                UI.MiniMuted);

            EditorGUILayout.EndVertical();
        }

        // ====== Logic (unchanged) ======
        private void GenerateItems()
        {
            if (template == null)
                return;

            isGenerating = true;
            statusMessage = "Generating items...";
            statusType = MessageType.Info;
            lastGeneratedItems.Clear();
            Repaint();

            var generator = ForgeTemplateGenerator.Instance;

            // Push discovered JSON into generator context
            if (discoveredItemsJson != null && discoveredItemsJson.Count > 0)
            {
                generator.Settings.existingItemsJson.Clear();
                foreach (var json in discoveredItemsJson)
                {
                    if (!string.IsNullOrEmpty(json))
                        generator.Settings.existingItemsJson.Add(json);
                }
                ForgeLogger.Log($"Added {discoveredItemsJson.Count} existing items to generation context");
            }

            generator.GenerateFromTemplate(template, itemCount, OnGenerationComplete, additionalContext);
        }

        private void OnGenerationComplete(ForgeTemplateGenerationResult result)
        {
            isGenerating = false;

            if (!result.success)
            {
                statusMessage = $"Generation failed: {result.errorMessage}";
                statusType = MessageType.Error;
                Repaint();
                return;
            }

            lastGeneratedItems.Clear();
            lastGeneratedItems.AddRange(result.items);

            if (autoSaveAsAsset && template != null)
            {
                string folder = useCustomFolder && !string.IsNullOrEmpty(customFolderName)
                    ? customFolderName
                    : template.GetType().Name;

                var savedCount = SaveGeneratedAssets(result.items, folder);

                statusMessage =
                    $"✓ Generated {result.items.Count} item(s) and saved {savedCount} asset(s)\n" +
                    $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)";
            }
            else
            {
                statusMessage =
                    $"✓ Generated {result.items.Count} item(s)\n" +
                    $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)";
            }

            statusType = MessageType.Info;
            Repaint();
        }

        private int SaveGeneratedAssets(List<ScriptableObject> items, string folder)
        {
            if (items == null || items.Count == 0)
                return 0;

            string folderPath = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            EnsureDirectoryExists(folderPath);

            int savedCount = 0;
            string batchTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null) continue;

                    string assetName = string.IsNullOrEmpty(item.name)
                        ? $"{item.GetType().Name}_{batchTimestamp}_{i + 1}"
                        : $"{item.name}_{batchTimestamp}_{i + 1}";

                    string fileName = GetUniqueFileName(folderPath, assetName);
                    string fullPath = Path.Combine(folderPath, fileName + ".asset");

                    AssetDatabase.CreateAsset(item, fullPath);
                    savedCount++;
                    ForgeLogger.Log($"Saved asset: {fullPath}");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            ForgeLogger.Log($"Batch save completed: {savedCount} assets saved to {folderPath}");
            return savedCount;
        }

        private void EnsureDirectoryExists(string path)
        {
            if (Directory.Exists(path))
                return;

            string parentPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath))
                EnsureDirectoryExists(parentPath);

            string parentFolder = Path.GetDirectoryName(path);
            string newFolderName = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(newFolderName))
            {
                AssetDatabase.CreateFolder(parentFolder, newFolderName);
                ForgeLogger.Log($"Created folder: {path}");
            }
        }

        private string GetUniqueFileName(string folderPath, string baseName)
        {
            if (string.IsNullOrEmpty(baseName))
                baseName = "Item";

            string fileName = baseName;
            string fullPath = Path.Combine(folderPath, fileName + ".asset");
            int counter = 1;

            while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath) != null)
            {
                fileName = $"{baseName}_{counter}";
                fullPath = Path.Combine(folderPath, fileName + ".asset");
                counter++;
            }

            return fileName;
        }

        private void TryOpenGeneratedFolder()
        {
            if (template == null) return;

            string folder = useCustomFolder && !string.IsNullOrEmpty(customFolderName)
                ? customFolderName
                : template.GetType().Name;

            var path = Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder);
            var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (folderAsset != null)
            {
                EditorGUIUtility.PingObject(folderAsset);
                Selection.activeObject = folderAsset;
            }
            else
            {
                EditorUtility.DisplayDialog("Folder Not Found", $"No folder at:\n{path}\nIt will be created on first save.", "OK");
            }
        }

        private void FindExistingItems()
        {
            if (template == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a template first.", "OK");
                return;
            }

            var itemType = template.GetType();
            var settings = ForgeConfig.GetGeneratorSettings();
            string searchPath = settings?.existingAssetsSearchPath ?? "Resources";

            var method = typeof(ForgeAssetDiscovery).GetMethod(nameof(ForgeAssetDiscovery.DiscoverAssetsAsJson),
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var genericMethod = method.MakeGenericMethod(itemType);
            var result = genericMethod.Invoke(null, new object[] { searchPath }) as List<string>;

            discoveredItemsJson = result ?? new List<string>();
            discoveredItemsCount = discoveredItemsJson.Count;

            if (discoveredItemsCount == 0)
            {
                EditorUtility.DisplayDialog("Existing Items",
                    $"No existing {itemType.Name} items found in '{searchPath}'.\n\n" +
                    "Make sure you have ScriptableObject assets of this type in the search path.",
                    "OK");
            }
            else
            {
                ForgeLogger.Log($"Discovered {discoveredItemsCount} existing {itemType.Name} items");
            }

            Repaint();
        }

        private void ShowExistingItemsPopup()
        {
            var popup = ScriptableObject.CreateInstance<ExistingItemsPopup>();
            popup.titleContent = new GUIContent($"Existing Items ({discoveredItemsCount})");
            popup.itemsJson = new List<string>(discoveredItemsJson);
            popup.minSize = new Vector2(520, 520);
            popup.maxSize = new Vector2(820, 1000);
            popup.ShowUtility();
        }
    }

    /// <summary>
    /// Popup: view discovered existing items (visual refresh only).
    /// </summary>
    public class ExistingItemsPopup : EditorWindow
    {
        public List<string> itemsJson = new List<string>();
        private Vector2 scrollPos;

        private void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Discovered Items", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These items guide generation to avoid duplicates and maintain naming/style consistency.",
                MessageType.Info);

            GUILayout.Space(6);
            var r = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(r, new Color(1, 1, 1, 0.07f));

            GUILayout.Space(8);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var json in itemsJson)
            {
                EditorGUILayout.BeginVertical("HelpBox");
                EditorGUILayout.TextArea(json, GUILayout.MinHeight(60));
                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            if (GUILayout.Button("Close", GUILayout.Height(26)))
                Close();
            GUILayout.Space(6);
        }
    }
}
#endif
