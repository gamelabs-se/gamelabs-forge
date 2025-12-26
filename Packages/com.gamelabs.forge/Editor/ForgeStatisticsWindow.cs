#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Window for viewing FORGE usage statistics.
    /// </summary>
    public class ForgeStatisticsWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private ForgeStatistics stats;
        
        // Accessible via FORGE window
        // [MenuItem("GameLabs/Forge/Statistics", priority = 21)]
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeStatisticsWindow>("Statistics");
            window.minSize = new Vector2(400, 500);
            window.maxSize = new Vector2(600, 800);
        }
        
        /// <summary>Static method for easy access from other windows.</summary>
        public static void Open() => OpenWindow();
        
        private void OnEnable()
        {
            stats = ForgeStatistics.Instance;
        }
        
        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(10);
            
            DrawGenerationStats();
            EditorGUILayout.Space(10);
            
            DrawTokenStats();
            EditorGUILayout.Space(10);
            
            DrawCostStats();
            EditorGUILayout.Space(10);
            
            DrawSessionStats();
            EditorGUILayout.Space(15);
            
            DrawActions();
            
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
            
            EditorGUILayout.LabelField("Statistics", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11
            };
            EditorGUILayout.LabelField("Usage Tracking", subtitleStyle);
            
            DrawSeparator();
        }
        
        private void DrawGenerationStats()
        {
            EditorGUILayout.LabelField("Generation Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawStatRow("Total Generations:", stats.totalGenerations.ToString("N0"));
            DrawStatRow("Items Requested:", stats.totalItemsRequested.ToString("N0"));
            DrawStatRow("Items Generated:", stats.totalItemsGenerated.ToString("N0"));
            DrawStatRow("Failures:", stats.totalFailures.ToString("N0"), stats.totalFailures > 0 ? Color.yellow : Color.white);
            
            EditorGUILayout.Space(5);
            
            float successRate = stats.GetSuccessRate();
            Color successColor = successRate >= 90f ? Color.green : (successRate >= 70f ? Color.yellow : Color.red);
            DrawStatRow("Success Rate:", $"{successRate:F1}%", successColor);
            
            float fulfillmentRate = stats.GetFulfillmentRate();
            Color fulfillmentColor = fulfillmentRate >= 95f ? Color.green : (fulfillmentRate >= 80f ? Color.yellow : Color.red);
            DrawStatRow("Fulfillment Rate:", $"{fulfillmentRate:F1}%", fulfillmentColor);
            DrawHelpText("Fulfillment = items generated / items requested");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTokenStats()
        {
            EditorGUILayout.LabelField("Token Usage", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawStatRow("Total Tokens:", stats.GetTotalTokens().ToString("N0"));
            DrawStatRow("Prompt Tokens:", stats.totalPromptTokens.ToString("N0"));
            DrawStatRow("Completion Tokens:", stats.totalCompletionTokens.ToString("N0"));
            
            if (stats.totalGenerations > 0)
            {
                EditorGUILayout.Space(5);
                long avgTokens = stats.GetTotalTokens() / stats.totalGenerations;
                DrawStatRow("Avg Tokens/Generation:", avgTokens.ToString("N0"));
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCostStats()
        {
            EditorGUILayout.LabelField("Cost Analysis", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawStatRow("Total Cost:", $"${stats.totalCostUSD:F4}", new Color(0.3f, 0.9f, 0.3f));
            
            if (stats.totalGenerations > 0)
            {
                DrawStatRow("Avg Cost/Generation:", $"${stats.GetAverageCostPerGeneration():F6}");
            }
            
            if (stats.totalItemsGenerated > 0)
            {
                DrawStatRow("Avg Cost/Item:", $"${stats.GetAverageCostPerItem():F6}");
            }
            
            EditorGUILayout.Space(5);
            DrawHelpText("Costs vary by model: GPT-4o ($2.50/$10 per 1M tokens), o1 ($15/$60 per 1M tokens)");
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawSessionStats()
        {
            EditorGUILayout.LabelField("Session & Timestamps", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawStatRow("Session Generations:", stats.sessionGenerations.ToString("N0"));
            DrawStatRow("Session Items:", stats.sessionItemsGenerated.ToString("N0"));
            
            EditorGUILayout.Space(5);
            
            if (!string.IsNullOrEmpty(stats.firstUsed))
            {
                DrawStatRow("First Used:", stats.firstUsed);
            }
            
            if (!string.IsNullOrEmpty(stats.lastUsed))
            {
                DrawStatRow("Last Used:", stats.lastUsed);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActions()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh", GUILayout.Height(30)))
            {
                stats = ForgeStatistics.Instance;
                Repaint();
            }
            
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Reset Statistics", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Reset Statistics", 
                    "Reset all statistics? This cannot be undone.", 
                    "Reset", "Cancel"))
                {
                    stats.Reset();
                    Repaint();
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Export Statistics to Console", GUILayout.Height(25)))
            {
                Debug.Log(stats.ToString());
                EditorUtility.DisplayDialog("Statistics", 
                    "Statistics exported to Console.", "OK");
            }
        }
        
        private void DrawStatRow(string label, string value, Color? valueColor = null)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(label, GUILayout.Width(180));
            
            var oldColor = GUI.color;
            if (valueColor.HasValue)
            {
                GUI.color = valueColor.Value;
            }
            
            var valueStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold
            };
            
            EditorGUILayout.LabelField(value, valueStyle);
            
            GUI.color = oldColor;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawHelpText(string text)
        {
            var helpStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true
            };
            
            EditorGUILayout.LabelField(text, helpStyle);
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
