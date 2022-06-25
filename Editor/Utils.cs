using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Flags = System.Reflection.BindingFlags;

namespace LevelDesigner.Editor
{
    public static class Utils
    {
        static readonly List<Color> guiColors = new();
        static readonly List<float> labelWidths = new();

        static LevelDesignerEditorSettings _editorSettings = null;
        static PrefabLibrary _prefabLibrary = null;

        public static LevelDesignerEditorSettings editorSettings => Get(ref _editorSettings, "Level Designer Editor Settings");
        public static PrefabLibrary prefabLibrary => Get(ref _prefabLibrary, "Level Designer Prefab Library");



        static readonly Dictionary<string, VisualTreeAsset> visualTreeAssets = new();

        public static TemplateContainer LoadVisualTreeAsset(object obj)
        {
            return LoadVisualTreeAsset(obj.GetType().Name);
        }

        public static TemplateContainer LoadVisualTreeAsset<T>()
        {
            return LoadVisualTreeAsset(typeof(T).Name);
        }

        public static TemplateContainer LoadVisualTreeAsset(string name)
        {
            if (!visualTreeAssets.ContainsKey(name))
            {
                var assetGUID = AssetDatabase.FindAssets("t:visualtreeasset " + name)[0];
                var assetPath = AssetDatabase.GUIDToAssetPath(assetGUID);
                var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
                visualTreeAssets[name] = visualTreeAsset;
            }
            return visualTreeAssets[name].CloneTree();
        }



        public static void Bind(VisualElement owner, SerializedProperty property, params (string name, bool visible)[] properties)
        {
            foreach (var (name, visible) in properties)
            {
                var element = owner.Q<BindableElement>(name);
                element.BindProperty(property.FindPropertyRelative(name));
                element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }



        public static Vector3 GetSize(GameObject gameObject, Vector3 forcedLocalScale)
        {
            Vector3? result = null;

            var collider = gameObject.GetComponentInChildren<Collider>();
            if (collider)
            {
                switch (collider)
                {
                    case BoxCollider c:
                        result = c.size;
                        break;

                    case SphereCollider c:
                        result = Vector3.one * c.radius * 2;
                        break;

                    case CapsuleCollider c:
                        var diameter = c.radius * 2;
                        result = new Vector3(diameter, c.height, diameter);
                        break;

                }

                if (result.HasValue)
                    result = Vector3.Scale(result.Value, forcedLocalScale);
            }

            if (!result.HasValue)
            {
                var renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer)
                    result = renderer.bounds.size;
            }

            return result ?? Vector3.one;
        }

        public static void AutoGround(IEnumerable<GameObject> gameObjects)
        {
            AutoGround(gameObjects.Select(x => x.transform));
        }

        public static void AutoGround(IEnumerable<Transform> transforms)
        {
            var result = new List<Transform>();
            foreach (var transform in transforms)
            {
                var entry = prefabLibrary[transform];
                if (entry != null && entry.autoGround)
                {
                    result.Add(transform);
                }
            }
            SetToGround(result.ToArray());
        }

        public static void SetToGround(params Transform[] transforms)
        {
            foreach (var transform in transforms)
            {
                var entry = prefabLibrary[transform];
                var offset = entry == null ? Vector3.zero : transform.rotation * entry.offset;
                float up = GetSize(transform.gameObject, Vector3.one).y * 5;
                var possibleHits = new List<RaycastHit>();
                foreach (var hitInfo in Physics.RaycastAll(transform.position - offset + Vector3.up * up, Vector3.down, up * 4, 1))
                {
                    if (!transforms.Contains(hitInfo.transform) && !transforms.Any(t => hitInfo.transform.IsChildOf(t)))
                    {
                        possibleHits.Add(hitInfo);
                    }
                }
                if (possibleHits.Count > 0)
                {
                    var hitInfo = possibleHits.OrderByDescending(x => x.point.y).FirstOrDefault();
                    transform.position = hitInfo.point;
                    if (entry != null)
                    {
                        var t = (entry.prefab as GameObject).transform;
                        transform.position += offset * transform.localScale.y;
                        transform.position += transform.rotation * (t.localRotation * t.localPosition);
                    }
                }
            }
        }

