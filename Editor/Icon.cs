using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LevelDesigner.Editor
{
    public static class Icon
    {
        static readonly Dictionary<string, Texture2D> _items = new();

        public static Texture2D Get(string iconName)
        {
            if (!_items.ContainsKey(iconName))
                _items[iconName] = AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/LevelDesigner/Icons/{iconName} Icon.png"
                );

            return _items[iconName];
        }
    }
}