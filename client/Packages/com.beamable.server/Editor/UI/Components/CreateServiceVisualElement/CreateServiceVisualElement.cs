using Beamable.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateServiceVisualElement : MicroserviceComponent
	{
		public CreateServiceVisualElement() : base(nameof(CreateServiceVisualElement)) { }

		public string NewServiceName { get; set; }
		public string ScriptName { get; }
		public ServiceType ServiceType { get; } = SERVICE_TYPE;

		public Action OnClose;
		public event Action OnCreateServiceClicked;
		public event Action OnCreateServiceFinished;

		protected ServiceCreateDependentService _serviceCreateDependentService;

		private const int MAX_NAME_LENGTH = 28;

		private VisualElement _serviceIcon;
		private TextField _nameTextField;
		private PrimaryButtonVisualElement _createBtn;
		private PrimaryButtonVisualElement _cancelBtn;

		private FormConstraint _isNameValid;
		private FormConstraint _isNameSizedRight;
		private FormConstraint _isNameUnique;

		private const ServiceType SERVICE_TYPE = ServiceType.MicroService;

		public void Refresh(Action onClose)
		{
			OnClose = onClose;
			base.Refresh();
			QueryVisualElements();
			UpdateVisualElements();
		}

		private void QueryVisualElements()
		{
			_serviceIcon = Root.Q<VisualElement>("serviceIcon");
			_nameTextField = Root.Q<TextField>("nameTextField");
			_createBtn = Root.Q<PrimaryButtonVisualElement>("createBtn");
			_cancelBtn = Root.Q<PrimaryButtonVisualElement>("cancelBtn");
		}
		private void UpdateVisualElements()
		{
			_serviceIcon.AddToClassList(ServiceType.ToString());
			_nameTextField.AddPlaceholder("Enter service name");

			_cancelBtn.Button.clicked += () =>
			{
				Root.RemoveFromHierarchy();
				OnClose?.Invoke();
			};
			_createBtn.Button.clicked += () =>
			{
				HandleContinueButtonClicked();
				OnClose?.Invoke();
			};

			_isNameValid = _nameTextField.AddErrorLabel("Name", PrimaryButtonVisualElement.IsValidClassName, .01);
			_isNameSizedRight = _nameTextField.AddErrorLabel(
				"Length", txt => PrimaryButtonVisualElement.IsBetweenCharLength(txt, MAX_NAME_LENGTH), .01);
			_isNameUnique = _nameTextField.AddErrorLabel("Name", IsNameUnique);

			_createBtn.AddGateKeeper(_isNameValid, _isNameSizedRight, _isNameUnique);

			ShowServiceCreateDependentService();
			_nameTextField.maxLength = MAX_NAME_LENGTH;
			_nameTextField.RegisterCallback<FocusEvent>(HandleNameLabelFocus, TrickleDown.TrickleDown);
			_nameTextField.RegisterCallback<KeyUpEvent>(HandleNameLabelKeyUp, TrickleDown.TrickleDown);

			RenameGestureBegin();
		}

		private void ShowServiceCreateDependentService()
		{
			if (!ShouldShowCreateDependentService)
				return;
			_serviceCreateDependentService = new ServiceCreateDependentService();
			_serviceCreateDependentService.Refresh();
			InitCreateDependentService();
			_createBtn.parent.parent.Insert(1, _serviceCreateDependentService);
		}

		private void HandleContinueButtonClicked()
		{
			if (!_createBtn.CheckGateKeepers())
				return;

			var additionalReferences = _serviceCreateDependentService?.GetReferences();
			_createBtn.SetText("Creating...");
			NewServiceName = _nameTextField.value;
			_createBtn.Load(CreateService(NewServiceName, additionalReferences));
			OnCreateServiceClicked?.Invoke();
		}

		private async Promise CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null)
		{
			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
			await codeService.CreateMicroservice(serviceName);
			OnCreateServiceFinished?.Invoke();
		}

		private void InitCreateDependentService()
		{
			//TODO
		}

		private bool ShouldShowCreateDependentService { get; }

		private void HandleNameLabelFocus(FocusEvent evt)
		{
			_nameTextField.SelectAll();
		}
		private void HandleNameLabelKeyUp(KeyUpEvent evt)
		{
			if ((evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return) && _isNameValid.IsValid && _isNameSizedRight.IsValid && _isNameUnique.IsValid)
				HandleContinueButtonClicked();
		}
		private void RenameGestureBegin()
		{
			NewServiceName = _nameTextField.value;
			_nameTextField.SetEnabled(true);
			_nameTextField.BeamableFocus();
		}

		private string IsNameUnique(string txt)
		{
			var localServices = MicroservicesDataModel.Instance.AllLocalServices;
			if (localServices.Any(x => string.Equals(x.Name, txt, StringComparison.CurrentCultureIgnoreCase)))
			{
				return "Service name must be unique.";
			}
			var remoteServices =
				MicroservicesDataModel.Instance.remoteServices.Where(model => !string.IsNullOrWhiteSpace(model.RemoteReference.imageId));

			if (remoteServices.Any(x => string.Equals(x.Name, txt, StringComparison.CurrentCultureIgnoreCase)))
			{
				return "There is already a working remote service with same name.";
			}

			return null; // all looks good
		}
	}
}
