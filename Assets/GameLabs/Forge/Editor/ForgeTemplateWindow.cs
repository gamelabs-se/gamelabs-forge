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
        
        // Generation state
        private bool isGenerating = false;
        private string statusMessage = "";
        private MessageType statusType = MessageType.None;
        
        // Results
        private List<ScriptableObject> lastGeneratedItems = new List<ScriptableObject>();
        
        [MenuItem("GameLabs/Forge/Template Generator", priority = 5)]
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeTemplateWindow>("Forge Template Generator");
            window.minSize = new Vector2(450, 550);
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawTemplateSelection();
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
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("🔥 Forge Template Generator", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField("Generate items from ScriptableObject templates", subtitleStyle);
            
            DrawSeparator();
        }
        
        private void DrawTemplateSelection()
        {
            EditorGUILayout.LabelField("Template", EditorStyles.boldLabel);
            
            template = (ScriptableObject)EditorGUILayout.ObjectField(
                "ScriptableObject Template", 
                template, 
                typeof(ScriptableObject), 
                false);
            
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
                    MessageType.Info);
                
                // Show a preview of the schema
                if (GUILayout.Button("Preview Schema", GUILayout.Height(25)))
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
                    MessageType.Info);
            }
        }
        
        private void DrawGenerationOptions()
        {
            EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);
            
            itemCount = EditorGUILayout.IntSlider("Item Count", itemCount, 1, 20);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Additional Context (Optional)");
            additionalContext = EditorGUILayout.TextArea(additionalContext, GUILayout.Height(60));
            
            EditorGUILayout.HelpBox(
                "Add specific instructions like 'Generate fire-themed items' or 'Items for a level 50 character'",
                MessageType.None);
        }
        
        private void DrawSaveOptions()
        {
            EditorGUILayout.LabelField("Save Options", EditorStyles.boldLabel);
            
            autoSaveAsAsset = EditorGUILayout.Toggle("Auto-Save as Asset", autoSaveAsAsset);
            
            if (autoSaveAsAsset)
            {
                EditorGUI.indentLevel++;
                
                useCustomFolder = EditorGUILayout.Toggle("Use Custom Folder Name", useCustomFolder);
                
                if (useCustomFolder)
                {
                    customFolderName = EditorGUILayout.TextField("Folder Name", customFolderName);
                }
                else if (template != null)
                {
                    EditorGUILayout.LabelField($"Save to: Generated/{template.GetType().Name}/");
                }
                
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
                    Path.Combine(ForgeAssetExporter.GeneratedBasePath, folder));
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
            
            string folderPath = Path.Combine(ForgeAssetExporter.GeneratedBasePath, folder);
            EnsureDirectoryExists(folderPath);
            
            int savedCount = 0;
            
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    
                    // Generate unique filename
                    string assetName = item.name;
                    if (string.IsNullOrEmpty(assetName))
                    {
                        assetName = $"{item.GetType().Name}_{DateTime.Now:yyyyMMdd_HHmmss}";
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
        
        private void DrawSeparator()
        {
            EditorGUILayout.Space(5);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(5);
        }
    }
}
#endif
