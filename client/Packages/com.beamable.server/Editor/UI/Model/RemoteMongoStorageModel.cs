using Beamable.Server.Editor;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Model
{
	public class RemoteMongoStorageModel : MongoStorageModel
	{
		public static new RemoteMongoStorageModel CreateNew(StorageObjectDescriptor descriptor, MicroservicesDataModel dataModel)
		{
			return new RemoteMongoStorageModel
			{
				RemoteReference = dataModel.GetStorageReference(descriptor),
				ServiceDescriptor = descriptor,
				ServiceBuilder = Microservices.GetStorageBuilder(descriptor),
				Config = MicroserviceConfiguration.Instance.GetStorageEntry(descriptor.Name)
			};
		}

		public override void PopulateMoreDropdown(ContextualMenuPopulateEvent evt)
		{
			// TO DO
		}
	}
}
