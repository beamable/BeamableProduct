using Beamable.Editor.UI.Buss;
using System;
using System.Collections.Generic;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class DropdownOptionsVisualElement : BeamableVisualElement
	{
		private VisualElement _mainContainer;

		private readonly List<DropdownSingleOptionVisualElement> _allOptions =
			new List<DropdownSingleOptionVisualElement>();

		private Action _onDestroy;

		public new class UxmlFactory : UxmlFactory<DropdownOptionsVisualElement, UxmlTraits>
		{
		}

		public DropdownOptionsVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(DropdownVisualElement)}/{nameof(DropdownOptionsVisualElement)}/{nameof(DropdownOptionsVisualElement)}")
		{
		}

		public override void Refresh()
		{
			base.Refresh();

			_mainContainer = Root.Q<VisualElement>("mainVisualElement");

			RenderOptions();
		}

		protected override void OnDestroy()
		{
			_onDestroy?.Invoke();
		}

		public DropdownOptionsVisualElement Setup(List<DropdownSingleOptionVisualElement> options, Action onDestroy)
		{
			_allOptions.Clear();
			_allOptions.AddRange(options);

			_onDestroy = onDestroy;
			return this;
		}

		public float GetHeight()
		{
			float overallHeight = 0.0f;

			foreach (DropdownSingleOptionVisualElement option in _allOptions)
			{
				overallHeight += option.Height;
			}

			return overallHeight;
		}

		private void RenderOptions()
		{
			foreach (VisualElement child in _mainContainer.Children())
			{
				_mainContainer.Remove(child);
			}

			foreach (DropdownSingleOptionVisualElement option in _allOptions)
			{
				_mainContainer.Add(option);
				option.Refresh();
			}

			_mainContainer.style.SetHeight(GetHeight());
		}
	}
}
