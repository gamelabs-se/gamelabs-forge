#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Simple template-based generator window.
    /// User provides a ScriptableObject template and generates instances.
    /// </summary>
    public class ForgeTemplateWindow : EditorWindow
    {
        // UI State
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
        
        [MenuItem("GameLabs/Forge/AI Item Generator", priority = 5)]
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeTemplateWindow>("Forge - AI Item Generator");
            window.minSize = new Vector2(500, 650);
            window.maxSize = new Vector2(700, 1000);
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawTemplateSelection();
            EditorGUILayout.Space(10);
            
            DrawExistingItemsSection();
            EditorGUILayout.Space(10);
            
            DrawGenerationOptions();
            EditorGUILayout.Space(10);
            
            DrawSaveOptions();
            EditorGUILayout.Space(15);
            
            DrawGenerateButton();
            
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
            
            EditorGUILayout.Space(15);
            
            DrawResults();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            // Header with settings button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("🔥 Forge - AI Item Generator", headerStyle);
            
            GUILayout.FlexibleSpace();
            
            // Settings cogwheel button
            if (GUILayout.Button("⚙️", GUILayout.Width(30), GUILayout.Height(25)))
            {
                ForgeSettingsWindow.OpenWindow();
            }
            
            EditorGUILayout.EndHorizontal();
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField("AI-Powered Item Generation from Templates", subtitleStyle);
            
            DrawSeparator();
        }
        
        private void DrawTemplateSelection()
        {
            EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
            
            template = (ScriptableObject)EditorGUILayout.ObjectField(
                "ScriptableObject Template", 
                template, 
                typeof(ScriptableObject), 
                false,
                GUILayout.MaxWidth(650));
            
            if (template != null)
            {
                var templateType = template.GetType();
                var schema = ForgeSchemaExtractor.ExtractSchema(templateType);
                
                EditorGUILayout.HelpBox(
                    $"Type: {schema.typeName}\n" +
                    $"Description: {schema.description}\n" +
                    $"Fields: {schema.fields.Count}\n\n" +
                    $"The AI will generate items matching this template's structure,\n" +
                    $"respecting all [Range], [Tooltip], and enum constraints.",
                    MessageType.Info, true);
                
                // Show a preview of the schema
                if (GUILayout.Button("Preview Schema", GUILayout.Height(28), GUILayout.MaxWidth(650)))
                {
                    var schemaDesc = ForgeSchemaExtractor.GenerateSchemaDescription(schema);
                    EditorUtility.DisplayDialog("Schema Preview", schemaDesc, "OK");
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a ScriptableObject template to use as the basis for generation.\n\n" +
                    "The template defines:\n" +
                    "• Field names and types\n" +
                    "• Value ranges ([Range] attribute)\n" +
                    "• Descriptions ([Tooltip] attribute)\n" +
                    "• Enum options\n\n" +
                    "Create one using Assets → Create menu.",
                    MessageType.Info, true);
            }
        }
        
        private void DrawGenerationOptions()
        {
            EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Item Count", GUILayout.Width(120));
            itemCount = EditorGUILayout.IntSlider(itemCount, 1, 20, GUILayout.MaxWidth(450));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Additional Context (Optional)", EditorStyles.miniLabel);
            additionalContext = EditorGUILayout.TextArea(additionalContext, 
                GUILayout.Height(60), GUILayout.MaxWidth(650), GUILayout.ExpandHeight(false));
            
            EditorGUILayout.HelpBox(
                "Add specific instructions like 'Generate fire-themed items' or 'Items for a level 50 character'",
                MessageType.None, true);
        }
        
        private void DrawSaveOptions()
        {
            EditorGUILayout.LabelField("Save Options", EditorStyles.boldLabel);
            
            autoSaveAsAsset = EditorGUILayout.Toggle("Auto-Save as Asset", autoSaveAsAsset, GUILayout.MaxWidth(650));
            
            if (autoSaveAsAsset)
            {
                EditorGUI.indentLevel++;
                
                useCustomFolder = EditorGUILayout.Toggle("Use Custom Folder Name", useCustomFolder, GUILayout.MaxWidth(650));
                
                if (useCustomFolder)
                {
                    customFolderName = EditorGUILayout.TextField("Folder Name", customFolderName, GUILayout.MaxWidth(650));
                }
                else if (template != null)
                {
                    EditorGUILayout.LabelField($"Save to: Generated/{template.GetType().Name}/", 
                        EditorStyles.miniLabel, GUILayout.MaxWidth(650));
                }
                
                EditorGUILayout.Space(3);
                
                // Show configured base path with override option
                var settings = ForgeConfig.GetGeneratorSettings();
                string basePath = ForgeAssetExporter.GetGeneratedBasePath();
                EditorGUILayout.HelpBox(
                    $"Base Path: {basePath}\n" +
                    $"Configure in ForgeGeneratorSettings or config file.",
                    MessageType.Info, true);
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(isGenerating || template == null);
            
            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
            
            string buttonText = isGenerating ? "Generating..." : $"🔥 Generate {itemCount} Item(s)";
            
            if (GUILayout.Button(buttonText, buttonStyle, GUILayout.Height(40)))
            {
                GenerateItems();
            }
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void DrawResults()
        {
            if (lastGeneratedItems.Count == 0)
                return;
                
            EditorGUILayout.LabelField("Last Generated Items", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            foreach (var item in lastGeneratedItems)
            {
                if (item != null)
                {
                    EditorGUILayout.LabelField($"• {item.name}");
                }
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Results"))
            {
                lastGeneratedItems.Clear();
                statusMessage = "";
            }
            
            if (autoSaveAsAsset && GUILayout.Button("Open Folder") && template != null)
            {
                string folder = useCustomFolder && !string.IsNullOrEmpty(customFolderName) 
                    ? customFolderName 
                    : template.GetType().Name;
                    
                var folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                    Path.Combine(ForgeAssetExporter.GetGeneratedBasePath(), folder));
                if (folderAsset != null)
                {
                    EditorGUIUtility.PingObject(folderAsset);
                    Selection.activeObject = folderAsset;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
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
            
            // Add discovered items to the generator settings before generation
            if (discoveredItemsJson != null && discoveredItemsJson.Count > 0)
            {
                generator.Settings.existingItemsJson.Clear();
                foreach (var json in discoveredItemsJson)
                {
                    if (!string.IsNullOrEmpty(json))
                    {
                        generator.Settings.existingItemsJson.Add(json);
                    }
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
            
            // Auto-save as assets if enabled
            if (autoSaveAsAsset && template != null)
            {
                string folder = useCustomFolder && !string.IsNullOrEmpty(customFolderName) 
                    ? customFolderName 
                    : template.GetType().Name;
                
                var savedCount = SaveGeneratedAssets(result.items, folder);
                
                statusMessage = $"✓ Generated {result.items.Count} item(s) and saved {savedCount} asset(s)\n" +
                               $"Cost: ${result.estimatedCost:F6} ({result.promptTokens} prompt, {result.completionTokens} completion tokens)";
            }
            else
            {
                statusMessage = $"✓ Generated {result.items.Count} item(s)\n" +
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
            
            // Generate a timestamp for this batch to ensure uniqueness
            string batchTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    if (item == null) continue;
                    
                    // Generate unique filename using the item's name
                    string assetName = item.name;
                    if (string.IsNullOrEmpty(assetName))
                    {
                        assetName = $"{item.GetType().Name}_{batchTimestamp}_{i + 1}";
                    }
                    else
                    {
                        // Add batch timestamp and index to ensure uniqueness
                        assetName = $"{assetName}_{batchTimestamp}_{i + 1}";
                    }
                    
                    string fileName = GetUniqueFileName(folderPath, assetName);
                    string fullPath = Path.Combine(folderPath, fileName + ".asset");
                    
                    // Save the asset
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
                
            // Create parent directories as needed
            string parentPath = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parentPath) && !Directory.Exists(parentPath))
            {
                EnsureDirectoryExists(parentPath);
            }
            
            // Create the directory via AssetDatabase for proper Unity integration
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
            {
                baseName = "Item";
            }
            
            string fileName = baseName;
            string fullPath = Path.Combine(folderPath, fileName + ".asset");
            int counter = 1;
            
            // Use AssetDatabase to check for existing assets
            while (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(fullPath) != null)
            {
                fileName = $"{baseName}_{counter}";
                fullPath = Path.Combine(folderPath, fileName + ".asset");
                counter++;
            }
            
            return fileName;
        }
        
        private void DrawExistingItemsSection()
        {
            EditorGUILayout.LabelField("Existing Items Context", EditorStyles.boldLabel);
            
            var settings = ForgeConfig.GetGeneratorSettings();
            
            EditorGUILayout.BeginHorizontal();
            
            // Button to find existing objects
            if (GUILayout.Button($"🔍 Find Existing Items", GUILayout.Height(30)))
            {
                FindExistingItems();
            }
            
            // Show count if discovered
            if (discoveredItemsCount > 0)
            {
                var countStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.3f, 0.7f, 0.3f) }
                };
                EditorGUILayout.LabelField($"Found: {discoveredItemsCount}", countStyle, GUILayout.Width(80));
                
                // Button to view
                if (GUILayout.Button("View", GUILayout.Width(60), GUILayout.Height(30)))
                {
                    ShowExistingItemsPopup();
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox(
                $"Search path: {settings?.existingAssetsSearchPath ?? "Resources"}\n" +
                "Discovered items will be used as context for AI generation.",
                MessageType.Info);
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
            
            // Use reflection to call the generic method
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
            popup.minSize = new Vector2(450, 500);
            popup.maxSize = new Vector2(700, 900);
            popup.ShowUtility();
        }
        
        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }
    }
    
    /// <summary>
    /// Popup window to display discovered existing items.
    /// Shows the JSON representation of items that will be used as context for generation.
    /// </summary>
    public class ExistingItemsPopup : EditorWindow
    {
        public List<string> itemsJson = new List<string>();
        private Vector2 scrollPos;
        
        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("Discovered Items", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These items will be used as context for AI generation to prevent duplicates and guide naming conventions.",
                MessageType.Info, true);
            
            EditorGUILayout.Space(10);
            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            foreach (var json in itemsJson)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.TextArea(json, GUILayout.ExpandHeight(true), GUILayout.MaxWidth(680));
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Close", GUILayout.Height(35), GUILayout.MaxWidth(680)))
            {
                Close();
            }
            
            EditorGUILayout.Space(10);
        }
    }
}
#endif
