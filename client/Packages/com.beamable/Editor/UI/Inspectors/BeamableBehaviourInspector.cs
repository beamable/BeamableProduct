#if !BEAMABLE_NO_OPTIONAL_DRAWERS
using Beamable.Common;
using Beamable.Editor.UI.Components;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Inspectors
{
    [CustomEditor(typeof(BeamableBehaviour))]
    public class BeamableBehaviourInspector : UnityEditor.Editor
    {
        private PrimaryButtonVisualElement _button;

        public override VisualElement CreateInspectorGUI()
        {
            // Create a new VisualElement to be the root of our inspector UI
            VisualElement myInspector = new VisualElement();
            myInspector.AddStyleSheet(Constants.Files.COMMON_USS_FILE);
            _button = new PrimaryButtonVisualElement();
            _button.SetText("Open Portal for user");
            _button.Refresh();
            
            _button.Button.clickable.clicked += OpenPortalForThisUser;
            UpdateElementState();
            // Attach a default inspector to the foldout
            InspectorElement.FillDefaultInspector(myInspector, serializedObject, this);
            myInspector.Add(_button);

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            return myInspector;
        }

        private void OnDestroy()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            UpdateElementState();
        }

        private void UpdateElementState()
        {
            if(_button != null)
            {
	            _button.style.marginBottom = (EditorApplication.isPlaying ? 15 : 0);
	            _button.style.marginTop = (EditorApplication.isPlaying ? 5 : 0);
                _button.style.opacity = (EditorApplication.isPlaying ? 1.0f : 0.0f);
                _button.SetEnabled(EditorApplication.isPlaying);
            }
        }

        void OpenPortalForThisUser()
        {
            if (target is BeamableBehaviour beamable)
            {
                string url = PortalPathForContext(beamable.Context);
                Application.OpenURL(url);
            }
        }

        public static string PortalPathForContext(BeamContext ctx)
        {
            var api = BeamEditorContext.Default;
            string url =
                $"{BeamableEnvironment.PortalUrl}/{ctx.Cid}/games/{api.ProductionRealm.Pid}/realms/{ctx.Pid}/players/{ctx.PlayerId}?refresh_token={api.Requester.Token.RefreshToken}";
            return url;
        }
    }
}
#endif
