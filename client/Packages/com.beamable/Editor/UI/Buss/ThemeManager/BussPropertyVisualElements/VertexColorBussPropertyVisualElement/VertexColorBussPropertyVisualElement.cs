using Beamable.UI.Buss;
using Beamable.UI.Sdf.Styles;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class VertexColorBussPropertyVisualElement : BussPropertyVisualElement<VertexColorBussProperty>
	{
		private VisualElement _topRow;
		private VisualElement _bottomRow;
		private ColorField _bottomLeftColor;
		private ColorField _bottomRightColor;
		private ColorField _topLeftColor;
		private ColorField _topRightColor;

		public VertexColorBussPropertyVisualElement(VertexColorBussProperty property) : base(property) { }

		public override void Init()
		{
			base.Init();

			Root.style.SetFlexDirection(FlexDirection.Column);

			_topRow = CreateRowContainer();
			_bottomRow = CreateRowContainer();

			_bottomLeftColor = CreateColorField(_bottomRow);
			_bottomRightColor = CreateColorField(_bottomRow);

			_topLeftColor = CreateColorField(_topRow);
			_topRightColor = CreateColorField(_topRow);

			OnPropertyChangedExternally();
		}

		private VisualElement CreateRowContainer()
		{
			var ve = new VisualElement();
			AddBussPropertyFieldClass(ve);
			ve.style.SetFlexDirection(FlexDirection.Row);
			Root.Add(ve);
			return ve;
		}

		private ColorField CreateColorField(VisualElement container)
		{
			var cf = new ColorField();
			AddBussPropertyFieldClass(cf);
			cf.RegisterValueChangedCallback(OnValueChange);
			container.Add(cf);
			return cf;
		}

		private void OnValueChange(ChangeEvent<Color> evt)
		{
			Property.ColorRect = new ColorRect(
				_bottomLeftColor.value,
				_bottomRightColor.value,
				_topLeftColor.value,
				_topRightColor.value);
			TriggerStyleSheetChange();
		}

		public override void OnPropertyChangedExternally()
		{
			var colorRect = Property.ColorRect;
			_bottomLeftColor.value = colorRect.BottomLeftColor;
			_bottomRightColor.value = colorRect.BottomRightColor;
			_topLeftColor.value = colorRect.TopLeftColor;
			_topRightColor.value = colorRect.TopRightColor;
		}
	}
}
