using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelDesigner
{
    public static class Utils
    {
#if UNITY_EDITOR
        public static GameObject GetPrefab(GameObject gameObject)
        {
            if (gameObject.scene.name is null)
                return gameObject;

            if (PrefabUtility.IsAnyPrefabInstanceRoot(gameObject))
                return PrefabUtility.GetCorrespondingObjectFromSource(gameObject);

            return null;
        }
#endif
    }
}