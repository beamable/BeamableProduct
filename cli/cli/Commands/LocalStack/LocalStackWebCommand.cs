using Beamable.Server;
using cli.Services.LocalStack;
using cli.Services.Web;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackWebCommandArgs : CommandArgs
{
	public string action;
	public bool force;
}

public class LocalStackWebCommandResult
{
	public string sdk;
	public string toolkit;
}

/// <summary>
/// What the stack's <c>web: build</c>, <c>web: publish</c>, <c>web: install</c> and <c>web: watch</c> steps run (see
/// <see cref="WebPackagesService"/>). Hidden: a stack runs them, a person runs the stack. Their folders come from
/// <see cref="WebPackagesService.ProductDirVariable"/> and <see cref="WebPackagesService.WorkspaceVariable"/>.
/// </summary>
public class LocalStackWebCommand
	: AtomicCommand<LocalStackWebCommandArgs, LocalStackWebCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public const string Build = "build", Publish = "publish", Install = "install", Watch = "watch";

	public override bool AutoLogOutput => false;

	public LocalStackWebCommand() : base("web", "INTERNAL The local web packages loop a stack's web: steps run")
	{
		IsHidden = true;
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("action", $"What to do: {Build}, {Publish}, {Install} or {Watch}"), (args, v) => args.action = v);
		AddOption(new Option<bool>("--force", "With build: rebuild both packages even when their sources haven't changed"), (args, v) => args.force = v);
	}

	public override async Task<LocalStackWebCommandResult> GetResult(LocalStackWebCommandArgs args)
	{
		var token = args.Lifecycle.CancellationToken;
		var productDir = Folder(WebPackagesService.ProductDirVariable);
		var workspace = Folder(WebPackagesService.WorkspaceVariable);
		const string registry = WebLocalRegistryService.DefaultRegistry;

		switch (args.action)
		{
			case Build:
				await WebPackagesService.Build(productDir, workspace, args.force, token);
				break;
			case Publish:
				await WebPackagesService.Publish(productDir, workspace, registry, token);
				break;
			case Install:
				WebPackagesService.WriteOverlay(workspace, registry);
				break;
			case Watch:
				// The stack's extensions install the overlay when they start, so restarting them is what delivers a build.
				var runStatePath = LocalStackCommand.ResolveRunStatePath(args.ConfigService, null);
				await WebPackagesService.Watch(productDir, workspace, registry, async restarting =>
				{
					var (steps, message) = await LocalStackPsCommand.Act(args, runStatePath, "restart",
						new List<string> { LocalStackStacks.ExtensionsTag }, all: false, quiet: true, restarting);
					Log.Information(steps.Length == 0 ? "No extension of the stack is running to restart." : message);
				}, token);
				break;
			default:
				throw new CliException($"Unknown action '{args.action}': use {Build}, {Publish}, {Install} or {Watch}.");
		}

		var state = WebPackagesService.LoadOverlay(workspace);
		return new LocalStackWebCommandResult
		{
			sdk = state?.packages.GetValueOrDefault(WebLocalRegistryService.SdkPackage),
			toolkit = state?.packages.GetValueOrDefault(WebLocalRegistryService.ToolkitPackage)
		};
	}

	private static string Folder(string variable)
	{
		var folder = Environment.GetEnvironmentVariable(variable);
		if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
			throw new CliException($"{variable} names no folder ('{folder}'). The web: steps get it from the stack; run them with `beam local run <stack>:web`.");
		return Path.GetFullPath(folder);
	}
}
