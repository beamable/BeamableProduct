using Beamable.Editor.UI.Common;
using Beamable.UI.Buss;
using System;
using Beamable.Common;
using UnityEngine.UIElements;
using static Beamable.Common.Constants.Features.Buss.ThemeManager;

namespace Beamable.Editor.UI.Components
{
	public abstract class BussPropertyVisualElement : BeamableBasicVisualElement
	{
		public BussStyleSheet UpdatedStyleSheet;
		protected bool IsTriggeringStyleSheetChange { get; private set; }

		public abstract IBussProperty BaseProperty
		{
			get;
		}

		protected BussPropertyVisualElement() : base(
			$"{BUSS_THEME_MANAGER_PATH}/BussPropertyVisualElements/BussPropertyVisualElement.uss", false)
		{ }

		public override void Init()
		{
			base.Init();
			AddToClassList("bussPropertyRoot");
		}

		protected void AddBussPropertyFieldClass(VisualElement ve)
		{
			ve.AddToClassList("bussPropertyField");
		}

		public abstract void OnPropertyChangedExternally();

		protected void TriggerStyleSheetChange()
		{
			IsTriggeringStyleSheetChange = true;
			try
			{
				if (UpdatedStyleSheet != null)
				{
					UpdatedStyleSheet.TriggerChange();
				}
			}
			catch (Exception e)
			{
				BeamableLogger.LogException(e);
			}
			IsTriggeringStyleSheetChange = false;
		}
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
