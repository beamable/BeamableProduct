using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
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
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public class IndentedLabelVisualElement : BeamableBasicVisualElement
	{
		public const float DEFAULT_SINGLE_INDENT_WIDTH = 20.0f;

		private float _singleIndentWidth;
		private float _level;
		private Action<IndentedLabelVisualElement> _onMouseClicked;

		private VisualElement _container;
		private TextElement _labelComponent;
		private BussElement _relatedBussElement;
		private Func<BussElement, string> _getLabelAction;

		public GameObject RelatedGameObject { get; private set; }

		public IndentedLabelVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/ComponentBasedHierarchyVisualElement/IndentedLabelVisualElement/IndentedLabelVisualElement.uss")
		{ }

		public void Setup(GameObject relatedGameObject,
						  Func<BussElement, string> getLabelAction,
						  Action<IndentedLabelVisualElement> onMouseClicked,
						  int level,
						  float width)
		{
			RelatedGameObject = relatedGameObject;
			_relatedBussElement = RelatedGameObject.GetComponent<BussElement>();

			_onMouseClicked = onMouseClicked;
			_getLabelAction = getLabelAction;

			_level = level;
			_singleIndentWidth = width;
		}

		public override void Init()
		{
			base.Init();

			_container = new VisualElement();
			_container.name = "indentedLabelContainer";

			_labelComponent = new TextElement();
			_labelComponent.name = "indentedLabel";
			_labelComponent.text = _getLabelAction.Invoke(_relatedBussElement);

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

			_relatedBussElement.Validate += RefreshLabel;
		}

		protected override void OnDestroy()
		{
			_container?.UnregisterCallback<MouseDownEvent>(OnMouseClicked);
			_container?.UnregisterCallback<MouseOverEvent>(OnMouseOver);
			_container?.UnregisterCallback<MouseOutEvent>(OnMouseOut);

			_relatedBussElement.Validate -= RefreshLabel;
		}

		public void Select()
		{
			_container.SetSelected(true);
		}

		public void Deselect()
		{
			_container.SetSelected(false);
		}

		public void RefreshLabel()
		{
			_labelComponent.text = _getLabelAction.Invoke(_relatedBussElement);
			_labelComponent.MarkDirtyRepaint();
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
