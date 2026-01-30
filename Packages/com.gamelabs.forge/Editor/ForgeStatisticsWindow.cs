#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Window for viewing FORGE usage statistics.
    /// All costs are calculated from token usage - no hardcoded estimates.
    /// </summary>
    public class ForgeStatisticsWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private ForgeStatistics stats;
        
        public static void OpenWindow()
        {
            var window = GetWindow<ForgeStatisticsWindow>("Statistics");
            window.minSize = new Vector2(450, 600);
            window.maxSize = new Vector2(650, 900);
        }
        
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
            
            DrawCurrentSession();
            EditorGUILayout.Space(10);
            
            DrawGenerationStats();
            EditorGUILayout.Space(10);
            
            DrawPerModelStats();
            EditorGUILayout.Space(10);
            
            DrawCostSummary();
            EditorGUILayout.Space(10);
            
            DrawTimestamps();
            EditorGUILayout.Space(15);
            
            DrawActions();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawCurrentSession()
        {
            EditorGUILayout.LabelField("Current Session", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var costTracker = ForgeCostTracker.Instance;
            
            if (costTracker.SessionGenerations > 0)
            {
                DrawStatRow("Generations:", costTracker.SessionGenerations.ToString());
                DrawStatRow("Items Generated:", costTracker.SessionItemsGenerated.ToString());
                DrawStatRow("Tokens Used:", $"{costTracker.SessionTokens:N0}");
                DrawStatRow("  Input:", $"{costTracker.SessionPromptTokens:N0}");
                DrawStatRow("  Output:", $"{costTracker.SessionCompletionTokens:N0}");
                
                // Calculate cost from tokens using current model
                var settings = ForgeConfig.GetGeneratorSettings();
                float sessionCost = ForgeAIModelHelper.CalculateCost(
                    settings?.model ?? ForgeAIModel.GPT5Mini,
                    costTracker.SessionPromptTokens,
                    costTracker.SessionCompletionTokens);
                DrawStatRow("Est. Cost:", $"~${sessionCost:F4}", new Color(0.5f, 1f, 0.5f));
                
                var duration = System.DateTime.Now - costTracker.SessionStartTime;
                DrawStatRow("Duration:", $"{duration.TotalMinutes:F0}m");
            }
            else
            {
                EditorGUILayout.LabelField("No generations in this session yet", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            
            bool showTokens = EditorPrefs.GetBool("GameLabs.Forge.ShowTokenTracking", true);
            bool newShowTokens = EditorGUILayout.ToggleLeft("Show in Footer", showTokens);
            if (newShowTokens != showTokens)
            {
                EditorPrefs.SetBool("GameLabs.Forge.ShowTokenTracking", newShowTokens);
            }
            
            GUILayout.FlexibleSpace();
            
            if (costTracker.SessionGenerations > 0)
            {
                if (GUILayout.Button("Reset Session", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("Reset Session",
                        $"Reset session tracker?\n\n{costTracker.GetSessionSummary()}",
                        "Reset", "Cancel"))
                    {
                        costTracker.ResetSession();
                    }
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
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
            EditorGUILayout.LabelField("Token-Based Usage Tracking", subtitleStyle);
            
            DrawSeparator();
        }
        
        private void DrawGenerationStats()
        {
            EditorGUILayout.LabelField("All-Time Generation Stats", EditorStyles.boldLabel);
            
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
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPerModelStats()
        {
            EditorGUILayout.LabelField("Per-Model Statistics", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // GPT-5-mini
            long gpt5Total = stats.gpt5MiniPromptTokens + stats.gpt5MiniCompletionTokens;
            if (stats.gpt5MiniGenerations > 0 || gpt5Total > 0)
            {
                EditorGUILayout.LabelField("GPT-5-mini", EditorStyles.miniBoldLabel);
                DrawStatRow("  Generations:", stats.gpt5MiniGenerations.ToString("N0"));
                DrawStatRow("  Items:", stats.gpt5MiniItemsGenerated.ToString("N0"));
                DrawStatRow("  Tokens:", $"{gpt5Total:N0} ({stats.gpt5MiniPromptTokens:N0} in + {stats.gpt5MiniCompletionTokens:N0} out)");
                DrawStatRow("  Avg Tokens/Item:", $"{stats.GetAvgTokensPerItem(ForgeAIModel.GPT5Mini):F0}");
                DrawStatRow("  Est. Cost:", $"~${stats.CalculateModelCost(ForgeAIModel.GPT5Mini):F4}", new Color(0.5f, 1f, 0.5f));
                DrawStatRow("  Avg Cost/Item:", $"~${stats.GetAvgCostPerItem(ForgeAIModel.GPT5Mini):F6}", new Color(0.7f, 0.9f, 0.7f));
                EditorGUILayout.Space(5);
            }
            
            // GPT-4o
            long gpt4oTotal = stats.gpt4oPromptTokens + stats.gpt4oCompletionTokens;
            if (stats.gpt4oGenerations > 0 || gpt4oTotal > 0)
            {
                EditorGUILayout.LabelField("GPT-4o", EditorStyles.miniBoldLabel);
                DrawStatRow("  Generations:", stats.gpt4oGenerations.ToString("N0"));
                DrawStatRow("  Items:", stats.gpt4oItemsGenerated.ToString("N0"));
                DrawStatRow("  Tokens:", $"{gpt4oTotal:N0} ({stats.gpt4oPromptTokens:N0} in + {stats.gpt4oCompletionTokens:N0} out)");
                DrawStatRow("  Avg Tokens/Item:", $"{stats.GetAvgTokensPerItem(ForgeAIModel.GPT4o):F0}");
                DrawStatRow("  Est. Cost:", $"~${stats.CalculateModelCost(ForgeAIModel.GPT4o):F4}", new Color(0.5f, 1f, 0.5f));
                DrawStatRow("  Avg Cost/Item:", $"~${stats.GetAvgCostPerItem(ForgeAIModel.GPT4o):F6}", new Color(0.7f, 0.9f, 0.7f));
                EditorGUILayout.Space(5);
            }
            
            // o1
            long o1Total = stats.o1PromptTokens + stats.o1CompletionTokens;
            if (stats.o1Generations > 0 || o1Total > 0)
            {
                EditorGUILayout.LabelField("o1", EditorStyles.miniBoldLabel);
                DrawStatRow("  Generations:", stats.o1Generations.ToString("N0"));
                DrawStatRow("  Items:", stats.o1ItemsGenerated.ToString("N0"));
                DrawStatRow("  Tokens:", $"{o1Total:N0} ({stats.o1PromptTokens:N0} in + {stats.o1CompletionTokens:N0} out)");
                DrawStatRow("  Avg Tokens/Item:", $"{stats.GetAvgTokensPerItem(ForgeAIModel.O1):F0}");
                DrawStatRow("  Est. Cost:", $"~${stats.CalculateModelCost(ForgeAIModel.O1):F4}", new Color(0.5f, 1f, 0.5f));
                DrawStatRow("  Avg Cost/Item:", $"~${stats.GetAvgCostPerItem(ForgeAIModel.O1):F6}", new Color(0.7f, 0.9f, 0.7f));
                EditorGUILayout.Space(5);
            }
            
            if (stats.totalGenerations == 0)
            {
                EditorGUILayout.LabelField("No data yet - generate some items!", EditorStyles.centeredGreyMiniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCostSummary()
        {
            EditorGUILayout.LabelField("Cost Summary (Calculated from Tokens)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            DrawStatRow("Total Tokens:", $"{stats.GetTotalTokens():N0}", new Color(0.3f, 0.9f, 0.9f));
            DrawStatRow("  Input:", $"{stats.GetTotalPromptTokens():N0}");
            DrawStatRow("  Output:", $"{stats.GetTotalCompletionTokens():N0}");
            
            EditorGUILayout.Space(5);
            DrawSeparator();
            EditorGUILayout.Space(5);
            
            DrawStatRow("Est. Total Cost:", $"~${stats.GetTotalCost():F4}", new Color(0.3f, 0.9f, 0.3f));
            
            if (stats.totalItemsGenerated > 0)
            {
                DrawStatRow("Avg Tokens/Item:", $"{stats.GetOverallAvgTokensPerItem():F0}");
                DrawStatRow("Avg Cost/Item:", $"~${stats.GetOverallAvgCostPerItem():F6}");
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Costs are estimates based on current OpenAI pricing. Actual costs may vary.", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTimestamps()
        {
            EditorGUILayout.LabelField("History", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
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
            
            if (GUILayout.Button("Export to Console", GUILayout.Height(30)))
            {
                Debug.Log(stats.ToString());
                EditorUtility.DisplayDialog("Statistics", "Statistics exported to Console.", "OK");
            }
            
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Reset All", GUILayout.Height(30)))
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
        }
        
        private void DrawStatRow(string label, string value, Color? valueColor = null)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(label, GUILayout.Width(160));
            
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
