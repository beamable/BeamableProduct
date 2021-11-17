using Beamable.Editor.UI.Buss;
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
	public class ExpandableListVisualElement : BeamableVisualElement
	{
		private const string HIDDEN_CLASS = "hidden";
		private const string EXPANDED_CLASS = "expanded";

		private List<string> elements;
		private string[] displayValues;
		private bool expanded;
		private Label label;
		private Label plusLabel;
		private VisualElement arrowImage;

		public new class UxmlFactory : UxmlFactory<ExpandableListVisualElement, UxmlTraits>
		{
		}

		public ExpandableListVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(ExpandableListVisualElement)}/{nameof(ExpandableListVisualElement)}")
		{
		}

		public override void Refresh()
		{
			base.Refresh();

			label = Root.Q<Label>("value");
			arrowImage = Root.Q<VisualElement>("arrowImage");
			plusLabel = Root.Q<Label>("plusLabel");

			SetupLabel();

			var button = Root.Q<VisualElement>("mainContainer");
			button.RegisterCallback<MouseDownEvent>(_ => ToggleExpand());
		}

		private void OnLabelSizeChanged(GeometryChangedEvent evt)
		{
			float width = evt.newRect.width;
			int maxCharacters = GetMaxNumberOfCharacters(width);
			displayValues = new string[elements.Count];
			for (int i = 0; i < elements.Count; i++)
			{
				displayValues[i] = TrimString(elements[i], maxCharacters);
			}

			SetupLabel();
		}

		private int GetMaxNumberOfCharacters(float width)
		{
			float charactersNumberFactor = elements.Count > 1 ? width / 11 : width / 10;
			return Mathf.CeilToInt(charactersNumberFactor);
		}

		private void SetupLabel()
		{
			if (displayValues == null || displayValues.Length == 0)
			{
				label.text = "";
				plusLabel.text = "";
				arrowImage.AddToClassList(HIDDEN_CLASS);
				return;
			}

			label.text = displayValues[0];
			label.RegisterCallback<GeometryChangedEvent>(OnLabelSizeChanged);

			if (displayValues.Length == 1)
			{
				arrowImage.AddToClassList("--positionHidden");
				plusLabel.AddToClassList("--positionHidden");
				return;
			}

			if (expanded)
			{
				for (int i = 1; i < displayValues.Length; i++)
				{
					label.text += "\n" + displayValues[i];
				}

				arrowImage.AddToClassList(EXPANDED_CLASS);
			}
			else
			{
				plusLabel.text = $"{displayValues.Length - 1}+";
			}
		}

		public void Setup(List<string> listElements, bool isExpanded = false)
		{
			elements = listElements;
			displayValues = listElements.ToArray();
			expanded = isExpanded;
			Refresh();
		}

		private void ToggleExpand()
		{
			expanded = !expanded;
			Refresh();
		}

		private string TrimString(string text, int maxCharacters)
		{
			if (string.IsNullOrEmpty(text))
			{
				return string.Empty;
			}

			return text.Length > maxCharacters ? text.Substring(0, maxCharacters) + "..." : text;
		}
	}
}
