using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LevelDesigner.Editor.UIElements
{
    public class LevelDesignerWindow : EditorWindow
    {
        [MenuItem("Tools/Level Designer")]
        public static void Open()
        {
            current.Show();
        }

        static LevelDesignerWindow _current;
        public static LevelDesignerWindow current
        {
            get
            {
                if (!_current)
                {
                    var consoleWindowType = System.Type.GetType("UnityEditor.ConsoleWindow, UnityEditor.CoreModule");
                    _current = GetWindow<LevelDesignerWindow>(consoleWindowType);
                    _current.titleContent.text = "Level Designer";
                }

                return _current;
            }
        }

        SceneViewUI sceneViewUI
        {
            get => SceneViewUI.instance;
            set => SceneViewUI.instance = value;
        }

        string selectedCategory;

        void OnInspectorUpdate()
        {
            try
            {
                if (!_current)
                {
                    _current = this;
                    _current.RefreshLayout();
                }

                if (sceneViewUI is null)
                {
                    sceneViewUI = new SceneViewUI();
                    sceneViewUI.StartSceneViewGUI(null);
                    RefreshLayout();
                    RefreshBrushLayout();
                }
            }
            catch
            {
                OnDisable();
            }
        }

        void OnDisable()
        {
            try
            {
                if (sceneViewUI is not null)
                    sceneViewUI.StopSceneViewGUI();

                sceneViewUI = null;
            }
            catch
            {

            }
        }

        void SetSelectedCategory(string category)
        {
            selectedCategory = category;
            UpdatePrefabBrowserItemDisplay();
        }

        void RefreshLayout()
        {
            var root = Utils.LoadVisualTreeAsset(this);

            rootVisualElement.Clear();
            rootVisualElement.Add(root);
            root.StretchToParentSize();

            var prefabBrowser = rootVisualElement.Q("prefabs");

            prefabBrowser.RegisterCallback<DragUpdatedEvent>(e =>
            {
                var prefabs = DragAndDrop.objectReferences;
                if (prefabs.Length > 0)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            });

            prefabBrowser.RegisterCallback<DragPerformEvent>(e =>
            {
                foreach (var prefab in DragAndDrop.objectReferences)
                    Utils.prefabLibrary.Include(prefab);

                RefreshPrefabBrowser();
            });

            var categoriesArea = rootVisualElement.Q("categories-area");
            var categoryButton = new Button(() => SetSelectedCategory("")) { text = "all" };
            categoryButton.AddToClassList("category-button");
            categoriesArea.Add(categoryButton);

            foreach (var category in Utils.prefabLibrary.categories.OrderBy(x => x))
            {
                categoryButton = new Button(() => SetSelectedCategory(category)) { text = category };
                categoryButton.AddToClassList("category-button");
                categoriesArea.Add(categoryButton);
            }

            categoryButton = new Button(() => SetSelectedCategory("none")) { text = "none" };
            categoryButton.AddToClassList("category-button");
            categoriesArea.Add(categoryButton);

            RefreshPrefabBrowser();

            rootVisualElement.Q<Button>("refresh-button").RegisterCallback<ClickEvent>(e => RefreshPrefabBrowser());

            rootVisualElement.Q<Button>("test-button").RemoveFromHierarchy();

            prefabBrowser.RegisterCallback<KeyDownEvent>(e =>
            {

                if (e.keyCode == KeyCode.F5)
                {
                    RefreshPrefabBrowser();
                    return;
                }

                if (e.keyCode == KeyCode.Escape)
                {
                    sceneViewUI.activeTool = Tool.None;
                    Utils.CancelAsyncJob();
                    return;
                }

                if (e.keyCode == KeyCode.Delete)
                {
                    prefabBrowser.Query<PrefabBrowserItem>(className: "active").ForEach(x =>
                    {
                        Utils.prefabLibrary.Exclude(x.prefab);
                    });
                    RefreshPrefabBrowser();
                }

            });

            rootVisualElement.Q<TextField>("search-field").RegisterValueChangedCallback(e =>
            {
                UpdatePrefabBrowserItemDisplay();
            });
        }

        public void RefreshBrushLayout()
        {
            rootVisualElement.Q("BrushEditor").Clear();
            rootVisualElement.Q("BrushEditor").Add(new BrushUI(sceneViewUI.brush).ToEditorElement());
        }

        void RefreshPrefabBrowser()
        {
            var prefabBrowser = rootVisualElement.Q("prefabs");
            prefabBrowser.Clear();

            var prefabLibrary = new SerializedObject(Utils.prefabLibrary);
            var prefabs = prefabLibrary.FindProperty("prefabs");
            var prefabBrowserItems = new List<PrefabBrowserItem>();

            for (int i = 0; i < prefabs.arraySize; i++)
            {
                var prefabProperty = prefabs.GetArrayElementAtIndex(i);
                var prefabBrowserItem = new PrefabBrowserItem(prefabProperty)
                {
                    prefab = Utils.prefabLibrary.prefabs[i]
                };

                prefabBrowserItem.onClick += e =>
                {
                    if (e.shiftKey)
                    {
                        prefabBrowserItem.ToggleInClassList("active");
                    }
                    else
                    {
                        var active = prefabBrowserItem.ClassListContains("active");
                        DeactivateAllPrefabBrowserItems();
                        if (!active)
                            prefabBrowserItem.AddToClassList("active");
                    }

                    var objects = rootVisualElement
                        .Q("prefabs")
                        .Query<PrefabBrowserItem>()
                        .ToList()
                        .Where(x => x.ClassListContains("active"))
                        .Select(x => x.prefab.prefab)
                        .ToArray();

                    sceneViewUI.brush = Utils.prefabLibrary.GetBrush(objects.Select(x => x as GameObject).Where(x => x));

                    if (sceneViewUI.brush is null)
                    {
                        sceneViewUI.material = objects.FirstOrDefault(x => x is Material) as Material;

                        if (sceneViewUI.material is not null)
                        {
                            sceneViewUI.activeTool = Tool.Material;
                        }
                        else
                        {
                            sceneViewUI.activeTool = Tool.None;
                        }
                    }
                    else
                    {
                        sceneViewUI.activeTool = Tool.Brush;
                    }
                    RefreshBrushLayout();

                    var properties = rootVisualElement.Q("properties") as BindableElement;
                    var prefab = prefabBrowserItem.prefab.prefab;
                    var isGameObject = prefab is GameObject;
                    properties.Q<ObjectField>("prefab").value = prefab;
                    var item = (e.target as Image).parent.parent.parent as PrefabBrowserItem;
                    Utils.Bind(
                        properties,
                        item.prefabProperty,
                        ("categories", true),
                        ("tags", true),
                        ("keepApartFactor", isGameObject),
                        ("autoGround", isGameObject),
                        ("autoAlignToNormals", isGameObject),
                        ("offset", isGameObject),
                        ("scale", isGameObject)
                    );
                    properties.Q("categories").Q("unity-text-input").RegisterCallback<MouseDownEvent>(_e =>
                    {
                        if (_e.button != 1)
                            return;

                        _e.StopPropagation();
                        var menu = new GenericMenu();
                        var categories = prefabBrowserItem.prefab.categories;
                        foreach (var category in Utils.prefabLibrary.categories)
                        {
                            menu.AddItem(new GUIContent(category), categories.Contains(category), () =>
                            {
                                var categoryList = prefabBrowserItem.prefab.categoryList;

                                if (categoryList.Contains(category))
                                    categoryList.Remove(category);
                                else
                                    categoryList.Add(category);

                                prefabProperty.FindPropertyRelative("categories").stringValue = string.Join(" ", categoryList);
                                prefabProperty.serializedObject.ApplyModifiedProperties();
                            });
                        }
                        menu.ShowAsContext();
                    });
                };
                prefabBrowserItems.Add(prefabBrowserItem);
            }
            foreach (var prefabBrowserItem in prefabBrowserItems.OrderBy(x => x.prefab.name))
                prefabBrowser.Add(prefabBrowserItem);

            rootVisualElement.Q<ObjectField>("prefab").objectType = typeof(GameObject);
            DeactivateAllPrefabBrowserItems();
            UpdatePrefabBrowserItemDisplay();
        }



        public void DeactivateAllPrefabBrowserItems()
        {
            rootVisualElement.Q("prefabs").Query<PrefabBrowserItem>().ForEach(x =>
            {
                x.RemoveFromClassList("active");
            });
        }

        void UpdatePrefabBrowserItemDisplay()
        {
            var searchValues = rootVisualElement.Q<TextField>("search-field").value
                .Trim()
                .ToLower()
                .Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            rootVisualElement.Q("prefabs").Query<PrefabBrowserItem>().ForEach(x =>
            {
                var found = true;
                if (!string.IsNullOrEmpty(selectedCategory))
                {
                    if (selectedCategory == "none")
                    {
                        if (!string.IsNullOrEmpty(x.prefab.categories))
                            found = false;
                    }
                    else if (!x.prefab.categoryList.Contains(selectedCategory))
                    {
                        found = false;
                    }
                }

                if (found)
                {
                    var matchcode = x.prefab.matchcode.ToLower();

                    foreach (var searchValue in searchValues)
                    {
                        if (!matchcode.Contains(searchValue))
                        {
                            found = false;
                            break;
                        }
                    }
                }

                x.style.display = found ? DisplayStyle.Flex : DisplayStyle.None;
            });

            rootVisualElement.Query<Button>(className: "category-button").ForEach(x =>
            {
                var isEmpty = string.IsNullOrEmpty(selectedCategory);
                var active = false;

                if (isEmpty && x.text == "all")
                    active = true;
                else if (!isEmpty && x.text == selectedCategory)
                    active = true;

                if (active)
                    x.AddToClassList("active");
                else
                    x.RemoveFromClassList("active");
            });
        }
    }
}