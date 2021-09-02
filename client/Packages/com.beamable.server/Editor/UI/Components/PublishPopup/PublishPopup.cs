using System.Collections.Generic;
using System;
using Beamable.Editor.UI.Components;
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

        public PublishPopup() : base(nameof(PublishPopup))
        {
        }

        public override void Refresh()
        {
            base.Refresh();

            if (Model?.Services == null)
                return;
            
            var container = Root.Q<VisualElement>("manifestElementsContainer");
            foreach (var kvp in Model.Services)
            {
                var newElement = new PublishManifestEntryVisualElement {Model = kvp.Value};
                newElement.Refresh();
                container.Add(newElement);
            }

            var generalComments = Root.Q<TextField>("largeCommentsArea");
            generalComments.RegisterValueChangedCallback(ce => Model.Comment = ce.newValue);

            var cancelButton = Root.Q<Button>("cancelBtn");
            cancelButton.clickable.clicked += () => OnCloseRequested?.Invoke();

            var continueButton = Root.Q<PrimaryButtonVisualElement>("continueBtn");
            continueButton.Button.clickable.clicked += () => OnSubmit?.Invoke(Model); 
        }
    }
}
