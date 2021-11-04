using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor
{
    public interface ISearchableDropDownElement
    {
        string DisplayName { get; }
        int Depth { get; set; }
        bool Archived { get; set; }

        bool IsElementToSkipInDropdown(string filter);
        string GetClassNameToAddInDropdown();
    }
}
