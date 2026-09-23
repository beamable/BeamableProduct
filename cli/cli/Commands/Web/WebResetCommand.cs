using Beamable.Server;
using cli.Services.Web;
using System.CommandLine;

namespace cli.Web;

public class WebResetCommandArgs : CommandArgs
{
	public string ProductDir;
	public bool KeepCaches;
}

public class WebResetCommandResults
{
	public string localdevDir;
	public bool cachesCleared;
}

/// <summary>
/// Wipes the local registry's storage and brings it back up empty, so every package resolves from npmjs
/// again. Backs <c>setup-web.sh</c>. Unlike the old script it never touches the user's global npm config,
/// because nothing in this flow writes there — so there is no state left to undo.
/// </summary>
public class WebResetCommand : AtomicCommand<WebResetCommandArgs, WebResetCommandResults>, IStandaloneCommand, ISkipManifest
{
	public WebResetCommand() : base("reset",
		"Wipe the local Beamable web package registry and restart it empty, so all packages resolve from npm again")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--product-dir", "Absolute path to the BeamableProduct checkout holding portal-localdev/; defaults to searching upwards from the working directory"),
			(args, i) => args.ProductDir = i);
		AddOption(new Option<bool>("--keep-caches", "Leave the package manager caches alone; by default the cached @beamable tarballs are evicted"),
			(args, i) => args.KeepCaches = i);
	}

	public override Task<WebResetCommandResults> GetResult(WebResetCommandArgs args)
	{
		var localdevDir = WebLocalRegistryService.ResolveLocaldevDir(args.ProductDir);

		Log.Information($"Wiping the local registry in [{localdevDir}]");
		WebLocalRegistryService.RunCompose(localdevDir, "compose down -v");
		WebLocalRegistryService.RunCompose(localdevDir, "compose up -d");

		if (!args.KeepCaches)
		{
			WebLocalRegistryService.EvictPackageCaches();
		}

		Log.Information("The local registry is empty. Run 'beam web publish' to populate it.");
		return Task.FromResult(new WebResetCommandResults
		{
			localdevDir = localdevDir,
			cachesCleared = !args.KeepCaches
		});
	}
}
