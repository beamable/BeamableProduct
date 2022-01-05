using System;
using Beamable.UI.Buss;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class EnumBussPropertyVisualElement : BussPropertyVisualElement<EnumBussProperty> {
		
		public EnumBussPropertyVisualElement(EnumBussProperty property) : base(property) { }
		
		private EnumField _field;

		public override void Refresh()
		{
			base.Refresh();
			
			_field = new EnumField();
			AddBussPropertyFieldClass(_field);
			_field.Init(Property.EnumValue);
			_mainElement.Add(_field);

			_field.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<Enum> changeEvent)
		{
			Property.EnumValue = changeEvent.newValue;
		}
		
		public override void OnPropertyChangedExternally()
		{
			_field.Init(Property.EnumValue);
		}
	}
}
