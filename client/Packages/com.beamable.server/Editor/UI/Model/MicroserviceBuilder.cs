using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.UI.Model
{
	[Serializable]
	public class MicroserviceBuilder : ServiceBuilderBase
	{
		public bool IsBuilding
		{
			get => _isBuilding;
			private set
			{
				if (value == _isBuilding) return;
				_isBuilding = value;
				// XXX: If OnIsBuildingChanged is mutated at before delayCall triggers, non-deterministic behaviour could occur
				EditorApplication.delayCall += () => OnIsBuildingChanged?.Invoke(value);
			}
		}
		[SerializeField]
		private bool _isBuilding;

		public string LastBuildImageId
		{
			get => _lastImageId;
			private set
			{
				if (value == _lastImageId) return;
				_lastImageId = value;
				EditorApplication.delayCall += () => OnLastImageIdChanged?.Invoke(value);
			}
		}
		[SerializeField]
		private string _lastImageId;

		public bool HasImage => IsRunning || LastBuildImageId?.Length > 0;
		public bool HasBuildDirectory => Directory.Exists(Path.GetFullPath(_buildPath));

		public Action<bool> OnIsBuildingChanged;
		public Action<string> OnLastImageIdChanged;

		[SerializeField]
		private string _buildPath;

		public void ForwardEventsTo(MicroserviceBuilder oldBuilder)
		{
			if (oldBuilder == null) return;
			OnIsRunningChanged += oldBuilder.OnIsRunningChanged;
			OnIsBuildingChanged += oldBuilder.OnIsBuildingChanged;
			OnLastImageIdChanged += oldBuilder.OnLastImageIdChanged;
		}
		public override async void Init(IDescriptor descriptor)
		{
			base.Init(descriptor);
			_buildPath = ((MicroserviceDescriptor)descriptor).BuildPath;
			await TryToGetLastImageId();
		}

		protected override async Task<RunImageCommand> PrepareRunCommand()
		{
			var beamable = BeamEditorContext.Default;
			await beamable.InitializePromise;
			var secret = await beamable.GetRealmSecret();
			var cid = beamable.CurrentCustomer.Cid;
			// check to see if the storage descriptor is running.
			var serviceRegistry = BeamEditor.GetReflectionSystem<MicroserviceReflectionCache.Registry>();
			var isWatch = MicroserviceConfiguration.Instance.EnableHotModuleReload;
			var connectionStrings = await serviceRegistry.GetConnectionStringEnvironmentVariables((MicroserviceDescriptor)Descriptor);
			return new RunServiceCommand((MicroserviceDescriptor)Descriptor, cid, secret, connectionStrings, isWatch);
		}

		public async Task<bool> TryToBuild(bool includeDebuggingTools)
		{
			if (IsBuilding) return true;

			IsBuilding = true;
			var isWatch = MicroserviceConfiguration.Instance.EnableHotModuleReload;
			if (isWatch)
			{
				// before we can build this container, we need to remove any contains that may have bind mounts open to the service's filesystems.
				//  because if we don't, its possible Docker might prevent the directory cleanup operations and flunk the build.
				await TryToStop(); // for it to stop.
				await BeamServicesCodeWatcher.StopClientSourceCodeGenerator((MicroserviceDescriptor)Descriptor);
			}
			var command = new BuildImageCommand((MicroserviceDescriptor)Descriptor, includeDebuggingTools, isWatch);
			command.OnStandardOut += message => MicroserviceLogHelper.HandleBuildCommandOutput(this, message);
			command.OnStandardErr += message => MicroserviceLogHelper.HandleBuildCommandOutput(this, message);
			try
			{
				await command.StartAsync();
				await TryToGetLastImageId();

				// Update the config with the code handle identifying the version of the code this is building with (see BeamServicesCodeWatcher).
				// Check for any local code changes to C#MS or it's dependent Storage/Common assemblies and update the hint state.
				var codeWatcher = default(BeamServicesCodeWatcher);
				BeamEditor.GetBeamHintSystem(ref codeWatcher);
				codeWatcher.UpdateBuiltImageCodeHandles(Descriptor.Name);
				codeWatcher.CheckForLocalChangesNotYetDeployed();

				return true;
			}
			catch (Exception e)
			{
				EditorApplication.delayCall += () =>
				{
					MicroservicesDataModel.Instance.AddLogMessage(
						Descriptor,
						new LogMessage
						{
							Level = LogLevel.ERROR,
							Message = e.Message,
							ParameterText = e.StackTrace,
							Timestamp = LogMessage.GetTimeDisplay(DateTime.Now)
						});
				};
				MicroserviceLogHelper.HandleBuildCommandOutput(this, "Error");
			}
			finally
			{
				IsBuilding = false;
				BeamServicesCodeWatcher.GenerateClientSourceCode((MicroserviceDescriptor)Descriptor);
			}

			return false;
		}
		public async Task TryToGetLastImageId()
		{
			var getChecksum = new GetImageIdCommand(Descriptor);
			try
			{
				LastBuildImageId = await getChecksum.StartAsync();
			}
			catch (Exception e)
			{
				System.Console.WriteLine(e);
				throw;
			}
		}
		public async Task TryToBuildAndRestart(bool includeDebuggingTools)
		{
			bool isBuilt = await TryToBuild(includeDebuggingTools);

			if (isBuilt)
				await TryToRestart();
			else
				await TryToStop();
		}
		public async Task TryToBuildAndStart(bool includeDebuggingTools)
		{
			bool isBuilded = await TryToBuild(includeDebuggingTools);

			if (isBuilded)
				await TryToStart();
		}
	}
}
