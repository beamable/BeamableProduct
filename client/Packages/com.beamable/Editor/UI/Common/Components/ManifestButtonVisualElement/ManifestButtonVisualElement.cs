using System.Collections.Generic;
using System.Linq;
using Beamable.Editor.Content;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using Beamable.Editor.UI.Common.Models;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
    public class ManifestButtonVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<ManifestButtonVisualElement, UxmlTraits>
        {
        }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var self = ve as ManifestButtonVisualElement;
            }
        }
        private ManifestModel Model { get; set; }
        private Button _manifestButton;
        private Label _manifestLabel;

        public ManifestButtonVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(ManifestButtonVisualElement)}/{nameof(ManifestButtonVisualElement)}")
        {

        }

        public override void Refresh()
        {
            base.Refresh();
            Model = new ManifestModel();
            Model.OnAvailableElementsChanged -= HandleAvailableManifestsChanged;
            Model.OnAvailableElementsChanged += HandleAvailableManifestsChanged;
            visible = false;
            Model.Initialize();
            _manifestButton = Root.Q<Button>("manifestButton");
            _manifestButton.clickable.clicked += () => { OnButtonClicked(_manifestButton.worldBound); };

            _manifestLabel = _manifestButton.Q<Label>();
            if (Model.Current == null || Model.Current.DisplayName == null)
            {
                _manifestLabel.text = "Select manifest ID";
            }
            else
            {
                _manifestLabel.text = Model.Current?.DisplayName;
            }
            Model.OnElementChanged -= HandleManifestChanged;
            Model.OnElementChanged += HandleManifestChanged;
        }

        private void HandleAvailableManifestsChanged(List<ISearchableElement> ids)
        {
            bool manyManifests = ids?.Count > 1;
            bool nonDefaultManifest = ids?.Count == 1 && ids[0].DisplayName != BeamableConstants.DEFAULT_MANIFEST_ID;

            visible = manyManifests || nonDefaultManifest;
        }


        private void HandleManifestChanged(ISearchableElement manifest)
        {
            _manifestLabel.text = Model.Current != null ? Model.Current.DisplayName : null;
        }

        private void OnButtonClicked(Rect visualElementBounds)
        {
            var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);

            var content = new SearchabledDropdownVisualElement();
            content.Model = Model;
            var wnd = BeamablePopupWindow.ShowDropdown("Select Manifest", popupWindowRect, new Vector2(200, 300), content);

            content.OnSelected += (manifest) =>
            {
                EditorAPI.Instance.Then(api =>
                {
                    if (manifest != null)
                    {
                        api.ContentIO.SwitchManifest(manifest.DisplayName);
                    }

                    wnd.Close();
                });
            };
            content.Refresh();
        }

    }
}