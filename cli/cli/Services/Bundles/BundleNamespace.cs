using Beamable.Server;
using System;
using System.Threading.Tasks;

namespace cli.Services.Bundles;

/// <summary>
/// Resolves the bundle namespace for the current workspace. The namespace is the customer's cid
/// alias in the catalog's <c>@&lt;alias&gt;</c> form — it is never authored in bundle files or passed
/// on the command line; it is derived from the runtime context (the configured cid setting when it
/// is an alias, otherwise the customer record). A bundle's logical name — the form the API and all
/// output use — is <c>@&lt;alias&gt;/&lt;bundle-name&gt;</c>; the short name is only an input/file-name
/// convenience.
/// </summary>
public static class BundleNamespace
{
	public static async Task<string> Get(CommandArgs args)
	{
		// when the configured cid setting is an alias, AppContext captured it during resolution.
		var alias = args.AppContext.Alias;
		if (!string.IsNullOrEmpty(alias))
			return FromAlias(alias);

		// otherwise the config stores a numeric cid; the customer record has the alias.
		var customer = await args.RealmsApi.GetCustomerData();
		if (string.IsNullOrEmpty(customer?.Alias))
			throw new CliException("Could not resolve the customer alias (bundle namespace) for the current cid. Bundles require the customer to have an alias.");
		return FromAlias(customer.Alias);
	}

	/// <summary>
	/// Like <see cref="Get"/>, but returns null instead of throwing when the namespace can't be
	/// resolved (e.g. offline / not logged in) — for commands that only need it for display.
	/// </summary>
	public static async Task<string> TryGet(CommandArgs args)
	{
		try
		{
			return await Get(args);
		}
		catch (Exception e)
		{
			Log.Verbose($"Could not resolve the bundle namespace: {e.Message}");
			return null;
		}
	}

	/// <summary>
	/// Resolve the namespace a command should operate in: the <paramref name="explicitNs"/> the user
	/// supplied inline (a fully-qualified <c>@&lt;namespace&gt;/&lt;bundle-name&gt;</c> argument) when present,
	/// otherwise the workspace's own namespace (see <see cref="Get"/>). Lets a user address a bundle in
	/// another customer's namespace instead of always inferring their own.
	/// </summary>
	public static async Task<string> Resolve(CommandArgs args, string explicitNs)
		=> string.IsNullOrEmpty(explicitNs) ? await Get(args) : explicitNs;

	/// <summary>
	/// Split an optional leading <c>@&lt;namespace&gt;/</c> prefix off a bundle name. When the name is
	/// written in its fully-qualified <c>@&lt;namespace&gt;/&lt;short-name&gt;</c> form the namespace is returned
	/// verbatim (including its leading <c>@</c>) and the short name separately; otherwise <c>ns</c> is
	/// null and the name is returned unchanged. The short name is not validated here — callers pass it
	/// to <see cref="BundleWorkspace.ValidateName"/>.
	/// </summary>
	public static (string ns, string name) SplitName(string raw)
	{
		if (raw != null && raw.StartsWith("@"))
		{
			var slash = raw.IndexOf('/');
			if (slash <= 1 || slash == raw.Length - 1)
				throw new CliException($"Bundle name=[{raw}] has a namespace prefix but is malformed. Use @<namespace>/<bundle-name>.");
			return (raw.Substring(0, slash), raw.Substring(slash + 1));
		}

		return (null, raw);
	}

	/// <summary>The catalog namespace for a customer alias: <c>@&lt;alias&gt;</c>.</summary>
	public static string FromAlias(string alias) => "@" + alias.TrimStart('@');

	/// <summary>The catalog's fully-qualified bundle name, <c>@&lt;alias&gt;/&lt;short-name&gt;</c>.</summary>
	public static string Qualify(string ns, string shortName) => $"{ns}/{shortName}";
}
