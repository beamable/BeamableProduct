using Beamable.UI.Buss;
using UnityEngine.UIElements;

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
