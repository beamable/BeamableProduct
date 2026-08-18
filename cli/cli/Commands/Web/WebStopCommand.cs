using Beamable.Server;
using cli.Services.Web;
using System.CommandLine;

namespace cli.Web;

public class WebStopCommandArgs : CommandArgs
{
	public string ProductDir;
	public bool Wipe;
}

public class WebStopCommandResults
{
	public string localdevDir;
	public bool wiped;
}

/// <summary>
/// Stops the local web package registry and CDN. Backs <c>teardown-web.sh</c>.
///
/// <para>
/// Nothing else needs undoing: no global npm config was ever written, and no project manifest or lock
/// file was ever edited. Once these containers are down, every project resolves from npm again — which is
/// also what happens on its own if the containers simply aren't running.
/// </para>
/// </summary>
public class WebStopCommand : AtomicCommand<WebStopCommandArgs, WebStopCommandResults>, IStandaloneCommand, ISkipManifest
{
	public WebStopCommand() : base("stop",
		"Stop the local Beamable web package registry and CDN, so all packages resolve from npm again")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--product-dir", "Absolute path to the BeamableProduct checkout holding portal-localdev/; defaults to searching upwards from the working directory"),
			(args, i) => args.ProductDir = i);
		AddOption(new Option<bool>("--wipe", "Also delete the published packages, so a later start comes up empty"),
			(args, i) => args.Wipe = i);
	}

	public override Task<WebStopCommandResults> GetResult(WebStopCommandArgs args)
	{
		var localdevDir = WebLocalRegistryService.ResolveLocaldevDir(args.ProductDir);

		Log.Information(args.Wipe
			? $"Stopping the local registry in [{localdevDir}] and deleting its published packages"
			: $"Stopping the local registry in [{localdevDir}], keeping its published packages");

		WebLocalRegistryService.RunCompose(localdevDir, args.Wipe ? "compose down -v" : "compose down");

		if (args.Wipe)
		{
			WebLocalRegistryService.EvictPackageCaches();
		}

		Log.Information("Stopped. Every project now resolves its web packages from npm.");
		return Task.FromResult(new WebStopCommandResults { localdevDir = localdevDir, wiped = args.Wipe });
	}
}
