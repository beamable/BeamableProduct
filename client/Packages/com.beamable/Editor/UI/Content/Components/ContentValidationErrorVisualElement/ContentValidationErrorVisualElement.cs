using Beamable.Common.Content.Validation;
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
	public class ContentValidationErrorVisualElement : ContentManagerComponent
	{

		public override void Refresh()
		{
			base.Refresh();

			var contentId = Root.Q<Label>("contentId");
			var count = Root.Q<CountVisualElement>();
			var numberOfErrors = ExceptionCollection?.Exceptions?.Count ?? 0;
			count.SetValue(numberOfErrors);
			contentId.text = ExceptionCollection.Content.Id;
		}

		public ContentExceptionCollection ExceptionCollection { get; set; }

		public ContentValidationErrorVisualElement() : base(nameof(ContentValidationErrorVisualElement))
		{
		}
	}
}
