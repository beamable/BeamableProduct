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

        public DependentServicesMicroserviceEntryVisualElement() : base(nameof(DependentServicesMicroserviceEntryVisualElement))
        {
        }
        public override void Refresh()
        {
            base.Refresh();
            UpdateVisualElements();
        }
        private void UpdateVisualElements()
        {
            Root.Q<Label>("microserviceName").text = Model.Name;
            var dependencyCheckboxes = Root.Q("dependencyCheckboxes");

            DependentServices = new List<DependentServicesCheckboxVisualElement>(MicroservicesDataModel.Instance.Storages.Count);
            foreach (var storageObjectModel in MicroservicesDataModel.Instance.Storages)
            {
                var isRelation = Model.Dependencies.Contains(storageObjectModel);
                var newElement = new DependentServicesCheckboxVisualElement(isRelation) { MongoStorageModel = storageObjectModel };
                newElement.OnServiceRelationChanged += TriggerServiceRelationChanged;
                newElement.Refresh();
                dependencyCheckboxes.Add(newElement);
                DependentServices.Add(newElement);
            }
        }
        private void TriggerServiceRelationChanged(MongoStorageModel storageObjectModel)
        {
            OnServiceRelationChanged?.Invoke(storageObjectModel);
        }
    }
}
