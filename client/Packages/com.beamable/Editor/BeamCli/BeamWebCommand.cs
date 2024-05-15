using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
		public string Owner => BeamCliUtil.CLI_PATH.ToLowerInvariant();

		public static string Version => BeamCliUtil.CLI_VERSION;
		
		
		// TODO: how do we store this port so that we don't have to ALWAYS go through mismatches to re-find it? 
		public int port = 8432; // beas 
		public BeamCommandFactory processFactory;
		public BeamCommands processCommands;
		public BeamableDispatcher dispatcher;

		public Promise onReady = null; 

		public BeamWebCommandFactory(IBeamableRequester requester, BeamableDispatcher dispatcher)
		{
			this.dispatcher = dispatcher;
			processFactory = new BeamCommandFactory(dispatcher);
			processCommands = new BeamCommands(requester, processFactory);
		}
		
		public IBeamCommand Create()
		{
			return new BeamWebCommand(this);
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
						break;
					case PingResult.Mismatch:
						// ah, this server is being used for a different project...
						CliLogger.Log("mismatch server :(");
						port++; // by increasing the port, maybe we'll find our server soon...
						break;
					case PingResult.NoServer:
						// bummer, no server exists for us, so we need to turn it on...
						CliLogger.Log("Starting server.... " + port + " , " + Owner);
						processCommands.defaultBeamArgs = processCommands.ConstructDefaultArgs();
						processCommands.defaultBeamArgs.log = "verbose";
						processCommands.defaultBeamArgs.pretty = true;
						var serverCommand = processCommands.ServerServe(new ServerServeArgs()
						{
							port = port, 
							owner = "\"" + Owner + "\"",
							autoIncPort = true,
							selfDestructSeconds = 15 // TODO: validate that a low ttl will restart the server
						});
						var waitForResult = new Promise();
						serverCommand.OnStreamServeCliCommandOutput(data =>
						{
							port = data.data.port;
							waitForResult.CompleteSuccess();
						});
						serverCommand.OnError(err =>
						{
							waitForResult.CompleteSuccess();
						});
						var _ = serverCommand.Run();
							
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
				var json = await localClient.GetStringAsync(InfoUrl);
				var res = JsonUtility.FromJson<ServerInfoResponse>(json);

				var ownerMatches = res.owner == Owner;
				var versionMatches = res.version == Version;
				
				if (!ownerMatches || !versionMatches)
				{
					CliLogger.Log($"ping mismatch. Required version=[{Version}] Received version=[{res.version}] Required owner=[{Owner}] Received owner=[{res.owner}]");
					return PingResult.Mismatch;
				}

				return PingResult.Match; 
			}
			catch 
			{
				return PingResult.NoServer;
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
		private string _command;
		private HttpClient _localClient;
		private Action<ReportDataPointDescription> _callbacks = (_) => { };
		private BeamWebCommandFactory _factory;

		public BeamWebCommand(BeamWebCommandFactory factory)
		{
			_factory = factory;
			_localClient = factory.localClient;
		}

		public void SetCommand(string command)
		{
			_command = command.Substring("beam".Length);
		}

		public async Promise Run()
		{
			await _factory.EnsureServerIsRunning();

			var req = new HttpRequestMessage(HttpMethod.Post, _factory.ExecuteUrl);
			var json = JsonUtility.ToJson(new BeamWebCommandRequest {commandLine = _command});
			req.Content = new StringContent(json, Encoding.UTF8, "application/json");
			CliLogger.Log("Sending cli web request, " + json);
			try
			{
				using HttpResponseMessage response =
					await _localClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);

				using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();
				using StreamReader reader = new StreamReader(streamToReadFrom);

				while (!reader.EndOfStream)
				{
					var line = await reader.ReadLineAsync();
					if (string.IsNullOrEmpty(line)) continue; // TODO: what if the message contains a \n character?

					// remove life-cycle zero-width character 
					line = line.Replace("\u200b", "");
					if (!line.StartsWith("data: "))
					{
						Debug.LogWarning($"CLI received a message over the local-server that did not start with the expected 'data: ' format. line=[{line}]");
						continue;
					}

					_factory.dispatcher.Schedule(() => // put callback on separate work queue.
					{
						var lineJson = line
							.Substring("data: ".Length); // remove the Server-Side-Event notation
						               
						CliLogger.Log("received, " + lineJson, "from " + _command);

						var res = JsonUtility.FromJson<ReportDataPointDescription>(lineJson);
						res.json = lineJson;

						_callbacks?.Invoke(res);
					});
				}
			}
			catch (HttpRequestException socketException)
			{
				CliLogger.Log($"Socket exception happened. command=[{_command}] url=[{_factory.ExecuteUrl}] " +
				              socketException.Message);
				throw;
			}
			catch (IOException ioException)
			{

				// in this event, it is likely that the CLI server was terminated without politely closing connections.
				//  that is _fine_, but we need to handle it.
				CliLogger.Log("cli server died, " + ioException.Message);
			}
			catch (Exception ex)
			{
				CliLogger.Log(
					$"Socket exception happened general. command=[{_command}] url=[{_factory.ExecuteUrl}] type=[{ex.GetType().FullName}]" +
					ex.Message);
				Debug.LogException(ex);
			}

		}

		public IBeamCommand On<T>(string type, Action<ReportDataPoint<T>> cb)
		{
			On(desc =>
			{
				if (desc.type != type) return;
				var pt = JsonUtility.FromJson<ReportDataPoint<T>>(desc.json);
				cb?.Invoke(pt);
			});

			return this;
		}

		public IBeamCommand On(Action<ReportDataPointDescription> cb)
		{
			_callbacks += cb;
			return this;
		}

		public IBeamCommand OnError(Action<ReportDataPoint<ErrorOutput>> cb)
		{
			return On<ErrorOutput>("error", cb);
		}

		public IBeamCommand OnTerminate(Action<ReportDataPoint<EofOutput>> cb)
		{
			return this;
		}
	}
}
