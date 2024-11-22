using Serilog;
using System.Diagnostics;

namespace cli.CliServerCommand;

public class ServerKillCommandArgs : CommandArgs, IServerFilterArgs
{
	public string VersionFilter { get; set; }
	public string OwnerFilter { get; set; }
	public int Pid { get; set; }
	public int Port { get; set; }
}

public class ServerKillCommandResult
{
	public List<ServerDescriptor> stoppedServers = new List<ServerDescriptor>();
}

public class ServerKillCommand : AtomicCommand<ServerKillCommandArgs, ServerKillCommandResult>, IStandaloneCommand, ISkipManifest
{
	public override bool AutoLogOutput => false;

	public ServerKillCommand() : base("clear", "Kill a running server instance")
	{
	}

	public override void Configure()
	{
		ServerPsCommand.ConfigureFilterArgs(this);
	}

	public override async Task<ServerKillCommandResult> GetResult(ServerKillCommandArgs args)
	{
		var res = new ServerKillCommandResult();
		var procs = Process.GetProcesses();
		Log.Information("Clearing servers...");
		
		await foreach (var server in ServerPsCommand.DiscoverServers(args))
		{
			if (server.pid == 0)
			{
				Log.Warning($"The server on port=[{server.port}] was started with an older version of the CLI that does not report the PID, and cannot be stopped via this command.");
				continue;
			}
			
			var proc = procs.FirstOrDefault(p => p.Id == server.pid);
			if (proc == null)
			{
				Log.Warning($"Unable to send kill signal to server at port=[{server.port}] because pid=[{server.pid}] was not available.");
				continue;
			}
			

			try
			{
				Log.Debug($"about to stop server at port=[{server.port}], given-id=[{server.pid}] proc=[{proc.Id}]");
				proc.Kill();
				res.stoppedServers.Add(server);
				Log.Information($"Stopped server at port=[{server.port}]");
			}
			catch (Exception ex)
			{
				Log.Warning($"Exception occured while trying to shutdown server at port=[{server.port}] with pid=[{server.pid}];\nerror=[{ex.GetType().Name}] message=[{ex.Message}]");
			}
		}
		return res;
	}
}
