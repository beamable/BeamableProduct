using Beamable.UI.Buss;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class Vector2BussPropertyVisualElement : BussPropertyVisualElement<Vector2BussProperty>
	{
		private FloatField _fieldX, _fieldY;

		public Vector2BussPropertyVisualElement(Vector2BussProperty property) : base(property) { }

		public override void Init()
		{
			base.Init();

			_fieldX = new FloatField();
			AddBussPropertyFieldClass(_fieldX);
			_fieldX.value = Property.Vector2Value.x;
			Root.Add(_fieldX);

			_fieldY = new FloatField();
			AddBussPropertyFieldClass(_fieldY);
			_fieldY.value = Property.Vector2Value.y;
			Root.Add(_fieldY);

			_fieldX.RegisterValueChangedCallback(OnValueChange);
			_fieldY.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<float> evt)
		{
			Property.Vector2Value = new Vector2(
				_fieldX.value,
				_fieldY.value);
			TriggerStyleSheetChange();
		}

		public override void OnPropertyChangedExternally()
		{
			var value = Property.Vector2Value;
			_fieldX.value = value.x;
			_fieldY.value = value.y;
		}
	}
}
