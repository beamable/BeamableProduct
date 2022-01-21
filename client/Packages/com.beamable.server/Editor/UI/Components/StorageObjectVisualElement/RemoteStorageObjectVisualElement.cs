using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Server.Editor.UI.Components;
using Beamable.Server.Editor.UI.Components.DockerLoginWindow;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif


namespace Beamable.Editor.Microservice.UI.Components
{
	public class RemoteStorageObjectVisualElement : StorageObjectVisualElement
	{
		public new class UxmlFactory : UxmlFactory<RemoteStorageObjectVisualElement, UxmlTraits>
		{
		}

		protected override string ScriptName => nameof(StorageObjectVisualElement);

		private RemoteMongoStorageModel _mongoStorageModel;

		protected override void UpdateVisualElements()
		{
			Root.Q<VisualElement>("buttonRow").RemoveFromHierarchy();
			Root.Q<VisualElement>("logContainer").RemoveFromHierarchy();
			Root.Q<VisualElement>("dependentServicesContainer").RemoveFromHierarchy();
			Root.Q("collapseContainer")?.RemoveFromHierarchy();

#if UNITY_2019_1_OR_NEWER
			Root.Q<VisualElement>("mainVisualElement").style.height = new StyleLength(DEFAULT_HEADER_HEIGHT);
#elif UNITY_2018
			Root.Q<VisualElement>("mainVisualElement").style.height = StyleValue<float>.Create(DEFAULT_HEADER_HEIGHT);
#endif

			_statusIcon.RemoveFromHierarchy();
			_statusLabel.RemoveFromHierarchy();

			var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
			manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_moreBtn.clickable.activators.Clear();
			_moreBtn.AddManipulator(manipulator);
			_moreBtn.tooltip = "More...";

			_checkbox.Refresh();
			_checkbox.SetText(Model.Name);
			_checkbox.SetWithoutNotify(Model.IsSelected);
			Model.OnSelectionChanged += _checkbox.SetWithoutNotify;
			_checkbox.OnValueChanged += b => Model.IsSelected = b;

			_separator.Refresh();

			UpdateButtons();
			UpdateStatusIcon();
			UpdateRemoteStatusIcon();
			UpdateHeaderColor();
			UpdateModel();
		}

		protected override void QueryVisualElements()
		{
			base.QueryVisualElements();

			_mongoStorageModel = (RemoteMongoStorageModel)Model;
		}

		protected override void UpdateRemoteStatusIcon()
		{
			_remoteStatusIcon.ClearClassList();
			string statusClassName = "remoteEnabled";
			_remoteStatusLabel.text = Constants.REMOTE_ONLY;
			_remoteStatusIcon.tooltip = _remoteStatusLabel.text;
			_remoteStatusIcon.AddToClassList(statusClassName);
		}

	}
}
