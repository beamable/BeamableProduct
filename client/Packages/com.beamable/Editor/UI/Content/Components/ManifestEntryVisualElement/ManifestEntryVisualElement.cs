#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using Beamable.Editor.UI.Components;

namespace Beamable.Editor.Content.Components {
    public class ManifestEntryVisualElement : ContentManagerComponent {
        public BeamableCheckboxVisualElement Checkbox { get; private set; }
        public readonly string labelText;
        public bool IsSelected => Checkbox != null && Checkbox.Value;

        public ManifestEntryVisualElement(string labelText) : base(nameof(ManifestEntryVisualElement)) {
            this.labelText = labelText;
        }

        public override void Refresh() {
            base.Refresh();
            Checkbox = Root.Q<BeamableCheckboxVisualElement>("checkbox");
            Checkbox.Refresh();
            Root.Q<Label>().text = labelText;
        }
    }
}