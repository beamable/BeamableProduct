using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor
{
    public interface ISearchableDropDownElement
    {
        string DisplayName { get; }

        bool GetOrder();
        bool IsAvailable();
        bool IsToSkip(string filter);
        string GetClassNameToAdd();
    }
}
