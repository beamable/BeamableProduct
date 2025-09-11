using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
using Beamable.Editor.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Beamable.Editor.BeamCli
{

	public enum BeamWebCommandDescriptorStatus
	{
		PENDING,
		RESOLVING_HOST,
		RUNNING,
		DONE,
	}

	[Serializable]
	public class BeamCliPingResultDescriptor
	{
		public int port;
		public string url;
		public bool ownerMatches;
		public bool versionMatches;
		public ServerInfoResponse response;
		public BeamWebCommandFactory.PingResult result;
	}

	[Serializable]
	public class BeamCliServerEvent
	{
		public float time;
		public string message;
	}
	
	[Serializable]
	public class BeamWebCommandDescriptor
	{
		public string id;
		public string commandString;
		public long createdTime = -1;
		public long resolveHostAtTime;
		public long startTime = -1;
		public long endTime = -1;
		public float latestMessageTime;
		public int exitCode;

		public List<ReportDataPointDescription> payloads = new List<ReportDataPointDescription>();
		public List<CliLogMessage> logs = new List<CliLogMessage>();
		public List<ErrorOutput> errors = new List<ErrorOutput>();
		
		// TODO: create a computed property from the argstring that turns it into a dictionary from ARG to VALUE
		
		
		public BeamWebCommandDescriptorStatus Status
		{
			get
			{
				if (createdTime <= 0) return BeamWebCommandDescriptorStatus.PENDING;
				if (startTime <= 0) return BeamWebCommandDescriptorStatus.RESOLVING_HOST;
				if (endTime <= 0) return BeamWebCommandDescriptorStatus.RUNNING;
				return BeamWebCommandDescriptorStatus.DONE;
			}
		}
		
		/// <summary>
		/// this will be null after a reload.
		/// </summary>
		[NonSerialized]
		public BeamWebCommand instance;

		public string url;
	}
	
	[Serializable]
	public class BeamWebCliCommandHistory : IStorageHandler<BeamWebCliCommandHistory>, Beamable.Common.Dependencies.IServiceStorable
	{
		public List<BeamWebCommandDescriptor> commands = new List<BeamWebCommandDescriptor>();
		public List<BeamCliServerEvent> serverEvents = new List<BeamCliServerEvent>();
		public List<CliLogMessage> serverLogs = new List<CliLogMessage>();
		public BeamCliPingResultDescriptor latestPing = new BeamCliPingResultDescriptor();

		[NonSerialized]
		private StorageHandle<BeamWebCliCommandHistory> _handle;

		[NonSerialized]
		private Dictionary<string, BeamWebCommandDescriptor> _idTable;

		[NonSerialized]
		private BeamWebCommandFactoryOptions _options;

		private readonly IDependencyProvider _provider;

		private UnityOtelManager OtelManager => _provider.GetService<UnityOtelManager>();

		public BeamWebCliCommandHistory(BeamWebCommandFactoryOptions options, IDependencyProvider provider)
		{
			_options = options;
			_provider = provider;
		}
		
		public void AddCommand(BeamWebCommand command)
		{
			var desc = new BeamWebCommandDescriptor {
				instance = command, 
				commandString = command.commandString,
				id = command.id,
				createdTime = DateTime.Now.ToFileTime()
			};

			_idTable.Add(desc.id, desc);
			commands.Add(desc);
		}

		public void ReceiveStorageHandle(StorageHandle<BeamWebCliCommandHistory> handle)
		{
			_handle = handle;
		}

		public void OnBeforeSaveState()
		{
			// only save at the given caps
			List<T> ResetList<T>(List<T> elements, int maxSize)
			{
				var count = (int)Math.Min(maxSize, elements.Count);
				var index = elements.Count - count;
				var temp = new List<T>();
				for (var i = index; i < elements.Count; i++)
				{
					temp.Add(elements[i]);
				}
				return temp;
			}

			serverLogs = ResetList(serverLogs, _options.serverLogCap.GetOrElse(5_000));
			serverEvents = ResetList(serverEvents, _options.serverEventLogCap.GetOrElse(100));
			commands = ResetList(commands, _options.commandInstanceCap.GetOrElse(30));

		}

		public void OnAfterLoadState()
		{
			_idTable = new Dictionary<string, BeamWebCommandDescriptor>();
			foreach (var command in commands)
			{
				if (command == null || string.IsNullOrEmpty(command.id)) continue;
				_idTable[command.id] = command;
			}

			serverLogs = serverLogs.Where(s => !string.IsNullOrEmpty(s.logLevel)).ToList();
		}

		private BeamWebCommandDescriptor GetCommand(string id)
		{
			if (!_idTable.TryGetValue(id, out var desc))
				throw new Exception($"No command found for id=[{id}]");
			if (desc == null)
				throw new Exception($"command for id=[{id}] maps to null");

			return desc;
		}

		public void UpdateCommand(string id, string commandString)
		{
			var desc = GetCommand(id);
			desc.commandString = commandString;
		}

		public void UpdateResolvingHostTime(string id)
		{
			var desc = GetCommand(id);
			desc.resolveHostAtTime = DateTime.Now.ToFileTime();
		}
		
		public void UpdateStartTime(string id, string url)
		{
			var desc = GetCommand(id);
			desc.startTime = DateTime.Now.ToFileTime();
			desc.url = url;
		}
		
		public void UpdateCompleteTime(string id)
		{
			var desc = GetCommand(id);
			desc.endTime = DateTime.Now.ToFileTime();
		}
		
		public void UpdateMessageTime(string id)
		{
			var desc = GetCommand(id);
			desc.latestMessageTime = Time.realtimeSinceStartup;
		}

		public void AddCustomLog(string id, string message, string level="Debug")
		{
			var desc = GetCommand(id);
			desc.logs.Add(new CliLogMessage
			{
				timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
				logLevel = level,
				message = message
			});
			OtelManager.AddLog(message, System.Environment.StackTrace, level);
		}
		
		public void HandleMessage(string id, ReportDataPointDescription res)
		{
			var desc = GetCommand(id);
			UpdateMessageTime(id);
			if (res.type == "logs")
			{
				var msg = JsonUtility.FromJson<ReportDataPoint<CliLogMessage>>(res.json);
				desc.logs.Add(msg.data);
			} else if (res.type.StartsWith("error"))
			{
				var msg = JsonUtility.FromJson<ReportDataPoint<ErrorOutput>>(res.json);
				desc.errors.Add(msg.data);

				//Also add to the logs list
				var logMessage = $"{msg.data.message} \n\n {msg.data.stackTrace}";
				var log = new CliLogMessage()
				{
					logLevel = "Error", message = logMessage, timestamp = msg.ts
				};
				desc.logs.Add(log);
			}
			else
			{
				desc.payloads.Add(res);
			}
		}

		public BeamWebCommandFactory.PingResult SetLatestServerPingResult(BeamWebCommandFactory.PingResult result)
		{
			latestPing.result = result;
			return result;
		}
		public void SetLatestServerPing(int port, string infoUrl, ServerInfoResponse res, bool ownerMatches, bool versionMatches)
		{
			latestPing.versionMatches = versionMatches;
			latestPing.port = port;
			latestPing.url = infoUrl;
			latestPing.response = res;
			latestPing.ownerMatches = ownerMatches;
		}

		public void AddServerEvent(BeamCliServerEvent beamCliServerEvent)
		{
			beamCliServerEvent.time = Time.realtimeSinceStartup;
			serverEvents.Add(beamCliServerEvent);
		}

		public void AddServerEvent(string message) => AddServerEvent(new BeamCliServerEvent {message = message});

		public void AddServerLog(int port, string serverLog)
		{
			var msg = JsonUtility.FromJson<ReportDataPoint<CliLogMessage>>(serverLog);
			var log = msg.data;
			serverLogs.Add(log);
		}
	}

}
