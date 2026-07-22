using cli.Services;
using cli.Services.Bundles;

namespace cli.BundleCommands;

public class BundlesCommand : CommandGroup
{
	public BundlesCommand() : base("bundles", "Commands for managing beamo manifest bundles")
	{
	}
}

/// <summary>
/// Resolves an ACL scope argument into the literal value the catalog expects
/// (<c>&lt;cid&gt;.&lt;pid&gt;</c>, <c>&lt;cid&gt;</c>, or <c>*</c>). Accepts the friendly keywords
/// <c>realm</c> / <c>org</c> / <c>public</c> (expanded from the current context) or a literal value,
/// which is validated for shape. Throws <see cref="CliException"/> on an unrecognized value. The
/// server still enforces authorization (you can only widen within your own cid, admin-gated).
/// </summary>
public static class BundleAclScope
{
	public const string Realm = "realm";
	public const string Org = "org";
	public const string Public = "public";

	public static string Resolve(string scope, IAppContext ctx)
	{
		if (string.IsNullOrWhiteSpace(scope))
			throw new CliException("--scope is required. Use 'realm', 'org', or 'public' (or a literal <cid>.<pid> / <cid> / *).");

		var value = scope.Trim();
		switch (value.ToLowerInvariant())
		{
			case Realm: return $"{ctx.Cid}.{ctx.Pid}";
			case Org: return ctx.Cid;
			case Public:
			case "*":
				return "*";
		}

		// A literal value must be '*', a bare '<cid>', or '<cid>.<pid>' (cids/pids contain no dots).
		if (value == "*") return value;
		var parts = value.Split('.');
		if (parts.Length == 1 && parts[0].Length > 0) return value;
		if (parts.Length == 2 && parts[0].Length > 0 && parts[1].Length > 0) return value;

		throw new CliException($"Invalid --scope=[{scope}]. Use 'realm', 'org', or 'public', or a literal <cid> / <cid>.<pid> / *.");
	}
}

/// <summary>
/// Helpers for parsing a bundle reference of the form <c>&lt;bundle-name&gt;</c> or
/// <c>&lt;bundle-name&gt;@&lt;selector&gt;</c> (where <c>selector</c> is a tag or a
/// <c>sha256:&lt;checksum&gt;</c>). Bundle names are short — the namespace is derived at runtime
/// (see <see cref="cli.Services.Bundles.BundleNamespace"/>), never part of a reference.
/// </summary>
public static class BundleRef
{
	/// <summary>
	/// Split <c>&lt;bundle-name&gt;@&lt;selector&gt;</c> into its short bundle name and the trailing
	/// selector. When there is no trailing <c>@selector</c>, <c>selector</c> is null.
	/// </summary>
	public static (string name, string selector) Split(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			throw new CliException("A bundle reference is required, e.g. <bundle-name> or <bundle-name>@sha256:<checksum>");

		var at = raw.LastIndexOf('@');
		var (name, selector) = at > 0 ? (raw.Substring(0, at), raw.Substring(at + 1)) : (raw, null);
		BundleWorkspace.ValidateName(name);
		return (name, selector);
	}

	/// <summary>Require a <c>sha256:</c> checksum selector on the reference, throwing otherwise.</summary>
	public static (string name, string checksum) RequireChecksum(string raw)
	{
		var (name, selector) = Split(raw);
		if (string.IsNullOrEmpty(selector) || !selector.StartsWith("sha256:"))
			throw new CliException($"Reference=[{raw}] must include a content checksum, e.g. <bundle-name>@sha256:<checksum>");
		return (name, selector);
	}
}
