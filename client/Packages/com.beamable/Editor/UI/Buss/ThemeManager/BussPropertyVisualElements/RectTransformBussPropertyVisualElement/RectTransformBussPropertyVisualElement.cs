using Beamable.Editor.UI.Components;
using Beamable.UI.Buss;
using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.UI.Components
{
	public class RectTransformBussPropertyVisualElement : BussPropertyVisualElement<RectTransformProperty>
	{
		private FloatField _leftField;
		private FloatField _rightField;
		private Label _leftLabel;
		private Label _rightLabel;
		private Label _topLabel;
		private Label _bottomLabel;
		public RectTransformBussPropertyVisualElement(RectTransformProperty property) : base(property) { }
		public override void OnPropertyChangedExternally()
		{
			// RectTransform
		}

		public override void Init()
		{
			// maintain a vector2 field... 
			base.Init();
			
			Root.EnableInClassList("rectTransform", true);
			
			_leftField = new FloatField();
			_leftField.value = Property.Left;
			_leftField.RegisterValueChangedCallback(BuildHandler<float>(v => Property.Left = v));
			
			var topField = new FloatField();
			topField.value = Property.Top;
			topField.RegisterValueChangedCallback(BuildHandler<float>(v => Property.Top = v));

			_rightField = new FloatField();
			_rightField.value = Property.Right;
			_rightField.RegisterValueChangedCallback(BuildHandler<float>(v => Property.Right = v));

			var lowField = new FloatField();
			lowField.value = Property.Bottom;
			lowField.RegisterValueChangedCallback(BuildHandler<float>(v => Property.Bottom = v));

			var pivotField = new Vector2Field();
			pivotField.value = Property.Pivot;
			pivotField.RegisterValueChangedCallback(BuildHandler<Vector2>(v => Property.Pivot = v));

			var anchorMinField = new Vector2Field();
			anchorMinField.value = Property.AnchorMin;
			anchorMinField.RegisterValueChangedCallback(BuildHandler<Vector2>(v => Property.AnchorMin = v));

			var anchorMaxField = new Vector2Field();
			anchorMaxField.value = Property.AnchorMax;
			anchorMaxField.RegisterValueChangedCallback(BuildHandler<Vector2>(v => Property.AnchorMax = v));

			_leftLabel = new Label("Left");
			_rightLabel = new Label("Right");
			_topLabel = new Label("Top");
			_bottomLabel = new Label("Bottom");
			
			AddRow(_leftLabel, _topLabel);
			AddRow(_leftField, topField);
			
			AddRow(_rightLabel, _bottomLabel);
			AddRow(_rightField, lowField);

			AddPrefixRow("Min", anchorMinField);
			AddPrefixRow("Max", anchorMaxField);
			AddPrefixRow("Pivot", pivotField);

			UpdateLabels();
		}

		void UpdateLabels()
		{
			var isXSame = Math.Abs(Property.AnchorMax.x - Property.AnchorMin.x) < .0001;
			var isYSame = Math.Abs(Property.AnchorMax.y - Property.AnchorMin.y) < .0001;

			_leftLabel.text = isXSame ? "X" : "Left";
			_rightLabel.text = isXSame ? "Width" : "Right";
			_topLabel.text = isYSame ? "Y" : "Top";
			_bottomLabel.text = isYSame ? "Height" : "Bottom";
		}


		EventCallback<ChangeEvent<T>> BuildHandler<T>(Action<T> setter)
		{
			return v =>
			{
				OnBeforeChange?.Invoke();
				setter(v.newValue);
				OnValueChanged?.Invoke(Property);
				UpdateLabels();
			};
		}

		void AddPrefixRow(string label, Vector2Field field)
		{
			var pivotRow = AddRow(field);
			var pivotLabel = new Label(label);
			pivotLabel.AddToClassList("prefix");
			pivotRow.AddToClassList("has-prefix");
			pivotRow.Insert(0, pivotLabel);
		}
		
		VisualElement AddRow(params VisualElement[] elements)
		{
			var row = new VisualElement();
			row.EnableInClassList("row", true);

			foreach (var element in elements)
			{
				row.Add(element);
			}
			Root.Add(row);
			return row;
		}
	}
}
