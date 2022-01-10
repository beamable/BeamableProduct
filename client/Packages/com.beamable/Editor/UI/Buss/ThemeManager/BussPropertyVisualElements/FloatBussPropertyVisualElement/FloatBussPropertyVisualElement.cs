using Beamable.UI.Buss;
using Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class FloatBussPropertyVisualElement : BussPropertyVisualElement<FloatBussProperty>
	{
		private FloatField _field;
		
		public FloatBussPropertyVisualElement(FloatBussProperty property) : base(property) { }

		public override void Refresh()
		{
			base.Refresh();
			
			_field = new FloatField();
			AddBussPropertyFieldClass(_field);
			_field.value = Property.FloatValue;
			_mainElement.Add(_field);

			_field.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<float> evt)
		{
			Property.FloatValue = evt.newValue;
		}

		public override void OnPropertyChangedExternally()
		{
			_field.value = Property.FloatValue;
		}
	}
}
