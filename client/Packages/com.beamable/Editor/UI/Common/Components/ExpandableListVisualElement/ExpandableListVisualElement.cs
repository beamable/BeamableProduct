using Beamable.Editor.UI.Buss;
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
    public class ExpandableListVisualElement : BeamableVisualElement
    {
        public new class UxmlFactory : UxmlFactory<ExpandableListVisualElement, UxmlTraits>
        {
        }
        
        public ExpandableListVisualElement() : base(
            $"{BeamableComponentsConstants.COMP_PATH}/{nameof(ExpandableListVisualElement)}/{nameof(ExpandableListVisualElement)}")
        {
        }
    }
}