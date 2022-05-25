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
		private Promise<string> GetPortalUrl(string DBID)
		{
			var api = BeamEditorContext.Default;
			return Promise<string>.Successful($"{BeamableEnvironment.PortalUrl}/{api.CurrentCustomer.Alias}/games/{api.ProductionRealm.Pid}/realms/{api.CurrentRealm.Pid}/players/{DBID}?refresh_token={api.Requester.Token.RefreshToken}");
		}
	}
}
