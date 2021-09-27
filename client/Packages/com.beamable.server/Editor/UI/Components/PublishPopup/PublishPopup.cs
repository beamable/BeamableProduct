using System.Collections.Generic;
using System;
using System.Linq;
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
    public class PublishPopup : MicroserviceComponent
    {

        public new class UxmlFactory : UxmlFactory<PublishPopup, UxmlTraits>
        {
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
                {name = "custom-text", defaultValue = "nada"};

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var self = ve as PublishPopup;
            }
        }

        public ManifestModel Model { get; set; }
        public Action OnCloseRequested;
        public Action<ManifestModel> OnSubmit;

        private TextField _generalComments;
        private Button _cancelButton;
        private PrimaryButtonVisualElement _continueButton;
        private ScrollView _scrollContainer;
        private List<PublishManifestEntryVisualElement> _publishManifestElements;

        public PublishPopup() : base(nameof(PublishPopup))
        {
        }

        public void PrepareParent()
        {
            parent.name = "PublishWindowContainer";
            parent.AddStyleSheet(USSPath);
        }

        public override void Refresh()
        {
            base.Refresh();

            if (Model?.Services == null)
                return;
            
            _scrollContainer = Root.Q<ScrollView>("manifestsContainer");
            _publishManifestElements = new List<PublishManifestEntryVisualElement>(Model.Services.Count);
            
            foreach (var kvp in Model.Services)
            {
                var newElement = new PublishManifestEntryVisualElement {Model = kvp.Value};
                newElement.Refresh();
                _publishManifestElements.Add(newElement);
                _scrollContainer.Add(newElement);
            }

            _generalComments = Root.Q<TextField>("largeCommentsArea");
            _generalComments.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

            _cancelButton = Root.Q<Button>("cancelBtn");
            _cancelButton.clickable.clicked += () => OnCloseRequested?.Invoke();

            _continueButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
            _continueButton.Button.clickable.clicked += () => OnSubmit?.Invoke(Model);
        }

        public void PrepareForPublish()
        {
            Root.Q<VisualElement>("header").RemoveFromHierarchy();
            _generalComments.RemoveFromHierarchy();
            _continueButton.RemoveFromHierarchy();
            _cancelButton.RemoveFromHierarchy();
            for (int i = 0; i < _publishManifestElements.Count; i++)
                _publishManifestElements[i].RemoveFromHierarchy();
            _publishManifestElements.Clear();

            
            foreach (var kvp in Model.Services)
            {
                var microserviceModel = MicroservicesDataModel.Instance.GetMicroserviceModelForName(kvp.Value.ServiceName);

                if (microserviceModel == null)
                {
                    Debug.LogError($"Cannot find model: {microserviceModel}");
                    continue;
                }
                
                var newElement = new LoadingBarElement();
                newElement.Refresh();
                _scrollContainer.Add(newElement);
                new DeployMSLogParser(newElement, microserviceModel, true);
            }
        }
    }
}
