using Beamable.Server.Editor;
using UnityEditor;

namespace Beamable.Editor.Microservice.UI.Components
{
	public class CreateStorageObjectVisualElement : CreateServiceBaseVisualElement
	{
		protected override string NewServiceName
		{
			get;
			set;
		} = "NewStorageObject";

		protected override string ScriptName => nameof(StorageObjectVisualElement);

		protected override void CreateService(string serviceName)
		{
			MicroserviceEditor.CreateNewServiceFile(ServiceType.StorageObject, serviceName);
		}

		protected override void HandleCreateButtonClicked()
		{
			if (EditorUtility.DisplayDialog(
				title: "Create Storage",
				message:
				"This feature is in Preview and deployment will not be possible until a future version of Beamable.",
				ok: "Ok"
			))
			{
				base.HandleCreateButtonClicked();
			}
		}
	}
}
