using Beamable.Editor.UI.Buss;
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
	public class ColorPickerVisualElement : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<ColorPickerVisualElement, UxmlTraits> { }

		public ColorPickerVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(ColorPickerVisualElement)}/{nameof(ColorPickerVisualElement)}") { }

		public Color SelectedColor
		{
			get;
			set;
		}

		public override void Refresh()
		{
			base.Refresh();

			ColorField colorField = Root.Q<ColorField>("colorField");

			colorField.UnregisterValueChangedCallback(OnColorChanged);
			colorField.RegisterValueChangedCallback(OnColorChanged);
		}

		private void OnColorChanged(ChangeEvent<Color> evt)
		{
			SelectedColor = evt.newValue;
		}
	}
}
