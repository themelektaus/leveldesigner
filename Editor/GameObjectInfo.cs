using UnityEditor;
using UnityEngine;

namespace LevelDesigner.Editor
{
    public class GameObjectInfo
    {
        public GameObject gameObject;
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;
        public bool forceScale;

        Vector3? _size;
        public Vector3 size
        {
            get
            {
                if (!_size.HasValue)
                    _size = Utils.GetSize(gameObject, forceScale ? scale : Vector3.one);

                return _size.Value;
            }
        }

        public void ApplyPosition()
        {
            ApplyPosition(gameObject.transform);
        }

        public void ApplyRotation()
        {
            ApplyRotation(gameObject.transform);
        }

        public void ApplyScale()
        {
            ApplyScale(gameObject.transform);
        }

        void ApplyPosition(Transform transform)
        {
            transform.position = position;
        }

        void ApplyRotation(Transform transform)
        {
            transform.localRotation = rotation;
        }

        void ApplyScale(Transform transform)
        {
            transform.localScale = Vector3.Scale(transform.localScale, scale);
        }

        public GameObject Instantiate(Transform parent, bool useParentLayer)
        {
            var gameObject = PrefabUtility.InstantiatePrefab(this.gameObject) as GameObject;
            var transform = gameObject.transform;

            ApplyPosition(transform);
            ApplyRotation(transform);
            ApplyScale(transform);

            if (parent)
            {
                if (useParentLayer)
                    Utils.SetLayer(gameObject, parent.gameObject.layer);

                transform.SetParent(parent, true);
            }

            return gameObject;
        }
    }
}