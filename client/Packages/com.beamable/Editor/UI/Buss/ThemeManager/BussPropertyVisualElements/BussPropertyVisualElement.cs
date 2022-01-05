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
		protected VisualElement _mainElement;

		public abstract IBussProperty BaseProperty { get; }

		protected BussPropertyVisualElement(string commonPath) : base(commonPath) { }
		protected BussPropertyVisualElement(string uxmlPath, string ussPath) : base(uxmlPath, ussPath) { }

		public override void Refresh()
		{
			base.Refresh();
			_mainElement = Root.Q("mainVisualElement");
			_mainElement.style.width = new StyleLength(StyleKeyword.Auto);
		}

		protected void AddBussPropertyFieldClass(VisualElement ve)
		{
			ve.AddToClassList("bussPropertyField");
		}

		public abstract void OnPropertyChangedExternally();
	}

	public abstract class BussPropertyVisualElement<T> : BussPropertyVisualElement where T : IBussProperty
	{
		public override IBussProperty BaseProperty => Property;

		public T Property
		{
			get;
		}

		protected BussPropertyVisualElement(T property) : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussPropertyVisualElements/BussPropertyVisualElement")
		{
			Property = property;
		}
	}
}
