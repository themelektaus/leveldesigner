using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelDesigner
{
    public class PrefabLibrary : ScriptableObject
    {
        [System.Serializable]
        public class Prefab
        {
            public Object prefab;
            public string friendlyName;
            public string categories;
            public string tags;
            public float keepApartFactor = 1;
            public bool autoGround = true;
            public bool autoAlignToNormals = true;
            public Vector3 offset = Vector3.zero;
            public float scale = 1;

            public string name
                => string.IsNullOrWhiteSpace(friendlyName) ? prefab.name : friendlyName.Trim();

            public string matchcode
                => Regex.Replace(friendlyName + " " + prefab.name + " " + categories + " " + tags, @"\s+", " ").Trim();

            public List<string> categoryList
            {
                get
                {
                    if (categories is null)
                        categories = "";

                    return categories.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
                }
            }
        }

        public List<Prefab> prefabs = new();

        [System.Serializable]
        public class AutoBrush
        {
            public GameObject[] key;
            public Brush brush;
        }
        public List<AutoBrush> autoBrushes = new();

#if UNITY_EDITOR
        readonly Dictionary<Object, Prefab> cache = new();

        public List<string> categories
        {
            get
            {
                var categories = new List<string>();
                foreach (var prefab in prefabs)
                    foreach (var category in prefab.categoryList)
                        if (!categories.Contains(category))
                            categories.Add(category);
                return categories;
            }
        }

        public void Include(Brush brush)
        {
            foreach (var prefab in brush.prefabs)
                Include(prefab);
        }

        public void Include(Object prefab)
        {
            if (prefabs.Any(x => x.prefab == prefab))
                return;

            var _prefab = new Prefab { prefab = prefab };
            if (prefab is Material)
                _prefab.categories = "material";
            prefabs.Add(_prefab);

            EditorUtility.SetDirty(this);

            Refresh();
        }

        public void Exclude(Prefab prefab)
        {
            prefabs.Remove(prefab);
            EditorUtility.SetDirty(this);
            Refresh();
        }

        public void Exclude(Object prefab)
        {
            if (!prefabs.Any(x => x.prefab == prefab))
                return;

            prefabs.RemoveAll(x => x.prefab == prefab);
            EditorUtility.SetDirty(this);
            Refresh();
        }

        public Prefab this[Transform transform] => this[transform.gameObject];

        public Prefab this[GameObject gameObject]
        {
            get
            {
                GameObject prefab;

                if (gameObject.scene.name is null)
                    prefab = gameObject;
                else if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                    prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                else
                    return null;

                if (!cache.ContainsKey(prefab))
                {
                    Refresh();
                    if (!cache.ContainsKey(prefab))
                        return null;
                }
                return cache[prefab];
            }
        }

        void Refresh()
        {
            var count = prefabs.Count;

            prefabs.RemoveAll(x => !x.prefab);

            if (prefabs.Count != count)
                EditorUtility.SetDirty(this);

            cache.Clear();
            foreach (var prefab in prefabs)
                cache[prefab.prefab] = prefab;
        }

        public Brush GetBrush(IEnumerable<GameObject> gameObjects)
        {
            var keyObjects = gameObjects.ToArray();

            RemoveUnusedAutoBrushes();

            if (keyObjects.Length == 0)
                return null;

            var count = autoBrushes.Count;

            AutoBrush autoBrush = null;
            foreach (var _autoBrush in autoBrushes)
            {
                if (_autoBrush.key.Length != keyObjects.Length)
                    continue;

                bool ok = true;
                foreach (var keyObject in keyObjects)
                {
                    if (!_autoBrush.key.Contains(keyObject))
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok)
                    autoBrush = _autoBrush;
            }

            if (autoBrush is null)
            {
                autoBrush = new()
                {
                    key = keyObjects,
                    brush = CreateInstance<Brush>()
                };

                autoBrush.brush.name = string.Join(", ", keyObjects.Select(x => x.name));
                autoBrush.brush.prefabs.AddRange(keyObjects);

                autoBrushes.Add(autoBrush);

                AssetDatabase.AddObjectToAsset(autoBrush.brush, this);
            }

            return autoBrush.brush;
        }

        public void RemoveUnusedAutoBrushes()
        {
            var unusedAutoBrushes = new List<AutoBrush>();

            foreach (var _autoBrush in autoBrushes)
                if (!_autoBrush.brush || _autoBrush.brush.HasDefaultValues())
                    unusedAutoBrushes.Add(_autoBrush);

            if (unusedAutoBrushes.Count == 0)
                return;

            foreach (var _autoBrush in unusedAutoBrushes)
            {
                if (_autoBrush.brush)
                    AssetDatabase.RemoveObjectFromAsset(_autoBrush.brush);

                autoBrushes.Remove(_autoBrush);
            }
        }
#endif
    }
}