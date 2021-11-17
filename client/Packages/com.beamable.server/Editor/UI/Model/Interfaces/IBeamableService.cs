using Beamable.Common;
using Beamable.Editor.Environment;
using Beamable.Server;
using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using Beamable.Server.Editor.ManagerClient;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Model
{
	public interface IBeamableService
	{
		bool IsRunning
		{
			get;
		}

		bool AreLogsAttached
		{
			get;
		}

		LogMessageStore Logs
		{
			get;
		}

		ServiceType ServiceType
		{
			get;
		}

		IDescriptor Descriptor
		{
			get;
		}

		string Name
		{
			get;
		}

		Action OnLogsDetached
		{
			get;
			set;
		}

		Action OnLogsAttached
		{
			get;
			set;
		}

		Action<bool> OnLogsAttachmentChanged
		{
			get;
			set;
		}

		Action<bool> OnSelectionChanged
		{
			get;
			set;
		}

		Action OnSortChanged
		{
			get;
			set;
		}

		Action<float, long, long> OnDeployProgress
		{
			get;
			set;
		}

		event Action<Task> OnStart;
		event Action<Task> OnStop;

		void Refresh(IDescriptor descriptor);
		void DetachLogs();
		void AttachLogs();
		void PopulateMoreDropdown(ContextualMenuPopulateEvent evt);
		Task Start();
		Task Stop();
	}
}
