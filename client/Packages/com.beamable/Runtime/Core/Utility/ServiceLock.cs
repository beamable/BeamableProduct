using System.Collections.Generic;
using UnityEngine;

public class ServiceLock
{
	public const string CloudSavingServiceName = "cloud_saving_service";
	
	public Dictionary<string, string> _serviceLocker = new();
	
	public bool AttemptToLock(string serviceName, string implementationName)
	{
		bool isLocked = _serviceLocker.TryGetValue(serviceName, out string implementation);
		if (!isLocked || implementation == implementationName)
		{
			Debug.Log($"Locking {implementationName} as the current CloudSaving service.");
			_serviceLocker[serviceName] = implementationName;
			return true;
		}

		Debug.LogWarning($"Attempting to Use {implementationName} as the CloudSaving service but {implementation} is already being used.");
		return false;
	}
}
