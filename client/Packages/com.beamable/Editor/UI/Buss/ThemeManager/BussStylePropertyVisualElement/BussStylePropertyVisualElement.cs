using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Validation;
using Beamable.UI.Buss;
using Beamable.UI.Sdf;
using Beamable.UI.Sdf.MaterialManagement;
using Beamable.UI.Tweening;
using System;
using Editor.UI.BUSS.ThemeManager.BussPropertyVisualElements;
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
	public class BussStylePropertyVisualElement : ValidableVisualElement<string>
	{
		public new class UxmlFactory : UxmlFactory<BussStylePropertyVisualElement, UxmlTraits> { }

		public BussStylePropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussStylePropertyVisualElement)}/{nameof(BussStylePropertyVisualElement)}") { }

		private BussPropertyProvider _property;
		private VisualElement _valueParent;

		public override void Refresh()
		{
			base.Refresh();

			_valueParent = Root.Q<VisualElement>("value");

			Label labelComponent = Root.Q<Label>("label");
			labelComponent.text = _property.Key;

			SetupEditableField(_property);
		}

		public void Setup(BussPropertyProvider property)
		{
			_property = property;
			Refresh();
		}

		private void SetupEditableField(BussPropertyProvider property)
		{
			var visualElement = property.GetVisualElement();
			if (visualElement != null)
			{
				_valueParent.Add(visualElement);
				visualElement.Refresh();
				return;
			}
			
			IBussProperty propertyType = property.GetProperty();

			if (propertyType is SingleColorBussProperty singleColorBussProperty)
			{
				ColorField field = new ColorField();
				// field.AddToClassList("equalWidth");
				field.value = singleColorBussProperty.Color;
				_valueParent.Add(field);

				ValueChanged<Color, ColorField>(field, property);
			}

			if (propertyType is VertexColorBussProperty vertexColorBussProperty)
			{
				// TODO: complex
			}

			if (propertyType is FloatBussProperty floatBussProperty)
			{
				FloatField field = new FloatField();
				field.value = floatBussProperty.FloatValue;
				_valueParent.Add(field);

				ValueChanged<float, FloatField>(field, property);
			}

			if (propertyType is FractionFloatBussProperty fractionFloatBussProperty)
			{
				Vector2Field field = new Vector2Field();
				field.value = new Vector2(fractionFloatBussProperty.Fraction, fractionFloatBussProperty.Offset);
				_valueParent.Add(field);
			}

			if (propertyType is FontBussAssetProperty fontBussProperty)
			{
				ObjectField field = new ObjectField();
				field.allowSceneObjects = false;
				field.objectType = typeof(Font);
				field.value = fontBussProperty.FontAsset;
				_valueParent.Add(field);
			}

			if (propertyType is SpriteBussProperty spriteBussProperty)
			{
				ObjectField field = new ObjectField();
				field.allowSceneObjects = false;
				field.objectType = typeof(Sprite);
				field.value = spriteBussProperty.SpriteValue;
				_valueParent.Add(field);
			}

			if (propertyType is SdfModeBussProperty sdfModeBussProperty)
			{
				ToolbarMenu menu = new ToolbarMenu();
				menu.AddToClassList("dropdown");

				Array values = Enum.GetValues(typeof(SdfImage.SdfMode));

				foreach (var value in values)
				{
					menu.menu.AppendAction(value.ToString(), action => {
						menu.text = action.name;
					});
				}

				_valueParent.Add(menu);
			}

			if (propertyType is ImageTypeBussProperty imageTypeBussProperty)
			{
				ToolbarMenu menu = new ToolbarMenu();
				menu.AddToClassList("dropdown");

				Array values = Enum.GetValues(typeof(SdfImage.ImageType));

				foreach (var value in values)
				{
					menu.menu.AppendAction(value.ToString(), action => {
						menu.text = action.name;
					});
				}

				_valueParent.Add(menu);
			}

			if (propertyType is BackgroundModeBussProperty backgroundModeBussProperty)
			{
				ToolbarMenu menu = new ToolbarMenu();
				menu.AddToClassList("dropdown");

				Array values = Enum.GetValues(typeof(SdfBackgroundMode));

				foreach (var value in values)
				{
					menu.menu.AppendAction(value.ToString(), action => {
						menu.text = action.name;
					});
				}

				_valueParent.Add(menu);
			}

			if (propertyType is BorderModeBussProperty borderModeBussProperty)
			{
				ToolbarMenu menu = new ToolbarMenu();
				menu.AddToClassList("dropdown");

				Array values = Enum.GetValues(typeof(SdfImage.BorderMode));

				foreach (var value in values)
				{
					menu.menu.AppendAction(value.ToString(), action => {
						menu.text = action.name;
					});
				}

				_valueParent.Add(menu);
			}

			if (propertyType is ShadowModeBussProperty shadowModeBussProperty)
			{
				ToolbarMenu menu = new ToolbarMenu();
				menu.AddToClassList("dropdown");

				Array values = Enum.GetValues(typeof(SdfShadowMode));

				foreach (var value in values)
				{
					menu.menu.AppendAction(value.ToString(), action => {
						menu.text = action.name;
					});
				}

				_valueParent.Add(menu);
			}

			if (propertyType is EasingBussProperty easingBussProperty)
			{
				ToolbarMenu menu = new ToolbarMenu();
				menu.AddToClassList("dropdown");

				Array values = Enum.GetValues(typeof(Easing));

				foreach (var value in values)
				{
					menu.menu.AppendAction(value.ToString(), action => {
						menu.text = action.name;
					});
				}

				_valueParent.Add(menu);
			}

			if (propertyType is Vector2BussProperty vectorBussProperty)
			{
				Vector2Field field = new Vector2Field();
				field.value = vectorBussProperty.Vector2Value;
				_valueParent.Add(field);
				
				ValueChanged<Vector2, Vector2Field>(field, property);
			}
		}

		private void ValueChanged<T, TS>(TS field, BussPropertyProvider property)  where TS : INotifyValueChanged<T>
		{
			void ValueChange(ChangeEvent<T> evt)
			{
				property.SerializedProperty.Set(evt.newValue);
			}

			field.RegisterValueChangedCallback(ValueChange);
		}
	}
}
