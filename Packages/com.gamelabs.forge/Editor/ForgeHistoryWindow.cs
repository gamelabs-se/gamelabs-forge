using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Window for viewing and managing generation history.
    /// Allows regeneration with same settings and tracking patterns.
    /// </summary>
    public class ForgeHistoryWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string filterBlueprint = "";
        private string filterMode = ""; // "", "New", "Variant"
        private bool showOnlySuccessful = false;
        private int displayCount = 25;
        
        private ForgeGenerationRecord selectedRecord;
        private Vector2 detailScrollPosition;
        
        // Styles
        private GUIStyle headerStyle;
        private GUIStyle recordStyle;
        private GUIStyle selectedRecordStyle;
        private GUIStyle successStyle;
        private GUIStyle failureStyle;
        private GUIStyle detailLabelStyle;
        private bool stylesInitialized = false;
        
        [MenuItem("Tools/GameLabs/FORGE/Generation History", false, 103)]
        public static void ShowWindow()
        {
            var window = GetWindow<ForgeHistoryWindow>("FORGE History");
            window.minSize = new Vector2(700, 400);
            window.Show();
        }
        
        private void InitStyles()
        {
            if (stylesInitialized) return;
            
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 10)
            };
            
            recordStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(0, 0, 2, 2)
            };
            
            selectedRecordStyle = new GUIStyle(recordStyle);
            selectedRecordStyle.normal.background = CreateColorTexture(new Color(0.3f, 0.5f, 0.7f, 0.3f));
            
            successStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.4f, 0.8f, 0.4f) }
            };
            
            failureStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(0.9f, 0.4f, 0.4f) }
            };
            
            detailLabelStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true
            };
            
            stylesInitialized = true;
        }
        
        private Texture2D CreateColorTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            EditorGUILayout.BeginHorizontal();
            
            // Left panel - Record list
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
            DrawRecordList();
            EditorGUILayout.EndVertical();
            
            // Divider
            EditorGUILayout.BeginVertical(GUILayout.Width(2));
            var dividerRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.Width(1));
            EditorGUI.DrawRect(dividerRect, new Color(0.3f, 0.3f, 0.3f));
            EditorGUILayout.EndVertical();
            
            // Right panel - Details
            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            DrawRecordDetails();
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawRecordList()
        {
            EditorGUILayout.LabelField("📜 Generation History", headerStyle);
            
            // Filters
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
            
            // Blueprint filter dropdown
            var blueprints = new List<string> { "All Blueprints" };
            blueprints.AddRange(ForgeGenerationHistory.Instance.GetUniqueBlueprints());
            int blueprintIndex = string.IsNullOrEmpty(filterBlueprint) ? 0 : blueprints.IndexOf(filterBlueprint);
            if (blueprintIndex < 0) blueprintIndex = 0;
            
            int newBlueprintIndex = EditorGUILayout.Popup(blueprintIndex, blueprints.ToArray(), GUILayout.Width(150));
            filterBlueprint = newBlueprintIndex == 0 ? "" : blueprints[newBlueprintIndex];
            
            // Mode filter
            string[] modeOptions = { "All Modes", "New", "Variant" };
            int modeIndex = string.IsNullOrEmpty(filterMode) ? 0 : (filterMode == "New" ? 1 : 2);
            int newModeIndex = EditorGUILayout.Popup(modeIndex, modeOptions, GUILayout.Width(100));
            filterMode = newModeIndex == 0 ? "" : modeOptions[newModeIndex];
            
            showOnlySuccessful = EditorGUILayout.ToggleLeft("Successful only", showOnlySuccessful, GUILayout.Width(110));
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Record list
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            var records = GetFilteredRecords();
            
            if (records.Count == 0)
            {
                EditorGUILayout.HelpBox("No generation history yet. Generate some items to see them here!", MessageType.Info);
            }
            else
            {
                foreach (var record in records)
                {
                    DrawRecordRow(record);
                }
                
                // Load more button
                if (ForgeGenerationHistory.Instance.records.Count > displayCount)
                {
                    EditorGUILayout.Space(5);
                    if (GUILayout.Button($"Load More ({ForgeGenerationHistory.Instance.records.Count - displayCount} remaining)"))
                    {
                        displayCount += 25;
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // Bottom actions
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"Total: {ForgeGenerationHistory.Instance.records.Count} records", EditorStyles.miniLabel);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Clear History", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Clear History", 
                    "Are you sure you want to clear all generation history? This cannot be undone.", 
                    "Clear", "Cancel"))
                {
                    ForgeGenerationHistory.Instance.Clear();
                    selectedRecord = null;
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private List<ForgeGenerationRecord> GetFilteredRecords()
        {
            IEnumerable<ForgeGenerationRecord> filtered = ForgeGenerationHistory.Instance.records;
            
            if (!string.IsNullOrEmpty(filterBlueprint))
            {
                filtered = filtered.Where(r => r.blueprintName == filterBlueprint || r.templateTypeName == filterBlueprint);
            }
            
            if (!string.IsNullOrEmpty(filterMode))
            {
                filtered = filtered.Where(r => r.generationMode == filterMode);
            }
            
            if (showOnlySuccessful)
            {
                filtered = filtered.Where(r => r.wasSuccessful);
            }
            
            return filtered.Take(displayCount).ToList();
        }
        
        private void DrawRecordRow(ForgeGenerationRecord record)
        {
            bool isSelected = selectedRecord == record;
            var style = isSelected ? selectedRecordStyle : recordStyle;
            
            EditorGUILayout.BeginVertical(style);
            
            EditorGUILayout.BeginHorizontal();
            
            // Mode icon
            string modeIcon = record.generationMode == "Variant" ? "🔄" : "✨";
            EditorGUILayout.LabelField(modeIcon, GUILayout.Width(20));
            
            // Name
            string name = !string.IsNullOrEmpty(record.blueprintName) ? record.blueprintName : record.templateTypeName;
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(150));
            
            // Items
            EditorGUILayout.LabelField($"{record.itemsGenerated}/{record.itemsRequested}", GUILayout.Width(40));
            
            // Status
            EditorGUILayout.LabelField(record.wasSuccessful ? "✓" : "✗", 
                record.wasSuccessful ? successStyle : failureStyle, GUILayout.Width(20));
            
            // Timestamp
            EditorGUILayout.LabelField(record.timestamp, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.EndHorizontal();
            
            // Click to select
            var lastRect = GUILayoutUtility.GetLastRect();
            var fullRect = new Rect(lastRect.x, lastRect.y - 8, lastRect.width, lastRect.height + 16);
            if (Event.current.type == EventType.MouseDown && fullRect.Contains(Event.current.mousePosition))
            {
                selectedRecord = record;
                Repaint();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawRecordDetails()
        {
            EditorGUILayout.LabelField("📋 Details", headerStyle);
            
            if (selectedRecord == null)
            {
                EditorGUILayout.HelpBox("Select a record from the list to view details.", MessageType.Info);
                return;
            }
            
            detailScrollPosition = EditorGUILayout.BeginScrollView(detailScrollPosition);
            
            var record = selectedRecord;
            
            // Header
            EditorGUILayout.BeginHorizontal();
            string modeLabel = record.generationMode == "Variant" ? "🔄 Variant Generation" : "✨ New Generation";
            EditorGUILayout.LabelField(modeLabel, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(record.wasSuccessful ? "✓ Successful" : "✗ Failed", 
                record.wasSuccessful ? successStyle : failureStyle);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Basic info
            DrawDetailField("ID", record.id);
            DrawDetailField("Timestamp", record.timestamp);
            DrawDetailField("Blueprint", record.blueprintName ?? "-");
            DrawDetailField("Template Type", record.templateTypeName ?? "-");
            DrawDetailField("Model", record.modelUsed);
            
            if (!string.IsNullOrEmpty(record.sourceAssetPath))
            {
                DrawDetailField("Source Asset", record.sourceAssetPath);
            }
            
            EditorGUILayout.Space(10);
            
            // Generation stats
            EditorGUILayout.LabelField("Generation Stats", EditorStyles.boldLabel);
            DrawDetailField("Items", $"{record.itemsGenerated} / {record.itemsRequested} requested");
            DrawDetailField("Duration", $"{record.durationSeconds:F1}s");
            DrawDetailField("Tokens", $"{record.promptTokens:N0} prompt + {record.completionTokens:N0} completion");
            DrawDetailField("Est. Cost", $"${record.estimatedCost:F6}");
            
            if (record.retryCount > 0)
            {
                DrawDetailField("Retries", $"{record.retryCount} (validation)");
            }
            
            EditorGUILayout.Space(10);
            
            // User instructions
            if (!string.IsNullOrEmpty(record.userInstructions))
            {
                EditorGUILayout.LabelField("User Instructions", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(record.userInstructions, detailLabelStyle);
                EditorGUILayout.Space(10);
            }
            
            // Generated items
            if (record.generatedItemNames.Count > 0)
            {
                EditorGUILayout.LabelField($"Generated Items ({record.generatedItemNames.Count})", EditorStyles.boldLabel);
                foreach (var itemName in record.generatedItemNames)
                {
                    EditorGUILayout.LabelField($"  • {itemName}");
                }
                EditorGUILayout.Space(10);
            }
            
            // Asset paths
            if (record.generatedAssetPaths.Count > 0)
            {
                EditorGUILayout.LabelField("Asset Paths", EditorStyles.boldLabel);
                foreach (var path in record.generatedAssetPaths)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(path, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                        if (asset != null)
                        {
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space(10);
            }
            
            // Error message
            if (!string.IsNullOrEmpty(record.errorMessage))
            {
                EditorGUILayout.LabelField("Error", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(record.errorMessage, MessageType.Error);
            }
            
            EditorGUILayout.EndScrollView();
            
            // Action buttons
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("📋 Copy Instructions"))
            {
                if (!string.IsNullOrEmpty(record.userInstructions))
                {
                    GUIUtility.systemCopyBuffer = record.userInstructions;
                    ForgeLogger.Success("Instructions copied to clipboard.");
                }
            }
            
            if (GUILayout.Button("🔍 Open FORGE"))
            {
                ForgeWindow.OpenWindow();
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawDetailField(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label + ":", GUILayout.Width(100));
            EditorGUILayout.LabelField(value, detailLabelStyle);
            EditorGUILayout.EndHorizontal();
        }
    }
}
