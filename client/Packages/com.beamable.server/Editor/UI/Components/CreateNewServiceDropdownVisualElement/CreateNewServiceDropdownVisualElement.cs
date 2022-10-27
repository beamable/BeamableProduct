using Beamable.Server.Editor;
using System;
using UnityEngine.UIElements;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateNewServiceDropdownVisualElement : MicroserviceComponent
	{
		public CreateNewServiceDropdownVisualElement() : base(nameof(CreateNewServiceDropdownVisualElement)) { }

		public event Action<ServiceType> OnCreateNewClicked;

		private VisualElement _servicesList;
		
		public override void Refresh()
		{
			base.Refresh();
			_servicesList = Root.Q<VisualElement>("servicesList");
			SetContent();
		}

		private void SetContent()
		{
			foreach (var serviceType in (ServiceType[]) Enum.GetValues(typeof(ServiceType)))
			{
				var serviceEntryButton = new VisualElement {name = "serviceEntryButton"};
				serviceEntryButton.Add(new Image {name = $"image{serviceType}"});
				serviceEntryButton.Add(new Label(serviceType.ToString()){name = "label"});
				serviceEntryButton.RegisterCallback<MouseDownEvent>(_ =>
				{
					OnCreateNewClicked?.Invoke(serviceType);
				});
				_servicesList.Add(serviceEntryButton);
			}
		}
	}
}
