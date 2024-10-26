using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
#if BEAMABLE_DEVELOPER
	[CreateAssetMenu(fileName = "OpenPortalMenuItem", menuName = "Beamable/Toolbar/Menu Items/Portal Window", order = BeamableMenuItemScriptableObjectCreationOrder)]
#endif
	public class BeamablePortalMenuItem : BeamableToolbarMenuItem
	{
		public override void OnItemClicked(BeamEditorContext ctx)
		{
			string url = $"{BeamableEnvironment.PortalUrl}/{ctx.CurrentCustomer.Cid}/games/{ctx.ProductionRealm.Pid}/realms/{ctx.CurrentRealm.Pid}/dashboard?refresh_token={ctx.Requester.Token.RefreshToken}";
			Application.OpenURL(url);
		}
	}
}
