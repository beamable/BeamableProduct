using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

namespace Beamable.Utility
{
	[Preserve]
	public class ServiceLock
	{
		
		public const string CloudSavingServiceName = "cloud_saving_service";

		public Dictionary<string, string> _serviceLocker = new();

		public bool AttemptToLock(string serviceName, string implementationName)
		{
			bool isLocked = _serviceLocker.TryGetValue(serviceName, out string implementation);
			if (!isLocked || implementation == implementationName)
			{
				_serviceLocker[serviceName] = implementationName;
				return true;
			}

			Debug.LogWarning(
				$"Attempting to Use {implementationName} as the CloudSaving service but {implementation} is already being used.");
			return false;
		}
	}
}
