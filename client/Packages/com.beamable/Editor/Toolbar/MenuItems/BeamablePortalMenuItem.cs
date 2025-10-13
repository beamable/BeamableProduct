using Beamable.Common;
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
			string url = $"{GetPortalUrl()}/{ctx.BeamCli.Cid}/games/{ctx.BeamCli.ProductionRealm.Pid}/realms/{ctx.BeamCli.Pid}/dashboard?refresh_token={ctx.Requester.Token.RefreshToken}";
			Application.OpenURL(url);
		}

		private string GetPortalUrl()
		{
			var context = BeamEditorContext.Default;
			string env = context?.BeamCli?.latestRouteInfo?.env ?? string.Empty;
			switch (env)
			{
				case "dev": return Constants.BEAM_DEV_PORTAL_URI;
				case "staging" : return Constants.BEAM_STAGE_PORTAL_URI;
				default: return Constants.BEAM_PROD_PORTAL_URI;
			}
		}
	}
}
