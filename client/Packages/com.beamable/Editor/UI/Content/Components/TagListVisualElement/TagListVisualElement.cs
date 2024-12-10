using Beamable.Editor.Content.Models;
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
#if UNITY_6000_0_OR_NEWER
	[UxmlElement]
#endif
	public partial class TagListVisualElement : ContentManagerComponent
	{

		private VisualElement _mainVisualElement;

		public List<ContentTagDescriptor> TagDescriptors { get; set; }

		public TagListVisualElement() : base(nameof(TagListVisualElement))
		{

		}

		public override void Refresh()
		{
			base.Refresh();

			_mainVisualElement = Root.Q<VisualElement>("mainVisualElement");

			foreach (var tagDescriptor in TagDescriptors)
			{
				AddTagVisualElement(tagDescriptor.Tag,
				   tagDescriptor.LocalStatus == HostStatus.AVAILABLE && tagDescriptor.ServerStatus != HostStatus.AVAILABLE,
				   tagDescriptor.LocalStatus == HostStatus.NOT_AVAILABLE && tagDescriptor.ServerStatus == HostStatus.AVAILABLE);
			}
		}

		private void AddTagVisualElement(string tag, bool localOnly, bool localDeleted)
		{
			TagVisualElement tagVisualElement = new TagVisualElement();
			tagVisualElement.Text = tag;
			tagVisualElement.IsLocalOnly = localOnly;
			tagVisualElement.IsLocalDeleted = localDeleted;
			tagVisualElement.Refresh();
			_mainVisualElement.Add(tagVisualElement);
		}
	}
}
