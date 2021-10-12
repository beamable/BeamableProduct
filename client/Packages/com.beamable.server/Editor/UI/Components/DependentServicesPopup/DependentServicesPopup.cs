using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
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
    public class DependentServicesPopup : MicroserviceComponent
    {
        public Dictionary<MicroserviceModel, DependentServicesMicroserviceEntryVisualElement> DependentServices { get; private set; }
        public bool IsAnyRelationChanged { get; private set; } = false;
        
        public Action OnClose;
        public Action OnConfirm;

        private VisualElement _storageObjectsContainer;
        private VisualElement _microservicesContainer;
        private PrimaryButtonVisualElement _confirmButton;
        private Button _cancelButton;

        private MicroserviceModel _lastRelactionChangedMicroservice;
        private MongoStorageModel _lastRelactionChangedStorageObject;

        public DependentServicesPopup() : base(nameof(DependentServicesPopup))
        {
        }
        public override void Refresh()
        {
            base.Refresh();
            QueryVisualElements();
            UpdateVisualElements();
        }
        private void QueryVisualElements()
        {
            _confirmButton = Root.Q<PrimaryButtonVisualElement>("confirmBtn");
            _cancelButton = Root.Q<Button>("cancelBtn");
            _storageObjectsContainer = Root.Q("storageObjectsContainer");
            _microservicesContainer = Root.Q("microservicesContainer");
        }
        private void UpdateVisualElements()
        {
            _confirmButton.Button.clickable.clicked += () => OnConfirm?.Invoke();
            _cancelButton.clickable.clicked += () => OnClose?.Invoke();
            
            SetStorageObjectsContainer();
            SetMicroservicesContainer();
        }
        public void PrepareParent()
        {
            parent.name = "PublishWindowContainer";
            parent.AddStyleSheet(USSPath);
        }
        private void SetStorageObjectsContainer()
        {
            foreach (var storageObjectModel in MicroservicesDataModel.Instance.Storages)
            {
                var newElement = new DependentServicesStorageObjectEntryVisualElement { Model = storageObjectModel };
                newElement.Refresh();
                _storageObjectsContainer.Add(newElement);
            }
        }
        private void SetMicroservicesContainer()
        {
            DependentServices = new Dictionary<MicroserviceModel, DependentServicesMicroserviceEntryVisualElement>(MicroservicesDataModel.Instance.Services.Count);
            foreach (var microserviceModel in MicroservicesDataModel.Instance.Services)
            {
                var newElement = new DependentServicesMicroserviceEntryVisualElement { Model = microserviceModel };
                newElement.Refresh();
                newElement.OnServiceRelationChanged += storageObjectModel => HandleServiceRelationChange(microserviceModel, storageObjectModel);
                _microservicesContainer.Add(newElement);
                DependentServices.Add(microserviceModel, newElement);
            }
        }
        public void SetServiceDependencies()
        {
            foreach (var service in DependentServices)
            {
                var microservice = service.Key;
                microservice.Dependencies.Clear();
                
                foreach (var dependentService in service.Value.DependentServices)
                {
                    if (!dependentService.IsServiceRelation)
                        continue;
                    microservice.Dependencies.Add(dependentService.MongoStorageModel);
                }
            }
        }
        private void HandleServiceRelationChange(MicroserviceModel microserviceModel ,MongoStorageModel storageObjectModel)
        {
            // TODO - Selection in USS
 
            _lastRelactionChangedMicroservice = microserviceModel;
            _lastRelactionChangedStorageObject = storageObjectModel;
            IsAnyRelationChanged = true;
        }
    }
}