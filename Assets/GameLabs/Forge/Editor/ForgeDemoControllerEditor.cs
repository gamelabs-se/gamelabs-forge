#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    [CustomEditor(typeof(ForgeDemoController))]
    public class ForgeDemoControllerEditor : UnityEditor.Editor
    {
        private bool showWeaponsGroup = true;
        private bool showConsumablesGroup = true;
        private bool showCollectiblesGroup = true;
        private bool showArmorGroup = true;
        private bool showSaveAsAssetsGroup = false;

        private ForgeDemoController _controller;

        private void OnEnable()
        {
            _controller = (ForgeDemoController)target;
            _controller.OnItemsGenerated += HandleItemsGenerated;
        }

        private void OnDisable()
        {
            if (_controller != null)
                _controller.OnItemsGenerated -= HandleItemsGenerated;
        }

        private void HandleItemsGenerated(object items, string customFolder)
        {
            if (items == null) return;

            string folder = string.IsNullOrEmpty(customFolder) ? null : customFolder;

            // Use reflection to call the generic CreateAssets method
            var itemsType = items.GetType();
            if (itemsType.IsGenericType && itemsType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = itemsType.GetGenericArguments()[0];
                var method = typeof(ForgeAssetExporter).GetMethod("CreateAssets");
                if (method != null)
                {
                    var genericMethod = method.MakeGenericMethod(elementType);
                    genericMethod.Invoke(null, new object[] { items, folder });
                }
            }
        }

        public override void OnInspectorGUI()
        {
            var controller = (ForgeDemoController)target;

            DrawHeader();

            EditorGUILayout.Space(10);

            // Draw default inspector for settings
            DrawDefaultInspector();

            EditorGUILayout.Space(15);

            DrawGenerationButtons(controller);
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);

            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("🔥 Forge Demo Controller", headerStyle);

            var subtitleStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.LabelField("Generate items using the buttons below", subtitleStyle);

            EditorGUILayout.Space(5);
        }

        private void DrawGenerationButtons(ForgeDemoController controller)
        {
            EditorGUILayout.LabelField("Generation Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Weapons
            showWeaponsGroup = EditorGUILayout.Foldout(showWeaponsGroup, "⚔️ Weapons", true);
            if (showWeaponsGroup)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Single", GUILayout.Height(25)))
                    controller.GenerateSingleWeapon();
                if (GUILayout.Button("Generate Batch", GUILayout.Height(25)))
                    controller.GenerateWeaponBatch();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Consumables
            showConsumablesGroup = EditorGUILayout.Foldout(showConsumablesGroup, "🧪 Consumables", true);
            if (showConsumablesGroup)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Single", GUILayout.Height(25)))
                    controller.GenerateSingleConsumable();
                if (GUILayout.Button("Generate Batch", GUILayout.Height(25)))
                    controller.GenerateConsumableBatch();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Collectibles
            showCollectiblesGroup = EditorGUILayout.Foldout(showCollectiblesGroup, "💎 Collectibles", true);
            if (showCollectiblesGroup)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Single", GUILayout.Height(25)))
                    controller.GenerateSingleCollectible();
                if (GUILayout.Button("Generate Batch", GUILayout.Height(25)))
                    controller.GenerateCollectibleBatch();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            // Armor
            showArmorGroup = EditorGUILayout.Foldout(showArmorGroup, "🛡️ Armor", true);
            if (showArmorGroup)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Generate Single", GUILayout.Height(25)))
                    controller.GenerateSingleArmor();
                if (GUILayout.Button("Generate Batch", GUILayout.Height(25)))
                    controller.GenerateArmorBatch();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Utility buttons
            EditorGUILayout.BeginHorizontal();

            GUI.color = new Color(1f, 0.8f, 0.8f);
            if (GUILayout.Button("Clear All Generated", GUILayout.Height(30)))
                controller.ClearAllGenerated();

            GUI.color = new Color(0.8f, 1f, 0.8f);
            if (GUILayout.Button("Add to Existing Context", GUILayout.Height(30)))
                controller.AddGeneratedToExisting();

            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Export buttons
            GUI.color = new Color(0.8f, 0.9f, 1f);
            if (GUILayout.Button("📁 Export All Items to JSON", GUILayout.Height(25)))
            {
                controller.ExportAllItems();
            }
            GUI.color = Color.white;

            EditorGUILayout.Space(5);

            // Save as Assets section
            showSaveAsAssetsGroup = EditorGUILayout.Foldout(showSaveAsAssetsGroup, "💾 Save as ScriptableObject Assets", true);
            if (showSaveAsAssetsGroup)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                string folder = string.IsNullOrEmpty(controller.customAssetFolder) ? null : controller.customAssetFolder;

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Weapons", GUILayout.Height(22)))
                {
                    if (controller.GeneratedWeapons.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedWeapons, folder);
                    else
                        ForgeLogger.Warn("No weapons to save as assets");
                }
                if (GUILayout.Button("Save Consumables", GUILayout.Height(22)))
                {
                    if (controller.GeneratedConsumables.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedConsumables, folder);
                    else
                        ForgeLogger.Warn("No consumables to save as assets");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Save Collectibles", GUILayout.Height(22)))
                {
                    if (controller.GeneratedCollectibles.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedCollectibles, folder);
                    else
                        ForgeLogger.Warn("No collectibles to save as assets");
                }
                if (GUILayout.Button("Save Armor", GUILayout.Height(22)))
                {
                    if (controller.GeneratedArmor.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedArmor, folder);
                    else
                        ForgeLogger.Warn("No armor to save as assets");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);

                GUI.color = new Color(0.9f, 1f, 0.9f);
                if (GUILayout.Button("💾 Save All Items as Assets", GUILayout.Height(25)))
                {
                    if (controller.GeneratedWeapons.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedWeapons, folder);
                    if (controller.GeneratedConsumables.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedConsumables, folder);
                    if (controller.GeneratedCollectibles.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedCollectibles, folder);
                    if (controller.GeneratedArmor.Count > 0)
                        ForgeAssetExporter.CreateAssets(controller.GeneratedArmor, folder);
                }
                GUI.color = Color.white;

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(5);

            // Bottom buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Setup Wizard", GUILayout.Height(25)))
            {
                ForgeSetupWizard.Open();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