        public static GameObjectInfo[] Paint(Brush brush, Vector2 position, float randomGrid, SceneViewUI.PaintSettings paintSettings)
        {
            var ps = paintSettings;
            var prefabs = brush.prefabs.Where(x => x).ToArray();

            if (prefabs.Length == 0)
                return new GameObjectInfo[0];

            Ray ray = HandleUtility.GUIPointToWorldRay(position);
            if (!Physics.Raycast(ray, out var hitInfo, Mathf.Infinity, 1))
                return new GameObjectInfo[0];

            var origin = hitInfo.point;
            var random = new RandomByPosition(position, randomGrid, brush.amount.y > 1 ? (int) (ps.rotationOffset.y / 45) : 0);
            var amount = random.Next(brush.amount);
            var result = new List<GameObjectInfo>();

            for (int i = 0; i < amount; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    var info = new GameObjectInfo
                    {
                        gameObject = prefabs[random.Next(0, prefabs.Length - 1)]
                    };

                    float up = GetSize(info.gameObject, Vector3.one).y * 5;
                    ray = new Ray(origin + random.NextCircle(brush.radius * ps.radiusMultiplicator * ps.scale, up), Vector3.down);

                    if (Physics.Raycast(ray, out hitInfo, Mathf.Infinity, 1))
                    {
                        info.position = hitInfo.point;

                        info.rotation = Quaternion.Euler(info.gameObject.transform.localEulerAngles);
                        info.rotation *= Quaternion.Euler(
                            Mathf.LerpAngle(0, random.Next(0f, 360f), brush.rotation.x) + ps.rotationOffset.x,
                            Mathf.LerpAngle(0, random.Next(0f, 360f), brush.rotation.y) + ps.rotationOffset.y,
                            Mathf.LerpAngle(0, random.Next(0f, 360f), brush.rotation.z) + ps.rotationOffset.z
                        );

                        info.scale = random.Next(brush.minScale, brush.maxScale) * ps.scale;

                        var entry = prefabLibrary[info.gameObject];
                        if (entry != null)
                        {
                            if (entry.autoAlignToNormals)
                                info.rotation = AlignNormals(hitInfo) * info.rotation;

                            info.position += info.rotation * entry.offset;
                            info.scale *= entry.scale;
                        }

                        result.Add(info);
                        break;
                    }
                }
            }

            return result.ToArray();
        }

        static Quaternion AlignNormals(RaycastHit hitInfo)
        {
            return Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
        }

        public static void Expose(TransformInfo[] infos, float factor)
        {
            var center = Vector3.zero;
            foreach (var info in infos)
                center += info.transform.position;

            center /= infos.Length;
            for (int i = 0; i < infos.Length; i++)
                infos[i].transform.position = infos[i].originPosition - (center - infos[i].originPosition).normalized * factor;
        }



        static EditorJob _asyncJob;
        public static bool asyncJobIsRunning => _asyncJob != null && _asyncJob.IsRunning;
        public static string asyncJobName => _asyncJob?.name;
        public static string asyncJobNameRunning => asyncJobIsRunning ? _asyncJob.name : null;

        public static void CancelAsyncJob()
        {
            if (_asyncJob is not null)
                _asyncJob.CancelImmediate();
        }



        public static void KeepApart(IEnumerable<GameObject> gameObjects, bool forceScale, int oversampling)
        {
            CancelAsyncJob();
            EditorJob.RunSync(_KeepApartProcess(gameObjects, forceScale, oversampling, null));
        }

