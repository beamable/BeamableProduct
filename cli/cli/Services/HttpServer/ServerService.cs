using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using Beamable.Common.Util;
using Beamable.Server.Common;
using cli.CliServerCommand;
using Newtonsoft.Json;
using Serilog;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace cli.Services.HttpServer;


[Serializable]
public class ServerRequest
{
	public string commandLine;
}

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

[Serializable]
public class ServerMessageResponse
{
	public string message;
}

[Serializable]
public class ServerErrorResponse
{
	public string type;
	public string stack;
	public string message;
}

public class ServerService
{
	private HttpListener _listener;

	public ServerService()
	{
		
	}

	public async Task RunServer(ServeCliCommandArgs args)
	{
		if (_listener != null) throw new InvalidOperationException("already running");
		_listener = new HttpListener();
		var uri = $"http://localhost:{args.port}/";
		_listener.Prefixes.Add(uri);
		_listener.Start();
		while (true)
		{
			HttpListenerContext ctx = await _listener.GetContextAsync();

			var _ = Task.Run(async () =>
			{
				await HandleRequest(args, uri, ctx);
			});
		}
		
	}

	static async Task HandleRequest(ServeCliCommandArgs args, string uri, HttpListenerContext ctx)
	{
		// while (true)
		{
			
			// Peel out the requests and response objects
			HttpListenerRequest req = ctx.Request;
			HttpListenerResponse resp = ctx.Response;
				
			var frag = ctx.Request.Url.ToString().Substring(uri.Length);
			Log.Verbose("got message: " + frag);
			string response = null;
			int status = 200;
			byte[] data;
			try
			{
				switch (frag)
				{
					case "info":
						var info = await HandleInfo(args);
						response = JsonConvert.SerializeObject(info);
						data = Encoding.UTF8.GetBytes(response);
						await resp.OutputStream.WriteAsync(data, 0, data.Length);
						resp.StatusCode = status;
						break;
					case "execute":
						resp.Headers.Set(HttpResponseHeader.ContentType, "text/event-stream");
						await HandleExec(req.InputStream, resp);
						break;
					default:
						Log.Information("Unknown route");
						response = JsonConvert.SerializeObject(new ServerErrorResponse
						{
							message = "unknown route",
							type = "UnhandledRoute"
						});
						status = 400;
						data = Encoding.UTF8.GetBytes(response);
						await resp.OutputStream.WriteAsync(data, 0, data.Length);
						resp.StatusCode = status;
						break;
				}
			}
			catch (Exception ex)
			{
				response = JsonConvert.SerializeObject(new ServerErrorResponse
				{
					message = ex.Message, type = ex.GetType().Name, stack = ex.StackTrace
				});
				status = 500;
				data = Encoding.UTF8.GetBytes(response);
				await resp.OutputStream.WriteAsync(data, 0, data.Length);
				resp.StatusCode = status;
			}

			
			resp.Close();
		}
	}

	static Task<ServerInfoResponse> HandleInfo(ServeCliCommandArgs args)
	{
		return Task.FromResult(new ServerInfoResponse
		{
			version = BeamAssemblyVersionUtil.GetVersion<ServerService>(),
			owner = args.owner
		});
	}

	static async Task HandleExec(Stream stream, HttpListenerResponse response)
	{
		using var inputStream = new StreamReader(stream);
		var input = await inputStream.ReadToEndAsync();
		var req = JsonConvert.DeserializeObject<ServerRequest>(input);
		
		Log.Verbose("virtualizing " + req.commandLine);
		
		var app = new App();
		app.Configure(builder =>
		{
			builder.Remove<IDataReporterService>();
			builder.AddSingleton<IDataReporterService, ServerReporterService>(provider => new ServerReporterService(provider, response));
		});
		app.Build();

		
		await app.RunWithSingleString(req.commandLine);
		// return new ServerMessageResponse { message = req.args };
	}
}

public class ServerReporterService : IDataReporterService
{
	private readonly HttpListenerResponse _resp;
	private StreamWriter _streamWriter;
	private IAppContext _appContext;

	public ServerReporterService(IDependencyProvider provider, HttpListenerResponse resp)
	{
		_appContext = provider.GetService<IAppContext>();
		_resp = resp;
		_streamWriter = new StreamWriter(_resp.OutputStream);
	} 
	
	public void Report(string rawMessage)
	{
		_streamWriter.WriteLine("data: " + rawMessage);
		_streamWriter.Flush();
	}
	
	public void Report<T>(string type, T data)
	{
		var pt = new ReportDataPoint<T>
		{
			data = data,
			type = type,
			ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
		};
		var json = JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance);
		Report(json);
	}

	public void Exception(Exception ex, int exitCode, string invocationContext)
	{
		var result = new ErrorOutput
		{
			exitCode = exitCode,
			invocation = invocationContext,
			message = ex?.Message,
			stackTrace = ex?.StackTrace,
			typeName = ex?.GetType().Name,
			fullTypeName = ex?.GetType().FullName
		};
		Report(DefaultErrorStream.CHANNEL, result);
	}
}
