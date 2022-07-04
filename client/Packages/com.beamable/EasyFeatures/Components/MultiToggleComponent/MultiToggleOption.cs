using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Components
{
	public class MultiToggleOption : MonoBehaviour
	{
		[SerializeField] private Toggle _toggle;
		[SerializeField] private TextMeshProUGUI _label;

		public BussElement MainBussElement;
		public BussElement LabelBussElement;
		
		private Action _onClickAction;

		public Toggle Toggle => _toggle;

		public void Setup(string option, Action onClickAction, ToggleGroup group, bool selected)
		{
			_onClickAction = onClickAction;
			_label.text = option;
			_toggle.group = group;
			_toggle.onValueChanged.AddListener(ToggleClicked);
			_toggle.SetIsOnWithoutNotify(selected);
			
			ApplyStyle(selected);
		}

		private void ToggleClicked(bool value)
		{
			if (value)
			{
				_onClickAction.Invoke();
			}
			
			ApplyStyle(value);
		}

		private void ApplyStyle(bool selected)
		{
			List<string> classes = new List<string>();
			
			if (selected)
			{
				classes.Add("toggle");
				classes.Add("option");
				classes.Add("selected");
			}
			else
			{
				classes.Add("toggle");
				classes.Add("option");
				classes.Add("deselected");
			}
			
			MainBussElement.UpdateClasses(classes);
			LabelBussElement.UpdateClasses(classes);
		}
	}
}
