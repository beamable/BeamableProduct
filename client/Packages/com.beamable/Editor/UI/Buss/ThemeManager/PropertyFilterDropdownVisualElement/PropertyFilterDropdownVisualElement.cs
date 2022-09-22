using Beamable.Editor.UI.Buss;
using System;
using System.Collections.Generic;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class PropertyFilterDropdownVisualElement : ThemeManagerComponent
	{
		public event Action<PropertyDisplayFilter> OnNewPropertyDisplayFilterSelected;
		
		private VisualElement _listRoot;
		
		private BussBreadcrumbsVisualElement _bussBreadcrumbsVisualElement;
		
		public PropertyFilterDropdownVisualElement(BussBreadcrumbsVisualElement bussBreadcrumbsVisualElement) : base(nameof(PropertyFilterDropdownVisualElement))
		{
			_bussBreadcrumbsVisualElement = bussBreadcrumbsVisualElement;
		}

		public override void Refresh()
		{
			base.Refresh();
			_listRoot = Root.Q<VisualElement>("popupContent");
			_listRoot.Clear();
			
			foreach (var propertyDisplayFilter in (PropertyDisplayFilter[]) Enum.GetValues(typeof(PropertyDisplayFilter)))
				AddButton(propertyDisplayFilter);
		}

		private void AddButton(PropertyDisplayFilter filter)
		{
			var propertyFilterButton = new Button();
			propertyFilterButton.text = _bussBreadcrumbsVisualElement.GetPropertyDisplayFilterText(filter);
			propertyFilterButton.SetEnabled(_bussBreadcrumbsVisualElement.Filter != filter);
			propertyFilterButton.clickable.clicked += () => OnNewPropertyDisplayFilterSelected?.Invoke(filter);
			_listRoot.Add(propertyFilterButton);
		}
	}
}
