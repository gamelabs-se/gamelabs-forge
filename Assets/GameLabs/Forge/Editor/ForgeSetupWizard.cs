#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace GameLabs.Forge.Editor
{
    public class ForgeSetupWizard : EditorWindow
    {
        const string ConfigPath = "Assets/GameLabs/Forge/Settings/forge.config.json";
        string apiKey = "";

        [MenuItem("GameLabs/Forge/Setup Wizard")]
        public static void Open() => GetWindow<ForgeSetupWizard>("Forge Setup");

        void OnGUI()
        {
            GUILayout.Label("FORGE – First-run Setup", EditorStyles.boldLabel);
            GUILayout.Space(4);

            GUILayout.Label("OpenAI API Key:");
            apiKey = EditorGUILayout.PasswordField(apiKey);

            if (GUILayout.Button("Create/Update Config"))
            {
                Directory.CreateDirectory("Assets/GameLabs/Forge/Settings");
                File.WriteAllText(ConfigPath, $"{{ \"openaiApiKey\": \"{apiKey}\" }}");
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Forge", "Config saved.", "OK");
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Add ForgeController to Scene"))
            {
                var go = new GameObject("ForgeController");
                go.AddComponent<GameLabs.Forge.ForgeController>();
                Selection.activeObject = go;
            }
        }
    }
}
#endif
