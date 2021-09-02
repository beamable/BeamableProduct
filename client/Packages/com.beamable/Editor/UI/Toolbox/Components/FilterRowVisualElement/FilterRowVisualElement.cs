using System;
using System.Collections.Generic;
using Beamable.Editor.Toolbox.UI.Components;
using Beamable.Editor.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Toolbox.Components
{
    public class FilterRowVisualElement : ToolboxComponent
    {
        private Toggle _checkbox;
        public string FilterName { set; get; }
        public event Action<bool> OnValueChanged;

        public FilterRowVisualElement() : base(nameof(FilterRowVisualElement))
        {

        }

        public override void Refresh()
        {
            base.Refresh();
            var title = Root.Q<Label>("tagName");
            _checkbox = Root.Q<Toggle>(name: "filterCheckbox");
            _checkbox.RegisterValueChangedCallback(evt => OnValueChanged?.Invoke(evt.newValue));

            title.text = FilterName;
        }

        public void SetValue(bool value)
        {
            _checkbox.SetValueWithoutNotify(value);
        }
    }


}

