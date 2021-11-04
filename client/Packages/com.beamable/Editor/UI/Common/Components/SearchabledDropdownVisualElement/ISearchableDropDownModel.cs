using Beamable.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
    public interface ISearchableDropDownModel
    {
        event Action<List<ISearchableDropDownElement>> OnAvailableChanged;
        event Action<ISearchableDropDownElement> OnChanged;

        ISearchableDropDownElement Current { get; set; }
        List<ISearchableDropDownElement> Elements { get; set; }

        void Initialize();
        Promise<List<ISearchableDropDownElement>> RefreshAvailable();
    }
}
