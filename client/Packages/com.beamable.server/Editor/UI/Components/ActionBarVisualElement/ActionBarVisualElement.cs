using Beamable.Server.Editor;
using Beamable.Server.Editor.DockerCommands;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class ActionBarVisualElement : MicroserviceComponent
	{
		public new class UxmlFactory : UxmlFactory<ActionBarVisualElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{ name = "custom-text", defaultValue = "nada" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as ActionBarVisualElement;

			}
		}

		public ActionBarVisualElement() : base(nameof(ActionBarVisualElement))
		{
		}

		public event Action OnStartAllClicked;
		public event Action OnPublishClicked;
		public event Action OnRefreshButtonClicked;
		public event Action<ServiceType> OnCreateNewClicked;
		private Button _refreshButton;
		private Button _createNew;
		private Button _startAll;
		private Button _infoButton;
		private Button _publish;

		public event Action OnInfoButtonClicked;

		public override void Refresh()
		{
			base.Refresh();
			_refreshButton = Root.Q<Button>("refreshButton");
			_refreshButton.clickable.clicked += () => { OnRefreshButtonClicked?.Invoke(); };
			_refreshButton.tooltip = "Refresh Window";
			_createNew = Root.Q<Button>("createNew");

			var manipulator = new ContextualMenuManipulator(PopulateCreateMenu);
			manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
			_createNew.clickable.activators.Clear();
			_createNew.AddManipulator(manipulator);

			_createNew.SetEnabled(!DockerCommand.DockerNotInstalled);

			_startAll = Root.Q<Button>("startAll");
			_startAll.clickable.clicked += () => { OnStartAllClicked?.Invoke(); };
			_startAll.SetEnabled(!DockerCommand.DockerNotInstalled);

			const string cannotPublishText = "Cannot open Publish Window, fix compilation errors first!";
			_publish = Root.Q<Button>("publish");
			_publish.clickable.clicked += () =>
			{
				if (!NoErrorsValidator.LastCompilationSucceded)
				{
					Debug.LogError(cannotPublishText);
					return;
				}
				OnPublishClicked?.Invoke();
			};
			if (!NoErrorsValidator.LastCompilationSucceded)
				_publish.tooltip = cannotPublishText;
			_publish.SetEnabled(!(DockerCommand.DockerNotInstalled));

			_infoButton = Root.Q<Button>("infoButton");
			_infoButton.clickable.clicked += () => { OnInfoButtonClicked?.Invoke(); };
			_infoButton.tooltip = "Open Documentation";
		}

		public void UpdateButtonsState(int selectedServicesAmount, int servicesAmount)
		{
			bool anyModelSelected = selectedServicesAmount > 0;
			UpdateTextButtonTexts(selectedServicesAmount == servicesAmount);
			_startAll.SetEnabled(anyModelSelected);
			_publish.SetEnabled(servicesAmount > 0);
		}

		private void PopulateCreateMenu(ContextualMenuPopulateEvent evt)
		{
			evt.menu.BeamableAppendAction("Microservice", pos => OnCreateNewClicked?.Invoke(ServiceType.MicroService));
			evt.menu.BeamableAppendAction("Storage", pos => OnCreateNewClicked?.Invoke(ServiceType.StorageObject));
		}

		private void UpdateTextButtonTexts(bool allServicesSelected)
		{
			var startLabel = _startAll.Q<Label>();
			startLabel.text = allServicesSelected ? "Play all" : "Play selected";
		}
	}


}
