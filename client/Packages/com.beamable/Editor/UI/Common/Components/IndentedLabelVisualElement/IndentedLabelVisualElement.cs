using Beamable.Editor.UI.Buss;
using Beamable.UI.BUSS;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.UI;
#if UNITY_2018
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

		private const float DEFAULT_SINGLE_INDENT_WIDTH = 20.0f;

		private Label _label;

		public GameObject RelatedGameObject
		{
			get;
			private set;
		}

		private VisualElement _container;
		private Action<IndentedLabelVisualElement> _onMouseClicked;

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
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(IndentedLabelVisualElement)}/{nameof(IndentedLabelVisualElement)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_container = Root.Q<VisualElement>("container");

			_label = Root.Q<Label>("label");
#if UNITY_2018
			_label.style.paddingLeft = new StyleValue<float>(Width);
#else
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
		                  int? level = null,
		                  int? width = null)
		{
			_onMouseClicked = onMouseClicked;

			RelatedGameObject = relatedGameObject;
			Label = label;
			Level = level ?? 0;
			SingleIndentWidth = width ?? DEFAULT_SINGLE_INDENT_WIDTH;
		}

		public void Select()
		{
			_container.AddToClassList("selected");
		}

		public void Deselect()
		{
			_container.RemoveFromClassList("selected");
		}

		private void OnMouseOver(MouseOverEvent evt)
		{
			if (!_container.ClassListContains("selected"))
			{
				_container.AddToClassList("hovered");
			}
		}

		private void OnMouseOut(MouseOutEvent evt)
		{
			_container.RemoveFromClassList("hovered");
		}

		private void OnMouseClicked(MouseDownEvent evt)
		{
			_onMouseClicked?.Invoke(this);
		}
	}
}
