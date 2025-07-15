#if !BEAMABLE_NO_OPTIONAL_DRAWERS
using Beamable.Common;
using Beamable.Editor.Util;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Inspectors
{
    [CustomEditor(typeof(BeamableBehaviour))]
    public class BeamableBehaviourInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            GUI.enabled = Application.isPlaying;
            var didClick = BeamGUI.PrimaryButton(new GUIContent("Open Portal for user"));
            GUI.enabled = true;
            base.OnInspectorGUI();

            if (didClick)
            {
                OpenPortalForThisUser();
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
                $"{BeamableEnvironment.PortalUrl}/{ctx.Cid}/games/{api.BeamCli.ProductionRealm.Pid}/realms/{ctx.Pid}/players/{ctx.PlayerId}?refresh_token={api.Requester.Token.RefreshToken}";
            return url;
        }
    }
}
#endif
