using Beamable.Server.Editor;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateMicroserviceVisualElement : CreateServiceBaseVisualElement
	{
		protected override string NewServiceName
		{
			get;
			set;
		} = "NewMicroService";

		protected override string ScriptName => nameof(MicroserviceVisualElement);

		protected override void CreateService(string serviceName)
		{
			MicroserviceEditor.CreateNewServiceFile(ServiceType.MicroService, serviceName);
			Microservices.MicroserviceCreated(serviceName);
		}
	}
}
