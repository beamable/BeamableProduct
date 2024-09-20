using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.Usam;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI2.Models
{
	public interface IServiceLogsVisualModel
	{
		bool AreLogsAttached { get; }
		Action OnLogsDetached { get; set; }
		Action OnLogsAttached { get; set; }
		LogMessageStore Logs { get; }

		void DetachLogs();
		void AttachLogs();
		Action<bool> OnLogsAttachmentChanged { get; set; }
		void PopulateMoreDropdown(ContextualMenuPopulateEvent evt);
	}

	public interface IMicroserviceVisualsModel : IServiceLogsVisualModel
	{
		float ElementHeight { get; set; }
		bool IsSelected { get; set; }
		bool IsCollapsed { get; set; }

		string Name { get; }
		Action<bool> OnSelectionChanged { get; set; }
		Action OnSortChanged { get; set; }
	}

	[Serializable]
	public class MicroserviceVisualsModel : IMicroserviceVisualsModel
	{
		private const float DEFAULT_HEIGHT = 300.0f;

		public bool AreLogsAttached
		{
			get => _areLogsAttached;
			protected set => _areLogsAttached = value;
		}

		public string Name => _name;

		public float ElementHeight
		{
			get => _visualHeight;
			set => _visualHeight = value;
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnSelectionChanged?.Invoke(value);
			}
		}

		public bool IsCollapsed
		{
			get => _isCollapsed;
			set => _isCollapsed = value;
		}

		public Action OnLogsDetached { get; set; }
		public Action OnLogsAttached { get; set; }
		public Action<bool> OnLogsAttachmentChanged { get; set; }
		public Action<bool> OnSelectionChanged { get; set; }
		public Action OnSortChanged { get; set; }
		public LogMessageStore Logs => _logs;

		[SerializeField] private LogMessageStore _logs = new LogMessageStore();
		[SerializeField] private bool _isSelected;
		[SerializeField] private bool _isCollapsed = false;
		[SerializeField] private float _visualHeight = DEFAULT_HEIGHT;
		[SerializeField] public string _name;
		[SerializeField] private bool _areLogsAttached = true;
		private IBeamoServiceDefinition _serviceDefinition;

		private void HandleLogMessage(string serviceName, BeamTailLogMessageForClient log)
		{
			if (serviceName == _name && !string.IsNullOrWhiteSpace(log.message))
			{
				Logs.AddMessage(FromBeamTailLog(log));
			}
		}

		public void Disconnect()
		{
			BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OnLogMessage -= HandleLogMessage;
		}

		public void Connect()
		{
			_serviceDefinition = BeamEditorContext.Default.ServiceScope.GetService<CodeService>().ServiceDefinitions
							 .FirstOrDefault(def => def.ServiceInfo.name == Name);
			BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OnLogMessage -= HandleLogMessage;
			BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OnLogMessage += HandleLogMessage;
		}

		static LogMessage FromBeamTailLog(BeamTailLogMessageForClient message)
		{
			LogLevel logLevel;
			var ts = message.timeStamp;
			if (DateTimeOffset.TryParse(ts, out var dto))
			{
				ts = dto.ToLocalTime().ToString("[HH:mm:ss]");
			}
			// DateTimeOffset.FromUnixTimeMilliseconds(log.data.timestamp).ToLocalTime().ToString("T")
			// message.timeStamp = 
			switch (message.logLevel.ToLowerInvariant())
			{
				case "verbose":
					logLevel = LogLevel.VERBOSE;
					break;
				case "debug":
					logLevel = LogLevel.DEBUG;
					break;
				case "information":
				case "info":
					logLevel = LogLevel.INFO;
					break;
				case "warning":
					logLevel = LogLevel.WARNING;
					break;
				case "fatal":
					logLevel = LogLevel.FATAL;
					break;
				default:
					logLevel = LogLevel.ERROR;
					break;
			}

			return new LogMessage() { Message = message.message, Level = logLevel, Timestamp = ts };
		}

		public void DetachLogs()
		{
			if (!AreLogsAttached) return;

			AreLogsAttached = false;
			OnLogsDetached?.Invoke();
			OnLogsAttachmentChanged?.Invoke(false);
		}

		public void AttachLogs()
		{
			if (AreLogsAttached) return;
			AreLogsAttached = true;
			OnLogsAttached?.Invoke();
			OnLogsAttachmentChanged?.Invoke(true);
		}

		public virtual void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
		{

			var existsOnRemote = _serviceDefinition.IsRunningOnRemote == BeamoServiceStatus.Running;
			var imageSuffix = !string.IsNullOrWhiteSpace(_serviceDefinition.ImageId) ? string.Empty : " (Build first)";
			var localCategory = _serviceDefinition.IsRunningLocally ? "Local" : "Local (not running)";
			var remoteCategory = existsOnRemote ? "Cloud" : "Cloud (not deployed)";
			evt.menu.BeamableAppendAction($"Reveal build directory{imageSuffix}", pos =>
			{
				var full = Path.GetFullPath(_serviceDefinition.ServiceInfo.projectPath);
				EditorUtility.RevealInFinder(full);
			});
			if (_serviceDefinition.ExistLocally)
			{
				evt.menu.BeamableAppendAction($"{localCategory}/View Documentation", pos => OpenDocs(false),
											  _serviceDefinition.IsRunningLocally);
				if (_serviceDefinition.ServiceInfo != null)
				{
					evt.menu.BeamableAppendAction($"{localCategory}/Regenerate {_serviceDefinition.BeamoId}Client.cs",
												  pos =>
												  {
													  BeamEditorContext
														  .Default.ServiceScope.GetService<CodeService>()
														  .GenerateClientCode(_name)
														  .Then(_ => { });
												  });
				}
			}

			evt.menu.BeamableAppendAction($"{remoteCategory}/View Documentation", pos => { OpenOnRemote("docs/"); }, existsOnRemote);
			evt.menu.BeamableAppendAction($"{remoteCategory}/View Metrics", pos => { OpenOnRemote("metrics"); }, existsOnRemote);
			evt.menu.BeamableAppendAction($"{remoteCategory}/View Logs", pos => { OpenDocs(true); }, existsOnRemote);
			evt.menu.BeamableAppendAction($"Open C# Code", _ =>
			{
				BeamEditorContext.Default.ServiceScope.GetService<CodeService>()
								 .OpenMicroserviceFile(_serviceDefinition.BeamoId);
			});
			evt.menu.BeamableAppendAction("Open Microservice Settings", pos =>
			{
				SettingsService.OpenProjectSettings($"Project/Beamable Services/{_serviceDefinition.BeamoId}");
			});

			if (!AreLogsAttached)
			{
				evt.menu.BeamableAppendAction($"Reattach Logs", pos => AttachLogs());
			}
		}


		protected void OpenRemoteDocs() => OpenOnRemote("docs");
		protected void OpenRemoteMetrics() => OpenOnRemote("metrics");
		protected void OpenOnRemote(string relativePath)
		{
			var api = BeamEditorContext.Default;
			var path =
				$"{BeamableEnvironment.PortalUrl}/{api.CurrentCustomer.Alias}/" +
				$"games/{api.ProductionRealm.Pid}/realms/{api.CurrentRealm.Pid}/" +
				$"microservices/{_serviceDefinition.BeamoId}/{relativePath}?refresh_token={api.Requester.Token.RefreshToken}";
			Application.OpenURL(path);
		}

		private void OpenDocs(bool remote)
		{
			if (_serviceDefinition.IsRunningLocally)
			{
				BeamEditorContext.Default.ServiceScope.GetService<CodeService>()
								 .OpenSwagger(_serviceDefinition.BeamoId, remote).Then(_ => { });
			}
		}
	}
}
