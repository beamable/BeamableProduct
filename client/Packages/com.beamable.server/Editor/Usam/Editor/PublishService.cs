using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Microservice.UI.Components;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Server.Editor.Usam
{
	public class PublishService : ILoadWithContext
	{
		private readonly BeamCommands _cli;
		private readonly CodeService _codeService;

		public Action<string> OnDeployFailed;
		public Action OnDeploySuccess;
		public Action<string, ServicePublishState> OnDeployStateProgress;
		public Action<string, double, double> OnServiceDeployProgress;
		public Action<string, string, string> OnDeployLogMessage;
		public Action<string, ServicePublishState> OnProgressInfoUpdated;

		public BeamCommandWrapper _command;

		public PublishService(BeamCommands cli, CodeService codeService)
		{
			_cli = cli;
			_codeService = codeService;
		}

		/// <summary>
		/// Clean up all actions in order to make sure everything is fine for a next publish.
		/// </summary>
		public void Init()
		{
			OnDeployFailed = null;
			OnDeploySuccess = null;
			OnDeployStateProgress = null;
			OnServiceDeployProgress = null;
			OnDeployLogMessage = null;
			OnProgressInfoUpdated = null;
		}


		/// <summary>
		/// Publish all services that are configured in the local manifest file.
		/// </summary>
		public async Promise PublishServices()
		{
			OnProgressInfoUpdated?.Invoke("Preparing publish process", ServicePublishState.Verifying);
			ServicesDeployArgs args = new ServicesDeployArgs();
			_command = _cli.ServicesDeploy(args).OnStreamServiceDeployReportResult((cb) =>
			{
				if (cb.data.Success)
				{
					OnProgressInfoUpdated?.Invoke($"Services deploy process completed!", ServicePublishState.Published);
					OnDeploySuccess?.Invoke();
					return;
				}

				OnProgressInfoUpdated?.Invoke($"The deployment failed.", ServicePublishState.Failed);
				OnDeployFailed?.Invoke(cb.data.FailureReason);
			}).OnRemote_progressServiceRemoteDeployProgressResult((cb) =>
			{
				var beamoId = cb.data.BeamoId;
				var buildProgress = cb.data.BuildAndTestProgress;
				var uploadProgress = cb.data.ContainerUploadProgress;

				OnServiceDeployProgress?.Invoke(beamoId, buildProgress, uploadProgress);

				if (buildProgress != 0 && uploadProgress == 0)
				{
					OnProgressInfoUpdated?.Invoke($"[{beamoId}] Building image", ServicePublishState.Verifying);
					OnDeployStateProgress?.Invoke(beamoId, ServicePublishState.Verifying);
				}
				else if (uploadProgress != 0)
				{
					if (Math.Abs(uploadProgress - 100D) < 0.5)
					{
						OnDeployStateProgress?.Invoke(beamoId, ServicePublishState.Published);
					}
					else
					{
						OnProgressInfoUpdated?.Invoke($"[{beamoId}] Uploading image",
													  ServicePublishState.InProgress);
						OnDeployStateProgress?.Invoke(beamoId, ServicePublishState.InProgress);
					}
				}
				else if (buildProgress == 0 && uploadProgress == 0)
				{
					OnDeployStateProgress?.Invoke(beamoId, ServicePublishState.Unpublished);
				}

			}).OnLogsServiceDeployLogResult((cb) =>
			{
				OnDeployLogMessage?.Invoke(cb.data.Level, cb.data.Message, cb.data.TimeStamp);
			});
			await _command.Run();
		}

		public void Cancel()
		{
			if (_command != null)
			{
				_command.Cancel();
				_command = null;
			}
		}
	}
}
