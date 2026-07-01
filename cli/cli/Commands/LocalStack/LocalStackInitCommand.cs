using Beamable.Server;
using cli.Services.LocalStack;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackInitCommandArgs : CommandArgs
{
	public string configPath;
	public bool force;
	public string host;
	public string portalUrl;
	public string apiDir;
	public string scalaDir;
	public string portalDir;
	public string scalaServices;
	public string services;
	public string extensions;
	public bool updateServices;
}

public class LocalStackInitCommandResult
{
	public string manifestPath;
	public int stepCount;
	public bool created;
}

/// <summary>
/// Interactively builds a reference local-stack manifest (see <see cref="LocalStackTemplate"/>) that
/// <c>beam local up</c> then runs. Each value is prompted for with its default shown — press Enter to
/// accept it. Any value passed as an option is used as-is (and not prompted for); <c>--quiet</c> (or a
/// non-interactive console) skips all prompts and uses the defaults / passed values. Repo paths left
/// empty become <c>&lt;EDIT: ...&gt;</c> placeholders to fill in by hand.
/// </summary>
public class LocalStackInitCommand
	: AtomicCommand<LocalStackInitCommandArgs, LocalStackInitCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackInitCommand() : base("init", "Write a reference local-stack manifest to edit and run")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--config", "Path to write the manifest to (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<bool>("--force", "Overwrite an existing manifest without asking"),
			(args, v) => args.force = v);
		AddOption(new Option<string>("--host", "Backend API host baked into the manifest"),
			(args, v) => args.host = v);
		AddOption(new Option<string>("--portal-url", "Portal frontend URL baked into the manifest"),
			(args, v) => args.portalUrl = v);
		AddOption(new Option<string>("--api-dir", "Absolute path to the BeamableAPI (C# gateway) repo"),
			(args, v) => args.apiDir = v);
		AddOption(new Option<string>("--scala-dir", "Absolute path to the BeamableBackend (Scala) repo"),
			(args, v) => args.scalaDir = v);
		AddOption(new Option<string>("--portal-dir", "Absolute path to the portal frontend repo"),
			(args, v) => args.portalDir = v);
		AddOption(new Option<string>("--scala-services", "Comma/space separated Scala tools/* services to run"),
			(args, v) => args.scalaServices = v);
		AddOption(new Option<string>("--services", "Comma/space separated microservice ids to run"),
			(args, v) => args.services = v);
		AddOption(new Option<string>("--extensions", "Comma/space separated portal extension ids to run"),
			(args, v) => args.extensions = v);
		AddOption(new Option<bool>("--update-services", "Only update the microservice/extension steps of an existing manifest, leaving everything else untouched"),
			(args, v) => args.updateServices = v);
	}

	private static List<string> Split(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

	private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

	/// <summary>
	/// Resolves one value: a passed option wins; otherwise in quiet mode the default is used; otherwise
	/// the user is prompted with the default shown (Enter accepts it).
	/// </summary>
	private static string Ask(string title, string passed, string def, bool quiet, bool allowEmpty)
	{
		// A non-null value was explicitly provided on the command line (even ""), so honor it verbatim —
		// this lets `--extensions ""` clear the list rather than falling back to the default.
		if (passed != null) return passed;
		if (quiet) return def ?? string.Empty;

		var prompt = new TextPrompt<string>(title).PromptStyle("green");
		if (!string.IsNullOrEmpty(def)) prompt.DefaultValue(def);
		if (allowEmpty || string.IsNullOrEmpty(def)) prompt.AllowEmpty();
		return AnsiConsole.Prompt(prompt);
	}

	public override Task<LocalStackInitCommandResult> GetResult(LocalStackInitCommandArgs args)
	{
		// Prompt only when we actually have an interactive console; otherwise fall back to defaults.
		var quiet = args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive;
		var defaults = new LocalStackTemplate.Options();

		// Where the manifest lives.
		var defaultPath = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		var path = Path.GetFullPath(Ask("Where is the manifest?",
			args.configPath, defaultPath, quiet, allowEmpty: false));

		// Update-only mode: rewrite just the microservice/extension steps of an existing manifest.
		if (args.updateServices)
			return Task.FromResult(UpdateServices(args, path, quiet));

		if (File.Exists(path) && !args.force)
		{
			if (quiet)
				throw new CliException($"A manifest already exists at {path}. Pass --force to overwrite, or edit it directly.");
			if (!AnsiConsole.Confirm($"[yellow]{path}[/] already exists. Overwrite it?", defaultValue: false))
				throw new CliException("Aborted — a manifest already exists.");
		}

		// Repo paths (no default — leaving empty writes an <EDIT: ...> placeholder).
		var apiDir = Ask("Absolute path to the [green]BeamableAPI[/] (C# gateway) repo [grey](empty = placeholder)[/]:",
			args.apiDir, null, quiet, allowEmpty: true);
		var scalaDir = Ask("Absolute path to the [green]BeamableBackend[/] (Scala) repo [grey](empty = placeholder)[/]:",
			args.scalaDir, null, quiet, allowEmpty: true);
		var portalDir = Ask("Absolute path to the [green]portal frontend[/] repo [grey](empty = placeholder)[/]:",
			args.portalDir, null, quiet, allowEmpty: true);

		// Endpoints (defaults — Enter accepts).
		var host = Ask("Backend API [green]host[/]:", args.host, defaults.host, quiet, allowEmpty: false);
		var portalUrl = Ask("[green]Portal[/] frontend URL:", args.portalUrl, defaults.portalUrl, quiet, allowEmpty: false);

		// Service lists (defaults — Enter accepts).
		var scalaServices = Ask("[green]Scala[/] services to run [grey](space separated)[/]:",
			args.scalaServices, string.Join(" ", LocalStackTemplate.DefaultScalaServices), quiet, allowEmpty: false);
		var services = Ask("[green]Microservice[/] ids to run [grey](space separated, empty = none)[/]:",
			args.services, "", quiet, allowEmpty: true);
		var extensions = Ask("[green]Portal extension[/] ids to run [grey](space separated, empty = none)[/]:",
			args.extensions, "", quiet, allowEmpty: true);

		var options = new LocalStackTemplate.Options
		{
			host = host,
			portalUrl = portalUrl,
			apiDir = NullIfEmpty(apiDir),
			scalaDir = NullIfEmpty(scalaDir),
			portalDir = NullIfEmpty(portalDir),
			scalaServices = Split(scalaServices),
			services = Split(services),
			extensions = Split(extensions),
		};

		var config = LocalStackTemplate.Create(options);
		LocalStackConfigIO.Save(path, config);

		Log.Information($"Wrote local-stack manifest to {path} ({config.steps.Count} steps).");
		Log.Information("Edit any <EDIT: ...> paths, then run: beam local up");

		return Task.FromResult(new LocalStackInitCommandResult
		{
			manifestPath = path,
			stepCount = config.steps.Count,
			created = true
		});
	}

	/// <summary>
	/// Rewrites only the microservice/extension steps of an existing manifest, leaving every other step
	/// (docker, gateway, Scala, portal) and all edits untouched. Current ids are offered as the prompt
	/// defaults; an empty answer removes all steps of that kind.
	/// </summary>
	private LocalStackInitCommandResult UpdateServices(LocalStackInitCommandArgs args, string path, bool quiet)
	{
		if (!File.Exists(path))
			throw new CliException($"No manifest at {path} to update. Run `beam local init` first.");

		var config = LocalStackConfigIO.Load(path);

		bool IsMicroservice(LocalStackStep s) => s.name?.StartsWith(LocalStackTemplate.MicroservicePrefix) == true;
		bool IsExtension(LocalStackStep s) => s.name?.StartsWith(LocalStackTemplate.ExtensionPrefix) == true;

		var currentServices = string.Join(" ", config.steps.Where(IsMicroservice)
			.Select(s => s.name.Substring(LocalStackTemplate.MicroservicePrefix.Length)));
		var currentExtensions = string.Join(" ", config.steps.Where(IsExtension)
			.Select(s => s.name.Substring(LocalStackTemplate.ExtensionPrefix.Length)));

		var services = Ask("[green]Microservice[/] ids to run [grey](space separated, empty = none)[/]:",
			args.services, currentServices, quiet, allowEmpty: true);
		var extensions = Ask("[green]Portal extension[/] ids to run [grey](space separated, empty = none)[/]:",
			args.extensions, currentExtensions, quiet, allowEmpty: true);

		// Drop the old beam steps and append the new set (microservices before extensions).
		config.steps.RemoveAll(s => IsMicroservice(s) || IsExtension(s));
		foreach (var svc in Split(services) ?? new List<string>())
			config.steps.Add(LocalStackTemplate.MicroserviceStep(svc));
		foreach (var ext in Split(extensions) ?? new List<string>())
			config.steps.Add(LocalStackTemplate.ExtensionStep(ext));

		LocalStackConfigIO.Save(path, config);

		Log.Information($"Updated microservice/extension steps in {path} ({config.steps.Count} steps total).");

		return new LocalStackInitCommandResult
		{
			manifestPath = path,
			stepCount = config.steps.Count,
			created = false
		};
	}
}
