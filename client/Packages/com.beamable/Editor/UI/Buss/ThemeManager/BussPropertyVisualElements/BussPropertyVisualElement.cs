using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
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
	public abstract class BussPropertyVisualElement : BeamableBasicVisualElement
	{
		public abstract IBussProperty BaseProperty { get; }

#if UNITY_2018
		protected BussPropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussPropertyVisualElements/BussPropertyVisualElement.2018.uss") { }
#elif UNITY_2019_1_OR_NEWER
		protected BussPropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussPropertyVisualElements/BussPropertyVisualElement.uss") { }
#endif
		
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

		protected BussPropertyVisualElement(T property) : base()
		{
			Property = property;
		}
	}
}
