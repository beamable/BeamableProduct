using Beamable.Common;
using Beamable.ConsoleCommands;
using UnityEngine;

namespace Beamable.Editor.Modules.Account
{
	[BeamableConsoleCommandProvider]
	public class PortalCommand
	{
		private readonly BeamContext _ctx;

		public PortalCommand(BeamContext ctx)
		{
			_ctx = ctx;
		}

		[BeamableConsoleCommand("portal", "Opens portal for the current user", "portal")]
		private string OpenPortal(string[] args)
		{
			_ctx.OnReady.Then(_ =>
			{
				var playerId = _ctx.PlayerId;
				Debug.Log($"Current user: {playerId}");
				Application.OpenURL(GetPortalUrl());
			});
			return "Opening portal..";
		}

		private string GetPortalUrl()
		{
			var api = BeamEditorContext.Default;
			string url =
				$"{BeamableEnvironment.PortalUrl}/{_ctx.Cid}/games/{api.BeamCli.ProductionRealm.Pid}/realms/{_ctx.Pid}/players/{_ctx.PlayerId}?refresh_token={api.Requester.Token.RefreshToken}";
			return url;
		}
	}
}
