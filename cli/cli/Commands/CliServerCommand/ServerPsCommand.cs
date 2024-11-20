using cli.Services.HttpServer;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.CommandLine;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;

namespace cli.CliServerCommand;

public class ServerPsCommandArgs : CommandArgs, IServerFilterArgs
{
	public string VersionFilter { get; set; }
	public string OwnerFilter { get; set; }
	public int Pid { get; set; }
	public int Port { get; set; }
}

public interface IServerFilterArgs
{
	public string VersionFilter { get; set; }
	public string OwnerFilter { get; set; }
	public int Pid { get; set; }
	public int Port { get; set; }
}


public class ServerPsCommandResult
{
	public List<ServerDescriptor> servers = new List<ServerDescriptor>();
}

public class ServerDescriptor
{
	public int port;
	public int pid;
	public long inflightRequests;
	public string url;
	public string owner;
	public string version;
}

public class ServerPsCommand : StreamCommand<ServerPsCommandArgs, ServerPsCommandResult>
{
	private static readonly IPAddress Ip = IPAddress.Loopback;
	private static HttpClient _client = new HttpClient();

	public ServerPsCommand() : base("ps", "List out available CLI servers")
	{
	}

	public override void Configure()
	{
		ConfigureFilterArgs(this);
	}

	public static void ConfigureFilterArgs<T>(AppCommand<T> command)
		where T : CommandArgs, IServerFilterArgs
	{
		command.AddOption(new Option<string>("--version", "only match servers that match the given version"),
			(args, i) => args.VersionFilter = i);
		command.AddOption(new Option<string>("--owner", "only match servers that match the given owner"),
			(args, i) => args.OwnerFilter = i);
		
		command.AddOption(new Option<int>("--port", "only match servers that match the given port"),
			(args, i) => args.Port = i);
		command.AddOption(new Option<int>("--pid", "only match servers that match the given process id"),
			(args, i) => args.Pid = i);
	}

	public override async Task Handle(ServerPsCommandArgs args)
	{
		var servers = await GetServers(args);
		servers = servers.OrderBy(a => a.port).ToList();
		
		SendResults(new ServerPsCommandResult
		{
			servers = servers
		});
		
		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]url[/]");
		table.AddColumn("[bold]pid[/]");
		table.AddColumn("[bold]owner[/]");
		table.AddColumn("[bold]version[/]");
		table.AddColumn("[bold]req count[/]");

		foreach (var server in servers)
		{
			table.AddRow(
				new Text(server.url + "/info"), 
				new Text(server.pid.ToString()), 
				new Text(server.owner),
				new Text(server.version),
				new Text(server.inflightRequests.ToString())
				);
		}
		
		AnsiConsole.Write(table);
	}

	public static async Task<List<ServerDescriptor>> GetServers(IServerFilterArgs filters)
	{
		var servers = new List<ServerDescriptor>();
		await foreach (var server in DiscoverServers(filters))
		{
			servers.Add(server);
		}
		return servers;
	}

	public static async Task<ServerInfoResponse> CheckServer(string url)
	{
		try
		{
			var json = await _client.GetStringAsync($"{url}/info");
			var res = JsonConvert.DeserializeObject<ServerInfoResponse>(json);
			return res;
		}
		catch
		{
			return null;
		}
	}
	

	public static async IAsyncEnumerable<ServerDescriptor> DiscoverServers(IServerFilterArgs filters)
	{
		var startPort = ServeCliCommand.DEFAULT_PORT - 1;
		var endPort = startPort + 2000;
		var port = startPort;
		const int maxThreads = 100;

		var channel = Channel.CreateUnbounded<ServerDescriptor>(new UnboundedChannelOptions
		{
			SingleReader = true, SingleWriter = false
		});
		var tasks = new List<Task>();
		
		for (var threadId = 0; threadId < maxThreads; threadId++)
		{
			var task = Task.Run(async () =>
			{
				while (true)
				{
					var thisPort = Interlocked.Increment(ref port);
					if (thisPort > endPort) break;

					var remoteEP = new IPEndPoint(Ip, thisPort);
					var sender = new Socket(Ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
					try
					{
						await sender.ConnectAsync(remoteEP);
						sender.Shutdown(SocketShutdown.Both);
						sender.Close();

						var url = $"http://127.0.0.1:{thisPort}";
						var info = await CheckServer(url);
						if (info != null)
						{
							if (!string.IsNullOrEmpty(filters?.VersionFilter) && !string.Equals(filters.VersionFilter,
								    info.version, StringComparison.InvariantCultureIgnoreCase))
							{
								Log.Debug($"Skipping server at port=[{thisPort}] with version=[{info.version}] because it did not match version filter");
								continue;
							}
							
							if (!string.IsNullOrEmpty(filters?.OwnerFilter) && !string.Equals(filters.OwnerFilter,
								    info.owner, StringComparison.InvariantCultureIgnoreCase))
							{
								Log.Debug($"Skipping server at port=[{thisPort}] with owner=[{info.owner}] because it did not match owner filter");
								continue;
							}

							if (filters?.Port > 0 && filters.Port != thisPort)
							{
								Log.Debug($"Skipping server at port=[{thisPort}] because it did not match port filter");
								continue;
							}
							
							if (filters?.Pid > 0 && filters.Pid != info.pid)
							{
								Log.Debug($"Skipping server at port=[{thisPort}] with pid=[{info.pid}] because it did not match pid filter");
								continue;
							}
							
							await channel.Writer.WriteAsync(new ServerDescriptor
							{
								inflightRequests = info.inflightRequests,
								version = info.version,
								owner = info.owner,
								pid = info.pid,
								port = thisPort,
								url = url
							});
						}
						
					}
					catch (SocketException)
					{
						// let it go.
					}

				}
			});
			tasks.Add(task);
		}

		var allTask = Task.WhenAll(tasks);

		while (!allTask.IsCompleted)
		{
			while (channel.Reader.TryRead(out var scannedPort))
			{
				yield return scannedPort;
			}
			await Task.Delay(1);
		}
		while (channel.Reader.TryRead(out var scannedPort))
		{
			yield return scannedPort;
		}
		
	}
}
