#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    [CustomEditor(typeof(ForgeController))]
    public class ForgeControllerEditor : UnityEditor.Editor
    {
        private ForgeController _controller;
        private bool showSaveAsAssetsGroup = false;
        
        private void OnEnable()
        {
            _controller = (ForgeController)target;
            _controller.OnItemsGenerated += HandleItemsGenerated;
        }
        
        private void OnDisable()
        {
            if (_controller != null)
                _controller.OnItemsGenerated -= HandleItemsGenerated;
        }
        
        private void HandleItemsGenerated(List<ForgeController.GeneratedItem> items, string customFolder)
        {
            if (items == null || items.Count == 0) return;
            
            string folder = string.IsNullOrEmpty(customFolder) ? "GeneratedItem" : customFolder;
            
            // Save items as ScriptableObject assets
            ForgeAssetExporter.CreateAssets(items, folder);
            ForgeLogger.Log($"Auto-saved {items.Count} items as ScriptableObject assets to Generated/{folder}/");
        }
        
        public override void OnInspectorGUI()
        {
            DrawHeader();
            
            EditorGUILayout.Space(5);
            
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);
            
            DrawGenerateButton();
            
            EditorGUILayout.Space(5);
            
            DrawSaveSection();
        }
        
        private new void DrawHeader()
        {
            EditorGUILayout.Space(5);
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("🔥 Forge Controller", headerStyle);
            
            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Generate items with AI", subtitleStyle);
            
            EditorGUILayout.Space(5);
        }
        
        private void DrawGenerateButton()
        {
            GUI.color = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("🔥 Call Forge Now", GUILayout.Height(35)))
            {
                _controller.CallForge();
            }
            GUI.color = Color.white;
            
            // Show status
            if (_controller.LastGeneratedItems != null && _controller.LastGeneratedItems.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"Last generation: {_controller.LastGeneratedItems.Count} items\n" +
                    (_controller.autoSaveAsAssets ? "✓ Auto-saving enabled" : "○ Auto-saving disabled"),
                    MessageType.Info);
            }
        }
        
        private void DrawSaveSection()
        {
            showSaveAsAssetsGroup = EditorGUILayout.Foldout(showSaveAsAssetsGroup, "💾 Manual Save Options", true);
            if (showSaveAsAssetsGroup)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                string folder = string.IsNullOrEmpty(_controller.customAssetFolder) ? "GeneratedItem" : _controller.customAssetFolder;
                
                EditorGUILayout.BeginHorizontal();
                
                EditorGUI.BeginDisabledGroup(_controller.LastGeneratedItems == null || _controller.LastGeneratedItems.Count == 0);
                
                if (GUILayout.Button("Save Items as Assets", GUILayout.Height(25)))
                {
                    ForgeAssetExporter.CreateAssets(_controller.LastGeneratedItems as List<ForgeController.GeneratedItem>, folder);
                }
                
                if (GUILayout.Button("Clear Items", GUILayout.Height(25)))
                {
                    _controller.ClearGeneratedItems();
                }
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.LabelField($"Save path: Generated/{folder}/", EditorStyles.miniLabel);
                
                EditorGUILayout.EndVertical();
            }
        }

        [MenuItem("GameLabs/Forge/Call Selected Controller")]
        public static void CallSelected()
        {
            foreach (var go in Selection.gameObjects)
                go.GetComponent<ForgeController>()?.CallForge();
        }
    }
}
#endif
