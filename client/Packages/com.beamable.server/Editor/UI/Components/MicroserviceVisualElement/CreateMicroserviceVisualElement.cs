using Beamable.Server.Editor;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
    public class CreateMicroserviceVisualElement : CreateServiceBaseVisualElement 
    {
        public CreateMicroserviceVisualElement() : base(nameof(MicroserviceVisualElement))
        {
        }

        protected override string NewServiceName { get; set; } = "NewMicroService";
        protected override void CreateService(string serviceName)
        {
            EditorApplication.delayCall += () => MicroserviceEditor.CreateNewServiceFile(ServiceType.MicroService, serviceName);
        }
    }
}