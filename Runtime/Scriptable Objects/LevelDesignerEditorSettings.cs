using UnityEngine;

namespace LevelDesigner
{
    public class LevelDesignerEditorSettings : ScriptableObject
    {
        public int keepApartIterations = 20;
        [Range(1, 200)] public float randomGrid = 50;
    }
}