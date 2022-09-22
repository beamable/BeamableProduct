using Beamable.Common;
using Beamable.Editor.UI.Buss;
using Beamable.Editor.UI.Common;
using Beamable.Editor.UI.Components;
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class BussBreadcrumbsVisualElement : ThemeManagerComponent
	{
		public event Action<PropertyDisplayFilter> OnNewPropertyDisplayFilterSelected;
		public PropertyDisplayFilter Filter => _filter;
		
		
		private Button _propertiesFilter;
		private Label _propertiesFilterLabel;

		private PropertyDisplayFilter _filter;

		private Dictionary<PropertyDisplayFilter, string> _propertyDisplayFilterTexts =
			new Dictionary<PropertyDisplayFilter, string>();


		public new class UxmlFactory : UxmlFactory<BussBreadcrumbsVisualElement, UxmlTraits>
		{
		}

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			UxmlStringAttributeDescription customText = new UxmlStringAttributeDescription
			{ name = "custom-text", defaultValue = "nada" };

			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get { yield break; }
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var self = ve as BussBreadcrumbsVisualElement;
			}
		}
		
		public BussBreadcrumbsVisualElement() : base(nameof(BussBreadcrumbsVisualElement))
		{
		}
		
		public override void Refresh()
		{
			base.Refresh();
			
			_propertiesFilter = Root.Q<Button>("propertiesFilter");
			_propertiesFilterLabel = _propertiesFilter.Q<Label>();
			_propertiesFilter.clickable.clicked -= HandlePropertiesFilterButton;
			_propertiesFilter.clickable.clicked += HandlePropertiesFilterButton;
			UpdateServicesFilterText(_filter);
			OnNewPropertyDisplayFilterSelected -= UpdateServicesFilterText;
			OnNewPropertyDisplayFilterSelected += UpdateServicesFilterText;
		}

		private void UpdateServicesFilterText(PropertyDisplayFilter filter) 
			=> _propertiesFilterLabel.text = GetPropertyDisplayFilterText(filter);
		
		private void HandlePropertiesFilterButton()
			=> HandlePropertiesFilterButton(_propertiesFilter.worldBound);

		private void HandlePropertiesFilterButton(Rect visualElementBounds)
		{
			var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);
			var content = new PropertyFilterDropdownVisualElement(this);
			content.Refresh();
			var wnd = BeamablePopupWindow.ShowDropdown("Select", popupWindowRect, new Vector2(150, 50), content);
			content.OnNewPropertyDisplayFilterSelected += filter =>
			{
				wnd.Close();
				_filter = filter;
				OnNewPropertyDisplayFilterSelected?.Invoke(filter);
			};
		}
		
		public string GetPropertyDisplayFilterText(PropertyDisplayFilter propertyDisplayFilter) 
			=> !_propertyDisplayFilterTexts.ContainsKey(propertyDisplayFilter) 
				? propertyDisplayFilter.ToString() 
				: _propertyDisplayFilterTexts[propertyDisplayFilter];
	}
	
	public enum PropertyDisplayFilter
	{
		All,
		Overridden
	}
}
