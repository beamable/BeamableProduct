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
	public class NotImplementedBussPropertyVisualElement : BussPropertyVisualElement<IBussProperty>
	{
		public NotImplementedBussPropertyVisualElement(IBussProperty property) : base(property) { }

		public override void Refresh()
		{
			base.Refresh();
			var label = new Label($"No visual element implemented for drawing a property of type {Property?.GetType().Name}.");
			AddBussPropertyFieldClass(label);
			label.style.SetFontSize(8f);
			Root.Add(label);
		}

		public override void OnPropertyChangedExternally()
		{
			
		}
	}
}
