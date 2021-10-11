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
    public class DependentServicesStorageObjectEntryVisualElement : MicroserviceComponent
    {
        public MongoStorageModel Model { get; set; }
        
        public DependentServicesStorageObjectEntryVisualElement() : base(nameof(DependentServicesStorageObjectEntryVisualElement))
        {
        }
        
        public override void Refresh()
        {
            base.Refresh();
            UpdateVisualElements();
        }
        
        private void UpdateVisualElements()
        {
            Root.Q<Label>("storageObjectName").text = Model.Name;
        }
    }
}