using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
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
		public Action OnValueChanged;
		public BussStyleSheet UpdatedStyleSheet;

		public abstract IBussProperty BaseProperty
		{
			get;
		}

		protected BussPropertyVisualElement() : base(
			$"{BeamableComponentsConstants.BUSS_THEME_MANAGER_PATH}/BussPropertyVisualElements/BussPropertyVisualElement.uss")
		{ }

		protected void AddBussPropertyFieldClass(VisualElement ve)
		{
			ve.AddToClassList("bussPropertyField");
		}

		public abstract void OnPropertyChangedExternally();

		protected void TriggerStyleSheetChange()
		{
			OnValueChanged?.Invoke();
			if (UpdatedStyleSheet != null)
			{
				UpdatedStyleSheet.TriggerChange();
			}
		}
	}

	public abstract class BussPropertyVisualElement<T> : BussPropertyVisualElement where T : IBussProperty
	{
		public override IBussProperty BaseProperty => Property;

		protected T Property
		{
			get;
		}

		protected BussPropertyVisualElement(T property) : base()
		{
			Property = property;
		}
	}
}
