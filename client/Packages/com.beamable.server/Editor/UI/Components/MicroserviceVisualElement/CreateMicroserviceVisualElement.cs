using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using System.Collections.Generic;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateMicroserviceVisualElement : CreateServiceBaseVisualElement
	{
		protected override ServiceType ServiceType => ServiceType.MicroService;
		protected override string NewServiceName { get; set; } = "NewMicroService";
		protected override string ScriptName => nameof(MicroserviceVisualElement);

		protected override void CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null)
		{
			MicroserviceEditor.CreateNewServiceFile(ServiceType, serviceName, additionalReferences);
			Microservices.MicroserviceCreated(serviceName);
		}
	}
}