        public static void KeepApartAsync(IEnumerable<GameObject> gameObjects, bool forceScale, System.Action<IEnumerable<GameObject>> postProcessing)
        {
            if (asyncJobIsRunning)
            {
                return;
            }
            _asyncJob = new EditorJob
            {
                name = "KeepApart",
                process = _KeepApartProcess(gameObjects, forceScale, 20, postProcessing)
            };
            _asyncJob.Run();
        }

        static IEnumerator _KeepApartProcess(IEnumerable<GameObject> gameObjects, bool forceScale, int oversampling, System.Action<IEnumerable<GameObject>> postProcessing)
        {
            var infos = gameObjects.Select(x => new GameObjectInfo
            {
                gameObject = x,
                position = x.transform.position,
                scale = x.transform.localScale,
                forceScale = forceScale
            }).ToArray();

            var iterations = editorSettings.keepApartIterations * oversampling;
            for (int i = 0; i < iterations; i++)
            {
                _KeepApart(infos);
                foreach (var info in infos)
                    info.ApplyPosition();

                postProcessing?.Invoke(infos.Select(x => x.gameObject));
                foreach (var info in infos)
                    info.position = info.gameObject.transform.position;

                if (asyncJobIsRunning)
                    Progress.Report(_asyncJob.progressID, i / (float) iterations);

                yield return null;
            }
        }

        static void _KeepApart(GameObjectInfo[] gameObjectInfos)
        {
            var infoGroups = new List<GameObjectInfo[]>();
            foreach (var info in gameObjectInfos)
            {
                var test = _FindTransformNear(gameObjectInfos, info);
                if (test.Length > 0)
                    infoGroups.Add(test.Append(info).ToArray());
            }

            if (infoGroups.Count == 0)
                return;

            foreach (var infos in infoGroups)
            {
                for (int i = 0; i < infos.Length; i++)
                {
                    GameObjectInfo a = infos[i];

                    GameObjectInfo b;
                    if (i == infos.Length - 1)
                        b = infos[0];
                    else
                        b = infos[i + 1];

                    var f = .01f;
                    if (a.position == b.position)
                    {
                        if (Random.value > 0.5f)
                            b.position = a.position + new Vector3(Random.Range(-f, f), 0, Random.Range(-f, f));
                        else
                            a.position = b.position + new Vector3(Random.Range(-f, f), 0, Random.Range(-f, f));
                    }

                    var correction = (b.position - a.position).normalized * f;
                    a.position -= correction;
                    b.position += correction;
                }
            }
        }

        static GameObjectInfo[] _FindTransformNear(GameObjectInfo[] gameObjectInfos, GameObjectInfo info)
        {
            var result = new List<GameObjectInfo>();
            for (int i = 0; i < gameObjectInfos.Length; i++)
            {
                var otherInfo = gameObjectInfos[i];
                if (info == otherInfo)
                    continue;

                var aEntry = prefabLibrary[info.gameObject];
                var bEntry = prefabLibrary[otherInfo.gameObject];
                var a = info.size.x * (aEntry == null ? 1 : aEntry.keepApartFactor);
                var b = otherInfo.size.x * (bEntry == null ? 1 : bEntry.keepApartFactor);
                var ab = (a + b) / 2;

                if ((info.position - otherInfo.position).sqrMagnitude < ab * ab)
                    result.Add(otherInfo);
            }
            return result.ToArray();
        }



        public static void BeginAlpha(float a)
        {
            var color = GUI.color;
            guiColors.Add(color);
            GUI.color = new Color(color.r, color.g, color.b, color.a * a);
        }

        public static void EndAlpha()
        {
            var index = guiColors.Count - 1;
            GUI.color = guiColors[index];
            guiColors.RemoveAt(index);
        }

        public static void BeginLabelWidth(float labelWidth)
        {
            var _labelWidth = EditorGUIUtility.labelWidth;
            labelWidths.Add(_labelWidth);
            EditorGUIUtility.labelWidth = labelWidth;
        }

