using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LevelDesigner.Editor
{
    public class Selection
    {
        Object[] _objects = new Object[0];
        readonly List<TransformInfo> _infos = new List<TransformInfo>();

        public TransformInfo[] infos => _infos.ToArray();

        public Object activeObject { get; private set; }

        public Transform activeTransform { get; private set; }

        public Transform[] transforms { get; private set; } = new Transform[0];

        public GameObject[] gameObjects { get; private set; } = new GameObject[0];

        public void Clear()
        {
            _objects = new Object[0];
            _infos.Clear();
            activeObject = null;
            activeTransform = null;
            transforms = new Transform[0];
            gameObjects = new GameObject[0];
            Undo.undoRedoPerformed -= Clear;
        }

        public void SetActive(Object @object)
        {
            UnityEditor.Selection.activeObject = @object;
            Update();
        }

        public void Select(params Object[] objects)
        {
            UnityEditor.Selection.objects = objects;
            Update();
        }

        public void SelectParent(bool collapse)
        {
            if (!activeTransform || !activeTransform.parent)
                return;

            Select(activeTransform.parent.gameObject);
            if (collapse)
                Utils.Collapse(activeObject);

            Utils.FocusSceneView();
        }

        public void SelectChildren()
        {
            if (!activeTransform)
                return;

            var newSelection = new List<GameObject>();
            foreach (Transform transform in activeTransform)
                newSelection.Add(transform.gameObject);

            if (newSelection.Count > 0)
                Select(newSelection.ToArray());

            Utils.FocusSceneView();
        }

        public void Update()
        {
            var objects = UnityEditor.Selection.objects;
            if (Utils.Equals(_objects, objects))
                return;

            _objects = objects;
            _infos.Clear();
            activeObject = UnityEditor.Selection.activeObject;
            activeTransform = UnityEditor.Selection.activeTransform;
            transforms = UnityEditor.Selection.transforms;
            gameObjects = transforms.Select(x => x.gameObject).ToArray();

            foreach (var transform in transforms)
            {
                _infos.Add(new TransformInfo
                {
                    transform = transform,
                    libraryEntry = Utils.prefabLibrary[transform],
                    active = transform == activeTransform
                });
            }

            UpdateOriginPositions();

            Undo.undoRedoPerformed += Clear;
        }

        public void UpdateOriginPositions()
        {
            for (int i = 0; i < _infos.Count; i++)
            {
                var info = _infos[i];
                info.originPosition = info.transform.position;
                _infos[i] = info;
            }
        }
    }
}