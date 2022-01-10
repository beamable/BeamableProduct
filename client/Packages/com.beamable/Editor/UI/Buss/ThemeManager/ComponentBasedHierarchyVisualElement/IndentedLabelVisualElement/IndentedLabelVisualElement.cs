using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using System;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class IndentedLabelVisualElement : BeamableBasicVisualElement
	{
		public const float DEFAULT_SINGLE_INDENT_WIDTH = 20.0f;

		private float _singleIndentWidth;
		private float _level;
		private string _label;
		private Action<IndentedLabelVisualElement> _onMouseClicked;

		private VisualElement _container;
		private TextElement _labelComponent;

		public GameObject RelatedGameObject
		{
			get;
			set;
		}

#if UNITY_2018
		public IndentedLabelVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/IndentedLabelVisualElement/IndentedLabelVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		public IndentedLabelVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/IndentedLabelVisualElement/IndentedLabelVisualElement.uss") { }
#endif

		public void Setup(GameObject relatedGameObject,
		                  string label,
		                  Action<IndentedLabelVisualElement> onMouseClicked,
		                  int level,
		                  float width)
		{
			_onMouseClicked = onMouseClicked;

			RelatedGameObject = relatedGameObject;
			_label = label;
			_level = level;
			_singleIndentWidth = width;
		}

		public override void Refresh()
		{
			base.Refresh();

			_container = new VisualElement();
			_container.name = "indentedLabelContainer";
			
			_labelComponent = new TextElement();
			_labelComponent.name = "indentedLabel";
			_labelComponent.text = _label;

			float width = (_singleIndentWidth * _level) + _singleIndentWidth;

#if UNITY_2018
	_labelComponent.SetLeft(width);
#elif UNITY_2019_1_OR_NEWER
			_labelComponent.style.paddingLeft = new StyleLength(width);
#endif
			

			_container.Add(_labelComponent);

			Root.Add(_container);

			_container.RegisterCallback<MouseDownEvent>(OnMouseClicked);
			_container.RegisterCallback<MouseOverEvent>(OnMouseOver);
			_container.RegisterCallback<MouseOutEvent>(OnMouseOut);
		}

		protected override void OnDestroy()
		{
			_container?.UnregisterCallback<MouseDownEvent>(OnMouseClicked);
			_container?.UnregisterCallback<MouseOverEvent>(OnMouseOver);
			_container?.UnregisterCallback<MouseOutEvent>(OnMouseOut);
		}

		public void Select()
		{
			_container.SetSelected(true);
		}

		public void Deselect()
		{
			_container.SetSelected(false);
		}

		private void OnMouseOver(MouseOverEvent evt)
		{
			if (!Root.IsSelected())
			{
				Root.SetHovered(true);
			}
		}

		private void OnMouseOut(MouseOutEvent evt)
		{
			Root.SetHovered(false);
		}

		private void OnMouseClicked(MouseDownEvent evt)
		{
			_onMouseClicked?.Invoke(this);
		}
	}
}
