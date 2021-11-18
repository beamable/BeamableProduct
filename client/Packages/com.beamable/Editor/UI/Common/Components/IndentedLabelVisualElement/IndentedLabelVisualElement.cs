using Beamable.Editor.UI.Buss;
using System;
using System.Collections.Generic;
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

		public float SingleIndentWidth
		{
			get;
			set;
		}

		public float Level
		{
			get;
			set;
		}

		public string Label
		{
			get;
			set;
		}

		public float Width => SingleIndentWidth * Level;

		public IndentedLabelVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(IndentedLabelVisualElement)}/{nameof(IndentedLabelVisualElement)}") { }

		public override void Refresh()
		{
			base.Refresh();

			_label = Root.Q<Label>("label");
			_label.style.paddingLeft = new StyleLength(Width);
			_label.text = Label;
			
			_label.RegisterCallback<MouseDownEvent>(Clicked);
		}

		private void Clicked(MouseDownEvent evt)
		{
			Debug.Log("clicked");
		}

		public void Setup(string value, int? level = null, int? width = null)
		{
			Label = value;
			Level = level ?? 0;
			SingleIndentWidth = width ?? DEFAULT_SINGLE_INDENT_WIDTH;
		}
	}
}
