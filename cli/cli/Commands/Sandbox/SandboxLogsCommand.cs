using System.Linq;
using Beamable.Common.Semantics;
using Beamable.Server;
using cli.Commands.Project;
using cli.Services;
using cli.Services.Sandbox;

namespace cli.Sandbox;

public class SandboxLogsCommandArgs : CommandArgs
{
	public string? Service;
}

public class SandboxLogsCommand : StreamCommand<SandboxLogsCommandArgs, TailLogMessageForClient>
{
	public SandboxLogsCommand() : base("logs", "Tail the logs of the running sandbox for this account")
	{
	}

	public override void Configure()
	{
		AddOption(new System.CommandLine.Option<string?>("--service",
				() => null,
				"Sandbox service name to tail. Defaults to the single local sandbox if there's exactly one."),
			(args, v) => args.Service = v);
	}

	public override async Task Handle(SandboxLogsCommandArgs args)
	{
		var serviceName = ResolveServiceName(args);

		// Thin alias over the existing project-logs flow. The sandbox MS registers as a
		// regular Beamable microservice from the gateway's perspective, so the same
		// log-tailing path that works for portal extensions and microservices works here.
		// CommandArgs.Provider is the seam: every framework service the inner command
		// needs (AppContext, Lifecycle, ConfigService, ...) resolves from it.
		var tailArgs = args.Create<TailLogsCommandArgs>();
		tailArgs.service = new ServiceName(serviceName);

		await ProjectLogsService.Handle(tailArgs, msg =>
		{
			Log.Information($"[{msg.logLevel}] {msg.message}");
			SendResults(new TailLogMessageForClient(msg));
		}, args.Lifecycle.Source);
	}

	private static string ResolveServiceName(SandboxLogsCommandArgs args)
	{
		if (!string.IsNullOrWhiteSpace(args.Service)) return args.Service!;

		var state = new SandboxStateService();
		var local = state.ListLocalSandboxes().Select(t => t.serviceName).ToList();

		return local.Count switch
		{
			0 => throw new CliException(
				"No local sandboxes found. Start one with `beam sandbox start`, or pass `--service <name>` to tail a remote sandbox."),
			1 => local[0],
			_ => throw new CliException(
				"Multiple local sandboxes found; pass `--service <name>` to pick one. Try `beam sandbox ps`.")
		};
	}
}
