using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.IO;

namespace Beamable.Server.Editor.Usam
{
	public class PublishService : ILoadWithContext
	{
		private readonly BeamCommands _cli;
		private readonly CodeService _codeService;

		public Action<string> OnDeployFailed;
		public Action OnDeploySuccess;
		public Action<string, double, double> OnDeployProgress;
		public Action<LogLevel, string, string> OnDeployLogMessage;

		public PublishService(BeamCommands cli, CodeService codeService)
		{
			_cli = cli;
			_codeService = codeService;
		}

		/// <summary>
		/// Publish all services that are configured in the local manifest file.
		/// </summary>
		public async Promise PublishServices()
		{
			ServicesDeployArgs args = new ServicesDeployArgs();
			BeamCommandWrapper deployer = _cli.ServicesDeploy(args).OnStreamServiceDeployReportResult((cb) =>
			{
				if (cb.data.Success)
				{
					OnDeploySuccess?.Invoke();
					return;
				}

				OnDeployFailed?.Invoke(cb.data.FailureReason);
			}).OnRemote_progressServiceRemoteDeployProgressResult((cb) =>
			{
				OnDeployProgress?.Invoke(cb.data.BeamoId, cb.data.BuildAndTestProgress,
				                         cb.data.ContainerUploadProgress);
			}).OnStreamServiceDeployLogResult((cb) =>
			{
				OnDeployLogMessage?.Invoke(cb.data.Level, cb.data.Message, cb.data.TimeStamp);
			});
			await deployer.Run();
		}
	}
}
