using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;

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
				if (value == _isBuilding)
					return;
				_isBuilding = value;
				// XXX: If OnIsBuildingChanged is mutated at before delayCall triggers, non-deterministic behaviour could occur
				EditorApplication.delayCall += () => OnIsBuildingChanged?.Invoke(value);
			}
		}

		private bool _isBuilding;

		public string LastBuildImageId
		{
			get => _lastImageId;
			private set
			{
				if (value == _lastImageId)
					return;
				_lastImageId = value;
				EditorApplication.delayCall += () => OnLastImageIdChanged?.Invoke(value);
			}
		}

		private string _lastImageId;

		public bool HasImage => IsRunning || LastBuildImageId?.Length > 0;
		public bool HasBuildDirectory => Directory.Exists(Path.GetFullPath(_buildPath));

		public Action<bool> OnIsBuildingChanged;
		public Action<string> OnLastImageIdChanged;

		private string _buildPath;

		public void ForwardEventsTo(MicroserviceBuilder oldBuilder)
		{
			if (oldBuilder == null)
				return;
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
			var beamable = await EditorAPI.Instance;
			var secret = await beamable.GetRealmSecret();
			var cid = beamable.CustomerView.Cid;
			var connectionStrings =
				await Microservices.GetConnectionStringEnvironmentVariables((MicroserviceDescriptor)Descriptor);
			return new RunServiceCommand((MicroserviceDescriptor)Descriptor, cid, secret, connectionStrings);
		}

		public async Task TryToBuild(bool includeDebuggingTools)
		{
			if (IsBuilding)
				return;

			IsBuilding = true;
			var command = new BuildImageCommand((MicroserviceDescriptor)Descriptor, includeDebuggingTools);
			command.OnStandardOut += message => MicroserviceLogHelper.HandleBuildCommandOutput(this, message);
			command.OnStandardErr += message => MicroserviceLogHelper.HandleBuildCommandOutput(this, message);
			try
			{
				await command.Start(null);
				await TryToGetLastImageId();
			}
			finally
			{
				IsBuilding = false;
			}
		}

		public async Task TryToGetLastImageId()
		{
			var getChecksum = new GetImageIdCommand(Descriptor);
			try
			{
				LastBuildImageId = await getChecksum.Start(null);
			}
			catch (Exception e)
			{
				System.Console.WriteLine(e);
				throw;
			}
		}

		public async Task TryToBuildAndRestart(bool includeDebuggingTools)
		{
			await TryToBuild(includeDebuggingTools);
			await TryToRestart();
		}

		public async Task TryToBuildAndStart(bool includeDebuggingTools)
		{
			await TryToBuild(includeDebuggingTools);
			await TryToStart();
		}
	}
}
