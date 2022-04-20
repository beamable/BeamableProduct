using Beamable.Common;
using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
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

namespace Beamable.Editor.Microservice.UI.Components
{
	public class MicroserviceBreadcrumbsVisualElement : MicroserviceComponent
	{
		public new class UxmlFactory : UxmlFactory<MicroserviceBreadcrumbsVisualElement, UxmlTraits>
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
				var self = ve as MicroserviceBreadcrumbsVisualElement;

			}
		}

		public event Action<bool> OnSelectAllCheckboxChanged;
		public event Action<ServicesDisplayFilter> OnNewServicesDisplayFilterSelected;

		private RealmButtonVisualElement _realmButton;
		private Button _servicesFilter;
		private Label _servicesFilterLabel;
		private LabeledCheckboxVisualElement _selectAllLabeledCheckbox;

		public MicroserviceBreadcrumbsVisualElement() : base(nameof(MicroserviceBreadcrumbsVisualElement))
		{
		}

		protected override void OnDestroy()
		{
			if (_selectAllLabeledCheckbox != null)
			{
				_selectAllLabeledCheckbox.OnValueChanged -= TriggerSelectAll;
			}

			base.OnDestroy();
		}

		public override void Refresh()
		{
			base.Refresh();

			_realmButton = Root.Q<RealmButtonVisualElement>("realmButton");
			_realmButton.Refresh();

			_servicesFilter = Root.Q<Button>("servicesFilter");
			_servicesFilter.tooltip = Constants.Tooltips.Microservice.FILTER;
			_servicesFilterLabel = _servicesFilter.Q<Label>();
			_servicesFilter.clickable.clicked -= HandleServicesFilterButter;
			_servicesFilter.clickable.clicked += HandleServicesFilterButter;
			OnNewServicesDisplayFilterSelected -= UpdateServicesFilterText;
			OnNewServicesDisplayFilterSelected += UpdateServicesFilterText;
			UpdateServicesFilterText(MicroservicesDataModel.Instance.Filter);
			_servicesFilter.visible = true;


			_selectAllLabeledCheckbox = Root.Q<LabeledCheckboxVisualElement>("selectAllLabeledCheckbox");
			_selectAllLabeledCheckbox.Refresh();
			_selectAllLabeledCheckbox.DisableIcon();
			_selectAllLabeledCheckbox.OnValueChanged -= TriggerSelectAll;
			_selectAllLabeledCheckbox.OnValueChanged += TriggerSelectAll;
		}

		void TriggerSelectAll(bool value)
		{
			OnSelectAllCheckboxChanged?.Invoke(value);
		}

		void UpdateServicesFilterText(ServicesDisplayFilter filter)
		{
			switch (filter)
			{
				case ServicesDisplayFilter.AllTypes:
					_servicesFilterLabel.text = "All types";
					break;
				default:
					_servicesFilterLabel.text = filter.ToString();
					break;
			}
		}

		private void HandleServicesFilterButter()
		{
			HandleServicesFilterButter(_servicesFilter.worldBound);
		}

		private void HandleServicesFilterButter(Rect visualElementBounds)
		{
			var popupWindowRect = BeamablePopupWindow.GetLowerLeftOfBounds(visualElementBounds);

			var content = new ServiceFilterDropdownVisualElement();
			content.Refresh();
			var wnd = BeamablePopupWindow.ShowDropdown("Select", popupWindowRect, new Vector2(150, 75), content);
			content.OnNewServicesDisplayFilterSelected += filter =>
			{
				wnd.Close();
				OnNewServicesDisplayFilterSelected?.Invoke(filter);
			};
		}

		public void UpdateSelectAllCheckboxValue(int selectedServicesAmount, int servicesAmount)
		{
			_selectAllLabeledCheckbox.SetWithoutNotify(selectedServicesAmount == servicesAmount);
			SetSelectAllVisibility(servicesAmount > 0);
		}

		private void SetSelectAllVisibility(bool value)
		{
			_selectAllLabeledCheckbox.EnableInClassList("hidden", !value);
		}
	}

}
