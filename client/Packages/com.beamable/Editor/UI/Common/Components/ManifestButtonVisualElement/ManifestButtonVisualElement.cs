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
            Model.OnAvailableManifestsChanged -= HandleAvailableManifestsChanged;
            Model.OnAvailableManifestsChanged += HandleAvailableManifestsChanged;
            visible = false;
            Model.Initialize();
            _manifestButton = Root.Q<Button>("manifestButton");
            _manifestButton.clickable.clicked += () => { OnButtonClicked(_manifestButton.worldBound); };

            _manifestLabel = _manifestButton.Q<Label>();
            if (Model.CurrentManifestId == null)
            {
                _manifestLabel.text = "Select manifest ID";
            }
            else
            {
                _manifestLabel.text = Model.CurrentManifestId;
            }
            Model.OnManifestChanged -= HandleManifestChanged;
            Model.OnManifestChanged += HandleManifestChanged;
        }

        private void HandleAvailableManifestsChanged(List<AvailableManifestModel> ids)
        {
            if (ids == null)
            {
                visible = false;
                return;
            }

            visible = ids.Count > 1 ||
                      (ids.Count == 1 &&
                       (ids[0].id != BeamableConstants.DEFAULT_MANIFEST_ID || ids[0].id != Model.CurrentManifestId));
        }


        private void HandleManifestChanged(string manifestId)
        {
            _manifestLabel.text = manifestId;
        }

        private void OnButtonClicked(Rect visualElementBounds)
        {
            var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);

            var content = new ManifestDropdownVisualElement();
            content.Model = Model;
            var wnd = BeamablePopupWindow.ShowDropdown("Select Manifest", popupWindowRect, new Vector2(200, 300), content);

            content.OnManifestSelected += (manifestId) =>
            {
                EditorAPI.Instance.Then(api =>
                {
                    api.ContentIO.SwitchManifest(manifestId);
                    wnd.Close();
                });
            };
            content.Refresh();
        }

    }
}