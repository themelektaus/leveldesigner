using UnityEngine;

namespace LevelDesigner.Editor
{
    public struct TransformInfo
    {
        public Transform transform;
        public Vector3 originPosition;
        public PrefabLibrary.Prefab libraryEntry;
        public bool active;
    }
}