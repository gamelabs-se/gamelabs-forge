#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GameLabs.Forge.Editor
{
    [CustomEditor(typeof(ForgeController))]
    public class ForgeControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(8);
            if (GUILayout.Button("Call Forge Now"))
                ((ForgeController)target).CallForge();
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
