using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beamable.Editor.Modules.EditorConfig;
using Beamable.Editor.Utility;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Beamable.Editor.BeamCli
{
	[Serializable]
	public class ServerInfoResponse
	{
		/// <summary>
		/// the nuget id of the code executing the server
		/// </summary>
		public string version;
	
		/// <summary>
		/// a server must be created with an "owner", some form of identification
		/// that can be used to determine if the server is operating for a Unity-client, or
		/// some other Unity-client, or a user mode.
		/// </summary>
		public string owner;
	}

	public class BeamWebCommandFactoryOptions : ScriptableObject
	{
		// public int startPort = 8432; // beas
		public OptionalInt startPortOverride = new OptionalInt();
		public int port;
		public OptionalInt selfDestructOverride = new OptionalInt();
		public OptionalString ownerOverride = new OptionalString();
		public OptionalString versionOverride = new OptionalString();

		public OptionalInt serverEventLogCap = new OptionalInt();
		public OptionalInt serverLogCap = new OptionalInt();
		public OptionalInt commandInstanceCap = new OptionalInt();
		// public int serverEventLogCap = 1_000;
		// public int serverLogCap = 3_000;
		// public int commandInstanceCap = 35;

	}
	
	public class BeamWebCommandFactory : IBeamCommandFactory
	{
		public enum PingResult
		{
			Match, 
			Mismatch,
			NoServer
		}
		
		public string Url => $"http://127.0.0.1:{port}";
		public string ExecuteUrl => $"{Url}/execute";
		public string InfoUrl => $"{Url}/info";
		public string Owner => _options.ownerOverride.GetOrElse(BeamCliUtil.OWNER);

		public string Version => _options.versionOverride.GetOrElse(BeamCliUtil.CLI_VERSION);
		
		
		// TODO: how do we store this port so that we don't have to ALWAYS go through mismatches to re-find it? 
		// public int port { get; set; }= 8432; // beas 
		public int port
		{
			get => _options.port;
			set => _options.port = value;
		}
		
		public BeamCommandFactory processFactory;
		public BeamCommands processCommands;
		public BeamableDispatcher dispatcher;

		private readonly BeamWebCliCommandHistory _history;
		private readonly BeamWebCommandFactoryOptions _options;

		public Promise onReady = null;
		private ServerServeWrapper _serverCommand;

		public BeamWebCommandFactory(
			BeamableDispatcher dispatcher, 
			BeamWebCliCommandHistory history, 
			BeamWebCommandFactoryOptions options, BeamCli beamCli)
		{
			this.dispatcher = dispatcher;
			_history = history;
			_options = options;
			processFactory = new BeamCommandFactory(dispatcher);
			processCommands = new BeamCommands(processFactory, beamCli);
			_options.port = _options.startPortOverride.GetOrElse(8432);
			
			dispatcher.Run("cli-server-discovery", ServerDiscoveryLoop());
		}
		
		public IBeamCommand Create()
		{
			var command = new BeamWebCommand(this, _history);
			return command;
		}


		private List<Action> serverReadyCallbacks = new List<Action>();

		public async Promise EnsureServerIsRunning()
		{
			var p = new Promise();
			discoveryRequest++;
			serverReadyCallbacks.Add(() =>
			{
				p.CompleteSuccess();
			});
			await p;
		}
		

		public void KillServer()
		{
			_serverCommand?.Cancel();
			_serverCommand = null;
		}

		public string GetServerProcess()
		{
			
			try
			{
				return (((BeamCommand)_serverCommand?.Command)?.process)?.StartInfo.Arguments;
			}
			catch
			{
				return null;
			}
		}


		private List<Action> serverCallbacks = new List<Action>();

		class WaitForCliRequest : CustomYieldInstruction
		{
			private readonly BeamWebCommandFactory _factory;

			public WaitForCliRequest( BeamWebCommandFactory factory)
			{
				_factory = factory;
			}
			
			public override bool keepWaiting
			{
				get
				{
					var hasPendingRequests = _factory.discoveryRequest > 0;
					return !hasPendingRequests;
				}
			}
		}
		

		public int discoveryRequest = 0;

		
		IEnumerator ServerDiscoveryLoop()
		{
			while (true)
			{
				if (discoveryRequest > 0)
				{
					_history.AddServerEvent($"CLI Discovery taking a moment before attempting discovery for  {discoveryRequest} pending requests...");
					yield return null;
				}
				else
				{
					_history.AddServerEvent($"CLI Discovery going into sleep waiting for discovery request");
					yield return new WaitForCliRequest(this);
				}
				
				_history.AddServerEvent($"CLI going into discovery process for {discoveryRequest} pending requests...");

				var ping = PingServer();
				ping.Error(e =>
				{
					Debug.LogError("PING FAILED: " + e.Message);
				});
				yield return ping.ToYielder();

				var pingResult = ping.GetResult();

				_history.AddServerEvent($"CLI received ping result={pingResult}");

				var startServer = false;
				switch (pingResult)
				{
					case PingResult.Match:
						_history.AddServerEvent($"Found existing server at port=[{port}]. Resolving callbacks");

						discoveryRequest = 0;
						var callbacks = serverReadyCallbacks.ToList();
						serverReadyCallbacks.Clear();
						foreach (var cb in callbacks)
						{
							cb?.Invoke();
						}
						
						// this is the happy case where the server is already running!
						break;
					case PingResult.Mismatch:
						_history.AddServerEvent($"Found mismatch server at port=[{port}], trying again");
						port++;
						break;
					case PingResult.NoServer:
						startServer = true;
						break;
				}

				if (startServer)
				{
					_history.AddServerEvent($"No server available, booting at port=[{port}]");
					var start = StartServer();
					yield return start.ToYielder();
					_history.AddServerEvent($"booted at port=[{port}]");

				}

			}
		}

		async Promise StartServer()
		{
			// processFactory.ClearAll();
			processCommands.argModifier = (defaultArgs =>
			{
				defaultArgs.log = "verbose";
				defaultArgs.pretty = true;
			});
			
			var args = new ServerServeArgs()
			{
				port = port,
				owner = "\"" + Owner + "\"",
				autoIncPort = true,
				// selfDestructSeconds = _options.selfDestructOverride.GetOrElse(15), // TODO: validate that a low ttl will restart the server
				customSplitter = true,
				skipContentPrewarm = true,
				requireProcessId = Process.GetCurrentProcess().Id
			};
			var p = args.port;
			_serverCommand = processCommands.ServerServe(args);
						
			var waitForResult = new Promise();
			_serverCommand.Command.On(data =>
			{
				if (data.type != "logs") return;
							
				_history.AddServerLog(p, data.json);
			});
			_serverCommand.OnStreamServeCliCommandOutput(data =>
			{
				port = data.data.port;
							
				_history.AddServerEvent(new BeamCliServerEvent
				{
					message = $"server established on port=[{port}] uri=[{data.data.uri}]"
				});
				waitForResult.CompleteSuccess();
			});
			_serverCommand.OnError(err =>
			{
				waitForResult.CompleteSuccess();
				Debug.LogError(err.data.message);
				Debug.LogError(err.data.fullTypeName);
				Debug.LogError(err.data.stackTrace);
				_history.AddServerEvent("ERROR! Server failed, killing old server.");
			});
						
			_history.AddServerEvent(new BeamCliServerEvent
			{
				message = $"starting server on port=[{args.port}]"
			});
			var _ = _serverCommand.Run();
			await waitForResult;
		}

		public async Promise<PingResult> PingServer()
		{

			var req = UnityWebRequest.Get(InfoUrl);
			var op = req.SendWebRequest();
			var p = new Promise();
			PingResult pingResult = PingResult.NoServer;
			
			op.completed += _ =>
			{
				if (req.responseCode != 200)
				{
					pingResult = PingResult.NoServer;
					p.CompleteSuccess();
					return;
				}
				
				var json = req.downloadHandler.text;
				var res = JsonUtility.FromJson<ServerInfoResponse>(json);

				var ownerMatches = String.Equals(res.owner, Owner, StringComparison.OrdinalIgnoreCase);
				var versionMatches = EditorConfiguration.Instance.IgnoreCliVersionRequirement || res.version == Version;

				_history.SetLatestServerPing(port, InfoUrl, res, ownerMatches, versionMatches);

				if (!ownerMatches || !versionMatches)
				{
					CliLogger.Log(
						$"ping mismatch. Required version=[{Version}] Received version=[{res.version}] Required owner=[{Owner}] Received owner=[{res.owner}]");
					pingResult = PingResult.Mismatch;
					p.CompleteSuccess();
					return;
				}

				pingResult = PingResult.Match;
				p.CompleteSuccess();
			};
			await p;
			
			return _history.SetLatestServerPingResult(pingResult);
		}
		
		
		public void ClearAll()
		{
			// TODO:
		}

	}

	[Serializable]
	public class BeamWebCommandRequest
	{
		public string commandLine;
	}
	
	public class BeamWebCommand : IBeamCommand
	{
		public string id;
		public string commandString;
		// private HttpClient _localClient;
		private Action<ReportDataPointDescription> _callbacks = (_) => { };
		private HashSet<string> _explicitOnCallbackTypes = new HashSet<string>();
		private Action _terminationCallbacks = () => { };
		private BeamWebCommandFactory _factory;
		private CancellationTokenSource _cts;
		private BeamWebCliCommandHistory _history;

		public BeamWebCommand(BeamWebCommandFactory factory, BeamWebCliCommandHistory history)
		{
			_history = history;
			id = Guid.NewGuid().ToString();
			_factory = factory;
			// _localClient = factory.localClient;
			_cts = new CancellationTokenSource();
			history.AddCommand(this);
		}

		public void SetCommand(string command)
		{
			commandString = command.Substring("beam".Length);
			_history.UpdateCommand(id, commandString);
		}
		
		public async Promise Run()
		{
			_history.UpdateResolvingHostTime(id);

			await _factory.EnsureServerIsRunning();

			_history.UpdateStartTime(id, _factory.ExecuteUrl);
			_history.AddCustomLog(id, "[Unity] starting request...");

			var req = new HttpRequestMessage(HttpMethod.Post, _factory.ExecuteUrl);
			var json = JsonUtility.ToJson(new BeamWebCommandRequest {commandLine = commandString});
			req.Content = new StringContent(json, Encoding.UTF8, "application/json");
			CliLogger.Log("Sending cli web request, " + json);
			var dispatchedIds = new List<long>();
			var p = new Promise();

			try
			{
				if (!_cts.IsCancellationRequested)
				{
					var client = new HttpClient() {Timeout = TimeSpan.FromDays(7),};
					var sendTask = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
					_history.AddCustomLog(id, "[Unity] sent request...");

					using HttpResponseMessage response = await sendTask;
					_history.AddCustomLog(id, "[Unity] opened response...");

					using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
					_history.AddCustomLog(id, "[Unity] opened response stream...");

					using StreamReader reader = new StreamReader(streamToReadFrom);

					Task<string> readTask = null;
					while (!reader.EndOfStream)
					{
						if (_cts.Token.IsCancellationRequested)
						{
							break;
						}

						readTask = reader.ReadLineAsync();
						while (!readTask.IsCompleted)
						{
							await Task.WhenAny(readTask, Task.Delay(50));
							if (_cts.Token.IsCancellationRequested)
							{
								break;
							}
						}
						if (_cts.Token.IsCancellationRequested)
						{
							break;
						}
						
						var line = await readTask;
						if (string.IsNullOrEmpty(line)) continue; // TODO: what if the message contains a \n character?

						// remove life-cycle zero-width character
						line = line.Replace("\u200b", "");
						if (!line.StartsWith("data: "))
						{
							Debug.LogWarning(
								$"CLI received a message over the local-server that did not start with the expected 'data: ' format. line=[{line}]");
							continue;
						}

						var jobId = _factory.dispatcher.Schedule(() => // put callback on separate work queue.
						{
							var lineJson = line
								.Substring("data: ".Length); // remove the Server-Side-Event notation

							CliLogger.Log("received, " + lineJson, "from " + commandString);

							var res = JsonUtility.FromJson<ReportDataPointDescription>(lineJson);
							res.json = lineJson;


							_history.HandleMessage(id, res);
							_callbacks?.Invoke(res);
						});
						dispatchedIds.Add(jobId);
					}
				}

			}
			finally
			{
				_history.AddCustomLog(id, "[Unity] ending...");
				_history.UpdateCompleteTime(id);
				await _factory.dispatcher.WaitForJobIds(dispatchedIds);
				_terminationCallbacks?.Invoke();
				req.Dispose();
				p.CompleteSuccess();
			}

			
			await p;
		}

		public void Cancel()
		{
			if (_cts.IsCancellationRequested) return; // no-op
			_cts.Cancel();
		}

		public IBeamCommand On<T>(Func<ReportDataPointDescription, bool> predicate, Action<ReportDataPoint<T>> cb)
		{
			On(desc =>
			{
				if (!predicate(desc)) return;
				var pt = JsonUtility.FromJson<ReportDataPoint<T>>(desc.json);
				cb?.Invoke(pt);
			});

			return this;
		}
		public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb)
		{
			_explicitOnCallbackTypes.Add(type);
			return On<T>(desc => desc.type == type, cb);
		}

		public IBeamCommand On(Action<ReportDataPointDescription> cb)
		{
			_callbacks += cb;
			return this;
		}

		public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb)
		{
			return On<ErrorOutput>(desc => desc.type.StartsWith("error"), data =>
			{
				if (_explicitOnCallbackTypes.Contains(data.type))
				{
					// if the caller has explicitly called an `.On("type")` method
					//  where the "type" is the same as this input, this the general
					//  purpose OnError() function shouldn't get the error. 
					return;
				}
				cb(data);
			});
		}

		public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> cb)
		{
			_terminationCallbacks += () =>
			{
				cb?.Invoke(new ReportDataPoint<EofOutput>());
			};
			return this;
		}
	}
}
