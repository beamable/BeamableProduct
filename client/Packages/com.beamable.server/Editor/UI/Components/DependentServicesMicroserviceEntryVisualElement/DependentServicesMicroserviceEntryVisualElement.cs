using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using Beamable.Server.Editor.UI.Components;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Microservice.UI.Components
{
    public class DependentServicesMicroserviceEntryVisualElement : MicroserviceComponent
    {
        public Action<MongoStorageModel> OnServiceRelationChanged;
        public MicroserviceModel Model { get; set; }
        public List<DependentServicesCheckboxVisualElement> DependentServices { get; private set; }

        public Label MicroserviceName { get; private set; }
        private VisualElement _dependencyCheckboxes;
        private readonly IEnumerable<string> _storageObjectsRelations;
        
        public DependentServicesMicroserviceEntryVisualElement(IEnumerable<string> storageObjectsRelations) : base(nameof(DependentServicesMicroserviceEntryVisualElement))
        {
            _storageObjectsRelations = storageObjectsRelations;
        }
        public override void Refresh()
        {
            base.Refresh();
            QueryVisualElements();
            UpdateVisualElements();
        }
        private void QueryVisualElements()
        {
            MicroserviceName = Root.Q<Label>("microserviceName");
            _dependencyCheckboxes = Root.Q("dependencyCheckboxes");
        }
        private void UpdateVisualElements()
        {
            MicroserviceName.text = Model.Name;
            DependentServices = new List<DependentServicesCheckboxVisualElement>(MicroservicesDataModel.Instance.Storages.Count);
            
            foreach (var storageObjectModel in MicroservicesDataModel.Instance.Storages)
            {
                var isRelation = _storageObjectsRelations.Contains(storageObjectModel.Name.ToLower());
                var newElement = new DependentServicesCheckboxVisualElement(isRelation) { MongoStorageModel = storageObjectModel };
                newElement.OnServiceRelationChanged += TriggerServiceRelationChanged;
                newElement.Refresh();
                _dependencyCheckboxes.Add(newElement);
                DependentServices.Add(newElement);
            }
        }
        private void TriggerServiceRelationChanged(MongoStorageModel storageObjectModel)
        {
            OnServiceRelationChanged?.Invoke(storageObjectModel);
        }
        public void SetEmptyEntries()
        {
            base.Refresh();
            QueryVisualElements();
            MicroserviceName.RemoveFromHierarchy();
            Root.AddToClassList("emptyColumnEntry");

            DependentServices = new List<DependentServicesCheckboxVisualElement>(MicroservicesDataModel.Instance.Storages.Count);
            foreach (var storageObjectModel in MicroservicesDataModel.Instance.Storages)
            {
                var newElement = new DependentServicesCheckboxVisualElement(false) { MongoStorageModel = storageObjectModel};
                newElement.Refresh();
                newElement.Q<BeamableCheckboxVisualElement>("checkbox").RemoveFromHierarchy();
                _dependencyCheckboxes.Add(newElement);
                DependentServices.Add(newElement);
            }
        }
    }
}
