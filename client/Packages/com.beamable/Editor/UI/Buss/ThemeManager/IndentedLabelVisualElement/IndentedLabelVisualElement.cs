using Beamable.Editor.UI.Buss;
using System;
using System.Collections.Generic;
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
	public class IndentedLabelVisualElement : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<IndentedLabelVisualElement, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			readonly UxmlStringAttributeDescription _label = new UxmlStringAttributeDescription
			{
				name = "label", defaultValue = "Label"
			};

			private readonly UxmlIntAttributeDescription _indentLevel = new UxmlIntAttributeDescription
			{
				name = "level", defaultValue = 0
			};

			private readonly UxmlIntAttributeDescription _indentWidth = new UxmlIntAttributeDescription
			{
				name = "width", defaultValue = (int)DEFAULT_SINGLE_INDENT_WIDTH
			};

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield break;
				}
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				if (ve is IndentedLabelVisualElement component)
				{
					component.Label = _label.GetValueFromBag(bag, cc);
					component.SingleIndentWidth = _indentWidth.GetValueFromBag(bag, cc);
					component.Level = _indentLevel.GetValueFromBag(bag, cc);
				}
			}
		}

		public const float DEFAULT_SINGLE_INDENT_WIDTH = 20.0f;

		private Label _label;
		private VisualElement _container;
		private Action<IndentedLabelVisualElement> _onMouseClicked;

		public GameObject RelatedGameObject
		{
			get;
			private set;
		}

		private float SingleIndentWidth
		{
			get;
			set;
		}

		private float Level
		{
			get;
			set;
		}

		private string Label
		{
			get;
			set;
		}

		private float Width => (SingleIndentWidth * Level) + SingleIndentWidth;

		public IndentedLabelVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(IndentedLabelVisualElement)}/{nameof(IndentedLabelVisualElement)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_container = Root.Q<VisualElement>("container");

			_label = Root.Q<Label>("label");
#if UNITY_2018
			_label.style.paddingLeft = new StyleValue<float>(Width);
#elif UNITY_2019_1_OR_NEWER
			_label.style.paddingLeft = new StyleLength(Width);
#endif
			_label.text = Label;

			_label.RegisterCallback<MouseDownEvent>(OnMouseClicked);
			_label.RegisterCallback<MouseOverEvent>(OnMouseOver);
			_label.RegisterCallback<MouseOutEvent>(OnMouseOut);
		}

		public void Setup(GameObject relatedGameObject,
		                  string label,
		                  Action<IndentedLabelVisualElement> onMouseClicked,
		                  int level,
		                  float width)
		{
			_onMouseClicked = onMouseClicked;

			RelatedGameObject = relatedGameObject;
			Label = label;
			Level = level;
			SingleIndentWidth = width;
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
			if (!_container.IsSelected())
			{
				_container.SetHovered(true);
			}
		}

		private void OnMouseOut(MouseOutEvent evt)
		{
			_container.SetHovered(false);
		}

		private void OnMouseClicked(MouseDownEvent evt)
		{
			_onMouseClicked?.Invoke(this);
		}
	}
}
