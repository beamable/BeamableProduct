using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using System.Collections.Generic;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateStorageObjectVisualElement : CreateServiceBaseVisualElement
	{
		protected override string NewServiceName { get; set; } = "NewStorageObject";
		protected override string ScriptName => nameof(StorageObjectVisualElement);
		protected override bool ShouldShowCreateDependentService => MicroservicesDataModel.Instance.Services.Count != 0;


		protected override void CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null)
		{
			MicroserviceEditor.CreateNewServiceFile(ServiceType.StorageObject, serviceName, additionalReferences);
		}
		protected override void InitCreateDependentService()
		{
			_serviceCreateDependentService.Init(MicroservicesDataModel.Instance.Services, "MicroServices");
		}
	}
}
