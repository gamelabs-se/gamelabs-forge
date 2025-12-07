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
    /// Editor window for generating items and saving them as ScriptableObject assets.
    /// Provides a simple, dynamic interface for any registered item type.
    /// </summary>
    public class ForgeGeneratorWindow : EditorWindow
    {
        // UI State
        private Vector2 scrollPos;
        private string[] availableTypes;
        private int selectedTypeIndex = 0;
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
        private List<object> lastGeneratedItems = new List<object>();
        
        [MenuItem("GameLabs/Forge/Generate Items", priority = 10)]
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeGeneratorWindow>("Forge Generator");
            window.minSize = new Vector2(400, 500);
            window.RefreshTypeList();
        }
        
        private void OnEnable()
        {
            RefreshTypeList();
        }
        
        private void RefreshTypeList()
        {
            // Auto-register all ForgeItemDefinition types
            ForgeTypeRegistry.AutoRegisterItemDefinitions();
            availableTypes = ForgeTypeRegistry.GetRegisteredTypeNames().ToArray();
            
            if (availableTypes.Length == 0)
            {
                availableTypes = new[] { "No types registered" };
            }
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawTypeSelection();
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
            
            EditorGUILayout.LabelField("🔥 Forge Item Generator", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField("Generate & Save Items as ScriptableObjects", subtitleStyle);
            
            DrawSeparator();
        }
        
        private void DrawTypeSelection()
        {
            EditorGUILayout.LabelField("Item Type", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, availableTypes);
            
            if (GUILayout.Button("↻", GUILayout.Width(25)))
            {
                RefreshTypeList();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (availableTypes.Length > 0 && selectedTypeIndex < availableTypes.Length)
            {
                var selectedType = ForgeTypeRegistry.GetType(availableTypes[selectedTypeIndex]);
                if (selectedType != null)
                {
                    var schema = ForgeTypeRegistry.GetSchema(selectedType);
                    
                    EditorGUILayout.HelpBox(
                        $"Type: {schema.typeName}\n{schema.description}\nFields: {schema.fields.Count}",
                        MessageType.Info);
                }
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
                else
                {
                    string defaultFolder = availableTypes.Length > selectedTypeIndex 
                        ? availableTypes[selectedTypeIndex] 
                        : "Items";
                    EditorGUILayout.LabelField($"Save to: Generated/{defaultFolder}/");
                }
                
                EditorGUILayout.Space(3);
                
                // Show configured base path
                var settings = ForgeConfig.GetGeneratorSettings();
                string basePath = ForgeAssetExporter.GetGeneratedBasePath();
                EditorGUILayout.HelpBox(
                    $"Base Path: {basePath}\n" +
                    $"Configure in ForgeGeneratorSettings or config file.",
                    MessageType.Info);
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawGenerateButton()
        {
            EditorGUI.BeginDisabledGroup(isGenerating || availableTypes.Length == 0 || 
                                          availableTypes[0] == "No types registered");
            
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
                string displayName = "Unknown";
                if (item is ForgeItemDefinition fid)
                {
                    displayName = $"{fid.name} (ID: {fid.id})";
                }
                else
                {
                    displayName = item.ToString();
                }
                
                EditorGUILayout.LabelField($"• {displayName}");
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Clear Results"))
            {
                lastGeneratedItems.Clear();
                statusMessage = "";
            }
            
            if (autoSaveAsAsset && GUILayout.Button("Open Folder"))
            {
                string folder = useCustomFolder && !string.IsNullOrEmpty(customFolderName) 
                    ? customFolderName 
                    : availableTypes[selectedTypeIndex];
                    
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
            if (selectedTypeIndex >= availableTypes.Length)
                return;
                
            var typeName = availableTypes[selectedTypeIndex];
            var itemType = ForgeTypeRegistry.GetType(typeName);
            
            if (itemType == null)
            {
                statusMessage = $"Type '{typeName}' not found in registry.";
                statusType = MessageType.Error;
                return;
            }
            
            isGenerating = true;
            statusMessage = "Generating items...";
            statusType = MessageType.Info;
            lastGeneratedItems.Clear();
            
            // Use reflection to call the generic method
            var method = typeof(ForgeGeneratorWindow).GetMethod(
                nameof(GenerateItemsOfType), 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var genericMethod = method.MakeGenericMethod(itemType);
            genericMethod.Invoke(this, null);
        }
        
        private void GenerateItemsOfType<T>() where T : class, new()
        {
            var generator = ForgeItemGenerator.Instance;
            
            if (itemCount == 1)
            {
                generator.GenerateSingle<T>(result => OnGenerationComplete(result), additionalContext);
            }
            else
            {
                generator.GenerateBatch<T>(itemCount, result => OnGenerationComplete(result), additionalContext);
            }
        }
        
        private void OnGenerationComplete<T>(ForgeGenerationResult<T> result) where T : class
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
            foreach (var item in result.items)
            {
                lastGeneratedItems.Add(item);
            }
            
            // Auto-save as assets if enabled
            if (autoSaveAsAsset)
            {
                string folder = useCustomFolder && !string.IsNullOrEmpty(customFolderName) 
                    ? customFolderName 
                    : null;
                
                var assets = ForgeAssetExporter.CreateAssets(result.items, folder);
                
                statusMessage = $"✓ Generated {result.items.Count} item(s) and saved {assets.Count} asset(s)\n" +
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
