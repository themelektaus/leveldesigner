using UnityEditor;
using UnityEngine;

namespace LevelDesigner.Editor
{
    [InitializeOnLoad]
    public static class Style
    {
        public const int TOOLBOX_HEIGHT = 272;

        static GUIStyle _toolboxWindow;
        static GUIStyle _toolboxFoldoutButton;
        static GUIStyle _toolboxWindowHeader;
        static GUIStyle _toolboxWindowButton;
        static GUIStyle _toolboxWindowButtonActive;
        static GUIStyle _toolboxWindowButtonDisabled;

        static GUIStyle _parametersWindow;

        static Style()
        {
            OnDomainReset();
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }

        static void PlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.EnteredEditMode)
            {
                OnDomainReset();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnDomainReset()
        {
            _toolboxWindow = null;
            _toolboxFoldoutButton = null;
            _toolboxWindowHeader = null;
            _toolboxWindowButton = null;
            _toolboxWindowButtonActive = null;
            _toolboxWindowButtonDisabled = null;
            _parametersWindow = null;
        }

        public static GUIStyle toolboxWindow
        {
            get
            {
                if (_toolboxWindow is null)
                {
                    _toolboxWindow = new GUIStyle(GUIStyle.none);
                    _toolboxWindow.normal.background = Utils.CreateTexture(new Color(.15f, .15f, .15f));
                }
                return _toolboxWindow;
            }
        }

        public static GUIStyle toolboxFoldoutButton
        {
            get
            {
                if (_toolboxFoldoutButton == null)
                {
                    _toolboxFoldoutButton = new(GUIStyle.none)
                    {
                        margin = new RectOffset(2, 2, 2, 2),
                        fixedWidth = 10,
                        stretchHeight = true
                    };
                    _toolboxFoldoutButton.normal.background = Utils.CreateTexture(new Color(.25f, .25f, .25f));
                    _toolboxFoldoutButton.hover.background = Utils.CreateTexture(new Color(.2f, .5f, .2f), 12, TOOLBOX_HEIGHT, 1, new Color(0, .9f, 0));
                    _toolboxFoldoutButton.active.background = Utils.CreateTexture(new Color(.1f, .4f, .1f), 12, TOOLBOX_HEIGHT, 1, new Color(0, .8f, 0));
                }
                return _toolboxFoldoutButton;
            }
        }

        public static GUIStyle toolboxWindowHeader
        {
            get
            {
                if (_toolboxWindowHeader is null)
                {
                    _toolboxWindowHeader = new(GUIStyle.none)
                    {
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(4, 4, 4, 4),
                        fixedWidth = 62,
                        fixedHeight = 24,
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10,
                        fontStyle = FontStyle.Bold
                    };
                    _toolboxWindowHeader.normal.textColor = new Color(1, 1, 1, .7f);
                    _toolboxWindowHeader.normal.background = Utils.CreateTexture(new Color(.25f, .25f, .25f));
                }
                return _toolboxWindowHeader;
            }
        }

        public static GUIStyle toolboxWindowButton
        {
            get
            {
                if (_toolboxWindowButton is null)
                {
                    _toolboxWindowButton = new(GUIStyle.none)
                    {
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 6, 6),
                        fixedWidth = 30,
                        fixedHeight = 30,
                    };
                    _toolboxWindowButton.normal.background = Utils.CreateTexture(new Color(.25f, .25f, .25f));
                    _toolboxWindowButton.hover.background = Utils.CreateTexture(new Color(.2f, .5f, .2f), 32, 32, 1, new Color(0, .9f, 0));
                    _toolboxWindowButton.active.background = Utils.CreateTexture(new Color(.1f, .4f, .1f), 32, 32, 1, new Color(0, .8f, 0));
                }
                return _toolboxWindowButton;
            }
        }

        public static GUIStyle toolboxWindowButtonActive
        {
            get
            {
                if (_toolboxWindowButtonActive is null)
                {
                    _toolboxWindowButtonActive = new(GUIStyle.none)
                    {
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 6, 6),
                        fixedWidth = 30,
                        fixedHeight = 30,
                    };
                    _toolboxWindowButtonActive.normal.background = Utils.CreateTexture(new Color(.4f, .4f, .1f), 32, 32, 1, new Color(.8f, .8f, 0));
                }
                return _toolboxWindowButtonActive;
            }
        }

        public static GUIStyle toolboxWindowButtonDisabled
        {
            get
            {
                if (_toolboxWindowButtonDisabled is null)
                {
                    _toolboxWindowButtonDisabled = new(GUIStyle.none)
                    {
                        margin = new RectOffset(2, 2, 2, 2),
                        padding = new RectOffset(6, 6, 6, 6),
                        fixedWidth = 30,
                        fixedHeight = 30,
                    };
                    _toolboxWindowButtonDisabled.normal.background = Utils.CreateTexture(new Color(.25f, .25f, .25f));
                }
                return _toolboxWindowButtonDisabled;
            }
        }

        public static GUIStyle parametersWindow
        {
            get
            {
                if (_parametersWindow is null)
                {
                    _parametersWindow = new(GUIStyle.none)
                    {
                        padding = new RectOffset(10, 10, 10, 10)
                    };
                    _parametersWindow.normal.background = Utils.CreateTexture(new Color(.17f, .17f, .17f, .95f));
                }
                return _parametersWindow;
            }
        }
    }
}