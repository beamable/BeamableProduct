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
	public class NotImplementedBussPropertyVisualElement : MessageBussPropertyVisualElement
	{
		protected override string Message =>
			$"No visual element implemented for drawing a property of type {Property?.GetType().Name ?? "NULL"}.";

		public NotImplementedBussPropertyVisualElement(IBussProperty property) : base(property) { }
	}
}
