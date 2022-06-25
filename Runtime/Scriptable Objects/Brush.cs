using System.Collections.Generic;
using UnityEngine;

namespace LevelDesigner
{
    public class Brush : ScriptableObject
    {
        public List<GameObject> prefabs = new();
        public Vector2Int amount = Vector2Int.one;
        public Vector2 radius = Vector2.zero;
        public Vector3 minScale = Vector3.one;
        public Vector3 maxScale = Vector3.one;
        public Vector3 rotation = Vector3.zero;

        public bool HasDefaultValues()
        {
            if (amount != Vector2Int.one) return false;
            if (radius != Vector2.zero) return false;
            if (minScale != Vector3.one) return false;
            if (maxScale != Vector3.one) return false;
            if (rotation != Vector3.zero) return false;
            return true;
        }

        public override string ToString()
        {
            return name.Replace(" Brush", "");
        }

    }
}