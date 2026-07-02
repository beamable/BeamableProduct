using Beamable.Server;
using CliWrap;
using Spectre.Console;
using System.CommandLine;
using System.Text;

namespace cli.Commands.LocalStack;

public class LocalStackValidateCommandArgs : CommandArgs
{
}

public class LocalStackDependencyCheck
{
	public string name;
	public bool ok;
	public string detail;
}

public class LocalStackValidateCommandResult
{
	public bool allOk;
	public List<LocalStackDependencyCheck> checks = new List<LocalStackDependencyCheck>();
}

/// <summary>
/// Checks that the tools the local stack needs are available: <c>dotnet</c>, <c>docker</c>, a Java 8 home,
/// <c>mvn</c>, and <c>npm</c>. Check-only — it reports what is missing with a remediation hint but does not
/// install anything. (Ported in spirit from #4258 <c>CheckDepsCommand</c>, without the auto-install.)
/// </summary>
public class LocalStackValidateCommand
	: AtomicCommand<LocalStackValidateCommandArgs, LocalStackValidateCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackValidateCommand() : base("validate", "Check that the local stack's dependencies are installed")
	{
	}

	public override void Configure() { }

	public override async Task<LocalStackValidateCommandResult> GetResult(LocalStackValidateCommandArgs args)
	{
		var result = new LocalStackValidateCommandResult();

		// dotnet (resolved by the CLI already).
		var dotnet = args.AppContext?.DotnetPath ?? "dotnet";
		result.checks.Add(await CheckProgram("dotnet", dotnet, "--version",
			hint: "install the .NET SDK from https://dotnet.microsoft.com/download"));

		// docker (via the CLI's resolver).
		if (DockerPathOption.TryGetDockerPath(out var dockerPath, out var dockerErr))
			result.checks.Add(await CheckProgram("docker", dockerPath, "--version",
				hint: "install Docker Desktop from https://docs.docker.com/get-docker/"));
		else
			result.checks.Add(new LocalStackDependencyCheck { name = "docker", ok = false, detail = dockerErr });

		// Java 8 home (required for the Scala backend).
		if (JavaPathOption.TryGetJavaHome(out var javaHome, out var javaErr))
			result.checks.Add(new LocalStackDependencyCheck { name = "java (8)", ok = true, detail = javaHome });
		else
			result.checks.Add(new LocalStackDependencyCheck { name = "java (8)", ok = false, detail = javaErr });

		// maven + npm (expected on PATH).
		result.checks.Add(await CheckProgram("mvn", "mvn", "-version",
			hint: "install Maven (e.g. `brew install maven`)"));
		result.checks.Add(await CheckProgram("npm", "npm", "--version",
			hint: "install Node.js/npm from https://nodejs.org"));

		result.allOk = result.checks.All(c => c.ok);
		Render(result);
		return result;
	}

	private static async Task<LocalStackDependencyCheck> CheckProgram(string name, string program, string arguments, string hint)
	{
		var check = new LocalStackDependencyCheck { name = name };
		var output = new StringBuilder();
		try
		{
			var res = await CliWrap.Cli.Wrap(program)
				.WithArguments(arguments)
				.WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
				.WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
				.WithValidation(CommandResultValidation.None)
				.ExecuteAsync();

			check.ok = res.ExitCode == 0;
			check.detail = check.ok
				? output.ToString().Split('\n').FirstOrDefault()?.Trim()
				: $"'{program}' exited {res.ExitCode} — {hint}";
		}
		catch (Exception)
		{
			check.ok = false;
			check.detail = $"'{program}' not found — {hint}";
		}

		return check;
	}

	private static void Render(LocalStackValidateCommandResult result)
	{
		var table = new Table();
		table.Border(TableBorder.Simple);
		table.AddColumn("[bold]dependency[/]");
		table.AddColumn("[bold]status[/]");
		table.AddColumn("[bold]detail[/]");

		foreach (var c in result.checks)
		{
			table.AddRow(
				new Markup(Markup.Escape(c.name)),
				new Markup(c.ok ? "[green]ok[/]" : "[red]missing[/]"),
				new Markup(Markup.Escape(c.detail ?? "")));
		}

		AnsiConsole.Write(table);
		if (result.allOk)
			Log.Information("All local-stack dependencies are available.");
		else
			Log.Warning("Some local-stack dependencies are missing — see the table above.");
	}
}
