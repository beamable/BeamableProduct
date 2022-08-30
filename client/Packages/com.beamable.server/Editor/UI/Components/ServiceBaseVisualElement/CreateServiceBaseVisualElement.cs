using Beamable.Common;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Services;
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

		private static readonly string[] ElementsToRemove = {
			"collapseContainer", "statusIcon", "remoteStatusIcon", "moreBtn", "startBtn"
		};

		private TextField _nameTextField;
		private Button _cancelBtn;
		private LabeledCheckboxVisualElement _checkbox;
		private PrimaryButtonVisualElement _createBtn;
		private VisualElement _logContainerElement;
		private VisualElement _rootVisualElement;


		private FormConstraint _isNameValid;
		private FormConstraint _isNameSizedRight;
		private FormConstraint _isNameUnique;

		public override void Refresh()
		{
			base.Refresh();
			QueryVisualElements();
			InjectStyleSheets();
			UpdateVisualElements();
		}
		protected virtual void QueryVisualElements()
		{
			foreach (string element in ElementsToRemove)
				Root.Q(element)?.RemoveFromHierarchy();

			_rootVisualElement = Root.Q<VisualElement>("mainVisualElement");
			_cancelBtn = Root.Q<Button>("cancelBtn");
			_createBtn = new PrimaryButtonVisualElement();
			_cancelBtn.parent.Add(_createBtn);
			_createBtn.Refresh();

			_checkbox = Root.Q<LabeledCheckboxVisualElement>("checkbox");
			_logContainerElement = Root.Q<VisualElement>("logContainer");
			_nameTextField = Root.Q<TextField>("microserviceNewTitle");
			_nameTextField.SetValueWithoutNotify(NewServiceName);

			_isNameValid = _nameTextField.AddErrorLabel("Name", PrimaryButtonVisualElement.IsValidClassName, .01);
			_isNameSizedRight = _nameTextField.AddErrorLabel(
				"Length", txt => PrimaryButtonVisualElement.IsBetweenCharLength(txt, MAX_NAME_LENGTH), .01);
			_isNameUnique = _nameTextField.AddErrorLabel("Name", IsNameUnique);
			
			_createBtn.AddGateKeeper(_isNameValid, _isNameSizedRight, _isNameUnique);

			Root.Q("foldContainer").visible = false;
		}
		private void InjectStyleSheets()
		{
			if (string.IsNullOrWhiteSpace(ScriptName)) return;
			_rootVisualElement.AddStyleSheet($"{COMPONENTS_PATH}/{ScriptName}/{ScriptName}.uss");
			_rootVisualElement.AddStyleSheet($"{COMPONENTS_PATH}/ServiceBaseVisualElement/CreateService.uss");
		}
		protected virtual void UpdateVisualElements()
		{
			ShowServiceCreateDependentService();
			_nameTextField.maxLength = MAX_NAME_LENGTH;
			_nameTextField.RegisterCallback<FocusEvent>(HandleNameLabelFocus, TrickleDown.TrickleDown);
			_nameTextField.RegisterCallback<KeyUpEvent>(HandleNameLabelKeyUp, TrickleDown.TrickleDown);

			_cancelBtn.clickable.clicked += Root.RemoveFromHierarchy;
			_createBtn.SetText("Create");
			_createBtn.Button.clickable.clicked += HandleContinueButtonClicked;


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
			if (!_createBtn.CheckGateKeepers())
			{
				return;
			}
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
			if ((evt.keyCode == KeyCode.KeypadEnter || evt.keyCode == KeyCode.Return) && _isNameValid.IsValid && _isNameSizedRight.IsValid && _isNameUnique.IsValid)
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

		private string IsNameUnique(string txt)
		{
			var localServices = MicroservicesDataModel.Instance.AllLocalServices;
			var remoteServices = MicroservicesDataModel.Instance.AllRemoteOnlyServices;

			return localServices.Any(x => string.Equals(x.Name, txt, StringComparison.CurrentCultureIgnoreCase)) || 
			       remoteServices.Any(x => string.Equals(x.Name, txt, StringComparison.CurrentCultureIgnoreCase))
				? "Service name must be unique "
				: null;
		}
	}
}
