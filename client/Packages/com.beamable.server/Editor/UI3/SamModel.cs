using Beamable.Common.Dependencies;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI3
{
	[Serializable]
	public class SamModel : IStorageHandler<SamModel>, ISerializationCallbackReceiver
	{
		public List<SamServiceModel> services = new List<SamServiceModel>();
		
	
		private StorageHandle<SamModel> _handle;


		[NonSerialized]
		public int version;
		[NonSerialized]
		public Dictionary<string, SamServiceModel> idToService = new Dictionary<string, SamServiceModel>();


		
		public void Refresh(CodeService codeService)
		{
			services = new List<SamServiceModel>();
			foreach (var service in codeService.ServiceDefinitions)
			{
				services.Add(new SamServiceModel
				{
					name = service.BeamoId,
					isRunning = service.IsRunningLocally
				});
			}
			_handle.Save();
			OnAfterDeserialize();
		}

		public void SetStatusForService(string beamoId, bool isRunning)
		{
			if (!idToService.TryGetValue(beamoId, out var service))
			{
				return;
			}
			if (service == null) return;
			
			service.isRunning = isRunning;
			_handle.Save();
		}

		public void ReceiveStorageHandle(StorageHandle<SamModel> handle)
		{
			_handle = handle;
			
		}

		public void OnBeforeSerialize()
		{
			version++;
		}

		public void OnAfterDeserialize()
		{
			idToService = services.ToDictionary(x => x.name);
		}
	}

	[Serializable]
	public class SamServiceModel
	{
		/// <summary>
		/// The name of the service
		/// </summary>
		public string name;
		
		/// <summary>
		/// when true, the card is "collapsed"
		/// </summary>
		public bool isFolded;
		
		/// <summary>
		/// when true, the service is running, or attempting to start/stop.
		/// </summary>
		public bool isRunning;
	}
}
