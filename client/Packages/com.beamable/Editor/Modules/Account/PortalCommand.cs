using Beamable.Common;
using Beamable.ConsoleCommands;
using UnityEngine;

namespace Beamable.Editor.Modules.Account
{
	[BeamableConsoleCommandProvider]
	public class PortalCommand
	{
		[BeamableConsoleCommand("portal", "Opens portal for the current user", "portal")]
		private string OpenPortal(string[] args)
		{
			API.Instance.Then(api =>
			{
				var DBID = api.User.id.ToString();
				Debug.Log($"Current user: {DBID}");
				GetPortalUrl(DBID).Then(Application.OpenURL);
			});
			return "Opening portal..";
		}
		private Promise<string> GetPortalUrl(string DBID) => EditorAPI.Instance.Map(api =>
			$"{BeamableEnvironment.PortalUrl}/{api.Alias}/games/{api.ProductionRealm.Pid}/realms/{api.Pid}/players/{DBID}?refresh_token={api.Token.RefreshToken}");
	}
}
