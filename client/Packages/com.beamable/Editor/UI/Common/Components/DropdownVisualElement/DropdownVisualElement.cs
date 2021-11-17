using Beamable.Common;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Buss.Components;
using System;
using System.Collections.Generic;
using UnityEditor;
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
	public class DropdownVisualElement : BeamableVisualElement
	{
		private readonly List<DropdownSingleOption> _optionModels;

		private VisualElement _button;
		private VisualElement _root;
		private BeamablePopupWindow _optionsPopup;
		private Label _label;
		private string _value;

		private Action<int> _onSelection;

		public string Value
		{
			get => _value;
			private set
			{
				_value = value;
				if (_label != null)
					_label.text = Value;
			}
		}

		public new class UxmlFactory : UxmlFactory<DropdownVisualElement, UxmlTraits>
		{
		}

		public DropdownVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(DropdownVisualElement)}/{nameof(DropdownVisualElement)}")
		{
			Value = String.Empty;
			_optionModels = new List<DropdownSingleOption>();
		}

		public override void Refresh()
		{
			base.Refresh();

			_root = Root.Q<VisualElement>("mainVisualElement");

			_label = Root.Q<Label>("value");
			_label.text = Value;

			_button = Root.Q<VisualElement>("button");
			_button.RegisterCallback<MouseDownEvent>(async (e) => await OnButtonClicked(worldBound));
		}

		public void Setup(List<string> labels, Action<int> onOptionSelected)
		{
			_optionModels.Clear();
			_onSelection = onOptionSelected;
			for (var i = 0; i < labels.Count; i++)
			{
				string label = labels[i];
				int currentId = i;
				DropdownSingleOption singleOption = new DropdownSingleOption(i, label, (s) =>
				{
					OnOptionSelectedInternal(currentId);
					onOptionSelected?.Invoke(currentId);
				});

				_optionModels.Add(singleOption);
			}

			if (_optionModels.Count > 0)
			{
				Value = _optionModels[0].Label;
				onOptionSelected?.Invoke(0);
			}
		}

		public void Set(int id)
		{
			OnOptionSelectedInternal(id);
			_onSelection?.Invoke(id);
		}

		private async Promise OnButtonClicked(Rect bounds)
		{
			if (_optionsPopup != null)
			{
				_optionsPopup.Close();
				OnOptionsClosed();
				return;
			}

			if (_optionModels.Count == 0)
			{
				Debug.LogWarning("Dropdown has no options to render");
				return;
			}

			Rect popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(bounds);

			List<DropdownSingleOptionVisualElement> allOptions = new List<DropdownSingleOptionVisualElement>();

			foreach (DropdownSingleOption option in _optionModels)
			{
				allOptions.Add(new DropdownSingleOptionVisualElement().Setup(option.Label,
																			 option.OnClick, _root.localBound.width,
																			 _root.localBound.height));
			}

			DropdownOptionsVisualElement optionsWindow =
				new DropdownOptionsVisualElement().Setup(allOptions, OnOptionsClosed);

			_optionsPopup = await BeamablePopupWindow.ShowDropdownAsync("", popupWindowRect,
																		new Vector2(
																			_root.localBound.width,
																			optionsWindow.GetHeight()), optionsWindow);
		}

		private void OnOptionsClosed()
		{
			_optionsPopup = null;
		}

		private void OnOptionSelectedInternal(int id)
		{
			Value = _optionModels.Find(opt => opt.Id == id).Label;
			if (_optionsPopup && _optionsPopup != null)
			{
				_optionsPopup.Close();
				OnOptionsClosed();
			}
		}
	}
}
