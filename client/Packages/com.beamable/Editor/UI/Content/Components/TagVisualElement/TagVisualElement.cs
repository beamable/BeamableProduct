using System.Collections.Generic;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Content.Components
{
	public class TagVisualElement : ContentManagerComponent
	{

		private VisualElement _backGroundElement;
		private Label _label;

		public string Text { get; set; }
		public bool IsLocalOnly { get; set; }
		public bool IsLocalDeleted { get; set; }

		public TagVisualElement() : base(nameof(TagVisualElement))
		{

		}

		public override void Refresh()
		{
			base.Refresh();

			_backGroundElement = Root.Q<VisualElement>("mainVisualElement");
			if (IsLocalOnly)
				_backGroundElement.AddToClassList("localOnly");
			else if (IsLocalDeleted)
				_backGroundElement.AddToClassList("localDeleted");
			else
			{
				_backGroundElement.RemoveFromClassList("localOnly");
				_backGroundElement.RemoveFromClassList("localDeleted");
			}

			_label = Root.Q<Label>("label");
			_label.text = Text;
		}
	}
}
