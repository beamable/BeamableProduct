using Beamable.UI.Buss;
using Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class FloatBussPropertyVisualElement : BussPropertyVisualElement
	{
		public FloatBussPropertyVisualElement(BussPropertyProvider propertyProvider) : base(propertyProvider) { }

		public override void Refresh()
		{
			base.Refresh();
			
			FloatField field = new FloatField();
			field.value = PropertyProvider.GetProperty<FloatBussProperty>().FloatValue;
			_mainElement.Add(field);

			field.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<float> evt)
		{
			PropertyProvider.GetProperty<FloatBussProperty>().FloatValue = evt.newValue;
		}
	}
}
