using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using System.Collections.Generic;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateMicroserviceVisualElement : CreateServiceBaseVisualElement
	{
		protected override string NewServiceName { get; set; } = "NewMicroService";
		protected override string ScriptName => nameof(MicroserviceVisualElement);
		protected override bool ShouldShowCreateDependentService => MicroservicesDataModel.Instance.Storages.Count != 0;

		protected override void CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null)
		{
			MicroserviceEditor.CreateNewServiceFile(ServiceType.MicroService, serviceName, additionalReferences);
			Microservices.MicroserviceCreated(serviceName);
		}
		protected override void InitCreateDependentService()
		{
			_serviceCreateDependentService.Init(MicroservicesDataModel.Instance.Storages, "StorageObjects");
		}
	}
}
