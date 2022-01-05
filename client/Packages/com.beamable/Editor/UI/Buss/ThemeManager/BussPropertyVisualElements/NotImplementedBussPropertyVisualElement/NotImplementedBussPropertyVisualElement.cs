using Beamable.UI.Buss;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class NotImplementedBussPropertyVisualElement : BussPropertyVisualElement
	{
		public NotImplementedBussPropertyVisualElement(BussPropertyProvider propertyProvider) : base(propertyProvider) { }

		public override void Refresh()
		{
			base.Refresh();
			var label = new Label($"No visual element implemented for drawing a property of type {PropertyProvider?.GetProperty()?.GetType().Name}.");
			label.style.SetFontSize(8f);
			_mainElement.Add(label);
		}
	}
}
