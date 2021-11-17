using Beamable.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor.UI.Common.Models
{
	public interface ISearchableModel
	{
		event Action<List<ISearchableElement>> OnAvailableElementsChanged;
		event Action<ISearchableElement> OnElementChanged;

		ISearchableElement Default
		{
			get;
			set;
		}

		ISearchableElement Current
		{
			get;
			set;
		}

		List<ISearchableElement> Elements
		{
			get;
			set;
		}

		void Initialize();
		Promise<List<ISearchableElement>> RefreshAvailable();
	}
}
