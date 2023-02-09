using Beamable.Common;
using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public abstract class BussPropertyVisualElement : BeamableBasicVisualElement
	{
		public BussStyleSheet UpdatedStyleSheet;
		protected bool IsTriggeringStyleSheetChange { get; private set; }

		public abstract IBussProperty BaseProperty { get; }

		public Action<IBussProperty> OnValueChanged;
		public Action OnBeforeChange;
		public Action onExternalChange;

		public bool IsRemoved { get; private set; }

		protected BussPropertyVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/BussPropertyVisualElements/{nameof(BussPropertyVisualElement)}.uss", false)
		{ }

		public override void Init()
		{
			base.Init();
			OnValueChanged += _ => BaseProperty?.NotifyValueChange();
			AddToClassList("bussPropertyRoot");

			RegisterCallback<DetachFromPanelEvent>(evt =>
			{
				// TODO: there is tech debt here- if someone detaches Theme Manager, this will trigger the clean up event by accident.
				IsRemoved = true;
			});
		}

		public void DisableInput(string tooltip = "Disabled")
		{
			foreach (var bindable in this.Query<BindableElement>().ToList())
			{
				bindable.SetEnabled(false);
			}
			this.tooltip = tooltip;
		}

		protected void AddBussPropertyFieldClass(VisualElement ve)
		{
			ve.AddToClassList("bussPropertyField");
		}

		public void NotifyPropertyChangedExternally()
		{
			OnPropertyChangedExternally();
			onExternalChange?.Invoke();
		}
		public abstract void OnPropertyChangedExternally();

	}

	public interface IBussPropertyVisualElementSupportsPreview
	{
		void SetValueFromProperty(IBussProperty property);

	}
	public abstract class BussPropertyVisualElement<T> : BussPropertyVisualElement where T : IBussProperty
	{
		public override IBussProperty BaseProperty => Property;

		protected T Property
		{
			get;
		}

		protected BussPropertyVisualElement(T property)
		{
			Property = property;
		}
	}
}
