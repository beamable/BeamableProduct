using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components.DockerLoginWindow;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class StorageObjectVisualElement : ServiceBaseVisualElement
	{
		public new class UxmlFactory : UxmlFactory<StorageObjectVisualElement, UxmlTraits> { }

		protected override string ScriptName => nameof(StorageObjectVisualElement);

		private MongoStorageModel _mongoStorageModel;

		protected override void OnDestroy()
		{
			base.OnDestroy();

			if (_mongoStorageModel == null) return;

			_mongoStorageModel.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
			_mongoStorageModel.ServiceBuilder.OnBuildingFinished -= OnBuildingFinished;
			_mongoStorageModel.ServiceBuilder.OnIsRunningChanged -= OnIsRunningChanged;
		}

		protected override void UpdateVisualElements()
		{
			base.UpdateVisualElements();
			_mongoStorageModel.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
			_mongoStorageModel.OnRemoteReferenceEnriched += OnServiceReferenceChanged;
			_mongoStorageModel.ServiceBuilder.OnBuildingFinished -= OnBuildingFinished;
			_mongoStorageModel.ServiceBuilder.OnBuildingFinished += OnBuildingFinished;
			_mongoStorageModel.ServiceBuilder.OnIsRunningChanged -= OnIsRunningChanged;
			_mongoStorageModel.ServiceBuilder.OnIsRunningChanged += OnIsRunningChanged;
		}

		protected override void UpdateLocalStatus()
		{
			base.UpdateLocalStatus();
			UpdateLocalStatusIcon(_mongoStorageModel.IsRunning, false);
		}

		private void OnIsRunningChanged(bool isRunning)
		{
			UpdateRemoteStatusIcon();
			UpdateLocalStatus();
		}

		private void OnBuildingFinished(bool isFinished)
		{
			UpdateRemoteStatusIcon();
			UpdateLocalStatus();
		}

		private void OnServiceReferenceChanged(ServiceStorageReference serviceReference)
		{
			UpdateRemoteStatusIcon();
		}

		protected override void UpdateRemoteStatusIcon()
		{
			_remoteStatusIcon.ClearClassList();
			string statusClassName;

			if (_mongoStorageModel.RemoteReference?.enabled ?? false)
			{
				statusClassName = "remoteEnabled";
				_remoteStatusLabel.text = REMOTE_ENABLED;
			}
			else
			{
				statusClassName = "remoteDisabled";
				_remoteStatusLabel.text = REMOTE_NOT_ENABLED;
			}

			_remoteStatusIcon.tooltip = _remoteStatusLabel.text;
			_remoteStatusIcon.AddToClassList(statusClassName);
		}

		protected override void SetupProgressBarForStart(Task _)
		{
			// left blank for no loading bar
		}

		protected override void SetupProgressBarForStop(Task _)
		{
			// left blank for no loading bar
		}

		protected override void QueryVisualElements()
		{
			base.QueryVisualElements();
			Root.Q("buildDropDown")?.RemoveFromHierarchy();
			_mongoStorageModel = (MongoStorageModel)Model;
		}
	}
}
