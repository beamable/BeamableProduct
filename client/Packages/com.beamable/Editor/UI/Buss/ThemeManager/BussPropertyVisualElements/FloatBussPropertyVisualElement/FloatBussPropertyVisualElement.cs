using Beamable.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class FloatBussPropertyVisualElement : BussPropertyVisualElement
	{
		public FloatBussPropertyVisualElement(BussPropertyProvider propertyProvider) : base(propertyProvider) { }

		public override void Refresh()
		{
			base.Refresh();
			
			FloatField field = new FloatField();
			//field.value = PropertyProvider.GetProperty<FloatBussProperty>().FloatValue;
			_mainElement.Add(field);

			field.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<float> evt)
		{
			//PropertyProvider.GetProperty<FloatBussProperty>().FloatValue = evt.newValue;
		}
	}
}
