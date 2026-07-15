using Beamable.Server;
using cli.Services.Sandbox;
using Spectre.Console;

namespace cli.Sandbox;

public class SandboxPsCommandArgs : CommandArgs
{
}

public class SandboxPsCommandResult
{
	public List<SandboxPsEntry> sandboxes = new();
}

public class SandboxPsEntry
{
	public string serviceName = string.Empty;
	public string? joinCode;
	public bool isLocal;
}

public class SandboxPsCommand : AtomicCommand<SandboxPsCommandArgs, SandboxPsCommandResult>, IStandaloneCommand, ISkipManifest
{
	public SandboxPsCommand() : base("ps", "List sandboxes visible to this account")
	{
	}

	public override void Configure()
	{
	}

	public override Task<SandboxPsCommandResult> GetResult(SandboxPsCommandArgs args)
	{
		// v1 skeleton: enumerate the local code files only. Once the MS wire-up lands, this
		// will also walk DiscoveryService for any BeamSandbox_* services this account owns
		// (including those running on other machines) and render the code column as `—`
		// for rows whose code file isn't on this machine.
		var state = new SandboxStateService();
		var entries = state.ListLocalSandboxes()
			.Select(t => new SandboxPsEntry
			{
				serviceName = t.serviceName,
				joinCode = t.code,
				isLocal = true,
			})
			.OrderBy(e => e.serviceName)
			.ToList();

		var result = new SandboxPsCommandResult { sandboxes = entries };

		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]service[/]");
		table.AddColumn("[bold]code[/]");
		table.AddColumn("[bold]source[/]");

		if (entries.Count == 0)
		{
			table.AddRow(new Markup("[grey](no sandboxes running)[/]"), new Text("—"), new Text("—"));
		}
		else
		{
			foreach (var e in entries)
			{
				table.AddRow(
					new Text(e.serviceName),
					new Text(e.joinCode ?? "—"),
					new Markup(e.isLocal ? "[green]local[/]" : "remote"));
			}
		}

		AnsiConsole.Write(table);

		return Task.FromResult(result);
	}
}
