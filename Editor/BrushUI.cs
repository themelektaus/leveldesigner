using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LevelDesigner.Editor.UIElements
{
    public class BrushUI
    {
        VisualElement prefabsElement;

        readonly Brush brush;

        public BrushUI(Brush brush)
        {
            this.brush = brush;
        }

        public VisualElement ToEditorElement()
        {
            if (!brush)
            {
                var label = new Label("(Select at least one prefab)");
                label.style.paddingLeft = 10;
                label.style.paddingTop = 10;
                label.style.fontSize = 14;
                return label;
            }

            var root = Utils.LoadVisualTreeAsset<BrushEditor>();
            root.Bind(new SerializedObject(brush));

            brush.prefabs.RemoveAll(x => !x);
            prefabsElement = root.Q("prefabs-list");
            prefabsElement.Clear();

            int index = 0;
            foreach (var prefab in brush.prefabs)
                AddPrefabField(prefab, index++);

            AddPrefabField();

            return root;
        }

        void AddPrefabField(GameObject prefab = null, int index = -1)
        {
            var prefabField = new ObjectField
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
                value = prefab,
                userData = new PrefabFieldUserData { index = index }
            };
            prefabField.AddToClassList("prefab");
            prefabField.RegisterValueChangedCallback(e =>
            {
                var @this = e.target as ObjectField;
                var userData = @this.userData as PrefabFieldUserData;
                var value = e.newValue as GameObject;
                if (userData.index == -1)
                {
                    userData.index = brush.prefabs.Count;
                    brush.prefabs.Add(value);
                    AddPrefabField();
                }
                else if (value)
                {
                    brush.prefabs[userData.index] = value;
                }
                else
                {
                    brush.prefabs.RemoveAt(userData.index);
                    prefabsElement.RemoveAt(userData.index);
                    foreach (ObjectField _prefabField in prefabsElement.Children())
                    {
                        var _userData = _prefabField.userData as PrefabFieldUserData;
                        if (_userData.index > userData.index)
                            _userData.index--;
                    }
                }
            });
            prefabsElement.Add(prefabField);
        }

        class PrefabFieldUserData
        {
            public int index;
        }
    }
}