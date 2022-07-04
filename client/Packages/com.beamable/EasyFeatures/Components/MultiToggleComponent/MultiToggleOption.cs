using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class MultiToggleOption : MonoBehaviour
	{
		[SerializeField] private Toggle _toggle;
		[SerializeField] private TextMeshProUGUI _label;
		[SerializeField] private Color _selectedColor;
		[SerializeField] private Color _deselectedColor;
		
		private Action _onClickAction;

		public Toggle Toggle => _toggle;

		public void Setup(string option, Action onClickAction, ToggleGroup group, bool selected)
		{
			_onClickAction = onClickAction;
			_label.text = option;
			_toggle.group = group;
			_toggle.onValueChanged.AddListener(ToggleClicked);
			_toggle.SetIsOnWithoutNotify(selected);

			_label.color = selected ? _selectedColor : _deselectedColor;
		}

		private void ToggleClicked(bool value)
		{
			if (value)
			{
				_onClickAction.Invoke();
			}
			
			_label.color = value ? _selectedColor : _deselectedColor;
		}
	}
}
