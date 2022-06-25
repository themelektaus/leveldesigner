using UnityEditor;
using UnityEngine;

namespace LevelDesigner.Editor.UIElements
{
    [CustomEditor(typeof(Brush))]
    public class BrushEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open Designer Window", new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(0, 0, 15, 10),
                padding = new RectOffset(20, 20, 5, 5)
            }, GUILayout.ExpandWidth(false)))
            {
                LevelDesignerWindow.current.Show();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

        }
    }
}