using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.UI.Components;
using Beamable.Server.Editor;
using UnityEditor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class CreateMicroserviceVisualElement : MicroserviceComponent
    {
        public event Action OnCreateMicroserviceClicked;

        private VisualElement _logListRoot;
        private ListView _listView;
        private string _statusClassName;

        public CreateMicroserviceVisualElement() : base(nameof(MicroserviceVisualElement))
        {
        }

        public new class UxmlFactory : UxmlFactory<CreateMicroserviceVisualElement, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }
        }

        private TextField _nameTextField;
        string _newMicroserviceName = "NewMicroservice";
        private Button _popupBtn;
        private Button _moreBtn;
        private BeamableCheckboxVisualElement _checkbox;

        private object _logVisualElement;
        private Button _createBtn;
        private VisualElement _logContainerElement;
        private Label _buildDefaultLabel;

        private Action _defaultBuildAction;
        private LogVisualElement _logElement;
        List<string> _microservicesNames;

        public override void Refresh()
        {
            base.Refresh();

            _microservicesNames = Microservices.Descriptors.Select(descriptor => descriptor.Name).ToList();
            RegisterCallback<MouseDownEvent>(OnMouseDownEvent,
                TrickleDown.TrickleDown);

            _nameTextField = Root.Q<TextField>("microserviceTitle");
            _nameTextField.SetValueWithoutNotify(_newMicroserviceName);

            _nameTextField.RegisterCallback<FocusEvent>(NameLabel_OnFocus,
                TrickleDown.TrickleDown);
            _nameTextField.RegisterCallback<KeyUpEvent>(NameLabel_OnKeyup,
                TrickleDown.TrickleDown);

            var cancelBtn = Root.Q<Button>("cancelBtn");
            cancelBtn.clickable.clicked += Root.RemoveFromHierarchy;

            _createBtn = Root.Q<Button>("start");
            _createBtn.text = "Create";
            _createBtn.clickable.clicked +=
                HandleCreateButtonClicked;

            var buildDropDown = Root.Q<Button>("buildDropDown");
            buildDropDown.RemoveFromHierarchy();

            _checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            _checkbox.Refresh();
            _checkbox.SetWithoutNotify(false);
            _checkbox.SetEnabled(false);

            _logContainerElement = Root.Q<VisualElement>("logContainer");
            _logElement = new LogVisualElement();
            _logContainerElement.Add(_logElement);
            _logElement.Refresh();
            _logElement.SetEnabled(false);
            RenameGestureBegin();
        }

        private void HandleCreateButtonClicked()
        {
            _createBtn.text = "Creating...";
            OnCreateMicroserviceClicked?.Invoke();
            EditorApplication.delayCall += () => MicroserviceEditor.CreateNewMicroservice(_newMicroserviceName);
        }

        void OnMouseDownEvent(MouseDownEvent evt)
        {
            RenameGestureBegin();
        }

        void NameLabel_OnFocus(FocusEvent evt)
        {
            _nameTextField.SelectAll();
        }

        void NameLabel_OnKeyup(KeyUpEvent evt)
        {
            CheckName();
        }

        void CheckName()
        {
            var newName = _nameTextField.value;
            _newMicroserviceName = newName.All(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                ? newName
                : _newMicroserviceName;
            _nameTextField.value = _newMicroserviceName;
            bool serviceAlreadyExist = _microservicesNames.Any(s => s.Equals(_newMicroserviceName));
            _createBtn.SetEnabled(!serviceAlreadyExist && _newMicroserviceName.Length > 2);
        }

        void RenameGestureBegin()
        {
            _newMicroserviceName = _nameTextField.value;
            _nameTextField.SetEnabled(true);
            _nameTextField.BeamableFocus();
            CheckName();
        }
    }
}