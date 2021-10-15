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
        public Dictionary<MicroserviceModel, DependentServicesMicroserviceEntryVisualElement> MicroserviceEntries { get; private set; }
        public Dictionary<MongoStorageModel, DependentServicesStorageObjectEntryVisualElement> StorageObjectEntries { get; private set; }
        public bool IsAnyRelationChanged { get; private set; } = false;
        
        public Action OnClose;
        public Action OnConfirm;

        private VisualElement _storageObjectsContainer;
        private VisualElement _microservicesContainer;
        private PrimaryButtonVisualElement _confirmButton;
        private Button _cancelButton;

        private MicroserviceModel _lastRelactionChangedMicroservice;
        private MongoStorageModel _lastRelactionChangedStorageObject;
        private DependentServicesMicroserviceEntryVisualElement _emptyRowFillEntry;
        
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
            StorageObjectEntries = new Dictionary<MongoStorageModel, DependentServicesStorageObjectEntryVisualElement>(MicroservicesDataModel.Instance.Storages.Count);
            foreach (var storageObjectModel in MicroservicesDataModel.Instance.Storages)
            {
                var newElement = new DependentServicesStorageObjectEntryVisualElement { Model = storageObjectModel };
                newElement.Refresh();
                _storageObjectsContainer.Add(newElement);
                StorageObjectEntries.Add(storageObjectModel, newElement);
            }
        }
        private void SetMicroservicesContainer()
        {
            MicroserviceEntries = new Dictionary<MicroserviceModel, DependentServicesMicroserviceEntryVisualElement>(MicroservicesDataModel.Instance.Services.Count);
            foreach (var microserviceModel in MicroservicesDataModel.Instance.Services)
            {
                var newElement = new DependentServicesMicroserviceEntryVisualElement { Model = microserviceModel };
                newElement.Refresh();
                newElement.OnServiceRelationChanged += storageObjectModel => HandleServiceRelationChange(microserviceModel, storageObjectModel);
                _microservicesContainer.Add(newElement);
                MicroserviceEntries.Add(microserviceModel, newElement);
            }

            _emptyRowFillEntry = new DependentServicesMicroserviceEntryVisualElement
            {
                name = "EmptyRowFillEntry", 
                style = { flexGrow = 1 }
            };
            _emptyRowFillEntry.SetEmptyEntries();
            _microservicesContainer.Add(_emptyRowFillEntry);
        }
        public void SetServiceDependencies()
        {
            foreach (var service in MicroserviceEntries)
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
            DependentServicesMicroserviceEntryVisualElement microserviceEntry; 
            DependentServicesStorageObjectEntryVisualElement storageObjectEntry;
            DependentServicesCheckboxVisualElement checkboxVisualElement;

            if (_lastRelactionChangedStorageObject != null && _lastRelactionChangedMicroservice != null)
            {
                //Remove Highlight - Row 
                microserviceEntry = MicroserviceEntries[_lastRelactionChangedMicroservice];
                microserviceEntry.RemoveFromClassList("sectionHighlight");
                microserviceEntry.MicroserviceName.RemoveFromClassList("sectionHighlightLabel");
                
                //Remove Highlight - Column 
                storageObjectEntry = StorageObjectEntries[_lastRelactionChangedStorageObject];
                storageObjectEntry.RemoveFromClassList("sectionHighlight");
                storageObjectEntry.StorageObjectName.RemoveFromClassList("sectionHighlightLabel");

                foreach (var entry in MicroserviceEntries.Values)
                {
                    checkboxVisualElement = entry.DependentServices.FirstOrDefault(x => x.MongoStorageModel == _lastRelactionChangedStorageObject);
                    checkboxVisualElement?.RemoveFromClassList("sectionHighlight");
                }
                checkboxVisualElement = _emptyRowFillEntry.DependentServices.FirstOrDefault(x => x.MongoStorageModel == _lastRelactionChangedStorageObject);
                checkboxVisualElement?.RemoveFromClassList("sectionHighlight");
            }
                
            _lastRelactionChangedMicroservice = microserviceModel;
            _lastRelactionChangedStorageObject = storageObjectModel;
            
            //Add Highlight - Row 
            microserviceEntry = MicroserviceEntries[_lastRelactionChangedMicroservice];
            microserviceEntry.AddToClassList("sectionHighlight");
            microserviceEntry.MicroserviceName.AddToClassList("sectionHighlightLabel");
            //Add Highlight - Column 
            storageObjectEntry = StorageObjectEntries[_lastRelactionChangedStorageObject];
            storageObjectEntry.AddToClassList("sectionHighlight");
            storageObjectEntry.StorageObjectName.AddToClassList("sectionHighlightLabel");
            
            foreach (var entry in MicroserviceEntries.Values)
            {
                checkboxVisualElement = entry.DependentServices.FirstOrDefault(x => x.MongoStorageModel == _lastRelactionChangedStorageObject);
                checkboxVisualElement?.AddToClassList("sectionHighlight");
            }
            checkboxVisualElement = _emptyRowFillEntry.DependentServices.FirstOrDefault(x => x.MongoStorageModel == _lastRelactionChangedStorageObject);
            checkboxVisualElement?.AddToClassList("sectionHighlight");
            
            IsAnyRelationChanged = true;
        }
    }
}