using Beamable.Editor.Microservice.UI.Components;
using Beamable.Server.Editor;
using UnityEditor;

namespace Editor.UI.Components.StorageObjectVisualElement
{
    public class CreateStorageObjectVisualElement : CreateServiceBaseVisualElement
    {
        protected override string NewServiceName { get; set; } = "NewStorageObject";
        
        protected override void CreateService(string serviceName)
        {
            EditorApplication.delayCall += () => MicroserviceEditor.CreateNewServiceFile(ServiceType.StorageObject, serviceName);
        }
    }
}