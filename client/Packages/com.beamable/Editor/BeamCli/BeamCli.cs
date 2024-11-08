using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Environment;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable.Editor.BeamCli
{
	public class BeamCli : ILoadWithContext
	{
		private readonly IDependencyProvider _provider;
		private readonly BeamEditorContext _ctx;

		public Promise OnReady { get; }

		public BeamCli(IDependencyProvider provider, BeamEditorContext ctx)
		{
			_provider = provider;
			_ctx = ctx;
			OnReady = Init();
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

		public async Promise Init()
		{
			await _ctx.OnReady;
			
			var args = new InitArgs
			{
				saveToFile = true,
				noTokenSave = true,
				cid = _ctx.Requester.Cid,
				pid = _ctx.Requester.Pid,
				host = BeamableEnvironment.ApiUrl
			};

			if (string.IsNullOrEmpty(args.cid) || string.IsNullOrEmpty(args.pid))
			{
				// cannot call the init command with a blank cid/pid. 
				return;
			}
			
			var extraPaths = BeamablePackages.GetManifestFileReferences();
			args.saveExtraPaths = extraPaths.ToArray();
			
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
			linkCommand.OnError(cb =>
			{
				Debug.LogError("Unable to register Unity project with local CLI project." + cb.data.message);
			});
			var _ = linkCommand.Run();
		}
	}
}
