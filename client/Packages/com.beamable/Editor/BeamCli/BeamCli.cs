using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using UnityEngine;

namespace Beamable.Editor.BeamCli
{
	public class BeamCli : ILoadWithContext
	{
		private readonly IDependencyProvider _provider;
		private readonly BeamEditorContext _ctx;
		private readonly IAccountService _accountService;

		public Promise OnReady { get; }

		public BeamCli(IDependencyProvider provider, BeamEditorContext ctx, IAccountService accountService)
		{
			_provider = provider;
			_ctx = ctx;
			_accountService = accountService;
			OnReady = Init();
			_accountService.OnUserChanged(Init);
		}

		public BeamCommands Command => DependencyBuilder.Instantiate<BeamCommands>(_provider);

		public async Promise<bool> IsAvailable()
		{
			var comm = new BeamCommand(_provider.GetService<BeamableDispatcher>());
			comm.SetCommand("beam --version");
			comm.AutoLogErrors = false;
			try
			{
				await comm.Run();
				return true;
			}
			catch
			{
				return false;
			}
		}

		async Promise Init(EditorAccountInfo _) => await Init();

		public async Promise Init()
		{
			await _ctx.OnReady;
			var initCommand = Command.Init(new InitArgs
			{
				saveToFile = true,
				refreshToken = _ctx.Requester.Token.RefreshToken,
				cid = _ctx.Requester.Cid,
				pid = _ctx.Requester.Pid,
				host = BeamableEnvironment.ApiUrl,
			});
			await initCommand.Run();
			Debug.Log("saved .beamable/config-defaults.json");


			var linkCommand = Command.ProjectAddUnityProject(new ProjectAddUnityProjectArgs
			{
				quiet = true, 
				path = "."
			});
			await linkCommand.Run();
			Debug.Log("linked project");
			
		}
	}
}
