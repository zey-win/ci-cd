using UnityEditor;
using UnityEngine;

namespace Editor
{
    // This is a tool to build boards in Unity Editor
    [CustomEditor(typeof(GameManager))]
    public class PlinkoGameEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10f);
            if (!GUILayout.Button("Generate board", GUILayout.Height(40f))) return;
            var gameManager = (GameManager)target;
            gameManager.BuildBoard();
        }
    }
}