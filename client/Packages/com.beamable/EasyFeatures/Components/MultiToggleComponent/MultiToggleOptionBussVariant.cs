using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class MultiToggleOptionBussVariant : MonoBehaviour
	{
		[SerializeField] private MultiToggleOption _base;
		[SerializeField] private TextMeshBussElement _labelBussElement;

		public void Setup(string option, Action onClickAction, ToggleGroup group, bool selected)
		{
			_base.Setup(option, onClickAction, group, selected);
			_base.Toggle.onValueChanged.AddListener(ToggleClicked);
		}

		private void ToggleClicked(bool value)
		{
			_labelBussElement.UpdateClasses(value
				                                ? new List<string> {"toggle", "label", "selected"}
				                                : new List<string> {"toggle", "label", "unselected"});
		}
	}
}
