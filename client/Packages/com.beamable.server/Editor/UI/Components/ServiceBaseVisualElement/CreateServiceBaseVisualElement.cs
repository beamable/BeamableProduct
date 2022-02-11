using Beamable.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
	public abstract class CreateServiceBaseVisualElement : MicroserviceComponent
	{
		protected CreateServiceBaseVisualElement() : base(nameof(ServiceBaseVisualElement))
		{
		}

		protected abstract string NewServiceName { get; set; }
		protected abstract string ScriptName { get; }

		public event Action OnCreateServiceClicked;

		protected ServiceCreateDependentService _serviceCreateDependentService;

		private const int MAX_NAME_LENGTH = 28;

		private TextField _nameTextField;
		private Button _cancelBtn;
		private Button _buildDropDownBtn;
		private LabeledCheckboxVisualElement _checkbox;
		private PrimaryButtonVisualElement _createBtn;
		private VisualElement _logContainerElement;
		private List<string> _servicesNames;
		private VisualElement _rootVisualElement;


		private FormConstraint _isNameValid;
		private FormConstraint _isNameSizedRight;

		public override void Refresh()
		{
			base.Refresh();
			QueryVisualElements();
			InjectStyleSheets();
			UpdateVisualElements();
		}
		protected virtual void QueryVisualElements()
		{
			_rootVisualElement = Root.Q<VisualElement>("mainVisualElement");
			Root.Q("dependentServicesContainer")?.RemoveFromHierarchy();
			Root.Q("collapseContainer")?.RemoveFromHierarchy();
			_cancelBtn = Root.Q<Button>("cancelBtn");

			var stopButton = Root.Q<Button>("stopBtn");
			stopButton.parent.Remove(stopButton);
			_createBtn = new PrimaryButtonVisualElement();
			_cancelBtn.parent.Add(_createBtn);
			_createBtn.Refresh();

			_buildDropDownBtn = Root.Q<Button>("buildDropDown");
			_checkbox = Root.Q<LabeledCheckboxVisualElement>("checkbox");
			_logContainerElement = Root.Q<VisualElement>("logContainer");
			_nameTextField = Root.Q<TextField>("microserviceNewTitle");
			_nameTextField.SetValueWithoutNotify(NewServiceName);

			_isNameValid = _nameTextField.AddErrorLabel("Name", PrimaryButtonVisualElement.IsValidClassName);
			_isNameSizedRight = _nameTextField.AddErrorLabel(
				"Length", txt => PrimaryButtonVisualElement.IsBetweenCharLength(txt, MAX_NAME_LENGTH));
			_createBtn.AddGateKeeper(_isNameValid);
		}
		private void InjectStyleSheets()
		{
			if (string.IsNullOrWhiteSpace(ScriptName)) return;
			_rootVisualElement.AddStyleSheet($"{Constants.COMP_PATH}/{ScriptName}/{ScriptName}.uss");
			_rootVisualElement.AddStyleSheet($"{Constants.COMP_PATH}/ServiceBaseVisualElement/CreateService.uss");
		}
		protected virtual void UpdateVisualElements()
		{
			_servicesNames = MicroservicesDataModel.Instance.AllLocalServices.Select(x => x.Descriptor.Name).ToList();

			ShowServiceCreateDependentService();
			_nameTextField.maxLength = MAX_NAME_LENGTH;
			_nameTextField.RegisterCallback<FocusEvent>(HandleNameLabelFocus, TrickleDown.TrickleDown);
			_nameTextField.RegisterCallback<KeyUpEvent>(HandleNameLabelKeyUp, TrickleDown.TrickleDown);

			_cancelBtn.clickable.clicked += Root.RemoveFromHierarchy;
			_createBtn.SetText("Create");
			_createBtn.Button.clickable.clicked += HandleContinueButtonClicked;

			_buildDropDownBtn.RemoveFromHierarchy();

			_checkbox.Refresh();
			_checkbox.SetWithoutNotify(false);
			_checkbox.SetEnabled(false);
			_checkbox.DisableLabel();

			_logContainerElement.RemoveFromHierarchy();
			RenameGestureBegin();
		}

		private void ShowServiceCreateDependentService()
		{
			if (!ShouldShowCreateDependentService) return;
			_serviceCreateDependentService = new ServiceCreateDependentService();
			_serviceCreateDependentService.Refresh();
			InitCreateDependentService();
			_createBtn.parent.parent.parent.Insert(3, _serviceCreateDependentService);
		}

		private void HandleContinueButtonClicked()
		{
			var additionalReferences = _serviceCreateDependentService?.GetReferences();
			_createBtn.SetText("Creating...");
			_createBtn.Load(new Promise()); // spin forever, because a re-compile will save us!
			NewServiceName = _nameTextField.value;
			OnCreateServiceClicked?.Invoke();
			EditorApplication.delayCall += () => CreateService(NewServiceName, additionalReferences);
		}

		protected abstract void CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null);
		protected abstract void InitCreateDependentService();
		protected abstract bool ShouldShowCreateDependentService { get; }

		private void HandleNameLabelFocus(FocusEvent evt)
		{
			_nameTextField.SelectAll();
		}
		private void HandleNameLabelKeyUp(KeyUpEvent evt)
		{
			if ((evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return) && _isNameValid.IsValid && _isNameSizedRight.IsValid)
			{
				HandleContinueButtonClicked();
				return;
			}
		}
		private void RenameGestureBegin()
		{
			NewServiceName = _nameTextField.value;
			_nameTextField.SetEnabled(true);
			_nameTextField.BeamableFocus();
		}
	}
}
