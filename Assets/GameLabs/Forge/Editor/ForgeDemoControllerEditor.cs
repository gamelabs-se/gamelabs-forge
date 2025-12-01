#if UNITY_EDITOR
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
            
            // Open setup wizard button
            if (GUILayout.Button("Open Setup Wizard", GUILayout.Height(25)))
            {
                ForgeSetupWizard.Open();
            }
        }
    }
}
#endif
