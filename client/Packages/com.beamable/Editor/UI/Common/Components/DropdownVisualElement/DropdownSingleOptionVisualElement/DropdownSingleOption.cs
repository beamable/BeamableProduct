using System;

namespace Beamable.Editor.UI.Components
{
    public class DropdownSingleOption
    {
        public string Label { get; }
        public Action<string> OnClick { get; }

        public DropdownSingleOption(string label, Action<string> onClick)
        {
            Label = label;
            OnClick = onClick;
        }
    }
}