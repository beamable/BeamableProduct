using Beamable.Editor.UI.Components;
using Beamable.Editor.UI.Model;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class ServicesDropdownVisualElement : MicroserviceComponent
	{
		private IReadOnlyList<IBeamableService> Services { get; }
		private VisualElement _servicesList;
		private PrimaryButtonVisualElement _playSelectedBtn;
		private readonly List<ServiceEntry> _serviceEntries = new List<ServiceEntry>();
		
		public ServicesDropdownVisualElement(IReadOnlyList<IBeamableService> services) : base(nameof(ServicesDropdownVisualElement))
		{
			Services = services;
		}

		public override void Refresh()
		{
			base.Refresh();
			_servicesList = Root.Q<VisualElement>("servicesList");
			_playSelectedBtn = Root.Q<PrimaryButtonVisualElement>("playSelectedBtn");
			_playSelectedBtn.Button.clicked += HandlePlaySelectedButton;
			SetContent();
		}

		private void SetContent()
		{
			foreach (var service in Services)
			{
				var serviceEntry = new ServiceEntry(service);
				serviceEntry.Dropdown.OnValueChanged += isSelected => HandleValueChanged(serviceEntry, isSelected);
				_serviceEntries.Add(serviceEntry);
				_servicesList.Add(serviceEntry.Dropdown);
			}
			_playSelectedBtn.Button.SetEnabled(_serviceEntries.Any(x => x.Service.IsSelected));
		}
		
		private void HandleValueChanged(ServiceEntry serviceEntry, bool isSelected)
		{
			serviceEntry.Service.IsSelected = isSelected;
			_playSelectedBtn.Button.SetEnabled(_serviceEntries.Any(x => x.Service.IsSelected));
		}

		private void HandlePlaySelectedButton()
		{
			foreach (var serviceEntry in _serviceEntries)
				if (!serviceEntry.Service.IsRunning)
					serviceEntry.Service.Start();
		}
	}

	public class ServiceEntry
	{
		public IBeamableService Service { get; }
		public LabeledCheckboxVisualElement Dropdown { get; private set; }

		public ServiceEntry(IBeamableService service)
		{
			Service = service;
			Setup();
		}

		private void Setup()
		{
			Service.Name.TryEllipseText(20, out var formattedText);
			Dropdown = new LabeledCheckboxVisualElement(formattedText, true, true);
			Dropdown.Refresh();
			Dropdown.name = $"{nameof(ServiceEntry)}-{Service.ServiceType}";
			Dropdown.tooltip = Service.Name;
			Dropdown.Value = Service.IsSelected;
		}
	}
}
