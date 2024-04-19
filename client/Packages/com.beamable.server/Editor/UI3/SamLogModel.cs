using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.UI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI3
{
	[Serializable]
	public class SamLogModel : ISerializationCallbackReceiver
	{
		public List<SamServiceLogs> serviceLogs = new List<SamServiceLogs>();

		[NonSerialized]
		public Dictionary<string, SamServiceLogs> idToLogs = new Dictionary<string, SamServiceLogs>();
		[NonSerialized]
		public int version;

		public SamServiceLogs GetLogsForService(string serviceId)
		{
			if (!idToLogs.TryGetValue(serviceId, out var logs))
			{
				idToLogs[serviceId] = logs = new SamServiceLogs
				{
					beamoId = serviceId
				};
				serviceLogs.Add(logs);
			}

			return logs;
		}
		
		public void AddLogMessage(string serviceId, BeamTailLogMessageForClient message)
		{
			var logs = GetLogsForService(serviceId);
			
			logs.messages.Add(new SamLogMessage
			{
				level = LogLevel.DEBUG, //TODO,
				timestamp = message.timeStamp,
				message = message.message
			});
		}

		public void OnBeforeSerialize()
		{
			version++;
		}

		public void OnAfterDeserialize()
		{
			idToLogs = serviceLogs.ToDictionary(x => x.beamoId);
		}
	}

	[Serializable]
	public class SamServiceLogs
	{
		public string beamoId;
		public string filter;
		public List<SamLogMessage> messages;
	}
	
	[Serializable]
	public class SamLogMessage
	{
		public LogLevel level;
		public string message;
		public string timestamp;
	}
}
