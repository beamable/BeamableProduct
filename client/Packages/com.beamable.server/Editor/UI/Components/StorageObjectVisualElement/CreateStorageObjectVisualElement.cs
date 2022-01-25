using Beamable.Editor.UI.Model;
using Beamable.Server.Editor;
using System.Collections.Generic;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateStorageObjectVisualElement : CreateServiceBaseVisualElement
	{
		protected override ServiceType ServiceType => ServiceType.StorageObject;
		protected override string NewServiceName { get; set; } = "NewStorageObject";
		protected override string ScriptName => nameof(StorageObjectVisualElement);

		protected override void CreateService(string serviceName, List<ServiceModelBase> additionalReferences = null)
		{
			MicroserviceEditor.CreateNewServiceFile(ServiceType, serviceName, additionalReferences);
		}
	}
}
