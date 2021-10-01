using Beamable.Server.Editor;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
    public class CreateStorageObjectVisualElement : CreateServiceBaseVisualElement
    {
        public CreateStorageObjectVisualElement() : base(nameof(MicroserviceVisualElement))
        {
        }
        
        protected override string NewServiceName { get; set; } = "NewStorageObject";
        protected override void CreateService(string serviceName)
        {
            EditorApplication.delayCall += () => MicroserviceEditor.CreateNewServiceFile(ServiceType.StorageObject, serviceName);
        }
    }
}