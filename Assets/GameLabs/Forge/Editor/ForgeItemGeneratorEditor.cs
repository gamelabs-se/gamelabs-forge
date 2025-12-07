#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Reflection;

namespace GameLabs.Forge.Editor
{
    [CustomEditor(typeof(ForgeItemGenerator))]
    public class ForgeItemGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty settingsProperty;
        private bool showAdvancedSettings = false;
        
        private void OnEnable()
        {
            settingsProperty = serializedObject.FindProperty("settings");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            DrawHeader();
            
            EditorGUILayout.Space(10);
            
            DrawSettingsSection();
            
            EditorGUILayout.Space(15);
            
            DrawActionsSection();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            
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
            EditorGUILayout.LabelField("AI-Powered Dynamic Item Generation", subtitleStyle);
            
            EditorGUILayout.Space(5);
            DrawSeparator();
        }
        
        private void DrawSettingsSection()
        {
            EditorGUILayout.LabelField("Generator Settings", EditorStyles.boldLabel);
            
            if (settingsProperty != null)
            {
                EditorGUI.indentLevel++;
                
                // Game Context
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("gameName"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("gameDescription"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("targetAudience"));
                
                EditorGUILayout.Space(5);
                
                // Generation Settings
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("defaultBatchSize"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("maxBatchSize"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("temperature"));
                EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("model"));
                
                EditorGUILayout.Space(5);
                
                // Advanced
                showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
                if (showAdvancedSettings)
                {
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("additionalRules"));
                    
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Asset Paths", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("existingAssetsSearchPath"));
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("generatedAssetsBasePath"));
                    
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Existing Items Context", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("autoLoadExistingAssets"));
                    EditorGUILayout.PropertyField(settingsProperty.FindPropertyRelative("intent"));
                    
                    EditorGUILayout.Space(5);
                    var existingItems = settingsProperty.FindPropertyRelative("existingItemsJson");
                    EditorGUILayout.LabelField($"Existing Items in Context: {existingItems.arraySize}");
                    
                    if (existingItems.arraySize > 0)
                    {
                        if (GUILayout.Button("Clear Existing Items Context"))
                        {
                            existingItems.ClearArray();
                        }
                    }
                }
                
                EditorGUI.indentLevel--;
            }
        }
        
        private void DrawActionsSection()
        {
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open Setup Wizard", GUILayout.Height(30)))
            {
                ForgeSetupWizard.Open();
            }
            
            if (GUILayout.Button("Reload Config", GUILayout.Height(30)))
            {
                ForgeConfig.ClearCache();
                var generator = (ForgeItemGenerator)target;
                var settings = ForgeConfig.GetGeneratorSettings();
                generator.UpdateSettings(settings);
                EditorUtility.SetDirty(target);
                ForgeLogger.Log("Configuration reloaded from file.");
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Status info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            var apiKey = ForgeConfig.GetOpenAIKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                EditorGUILayout.HelpBox(
                    "⚠️ No API key configured.\nPlease run the Setup Wizard to configure your OpenAI API key.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.LabelField("✓ API Key configured");
                EditorGUILayout.LabelField($"Model: {ForgeConfig.GetModel()}");
            }
            
            EditorGUILayout.EndVertical();
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
