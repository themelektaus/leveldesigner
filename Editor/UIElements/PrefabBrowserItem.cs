using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace LevelDesigner.Editor.UIElements
{
    public class PrefabBrowserItem : VisualElement
    {
        PrefabLibrary.Prefab _prefab;

        public PrefabLibrary.Prefab prefab
        {
            get => _prefab;
            set
            {
                if (_prefab == value)
                    return;

                _prefab = value;
                var image = AssetPreview.GetAssetPreview(prefab.prefab);
                this.Q<Image>().image = image;
                UpdateLabel();
            }
        }

        public event EventCallback<MouseDownEvent> onClick;

        public readonly SerializedProperty prefabProperty;
        readonly Image previewImage;
        readonly TextField editNameField;
        readonly Label nameLabel;

        public PrefabBrowserItem(SerializedProperty prefabProperty)
        {
            this.prefabProperty = prefabProperty;

            Add(Utils.LoadVisualTreeAsset(this));

            previewImage = this.Q<Image>();
            editNameField = this.Q<TextField>();
            nameLabel = this.Q<Label>();

            previewImage.RegisterCallback<MouseDownEvent>(e => onClick?.Invoke(e));

            if (prefabProperty is not null)
            {
                editNameField.BindProperty(prefabProperty.FindPropertyRelative("friendlyName"));
                editNameField.RegisterValueChangedCallback(e => UpdateLabel());
            }

            nameLabel.RegisterCallback<MouseUpEvent>(e =>
            {
                nameLabel.style.display = DisplayStyle.None;
                editNameField.style.display = DisplayStyle.Flex;
                editNameField[0].Focus();
            });

            editNameField[0].RegisterCallback<FocusOutEvent>(e =>
            {
                nameLabel.style.display = DisplayStyle.Flex;
                editNameField.style.display = DisplayStyle.None;
            });
        }

        void UpdateLabel()
        {
            var labelText = prefab.name;

            if (labelText.Length > 12)
                labelText = labelText[..10] + "...";

            nameLabel.text = labelText;
        }
    }
}