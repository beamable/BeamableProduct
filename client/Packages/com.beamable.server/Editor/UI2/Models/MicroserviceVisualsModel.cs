using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor.Usam;
using System;
using UnityEngine;
using UnityEngine.Serialization;
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

		public bool AreLogsAttached { get; set; }
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
		[FormerlySerializedAs("name")] [SerializeField] public string _name;

		private void HandleLogMessage(string arg1, BeamTailLogMessage arg2)
		{
			if (arg1 == _name && !string.IsNullOrWhiteSpace(arg2.message))
			{
				Logs.AddMessage(FromBeamTailLog(arg2));
			}
		}

		public void Disconnect()
		{
			BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OnLogMessage -= HandleLogMessage;
		}

		public void ConnectToLogMessages()
		{
			BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OnLogMessage -= HandleLogMessage;
			BeamEditorContext.Default.ServiceScope.GetService<CodeService>().OnLogMessage += HandleLogMessage;
		}

		static LogMessage FromBeamTailLog(BeamTailLogMessage message)
		{
			LogLevel logLevel;
			switch (message.logLevel.ToLowerInvariant())
			{
				case "debug":
					logLevel = LogLevel.DEBUG;
					break;
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

			return new LogMessage() { Message = message.message, Level = logLevel, Timestamp = message.timeStamp };
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
			evt.menu.AppendAction("TEST ACTION", action => {});
		}

	}
}
