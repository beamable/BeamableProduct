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
    public class RemoteMicroserviceVisualElement : ServiceBaseVisualElement
    {
        public new class UxmlFactory : UxmlFactory<RemoteMicroserviceVisualElement, UxmlTraits>
        { }

        private RemoteMicroserviceModel _microserviceModel;

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_microserviceModel == null) return;


            _microserviceModel.OnDockerLoginRequired -= LoginToDocker;
            _microserviceModel.ServiceBuilder.OnLastImageIdChanged -= HandleLastImageIdChanged;
        }

        protected override void UpdateVisualElements()
        {
            Root.Q<Button>("buildDropDown").RemoveFromHierarchy();
            Root.Q<VisualElement>("buttonRow").RemoveFromHierarchy();
            Root.Q<VisualElement>("logContainer").RemoveFromHierarchy();
            Root.Q<VisualElement>("mainVisualElement").style.height = StyleValue<float>.Create(DEFAULT_HEADER_HEIGHT);
            Root.Q<Label>("microserviceTitle").text = Model.Name;

            _statusIcon.RemoveFromHierarchy();
            _statusLabel.RemoveFromHierarchy();

            var manipulator = new ContextualMenuManipulator(Model.PopulateMoreDropdown);
            manipulator.activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            _moreBtn.clickable.activators.Clear();
            _moreBtn.AddManipulator(manipulator);
            _moreBtn.tooltip = "More...";

            _checkbox.Refresh();
            _checkbox.SetWithoutNotify(Model.IsSelected);
            Model.OnSelectionChanged += _checkbox.SetWithoutNotify;
            _checkbox.OnValueChanged += b => Model.IsSelected = b;

            _microserviceModel.OnDockerLoginRequired -= LoginToDocker;
            _microserviceModel.OnDockerLoginRequired += LoginToDocker;

            _microserviceModel.ServiceBuilder.OnLastImageIdChanged -= HandleLastImageIdChanged;
            _microserviceModel.ServiceBuilder.OnLastImageIdChanged += HandleLastImageIdChanged;
            _microserviceModel.OnRemoteReferenceEnriched -= OnServiceReferenceChanged;
            _microserviceModel.OnRemoteReferenceEnriched += OnServiceReferenceChanged;

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

            _microserviceModel = (RemoteMicroserviceModel)Model;
        }

        private void LoginToDocker(Promise<Unit> onLogin)
        {
            DockerLoginVisualElement.ShowUtility().Then(onLogin.CompleteSuccess).Error(onLogin.CompleteError);
        }

        private void HandleLastImageIdChanged(string newId)
        {
            UpdateButtons();
        }

        private void OnServiceReferenceChanged(ServiceReference serviceReference)
        {
            UpdateRemoteStatusIcon();
        }

        protected override void UpdateRemoteStatusIcon()
        {
            _remoteStatusIcon.ClearClassList();
            string statusClassName = "remoteEnabled";
            _remoteStatusLabel.text = Constants.REMOTE_ONLY;
            _remoteStatusIcon.tooltip = _remoteStatusLabel.text;
            _remoteStatusIcon.AddToClassList(statusClassName);
        }

        protected override void UpdateStatusIcon()
        {

        }
    }
}