using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Editor.UI.Components.ServiceBaseVisualElement;
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
        public event Action OnCreateServiceClicked;

        protected virtual string NewServiceName { get; set; } = "NewService";
        
        private const int MAX_NAME_LENGTH = 32;
        private bool _canCreateService;

        private VisualElement _logListRoot;
        private ListView _listView;
        private string _statusClassName;
        private TextField _nameTextField;
        private Button _popupBtn;
        private Button _moreBtn;
        private BeamableCheckboxVisualElement _checkbox;

        private object _logVisualElement;
        private Button _createBtn;
        private VisualElement _logContainerElement;
        private Label _buildDefaultLabel;

        private Action _defaultBuildAction;
        private LogVisualElement _logElement;
        private List<string> _servicesNames;

        public CreateServiceBaseVisualElement() : base(nameof(ServiceBaseVisualElement))
        {
        }

        // public new class UxmlFactory : UxmlFactory<CreateServiceBaseVisualElement, UxmlTraits>
        // {
        // }
        //
        // public new class UxmlTraits : VisualElement.UxmlTraits
        // {
        //     public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        //     {
        //         get { yield break; }
        //     }
        // }

        public override void Refresh()
        {
            base.Refresh();
            
            _servicesNames = GetServicesNames();
            RegisterCallback<MouseDownEvent>(HandeMouseDownEvent, TrickleDown.TrickleDown);
            
            Root.Q("microserviceTitle")?.RemoveFromHierarchy();
            
            _nameTextField = Root.Q<TextField>("microserviceNewTitle");
            _nameTextField.SetValueWithoutNotify(NewServiceName);
            _nameTextField.maxLength = MAX_NAME_LENGTH;

            _nameTextField.RegisterCallback<FocusEvent>(HandleNameLabelFocus, TrickleDown.TrickleDown);
            _nameTextField.RegisterCallback<KeyUpEvent>(HandleNameLabelKeyUp, TrickleDown.TrickleDown);

            var cancelBtn = Root.Q<Button>("cancelBtn");
            cancelBtn.clickable.clicked += Root.RemoveFromHierarchy;

            _createBtn = Root.Q<Button>("start");
            _createBtn.text = "Create";
            _createBtn.clickable.clicked += HandleCreateButtonClicked;

            var buildDropDown = Root.Q<Button>("buildDropDown");
            buildDropDown.RemoveFromHierarchy();

            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _checkbox.Refresh();
            _checkbox.SetWithoutNotify(false);
            _checkbox.SetEnabled(false);

            _logContainerElement = Root.Q<VisualElement>("logContainer");
            _logContainerElement.RemoveFromHierarchy();
            RenameGestureBegin();
        }
        private List<string> GetServicesNames()
        {
            return new List<string>();
            //return MicroservicesDataModel.Instance.AllServices.Select(x => x.Name).ToList();
        }
        private void HandleCreateButtonClicked()
        {
            if (string.IsNullOrWhiteSpace(NewServiceName)) 
                return;
            _createBtn.text = "Creating...";
            OnCreateServiceClicked?.Invoke();
            CreateService(NewServiceName);
        }
        protected abstract void CreateService(string serviceName);
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
            NewServiceName = _nameTextField.value;
            _nameTextField.SetEnabled(true);
            _nameTextField.BeamableFocus();
            CheckName();
        }
        private void CheckName()
        {
            var newName = _nameTextField.value;
            NewServiceName = newName.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                ? newName
                : NewServiceName;
            _nameTextField.value = NewServiceName;
            _canCreateService = !_servicesNames.Any(s => s.Equals(NewServiceName)) 
                               && NewServiceName.Length > 2 && NewServiceName.Length <= MAX_NAME_LENGTH;
            _createBtn.SetEnabled(_canCreateService);
        }
    }
}