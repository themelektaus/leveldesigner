using UnityEditor.Toolbars;
using UnityEditor.Overlays;
using UnityEditor;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace LevelDesigner.Editor
{
    abstract class EditorToolbarButton : UnityEditor.Toolbars.EditorToolbarButton, IEditorToolbarObject
    {
        protected abstract string GetText();
        protected abstract string GetIconName();
        protected abstract string GetTooltip();
        protected abstract bool IsEnabled();
        protected abstract void OnClick();

        protected virtual bool requiresSceneViewGUI => true;

        protected SceneViewUI sceneViewUI => SceneViewUI.instance;

        protected Selection _selection => sceneViewUI?.selection;

        protected EditorToolbarButton()
        {
            SceneViewOverlay.objects.Add(this);
            text = GetText();
            icon = Icon.Get(GetIconName());
            tooltip = GetTooltip();
            clicked += OnClick;
            Update();
        }

        public void Update()
        {
            if (!requiresSceneViewGUI || sceneViewUI is not null)
            {
                SetEnabled(IsEnabled());
                return;
            }

            SetEnabled(false);
        }
    }

    abstract class EditorToolbarToggle : UnityEditor.Toolbars.EditorToolbarToggle, IEditorToolbarObject
    {
        protected abstract string GetText();
        protected abstract string GetIconName();
        protected abstract string GetTooltip();
        protected abstract bool IsEnabled();
        protected abstract bool IsActive();
        protected abstract void OnChange(bool value);

        protected virtual bool requiresSceneViewGUI => true;

        protected SceneViewUI sceneViewUI => SceneViewUI.instance;

        protected Selection selection => sceneViewUI?.selection;

        protected EditorToolbarToggle()
        {
            SceneViewOverlay.objects.Add(this);
            text = GetText();
            icon = Icon.Get(GetIconName());
            tooltip = GetTooltip();
            this.RegisterValueChangedCallback(e => OnChange(e.newValue));
            Update();
        }

        public void Update()
        {
            if (!requiresSceneViewGUI || sceneViewUI is not null)
            {
                SetEnabled(IsEnabled());
                SetValueWithoutNotify(IsActive());
                return;
            }

            SetEnabled(false);
            SetValueWithoutNotify(false);
        }
    }

    public interface IEditorToolbarObject
    {
        void Update();
    }

    [Overlay(typeof(SceneView), ID, "Level Designer")]
    public class SceneViewOverlay : ToolbarOverlay
    {
        public static SceneViewOverlay instance { get; private set; }

        public static readonly List<IEditorToolbarObject> objects = new List<IEditorToolbarObject>();

        public const string ID = "mt-packages-leveldesigner-sceneview-ui";

        SceneViewOverlay() : base(
            SceneViewOverlay_OpenLevelDesigner.ID,
            SceneViewOverlay_SelectChildren.ID,
            SceneViewOverlay_SelectParent.ID,
            SceneViewOverlay_DefaultMode.ID,
            SceneViewOverlay_ExpandMode.ID,
            SceneViewOverlay_PaintMode.ID,
            SceneViewOverlay_MaterialMode.ID,
            SceneViewOverlay_RoundTransformValues.ID,
            SceneViewOverlay_KeepApart.ID,
            SceneViewOverlay_SetToGround.ID,
            SceneViewOverlay_RefreshAssetDatabase.ID,
            SceneViewOverlay_LevelDesignerSettings.ID
        )
        {
            instance = this;
            //EditorToolbarUtility.SetupChildrenAsButtonStrip();
        }

        public void Update()
        {
            foreach (var @object in objects)
                @object.Update();
        }
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_OpenLevelDesigner : EditorToolbarButton
    {
        public const string ID = SceneViewOverlay.ID + "-open-level-designer";
        protected override string GetText() => ""; //"Open Level Designer";
        protected override string GetIconName() => "Open";
        protected override string GetTooltip() => "Open Level Designer";
        protected override bool requiresSceneViewGUI => false;
        protected override bool IsEnabled() => sceneViewUI == null;
        protected override void OnClick() => UIElements.LevelDesignerWindow.Open();
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_SelectChildren : EditorToolbarButton
    {
        public const string ID = SceneViewOverlay.ID + "-select-children";
        protected override string GetText() => ""; //"Select Children";
        protected override string GetIconName() => "List";
        protected override string GetTooltip() => "Select all child objects";
        protected override bool IsEnabled() => (_selection.activeTransform?.childCount ?? 0) > 0;
        protected override void OnClick() => _selection.SelectChildren();
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_SelectParent : EditorToolbarButton
    {
        public const string ID = SceneViewOverlay.ID + "-select-parent";
        protected override string GetText() => ""; //"Select Parent";
        protected override string GetIconName() => "Up";
        protected override string GetTooltip() => "Select parent object";
        protected override bool IsEnabled() => _selection.activeTransform?.parent;
        protected override void OnClick() => _selection.SelectParent(true);
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_DefaultMode : EditorToolbarToggle
    {
        public const string ID = SceneViewOverlay.ID + "-default-mode";
        protected override string GetText() => ""; //"Default Mode";
        protected override string GetIconName() => "Cursor";
        protected override string GetTooltip() => "Default Mode";
        protected override bool IsEnabled() => true;
        protected override bool IsActive() => sceneViewUI.activeTool == Tool.None;
        protected override void OnChange(bool value) => sceneViewUI.activeTool = Tool.None;
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_ExpandMode : EditorToolbarToggle
    {
        public const string ID = SceneViewOverlay.ID + "-expand-mode";
        protected override string GetText() => ""; //"Expand Mode";
        protected override string GetIconName() => "Expand";
        protected override string GetTooltip() => "Expand Mode";
        protected override bool IsEnabled() => true;
        protected override bool IsActive() => sceneViewUI.activeTool == Tool.Expand;
        protected override void OnChange(bool value) => sceneViewUI.activeTool = value ? Tool.Expand : Tool.None;
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_PaintMode : EditorToolbarToggle
    {
        public const string ID = SceneViewOverlay.ID + "-paint-mode";
        protected override string GetText() => ""; //"Paint Mode";
        protected override string GetIconName() => "Brush";
        protected override string GetTooltip() => "Paint Mode";
        protected override bool IsEnabled() => true;
        protected override bool IsActive() => sceneViewUI.activeTool == Tool.Brush;
        protected override void OnChange(bool value) => sceneViewUI.activeTool = value ? Tool.Brush : Tool.None;
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_MaterialMode : EditorToolbarToggle
    {
        public const string ID = SceneViewOverlay.ID + "-material-mode";
        protected override string GetText() => ""; //"Material Mode";
        protected override string GetIconName() => "Sheet Metal";
        protected override string GetTooltip() => "Material Mode";
        protected override bool IsEnabled() => true;
        protected override bool IsActive() => sceneViewUI.activeTool == Tool.Material;
        protected override void OnChange(bool value) => sceneViewUI.activeTool = value ? Tool.Material : Tool.None;
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_RoundTransformValues : EditorToolbarButton
    {
        public const string ID = SceneViewOverlay.ID + "-round-transform-values";
        protected override string GetText() => ""; //"Round Transform Values";
        protected override string GetIconName() => "Grid";
        protected override string GetTooltip() => "Round Transform Values";
        protected override bool IsEnabled() => _selection.transforms.Length > 0;
        protected override void OnClick() => sceneViewUI.RoundTransformValues(_selection.transforms);
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_KeepApart : EditorToolbarToggle
    {
        public const string ID = SceneViewOverlay.ID + "-keep-apart";
        protected override string GetText() => ""; //"Keep Apart";
        protected override string GetIconName() => "Distance";
        protected override string GetTooltip() => "Try to keep selected objects apart (Starts a Background Task)";
        protected override bool IsEnabled() => jobIsRunning || selection.gameObjects.Length >= 2;
        protected override bool IsActive() => jobIsRunning;
        protected override void OnChange(bool value)
        {
            if (value)
                SceneViewUI.instance.activeTool = Tool.None;

            if (value && !jobIsRunning)
            {
                Undo.RecordObjects(SceneViewUI.instance.selection.gameObjects, "Keep Apart");

                Utils.KeepApartAsync(SceneViewUI.instance.selection.gameObjects, true, gameObjects =>
                {
                    Utils.AutoGround(gameObjects);
                });

                return;
            }
            Utils.CancelAsyncJob();
        }

        bool jobIsRunning => Utils.asyncJobNameRunning == "KeepApart";
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_SetToGround : EditorToolbarButton
    {
        public const string ID = SceneViewOverlay.ID + "-set-to-ground";
        protected override string GetText() => ""; //"Set to Ground";
        protected override string GetIconName() => "Down";
        protected override string GetTooltip() => "Set selected objects to the ground";
        protected override bool IsEnabled() => _selection.transforms.Length > 0;
        protected override void OnClick()
        {
            sceneViewUI.activeTool = Tool.None;
            Undo.RecordObjects(_selection.transforms, "Set To Ground");
            Utils.SetToGround(_selection.transforms);
        }
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_RefreshAssetDatabase : EditorToolbarButton
    {
        public const string ID = SceneViewOverlay.ID + "-refresh-asset-database";
        protected override string GetText() => ""; //"Refresh Asset Database";
        protected override string GetIconName() => "Refresh";
        protected override string GetTooltip() => "Refresh Asset Database";
        protected override bool requiresSceneViewGUI => false;
        protected override bool IsEnabled() => true;
        protected override void OnClick()
        {
            foreach (var guid in AssetDatabase.FindAssets(typeof(SceneViewUI).Name))
            {
                AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
                break;
            }

            AssetDatabase.Refresh();
        }
    }

    [EditorToolbarElement(ID, typeof(SceneView))]
    class SceneViewOverlay_LevelDesignerSettings : EditorToolbarToggle
    {
        public const string ID = SceneViewOverlay.ID + "-level-designer-settings";
        protected override string GetText() => ""; //"Level Designer Settings";
        protected override string GetIconName() => "Config";
        protected override string GetTooltip() => "Level Designer Settings";
        protected override bool requiresSceneViewGUI => false;
        protected override bool IsEnabled() => true;
        protected override bool IsActive()
        {
            if (sceneViewUI is null)
                return UnityEditor.Selection.activeObject == Utils.editorSettings;

            return selection.activeObject == Utils.editorSettings;
        }

        protected override void OnChange(bool value)
        {
            if (sceneViewUI is null)
            {
                if (UnityEditor.Selection.activeObject == Utils.editorSettings)
                {
                    UnityEditor.Selection.activeObject = null;
                    return;
                }

                UnityEditor.Selection.activeObject = Utils.editorSettings;
                return;
            }

            if (selection.activeObject == Utils.editorSettings)
            {
                selection.SetActive(null);
                return;
            }

            selection.SetActive(Utils.editorSettings);
        }
    }
}