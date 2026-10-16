using Beamable.Security;
using UnityEngine;

namespace DefaultNamespace
{
	public class Test : MonoBehaviour
	{
		public void Start()
		{
			ISecureStorage storage = new SecureStorage(Application.persistentDataPath);
			Debug.Log(Application.persistentDataPath);
			bool accepted = storage.SetString("analytics-batch", "SUPER SECURE DATA");
			accepted &= storage.SetString("analytics-batch-2", "SUPER SECURE DATA");
			bool persisted = accepted && storage.Flush();

			if (storage.TryGetString("analytics-batch", out string restoredJson))
			{
				Debug.Log(restoredJson);
				// Deserialize restoredJson.
			}

			bool deletionAccepted = storage.DeleteKey("analytics-batch");
			bool deletionPersisted = deletionAccepted && storage.Flush();

			storage.Dispose(); // Releases the directory; does not flush automatically.
		}
	}
}
