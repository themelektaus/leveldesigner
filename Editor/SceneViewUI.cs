using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LevelDesigner.Editor
{
    public class SceneViewUI
    {
        public static SceneViewUI instance;

        Brush _brush;
        public Brush brush
        {
            get => _brush;
            set
            {
                material = null;

                if (_brush == value)
                    return;

                _brush = value;

                var window = UIElements.LevelDesignerWindow.current;
                if (!window)
                    return;

                window.RefreshBrushLayout();
            }
        }

        public class PaintSettings
        {
            public Vector3 rotationOffset;
            public float scale;
            public float radiusMultiplicator;

            public PaintSettings() => Reset();

            public void Reset()
            {
                rotationOffset = Vector3.zero;
                scale = 1;
                radiusMultiplicator = 1;
            }

            public bool Update(Event e)
            {
                if (e.type == EventType.MouseDown && e.button == 2 && (e.shift || e.control))
                {
                    Reset();
                    return true;
                }

                if (e.type == EventType.ScrollWheel)
                {
                    if (e.shift && e.control)
                    {
                        radiusMultiplicator = Mathf.Clamp(radiusMultiplicator - e.delta.y / 100, .05f, 100);
                        return true;
                    }

                    if (e.shift)
                    {
                        rotationOffset.y += e.delta.y;
                        return true;
                    }

                    if (e.control)
                    {
                        scale -= e.delta.y / 100;
                        scale = Mathf.Clamp(scale, .05f, 100);
                        return true;
                    }
                }

                return false;
            }
        }

        readonly PaintSettings tempPaintSettings = new();

        GameObject _levelDesignerCursor;
        public GameObject levelDesignerCursor
        {
            get
            {
                if (!_levelDesignerCursor)
                {
                    _levelDesignerCursor = Resources.FindObjectsOfTypeAll<GameObject>()
                        .Where(x => x.hideFlags == HideFlags.None)
                        .Where(x => x.transform.root)
                        .Where(x => x.name == "<Level Designer Cursor>")
                        .FirstOrDefault();

                    if (!_levelDesignerCursor)
                        _levelDesignerCursor = new GameObject("<Level Designer Cursor>");

                    _levelDesignerCursor.layer = 2;
                }

                return _levelDesignerCursor;
            }
        }

        public Material material;

        Vector2 lastMousePosition;

        public readonly Selection selection = new();

        Tool _activeTool = Tool.None;

        public Tool activeTool
        {
            get => _activeTool;
            set
            {
                if (_activeTool == value)
                    return;

                _activeTool = value;
                tempPaintSettings.Reset();
                switch (_activeTool)
                {
                    case Tool.None:
                        Tools.current = UnityEditor.Tool.Move;
                        break;
                    case Tool.Brush:
                    case Tool.Expand:
                    case Tool.Material:
                        Tools.current = UnityEditor.Tool.None;
                        break;
                }
            }
        }

        public GUIStyle GetToolButtonStyle(bool enabled, bool active)
        {
            if (active)
                return Style.toolboxWindowButtonActive;

            if (enabled)
                return Style.toolboxWindowButton;

            return Style.toolboxWindowButtonDisabled;
        }

        public GUIStyle GetToolButtonStyle(Tool tool)
        {
            if (activeTool == tool)
                return Style.toolboxWindowButtonActive;

            return Style.toolboxWindowButton;
        }

        void ClearLevelDesignerCursor(bool destroy)
        {
            if (!_levelDesignerCursor)
                return;

            if (destroy)
            {
                Object.DestroyImmediate(_levelDesignerCursor);
                return;
            }
            List<Transform> transforms = new();

            foreach (Transform transform in _levelDesignerCursor.transform)
                transforms.Add(transform);

            foreach (Transform transform in transforms)
                Object.DestroyImmediate(transform.gameObject);
        }

        public void StartSceneViewGUI(Brush brush)
        {
            this.brush = brush;
            SceneView.duringSceneGui -= OnSceneViewGUI;
            SceneView.duringSceneGui += OnSceneViewGUI;
            EditorApplication.playModeStateChanged -= ModeChanged;
            EditorApplication.playModeStateChanged += ModeChanged;
        }

        public void StopSceneViewGUI()
        {
            if (_levelDesignerCursor)
                Object.DestroyImmediate(_levelDesignerCursor);

            SceneView.duringSceneGui -= OnSceneViewGUI;
            EditorApplication.playModeStateChanged -= ModeChanged;
            brush = null;
            instance = null;
            SceneViewOverlay.instance?.Update();
        }

        void OnSceneViewGUI(SceneView sceneView)
        {
            try
            {
                var e = Event.current;
                if (tempPaintSettings.Update(e))
                {
                    Repaint(e);
                    JustUse(e);
                    return;
                }
                switch (e.type)
                {
                    case EventType.Layout:
                        selection.Update();
                        SceneViewOverlay.instance?.Update();
                        break;

                    case EventType.Repaint:
                        if (lastMousePosition == e.mousePosition)
                        {
                            break;
                        }
                        Repaint(e);
                        break;

                    case EventType.KeyDown:
                        switch (e.keyCode)
                        {
                            case KeyCode.PageUp:
                                selection.SelectParent(true);
                                JustUse(e);
                                break;

                            case KeyCode.PageDown:
                                selection.SelectChildren();
                                JustUse(e);
                                break;

                            case KeyCode.A:
                                if (activeTool == Tool.Brush)
                                {
                                    // MyTODO: Previous Brush/Prefab
                                    JustUse(e);
                                }
                                break;

                            case KeyCode.D:
                                if (activeTool == Tool.Brush)
                                {
                                    // MyTODO: Next Brush/Prefab
                                    JustUse(e);
                                }
                                break;
                        }
                        break;

                    case EventType.KeyUp:
                        switch (e.keyCode)
                        {
                            case KeyCode.Escape:
                                activeTool = Tool.None;
                                Utils.CancelAsyncJob();
                                break;
                        }
                        break;

                    case EventType.MouseDown:
                        if (e.button == 0 && e.modifiers == EventModifiers.None)
                        {
                            switch (activeTool)
                            {
                                case Tool.Brush:
                                    Paint(e);
                                    break;

                                case Tool.Expand:
                                    JustUse(e);
                                    break;

                                case Tool.Material:
                                    Material(e);
                                    break;
                            }
                        }
                        break;

                    case EventType.ScrollWheel:
                        switch (activeTool)
                        {
                            case Tool.Expand:
                                selection.UpdateOriginPositions();
                                Undo.RecordObjects(selection.transforms, "Expose");
                                Utils.Expose(selection.infos, e.delta.y / 10);
                                JustUse(e);
                                break;
                        }
                        break;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                SceneView.duringSceneGui -= OnSceneViewGUI;
                EditorApplication.playModeStateChanged -= ModeChanged;
                UIElements.LevelDesignerWindow.current?.Close();
            }
        }

        void Repaint(Event e)
        {
            lastMousePosition = e.mousePosition;
            ClearLevelDesignerCursor(false);
            if (activeTool == Tool.Brush)
            {
                if (_levelDesignerCursor)
                    _levelDesignerCursor.SetActive(true);

                var gameObjects = new List<GameObject>();
                foreach (var gameObjectInfo in Paint(e.mousePosition))
                    gameObjects.Add(gameObjectInfo.Instantiate(levelDesignerCursor.transform, true));

                Utils.KeepApart(gameObjects, true, 1);
                Utils.AutoGround(gameObjects);
                return;
            }
            if (_levelDesignerCursor)
                _levelDesignerCursor.SetActive(false);
        }

        void Paint(Event e)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Paint");
            int undoGroup = Undo.GetCurrentGroup();
            var parent = selection.transforms.Length >= 2 ? selection.transforms[0].parent : selection.activeTransform;

            while (parent && (!Utils.HasDefaultValues(parent) || parent.GetComponents<Component>().Any(x => !(x is Transform))))
                parent = parent.parent;

            if (!parent)
            {
                var newGameObject = new GameObject("New Paint Container");
                selection.SetActive(newGameObject);
                parent = newGameObject.transform;
                Undo.RegisterCreatedObjectUndo(newGameObject, "");
            }
            var gameObjects = new List<GameObject>();
            foreach (var gameObjectInfo in Paint(e.mousePosition))
                gameObjects.Add(gameObjectInfo.Instantiate(parent, false));

            Utils.KeepApart(gameObjects, true, 10);
            Utils.AutoGround(gameObjects);
            foreach (var gameObject in gameObjects)
                Undo.RegisterCreatedObjectUndo(gameObject, "");
            Undo.CollapseUndoOperations(undoGroup);
            JustUse(e);
        }

        void Material(Event e)
        {
            if (material)
            {
                var gameObject = HandleUtility.PickGameObject(e.mousePosition, false);
                if (gameObject && gameObject.TryGetComponent(out Renderer renderer))
                {
                    Undo.RecordObject(renderer, "Material");

                    // MyTODO: Support for Material Property Override
                    //if (gameObject.TryGetComponent(out MaterialPropertyOverride.MaterialPropertyOverride @override))
                    //{
                    //    Undo.RecordObject(@override, "Material Override");
                    //    @override.enabled = false;
                    //}

                    renderer.material = material;
                }
            }

            JustUse(e);
        }

        void JustUse(Event e)
        {
            GUIUtility.hotControl = 0;
            e.Use();
        }

        void ModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ClearLevelDesignerCursor(true);
        }

        IEnumerable<GameObjectInfo> Paint(Vector2 position)
        {
            if (!brush)
                yield break;

            foreach (var info in Utils.Paint(brush, position, Utils.editorSettings.randomGrid, tempPaintSettings))
                yield return info;
        }

        public void RoundTransformValues(Transform[] transforms)
        {
            Undo.RecordObjects(transforms, "Round Transform Values");
            foreach (var t in transforms)
            {
                Vector3 p = t.localPosition;
                p.x = Mathf.Round(p.x * 2) / 2;
                p.y = Mathf.Round(p.y * 2) / 2;
                p.z = Mathf.Round(p.z * 2) / 2;
                t.localPosition = p;

                Vector3 a = t.localEulerAngles;
                a.x = Mathf.Round(a.x);
                a.y = Mathf.Round(a.y);
                a.z = Mathf.Round(a.z);
                t.localEulerAngles = a;

                Vector3 s = t.localScale;
                s.x = Mathf.Round(s.x * 20) / 20;
                s.y = Mathf.Round(s.y * 20) / 20;
                s.z = Mathf.Round(s.z * 20) / 20;
                t.localScale = s;
            }
        }
    }
}