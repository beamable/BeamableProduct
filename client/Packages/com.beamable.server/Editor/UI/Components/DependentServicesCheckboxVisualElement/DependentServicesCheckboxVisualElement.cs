using System;
using System.Collections.Generic;
using System.Linq;
using Beamable.Server.Editor.ManagerClient;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
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
    public class DependentServicesCheckboxVisualElement : MicroserviceComponent
    {
        public Action<MongoStorageModel> OnServiceRelationChanged; 
        public MongoStorageModel MongoStorageModel { get; set; }
        public bool IsServiceRelation
        {
            get => _isServiceRelation;
            private set
            {
                _isServiceRelation = value;
                OnServiceRelationChanged?.Invoke(MongoStorageModel);
            }
        }
        private bool _isServiceRelation;

        public DependentServicesCheckboxVisualElement(bool isServiceRelation) : base(nameof(DependentServicesCheckboxVisualElement))
        {
            // Silent set
            _isServiceRelation = isServiceRelation;
        }

        public override void Refresh()
        {
            base.Refresh();
            var checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            checkbox.Refresh();
            checkbox.SetWithoutNotify(_isServiceRelation);
            checkbox.OnValueChanged += state => IsServiceRelation = state;
        }
    }
}