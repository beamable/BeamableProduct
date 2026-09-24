using Beamable.Server;
using cli.Services.Bundles;
using System.CommandLine;

namespace cli.BundleCommands;

public class UninstallBundleCommandArgs : CommandArgs
{
	public string bundleName;
}

public class UninstallBundleCommandOutput
{
	public string name;

	/// <summary>True when the reference was found and removed from the local manifest.</summary>
	public bool removed;
}

public class UninstallBundleCommand : AtomicCommand<UninstallBundleCommandArgs, UninstallBundleCommandOutput>, ISkipManifest
{
	public UninstallBundleCommand() : base("uninstall", "Uninstall a bundle: remove its local pin from the manifest")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("bundle-name", "The bundle name, optionally namespaced as @<namespace>/<bundle-name>"),
			(args, i) => args.bundleName = i);
	}

	public override async Task<UninstallBundleCommandOutput> GetResult(UninstallBundleCommandArgs args)
	{
		var (explicitNs, name) = BundleNamespace.SplitName(args.bundleName);
		BundleWorkspace.ValidateName(name);
		var ns = await BundleNamespace.Resolve(args, explicitNs);
		var fullName = BundleNamespace.Qualify(ns, name);

		var manifest = args.ConfigService.LoadManifestReferences() ?? new ManifestReferences();

		// A bundle lives in exactly one scope, but its section isn't known locally without a catalog
		// lookup — so remove the reference wherever it appears (realm and/or zone).
		var removed = manifest.realm.Remove(fullName);
		removed |= manifest.zone.Remove(fullName);

		if (removed)
		{
			args.ConfigService.SaveManifestReferences(manifest);
			Log.Information($"Uninstalled [{fullName}] from {ConfigService.MANIFEST_FILE_NAME}");
		}
		else
		{
			Log.Warning($"[{fullName}] was not pinned in {ConfigService.MANIFEST_FILE_NAME}; nothing to uninstall.");
		}

		return new UninstallBundleCommandOutput { name = fullName, removed = removed };
	}
}
