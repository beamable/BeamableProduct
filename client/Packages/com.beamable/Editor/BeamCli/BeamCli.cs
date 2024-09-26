using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using System.Threading.Tasks;
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

			var instance = Command.Version();
			comm.AutoLogErrors = false;
			try
			{
				await instance.Run();
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
			
			var args = new InitArgs
			{
				saveToFile = true,
				noTokenSave = true,
				cid = _ctx.Requester.Cid,
				pid = _ctx.Requester.Pid,
				host = BeamableEnvironment.ApiUrl,
			};
			
			var token = _ctx.Requester.Token;
			if (token == null)
			{
				// there is no token, but we can still save the cid/pid info.
				args.noTokenSave = true;
				args.saveToFile = false;
			}
			else
			{
				args.refreshToken = token.RefreshToken;
				args.saveToFile = true;
				args.noTokenSave = false;
			}
			
			var initCommand = Command.Init(args);
			await initCommand.Run();


			var linkCommand = Command.ProjectAddUnityProject(new ProjectAddUnityProjectArgs
			{
				path = "."
			});
			await linkCommand.Run();
		}
	}
}
