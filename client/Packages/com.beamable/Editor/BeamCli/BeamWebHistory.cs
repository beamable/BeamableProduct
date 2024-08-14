using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Dependencies;
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
	public class BeamWebCommandDescriptor
	{
		public string id;
		public string commandString;
		public float createdTime;
		public float resolveHostAtTime;
		public float startTime;
		public float endTime;
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

	}
	
	[Serializable]
	public class BeamWebCliCommandHistory : IStorageHandler<BeamWebCliCommandHistory>, Beamable.Common.Dependencies.IServiceStorable
	{
		public List<BeamWebCommandDescriptor> commands = new List<BeamWebCommandDescriptor>();
		
		
		[NonSerialized]
		private StorageHandle<BeamWebCliCommandHistory> _handle;

		[NonSerialized]
		private Dictionary<string, BeamWebCommandDescriptor> _idTable;

		public void AddCommand(BeamWebCommand command)
		{
			var desc = new BeamWebCommandDescriptor {
				instance = command, 
				commandString = command.commandString,
				id = command.id,
				createdTime = Time.realtimeSinceStartup
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
		}

		public void OnAfterLoadState()
		{
			_idTable = new Dictionary<string, BeamWebCommandDescriptor>();
			foreach (var command in commands)
			{
				if (command == null || string.IsNullOrEmpty(command.id)) continue;
				_idTable[command.id] = command;
			}
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
			desc.resolveHostAtTime = Time.realtimeSinceStartup;
		}
		
		public void UpdateStartTime(string id)
		{
			var desc = GetCommand(id);
			desc.startTime = Time.realtimeSinceStartup;
		}
		
		public void UpdateCompleteTime(string id)
		{
			var desc = GetCommand(id);
			desc.endTime = Time.realtimeSinceStartup;
		}
		
		public void UpdateMessageTime(string id)
		{
			var desc = GetCommand(id);
			desc.latestMessageTime = Time.realtimeSinceStartup;
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
			}
			else
			{
				desc.payloads.Add(res);
			}
		}
		
	}

}
