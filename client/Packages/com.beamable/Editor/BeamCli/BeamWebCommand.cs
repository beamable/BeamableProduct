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
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
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

		public HttpClient localClient = new HttpClient
		{
			// this timeout is how long a single HTTP call can stay open receiving server-side-events.
			Timeout = TimeSpan.FromDays(7)
		};

		public string Url => $"http://127.0.0.1:{port}";
		public string ExecuteUrl => $"{Url}/execute";
		public string InfoUrl => $"{Url}/info";
		public string Owner => _options.ownerOverride.GetOrElse(BeamCliUtil.CLI_PATH.ToLowerInvariant());

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



		public BeamWebCommandFactory(IBeamableRequester requester, BeamableDispatcher dispatcher, BeamWebCliCommandHistory history, BeamWebCommandFactoryOptions options)
		{
			this.dispatcher = dispatcher;
			_history = history;
			_options = options;
			processFactory = new BeamCommandFactory(dispatcher);
			processCommands = new BeamCommands(requester, processFactory);
			_options.port = _options.startPortOverride.GetOrElse(8432);
		}

		public IBeamCommand Create()
		{
			var command = new BeamWebCommand(this, _history);
			return command;
		}

		public async Promise EnsureServerIsRunning()
		{
			if (onReady != null)
			{
				if (onReady.IsCompleted)
				{
					// in case the server dies, we should re-ping the server.
					var ping = await PingServer();
					if (ping != PingResult.Match)
					{
						onReady = null;

						await EnsureServerIsRunning();
						return;
					}
				}

				await onReady;
				return;
			}

			onReady = new Beamable.Common.Promise();

			dispatcher.Run("cli-server-init", InitServer());

			await onReady;
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

		IEnumerator InitServer()
		{
			yield return null; // important, wait a frame to accrue all requests in one "tick" 
			var serverIdentified = false;
			CliLogger.Log("Checking server init ....");
			while (!serverIdentified)
			{
				var pingPromise = PingServer();
				yield return pingPromise.ToYielder();
				var pingResult = pingPromise.GetResult();

				switch (pingResult)
				{
					case PingResult.Match:
						// perfect, the server is running! Nothing more to do :) 
						CliLogger.Log("found server ! ");
						serverIdentified = true;
						_history.AddServerEvent(new BeamCliServerEvent
						{
							message = $"server identified at port=[{port}]"
						});
						break;
					case PingResult.Mismatch:
						// ah, this server is being used for a different project...
						CliLogger.Log("mismatch server :(");
						port++; // by increasing the port, maybe we'll find our server soon...
						_history.AddServerEvent(new BeamCliServerEvent
						{
							message = "server mismatched detected, bumping local port"
						});
						break;
					case PingResult.NoServer:
						// bummer, no server exists for us, so we need to turn it on...
						CliLogger.Log("Starting server.... " + port + " , " + Owner);
						processCommands.defaultBeamArgs = processCommands.ConstructDefaultArgs();
						processCommands.defaultBeamArgs.log = "verbose";
						processCommands.defaultBeamArgs.pretty = true;

						var args = new ServerServeArgs()
						{
							port = port,
							owner = "\"" + Owner + "\"",
							autoIncPort = true,
							selfDestructSeconds = _options.selfDestructOverride.GetOrElse(15) // TODO: validate that a low ttl will restart the server
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
						});

						_history.AddServerEvent(new BeamCliServerEvent
						{
							message = $"starting server on port=[{args.port}]"
						});
						var _ = _serverCommand.Run();

						yield return waitForResult.ToYielder();

						break;
				}
			}

			onReady.CompleteSuccess();
		}

		public async Promise<PingResult> PingServer()
		{
			try
			{
				// #if UNITY_2021_1_OR_NEWER
				// var json = await localClient.GetStringAsync(InfoUrl);
				// #else
				var json = await localClient.GetStringAsync(InfoUrl).ToPromiseRoutine(); ;
				// #endif
				var res = JsonUtility.FromJson<ServerInfoResponse>(json);

				var ownerMatches = String.Equals(res.owner, Owner, StringComparison.OrdinalIgnoreCase);
				var versionMatches = res.version == Version;

				_history.SetLatestServerPing(port, InfoUrl, res, ownerMatches, versionMatches);

				if (!ownerMatches || !versionMatches)
				{
					CliLogger.Log($"ping mismatch. Required version=[{Version}] Received version=[{res.version}] Required owner=[{Owner}] Received owner=[{res.owner}]");
					return _history.SetLatestServerPingResult(PingResult.Mismatch);
				}

				return _history.SetLatestServerPingResult(PingResult.Match);

			}
			catch
			{
				return _history.SetLatestServerPingResult(PingResult.NoServer);
			}
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
		private HttpClient _localClient;
		private Action<ReportDataPointDescription> _callbacks = (_) => { };
		private HashSet<string> _explicitOnCallbackTypes = new HashSet<string>();
		private BeamWebCommandFactory _factory;
		private CancellationTokenSource _cts;
		private BeamWebCliCommandHistory _history;

		public BeamWebCommand(BeamWebCommandFactory factory, BeamWebCliCommandHistory history)
		{
			_history = history;
			id = Guid.NewGuid().ToString();
			_factory = factory;
			_localClient = factory.localClient;
			_cts = new CancellationTokenSource();
			history.AddCommand(this);
		}

		public void SetCommand(string command)
		{
			commandString = command.Substring("beam".Length);
			_history.UpdateCommand(id, commandString);
		}


		// async Task ReadLoop(HttpRequestMessage req, List<long> dispatchedIds)
		// {
		// 	using HttpResponseMessage response =
		// 		await _localClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
		// 	using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
		// 	using StreamReader reader = new StreamReader(streamToReadFrom);
		// 	
		// 	while (!reader.EndOfStream)
		// 	{
		// 		_cts.Token.ThrowIfCancellationRequested();
		// 		var line = await reader.ReadLineAsync();
		// 		if (string.IsNullOrEmpty(line)) continue; // TODO: what if the message contains a \n character?
		//
		// 		// remove life-cycle zero-width character
		// 		line = line.Replace("\u200b", "");
		// 		if (!line.StartsWith("data: "))
		// 		{
		// 			Debug.LogWarning(
		// 				$"CLI received a message over the local-server that did not start with the expected 'data: ' format. line=[{line}]");
		// 			continue;
		// 		}
		//
		// 		var jobId = _factory.dispatcher.Schedule(() => // put callback on separate work queue.
		// 		{
		// 			var lineJson = line
		// 				.Substring("data: ".Length); // remove the Server-Side-Event notation
		//
		// 			CliLogger.Log("received, " + lineJson, "from " + commandString);
		//
		// 			var res = JsonUtility.FromJson<ReportDataPointDescription>(lineJson);
		// 			res.json = lineJson;
		//
		// 				
		// 			_history.HandleMessage(id, res);
		// 			_callbacks?.Invoke(res);
		// 		});
		// 		dispatchedIds.Add(jobId);
		// 	}
		//
		// }
		//
		public async Promise Run()
		{
			_history.UpdateResolvingHostTime(id);

			await _factory.EnsureServerIsRunning();

			_history.UpdateStartTime(id);

			var req = new HttpRequestMessage(HttpMethod.Post, _factory.ExecuteUrl);
			var json = JsonUtility.ToJson(new BeamWebCommandRequest { commandLine = commandString });
			req.Content = new StringContent(json, Encoding.UTF8, "application/json");
			CliLogger.Log("Sending cli web request, " + json);
			var dispatchedIds = new List<long>();
			var p = new Promise();

			try
			{
				_cts.Token.ThrowIfCancellationRequested();
				using HttpResponseMessage response =
					await _localClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
				using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
				using StreamReader reader = new StreamReader(streamToReadFrom);


				// spawn a background task
				// var _ = Task.Run(async () =>
				// {
				// 	try
				// 	{
				// 		await ReadLoop(req, dispatchedIds);
				// 	}
				// 	catch (HttpRequestException socketException)
				// 	{
				// 		CliLogger.Log($"Socket exception happened. command=[{commandString}] url=[{_factory.ExecuteUrl}] " +
				// 		              socketException.Message);
				// 		throw;
				// 	}
				// 	catch (IOException ioException)
				// 	{
				//
				// 		// in this event, it is likely that the CLI server was terminated without politely closing connections.
				// 		//  that is _fine_, but we need to handle it.
				// 		CliLogger.Log("cli server died, " + ioException.Message);
				// 	}
				// 	catch (OperationCanceledException cancelledException)
				// 	{
				// 		// A cancellation was requested so the connection was terminated
				// 		CliLogger.Log("cli command was cancelled, " + cancelledException.Message);
				// 	}
				// 	catch (Exception ex)
				// 	{
				// 		CliLogger.Log(
				// 			$"Socket exception happened general. command=[{commandString}] url=[{_factory.ExecuteUrl}] type=[{ex.GetType().FullName}]" +
				// 			ex.Message);
				// 		Debug.LogException(ex);
				// 	}
				// 	finally
				// 	{
				// 		_history.UpdateCompleteTime(id);
				// 		await _factory.dispatcher.WaitForJobIds(dispatchedIds);
				// 		p.CompleteSuccess();
				// 		
				// 		req.Dispose();
				// 	}
				// });


				while (!reader.EndOfStream)
				{
					_cts.Token.ThrowIfCancellationRequested();
					var line = await reader.ReadLineAsync().ToPromiseRoutine();
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
			finally
			{
				await p;
			}
		}

		public void Cancel()
		{
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
			return this;
		}
	}
}