        public static void EndLabelWidth()
        {
            var index = labelWidths.Count - 1;
            EditorGUIUtility.labelWidth = labelWidths[index];
            labelWidths.RemoveAt(index);
        }



        public static bool Equals(object[] a, object[] b)
        {
            if (a is null && b is not null)
                return false;

            if (a is not null && b is null)
                return false;

            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }

        public static bool Equals(IList a, IList b)
        {
            if (a is null && b is not null)
                return false;

            if (a is not null && b is null)
                return false;

            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }

        public static void SetLayer(GameObject gameObject, int layer)
        {
            if (!gameObject)
                return;

            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                SetLayer(child.gameObject, layer);
        }



        public static void Collapse(Object @object)
        {
            var sceneHierarchy = GetSceneHierarchy();

            var methodInfo = sceneHierarchy
                .GetType()
                .GetMethod("ExpandTreeViewItem", Flags.NonPublic | Flags.Instance);

            methodInfo.Invoke(sceneHierarchy, new object[] {
                @object.GetInstanceID(),
                false
            });
        }

        static object GetSceneHierarchy()
        {
            return typeof(EditorWindow).Assembly
                .GetType("UnityEditor.SceneHierarchyWindow")
                .GetProperty("sceneHierarchy")
                .GetValue(GetHierarchyWindow());
        }

        static EditorWindow GetHierarchyWindow()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
            return EditorWindow.focusedWindow;
        }

        public static void FocusSceneView()
        {
            if (SceneView.sceneViews.Count == 0)
                return;

            if (SceneView.sceneViews[0] is not SceneView sceneView)
                return;

            sceneView.Focus();
        }



        public static T Get<T>(ref T scriptableObject, string name) where T : ScriptableObject
        {
            if (scriptableObject)
                return scriptableObject;

#if UNITY_EDITOR
            var t = typeof(T);
            foreach (var guid in AssetDatabase.FindAssets($"t:{t.Namespace}.{t.Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (!asset)
                    return null;

                if (asset.name != name)
                    continue;

                scriptableObject = asset;
                break;
            }

            if (scriptableObject)
                return scriptableObject;

            var path = $"Assets/{name}.asset";
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<T>(), path);
            AssetDatabase.Refresh();
            scriptableObject = AssetDatabase.LoadAssetAtPath<T>(path);
#else
			scriptableObject = Resources.Load<T>(name);
#endif
            return scriptableObject;
        }



        static readonly Dictionary<int, Texture2D> textureCache = new();

        public static Texture2D CreateTexture(Color color, int width = 1, int height = 1, int border = 0, Color? borderColor = null, bool cached = true)
        {
            int hashCode = 0;

            if (cached)
            {
                hashCode = System.HashCode.Combine(color, width, height, border, borderColor);
                if (textureCache.ContainsKey(hashCode))
                    return textureCache[hashCode];
            }

            width += border * 2;
            height += border * 2;

            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            var texture = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point
            };

            texture.SetPixels32(pixels);
            if (border > 0)
            {
                Color finalBorderColor;
                if (borderColor.HasValue)
                {
                    finalBorderColor = borderColor.Value;
                }
                else
                {
                    finalBorderColor = color * .5f;
                    finalBorderColor.a = color.a * 2;
                }

                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        if (x < border || x >= width - border || y < border || y >= height - border)
                            texture.SetPixel(x, y, finalBorderColor);
            }
            texture.Apply();

            if (cached)
                textureCache.Add(hashCode, texture);

            return texture;
        }

        public static bool HasDefaultValues(Transform @this)
        {
            if (!Approximately(@this.localPosition, Vector3.zero))
                return false;

            if (!Approximately(@this.localRotation, Quaternion.identity))
                return false;

            if (!Approximately(@this.localScale, Vector3.one))
                return false;

            return true;
        }

        static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.z, b.z);
        }

        static bool Approximately(Quaternion a, Quaternion b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.z, b.z)
                && Mathf.Approximately(a.w, b.w);
        }
    }
}