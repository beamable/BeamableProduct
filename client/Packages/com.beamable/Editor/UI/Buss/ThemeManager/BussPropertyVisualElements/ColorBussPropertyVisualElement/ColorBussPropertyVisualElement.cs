﻿using Beamable.UI.Buss;
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
	public class ColorBussPropertyVisualElement : BussPropertyVisualElement<SingleColorBussProperty>
	{
		private ColorField _field;

		public ColorBussPropertyVisualElement(SingleColorBussProperty property) : base(property) { }

		public override void Init()
		{
			base.Init();

			_field = new ColorField();
			AddBussPropertyFieldClass(_field);
			_field.value = Property.Color;
			Root.Add(_field);

			_field.RegisterValueChangedCallback(OnValueChange);
		}

		private void OnValueChange(ChangeEvent<Color> evt)
		{
			Property.Color = evt.newValue;
			TriggerStyleSheetChange();
		}

		public override void OnPropertyChangedExternally()
		{
			_field.value = Property.Color;
		}
	}
}
