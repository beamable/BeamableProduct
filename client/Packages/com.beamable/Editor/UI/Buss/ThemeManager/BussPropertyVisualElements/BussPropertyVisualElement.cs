using Beamable.Editor.UI.Buss;
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
	public abstract class BussPropertyVisualElement : BeamableVisualElement
	{
		public BussPropertyProvider PropertyProvider { get; }
		protected VisualElement _mainElement;

		protected BussPropertyVisualElement(BussPropertyProvider propertyProvider) : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/{nameof(BussPropertyVisualElement)}s/{nameof(BussPropertyVisualElement)}")
		{
			PropertyProvider = propertyProvider;
		}
		
		protected BussPropertyVisualElement(string commonPath) : base(commonPath) { }
		protected BussPropertyVisualElement(string uxmlPath, string ussPath) : base(uxmlPath, ussPath) { }

		public override void Refresh()
		{
			base.Refresh();
			_mainElement = Root.Q("mainVisualElement");
		}
	}
}
