using Beamable.Server;
using cli.Services.Bundles;
using System.CommandLine;

namespace cli.BundleCommands;

public class UninstallBundleCommandArgs : CommandArgs
{
	public string bundleName;

	// --force path (server-side forced-bundle override)
	public bool force;
	public string ns;
	public string cid;
	public string realmId;
}

public class UninstallBundleCommandOutput
{
	public string name;

	/// <summary>True when the reference was found and removed (local pin), or the override was cleared (--force).</summary>
	public bool removed;

	/// <summary>True when uninstalled via <c>--force</c> (a server-side override) rather than a local pin.</summary>
	public bool forced;

	/// <summary>The target cid of the cleared forced-bundle override (only set on the <c>--force</c> path).</summary>
	public string cid;

	/// <summary>The target realm of the cleared forced-bundle override, or null for a CID-wide override (only set on the <c>--force</c> path).</summary>
	public string realmId;
}

public class UninstallBundleCommand : AtomicCommand<UninstallBundleCommandArgs, UninstallBundleCommandOutput>, ISkipManifest
{
	public UninstallBundleCommand() : base("uninstall", "Uninstall a bundle: remove its local pin, or with --force clear its server-side force-injection")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("bundle-name", "The bundle name, optionally namespaced as @<namespace>/<bundle-name>"),
			(args, i) => args.bundleName = i);
		AddOption(new Option<bool>(new[] { "--force" }, "Clear the bundle's server-side force-injection instead of removing a local pin (Beamable-internal)"),
			(args, i) => args.force = i);
		AddOption(new Option<string>(new[] { "--namespace", "-ns" }, "With --force: the bundle's namespace (a customer alias). Defaults to the current context's customer alias; pass it to clear a bundle owned by a different customer"),
			(args, i) => args.ns = i);
		AddOption(new Option<string>(new[] { "--target-cid" }, "With --force: the target customer id to clear the forced bundle from. Defaults to the current project's cid; pass it to clear from a different customer"),
			(args, i) => args.cid = i);
		AddOption(new Option<string>(new[] { "--target-realm", "-r" }, "With --force: the target realm id to clear the forced bundle from. When omitted, the CID-wide override is cleared"),
			(args, i) => args.realmId = i);
	}

	public override async Task<UninstallBundleCommandOutput> GetResult(UninstallBundleCommandArgs args)
	{
		if (args.force)
		{
			var cleared = await BundleForcedInject.Apply(args, args.bundleName, args.ns, args.cid, args.realmId, checksum: null, unset: true);
			Log.Information($"Cleared force-injection of [{cleared.bundleName}] from {(string.IsNullOrEmpty(cleared.realmId) ? $"cid [{cleared.cid}] (all realms)" : $"realm [{cleared.realmId}]")}");
			return new UninstallBundleCommandOutput
			{
				name = cleared.bundleName, removed = true, forced = true, cid = cleared.cid, realmId = cleared.realmId
			};
		}

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
