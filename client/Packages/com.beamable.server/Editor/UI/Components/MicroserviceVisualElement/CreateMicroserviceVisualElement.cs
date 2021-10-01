using Beamable.Server.Editor;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
    public class CreateMicroserviceVisualElement : CreateServiceBaseVisualElement 
    {
        protected override string NewServiceName { get; set; } = "NewMicroService";
        protected override void CreateService(string serviceName)
        {
            EditorApplication.delayCall += () => MicroserviceEditor.CreateNewServiceFile(ServiceType.MicroService, serviceName);
        }
    }
}