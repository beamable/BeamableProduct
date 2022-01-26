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
		private bool _canCreateService;

		private TextField _nameTextField;
		private Button _cancelBtn;
		private Button _buildDropDownBtn;
		private LabeledCheckboxVisualElement _checkbox;
		private Button _createBtn;
		private VisualElement _logContainerElement;
		private List<string> _servicesNames;
		private VisualElement _rootVisualElement;

		private bool _isServiceNameConfirmed;

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
			_createBtn = Root.Q<Button>("stopBtn");
			_buildDropDownBtn = Root.Q<Button>("buildDropDown");
			_checkbox = Root.Q<LabeledCheckboxVisualElement>("checkbox");
			_logContainerElement = Root.Q<VisualElement>("logContainer");
			_nameTextField = Root.Q<TextField>("microserviceNewTitle");
		}
		private void InjectStyleSheets()
		{
			if (string.IsNullOrWhiteSpace(ScriptName)) return;
			_rootVisualElement.AddStyleSheet($"{Constants.COMP_PATH}/{ScriptName}/{ScriptName}.uss");
		}
		protected virtual void UpdateVisualElements()
		{
			_servicesNames = MicroservicesDataModel.Instance.AllLocalServices.Select(x => x.Descriptor.Name).ToList();
			RegisterCallback<MouseDownEvent>(HandeMouseDownEvent, TrickleDown.TrickleDown);

			_nameTextField.SetValueWithoutNotify(NewServiceName);
			_nameTextField.maxLength = MAX_NAME_LENGTH;
			_nameTextField.RegisterCallback<FocusEvent>(HandleNameLabelFocus, TrickleDown.TrickleDown);
			_nameTextField.RegisterCallback<KeyUpEvent>(HandleNameLabelKeyUp, TrickleDown.TrickleDown);

			_cancelBtn.clickable.clicked += Root.RemoveFromHierarchy;
			_createBtn.text = "Create";
			_createBtn.clickable.clicked += HandleCreateButtonClicked;

			_buildDropDownBtn.RemoveFromHierarchy();

			_checkbox.Refresh();
			_checkbox.SetWithoutNotify(false);
			_checkbox.SetEnabled(false);
			_checkbox.DisableLabel();

			_logContainerElement.RemoveFromHierarchy();
			RenameGestureBegin();
		}
		protected virtual void HandleCreateButtonClicked()
		{
			if (string.IsNullOrWhiteSpace(NewServiceName))
				return;

			_isServiceNameConfirmed = true;
			_nameTextField.SetEnabled(false);

			if (!ShouldShowCreateDependentService)
			{
				HandleContinueButtonClicked();
				return;
			}

			_createBtn.clickable.clicked -= HandleCreateButtonClicked;
			_createBtn.text = "Continue";
			_createBtn.clickable.clicked += HandleContinueButtonClicked;
			ShowServiceCreateDependentService();
		}

		private void ShowServiceCreateDependentService()
		{
			_serviceCreateDependentService = new ServiceCreateDependentService();
			_serviceCreateDependentService.Refresh();
			InitCreateDependentService();
			_rootVisualElement.Add(_serviceCreateDependentService);
		}

		private void HandleContinueButtonClicked()
		{
			var additionalReferences = _serviceCreateDependentService?.GetReferences();
			_createBtn.text = "Creating...";
			OnCreateServiceClicked?.Invoke();
			EditorApplication.delayCall += () => CreateService(NewServiceName, additionalReferences);
		}

		protected abstract void CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null);
		protected abstract void InitCreateDependentService();
		protected abstract bool ShouldShowCreateDependentService { get; }

		private void HandeMouseDownEvent(MouseDownEvent evt)
		{
			RenameGestureBegin();
		}
		private void HandleNameLabelFocus(FocusEvent evt)
		{
			_nameTextField.SelectAll();
		}
		private void HandleNameLabelKeyUp(KeyUpEvent evt)
		{
			if ((evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return) && _canCreateService)
			{
				HandleCreateButtonClicked();
				return;
			}
			CheckName();
		}
		private void RenameGestureBegin()
		{
			if (_isServiceNameConfirmed)
				return;

			NewServiceName = _nameTextField.value;
			_nameTextField.SetEnabled(true);
			_nameTextField.BeamableFocus();
			CheckName();
		}
		private void CheckName()
		{
			var newName = _nameTextField.value;
			if (Regex.IsMatch(newName, @"^[a-zA-Z]+$"))
			{
				NewServiceName = newName;
			}
			_nameTextField.value = NewServiceName;
			_canCreateService = !_servicesNames.Contains(NewServiceName)
													&& NewServiceName.Length > 2
													&& NewServiceName.Length <= MAX_NAME_LENGTH;
			_createBtn.SetEnabled(_canCreateService);
		}
	}
}
