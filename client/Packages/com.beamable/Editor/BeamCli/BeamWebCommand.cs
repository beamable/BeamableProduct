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
		public int pid;
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
		internal Process ServerProcess { get; private set; }

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

			BeamStopDiagnostics.Factory(this, "constructed", () => "port=" + port + " override=" + _options.versionOverride.GetOrElse("<unset>"));
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

		public Promise InvalidateServer(Exception cause)
		{
			// Marshal back to the editor coroutine thread — port, _serverCommand,
			// and discoveryRequest are all touched by ServerDiscoveryLoop and
			// shouldn't race with an async continuation.
			var jobId = dispatcher.Schedule(() =>
			{
				_history.AddServerEvent(
					$"Server invalidated: {cause.GetType().Name}: {cause.Message}");

				KillServer();
				port = _options.startPortOverride.GetOrElse(8432);
				discoveryRequest++;
			});

			return dispatcher.WaitForJobIds(new List<long> { jobId });
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
				BeamStopDiagnostics.Factory(this, "server-ready", () => "port=" + port);
							
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
			BeamStopDiagnostics.Factory(this, "server-launched", () => "port=" + port + " childPid=" + ((BeamCommand)_serverCommand.Command).process.Id + " trackedChildren=" + processFactory.PidCollection.pids.Count + " trackedPids=" + string.Join(",", processFactory.PidCollection.pids));
			await waitForResult;
		}

		public async Promise<PingResult> PingServer()
		{

			var req = UnityWebRequest.Get(InfoUrl);
			var op = req.SendWebRequest();
			var p = new Promise();
			PingResult pingResult = PingResult.NoServer;
			
			op.completed += operation =>
			{
				BeamStopDiagnostics.Factory(this, "ping-response", () => "port=" + port + " status=" + req.responseCode);
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
				BeamStopDiagnostics.Factory(this, "ping-contract", () => "port=" + port + " expectedVersion=" + Version + " returnedVersion=" + res.version + " versionMatches=" + versionMatches + " ownerMatches=" + ownerMatches + " ignoreVersion=" + EditorConfiguration.Instance.IgnoreCliVersionRequirement + " override=" + _options.versionOverride.GetOrElse("<unset>") + " returnedPid=" + res.pid);

				_history.SetLatestServerPing(port, InfoUrl, res, ownerMatches, versionMatches);

				if (!ownerMatches || !versionMatches)
				{
					CliLogger.Log(
						$"ping mismatch. Required version=[{Version}] Received version=[{res.version}] Required owner=[{Owner}] Received owner=[{res.owner}]");
					pingResult = PingResult.Mismatch;
					p.CompleteSuccess();
					return;
				}

				try
				{
					if (ServerProcess == null || ServerProcess.HasExited || ServerProcess.Id != res.pid)
					{
						var process = Process.GetProcessById(res.pid);
						_ = process.Handle; // Retain this process identity, not a PID that can be reused.
						ServerProcess = process;
					}
				}
				catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
				{
					pingResult = PingResult.NoServer;
					p.CompleteSuccess();
					return;
				}
				catch (Exception error)
				{
					_history.AddServerEvent($"Cannot own CLI process at port=[{port}]: {error.Message}");
					pingResult = PingResult.Mismatch;
					p.CompleteSuccess();
					return;
				}
				pingResult = PingResult.Match;
				p.CompleteSuccess();
			};
			await p;
			
			BeamStopDiagnostics.Factory(this, "ping-result", () => "port=" + port + " result=" + pingResult);
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
		public string requestId;
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
		private Promise _runPromise;
		private Promise _stopPromise;
		private readonly TaskCompletionSource<string> _stopUrl = new TaskCompletionSource<string>();
		private Task<HttpResponseMessage> _responseTask;
		private Process _requestServer;
		private string _requestUrl;
		private const int MaxPreResponseTransportRetries = 2;

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
		
		public Promise Run()
		{
			if (_stopPromise != null) return _stopPromise;
			_runPromise = RunImpl();
			return _runPromise;
		}

		private async Promise RunImpl()
		{
			_history.UpdateResolvingHostTime(id);

			try
			{
				for (var attempt = 0;; attempt++)
				{
					try
					{
						await RunHttpRequestOnce();
						return;
					}
					catch (PreResponseTransportException ex) when (attempt < MaxPreResponseTransportRetries)
					{
						if (attempt == 0)
						{
							_history.AddCustomLog(id,
								$"[Unity] local CLI transport failed before response; confirming server before retry. attempt=[{attempt + 1}]");
							await _factory.EnsureServerIsRunning();
						}
						else
						{
							_history.AddCustomLog(id,
								$"[Unity] local CLI transport failed before response; invalidating server before retry. attempt=[{attempt + 1}]");
							await _factory.InvalidateServer(ex.InnerException ?? ex);
						}
					}
				}
			}
			finally
			{
				_stopUrl.TrySetResult(null);
			}

		}

		private async Promise RunHttpRequestOnce()
		{
			await _factory.EnsureServerIsRunning();

			_history.UpdateStartTime(id, _factory.ExecuteUrl);
			_history.AddCustomLog(id, "[Unity] starting request...");

			var executeUrl = _factory.ExecuteUrl;
			var req = new HttpRequestMessage(HttpMethod.Post, executeUrl);
			var json = JsonUtility.ToJson(new BeamWebCommandRequest {commandLine = commandString, requestId = id});
			req.Content = new StringContent(json, Encoding.UTF8, "application/json");
			CliLogger.Log("Sending cli web request, " + json);
			var dispatchedIds = new List<long>();
			var p = new Promise();
			var responseStarted = false;

			try
			{
				// Per-call HttpClient. We tried sharing one on the factory but
				// pooled connections to the localhost CLI server went bad on
				// Windows (login would surface as TaskCanceledException from a
				// poisoned half-closed socket); a fresh client per command
				// avoids that.
				using var client = new HttpClient { Timeout = TimeSpan.FromDays(7) };
				_requestServer = _factory.ServerProcess;
				_requestUrl = executeUrl;
				var sendTask = _responseTask = client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, _cts.Token);
				_history.AddCustomLog(id, "[Unity] sent request...");

				using HttpResponseMessage response = await sendTask;
				responseStarted = true;
				_stopUrl.TrySetResult(response.Headers.TryGetValues("X-Beam-Request-Id", out var requestIds) && requestIds.Contains(id)
					? new Uri(new Uri(executeUrl), "stop").ToString() : "");
				_history.AddCustomLog(id, "[Unity] opened response...");

				using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
				_history.AddCustomLog(id, "[Unity] opened response stream...");

				using StreamReader reader = new StreamReader(streamToReadFrom);

				Task<string> readTask = null;
				while (true)
				{
					if (_cts.Token.IsCancellationRequested)
					{
						break;
					}

					readTask = reader.ReadLineAsync();
					var cancelTcs = new TaskCompletionSource<bool>();
					using (_cts.Token.Register(() => cancelTcs.TrySetResult(true)))
					{
						await Task.WhenAny(readTask, cancelTcs.Task);
					}

					if (_cts.Token.IsCancellationRequested)
					{
						_ = readTask.ContinueWith(completedReadTask => { _ = completedReadTask.Exception;},
							CancellationToken.None,
							TaskContinuationOptions.OnlyOnFaulted |
							TaskContinuationOptions.ExecuteSynchronously,
							TaskScheduler.Default);
						break;
					}

					var line = await readTask;
					if (line == null)
					{
						break;
					}

					if (line.Length == 0)
					{
						continue;
					}

					// remove life-cycle zero-width character
					line = line.Replace("\u200b", "");
					if (line.StartsWith(":")) continue; // SSE registration/keepalive comments.
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
			catch (Exception ex) when (IsRecoverableLocalTransportFailure(ex) && !responseStarted)
			{
				throw new PreResponseTransportException(ex);
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

		/// <summary>Cancel this server invocation and wait for its workers and dispatched callbacks to finish.</summary>
		public Promise StopAsync()
		{
			// Share an in-flight or acknowledged stop, but let the caller retry a failed acknowledgement.
			if (_stopPromise?.IsFailed == true) _stopPromise = null;
			return _stopPromise ??= _runPromise == null ? Promise.Success : StopImpl();
		}

		internal void StopForAssemblyReload()
		{
			using var timeout = new CancellationTokenSource(3000);
			try
			{
				if (_responseTask == null) return; // Still waiting for discovery; no request was sent.
				var body = JsonUtility.ToJson(new BeamWebCommandRequest { requestId = id });
				var stop = StopTransportForReload(_responseTask, _requestUrl, id, body, timeout.Token);
				_ = stop.ContinueWith(task => { _ = task.Exception; }, CancellationToken.None,
					TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
				if (!stop.Wait(3000)) throw new TimeoutException("CLI reload shutdown timed out.");
			}
			catch
			{
				// A broken transport cannot acknowledge cleanup. Stop the verified server that
				// received this request (the Windows launcher may only be a cmd.exe parent).
				if (_requestServer == null) throw;
				if (!_requestServer.HasExited)
				{
					_requestServer.Kill();
					if (!_requestServer.WaitForExit(1000))
						throw new TimeoutException("The CLI process did not exit during assembly reload.");
				}
			}
			finally { timeout.Cancel(); Cancel(); }
		}

		private static async Task StopTransportForReload(Task<HttpResponseMessage> responseTask,
			string executeUrl, string requestId, string body, CancellationToken cancellationToken)
		{
			// This path must not capture Unity's synchronization context or wait for its dispatcher.
			var response = await responseTask.ConfigureAwait(false);
			if (!response.Headers.TryGetValues("X-Beam-Request-Id", out var ids) || !ids.Contains(requestId))
				throw new InvalidOperationException("The CLI did not acknowledge request registration.");
			using var client = new HttpClient();
			using var content = new StringContent(body, Encoding.UTF8, "application/json");
			using var stopped = await client.PostAsync(new Uri(new Uri(executeUrl), "stop"), content,
				cancellationToken).ConfigureAwait(false);
			stopped.EnsureSuccessStatusCode();
		}

		private async Promise StopImpl()
		{
			using var diagnostic = BeamStopDiagnostics.Begin(id);
			if (_requestServer?.HasExited == true && _stopUrl.Task.IsCompleted)
			{
				// Registration has settled, so no discovery retry remains. Process exit proves its workers are gone.
				Cancel();
				try { await _runPromise; }
				catch (Exception) { }
				return;
			}
			// Registration is acknowledged before the response headers are sent. Never race /execute with /stop.
			string url;
			using (var timeout = new CancellationTokenSource())
			{
				timeout.CancelAfter(10000);
				diagnostic?.AttachTimeout(timeout.Token);
				try
				{
					var deadline = Task.Delay(Timeout.Infinite, timeout.Token);
					if (await Task.WhenAny(_stopUrl.Task, deadline) != _stopUrl.Task)
						throw new TimeoutException("CLI command registration timed out during shutdown. Retry shutdown before restarting the watcher.");
					url = await _stopUrl.Task;
					diagnostic?.Mark("registration.ready");
					if (url == "") throw new InvalidOperationException("The CLI server does not support acknowledged command shutdown. Update the CLI before switching content modes.");
					if (url != null)
					{
						using var client = new HttpClient();
						using var body = new StringContent(JsonUtility.ToJson(new BeamWebCommandRequest { requestId = id }), Encoding.UTF8, "application/json");
						try
						{
							var acknowledgement = client.PostAsync(url, body, timeout.Token);
							diagnostic?.AttachHttp(acknowledgement);
							using var response = await acknowledgement;
							diagnostic?.Mark("ack.continuation");
							response.EnsureSuccessStatusCode();
							diagnostic?.Mark("ack.success");
						}
						catch (Exception error)
						{
							diagnostic?.Mark("ack.throw." + error.GetType().Name);
							throw;
						}
					}
				}
				finally { diagnostic?.Mark("cancel.begin"); timeout.Cancel(); diagnostic?.Mark("cancel.end"); }
			}
			diagnostic?.Mark("run.drain.begin");
			// Run already drains the dispatcher in its finally block.
			try { await _runPromise; }
			catch (Exception) when (url != null)
			{
				// The server acknowledged cleanup. Keep the run failed for its callers,
				// but do not turn that original failure into a shutdown failure.
			}
		}

		public void Cancel()
		{
			if (_cts.IsCancellationRequested) return; // no-op
			// Mark the run's promise as observed so the cancellation
			// exception we're about to trigger isn't logged as an
			// UncaughtPromiseException. Awaiters still see the throw.
			_runPromise?.Error(_ => { });
			_cts.Cancel();
		}

		private static bool IsTransportFailure(Exception ex)
		{
			return ex is HttpRequestException
			       || ex is SocketException
			       || ex is ObjectDisposedException
			       || ex is IOException;
		}

		private bool IsRecoverableLocalTransportFailure(Exception ex)
		{
			return !_cts.IsCancellationRequested &&
			       (IsTransportFailure(ex) || ex is OperationCanceledException);
		}

		private class PreResponseTransportException : Exception
		{
			public PreResponseTransportException(Exception innerException)
				: base(innerException.Message, innerException)
			{
			}
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
