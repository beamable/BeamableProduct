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
			throw new CliException("--scope is required. Use 'realm', 'org', or 'public'.");

		var value = scope.Trim();
		switch (value.ToLowerInvariant())
		{
			case Realm: return "cid.pid";
			case Org: return "cid";
			case Public:
			case "*":
				return "*";
		}

		// A literal value must be '*', a bare '<cid>', or '<cid>.<pid>' (cids/pids contain no dots).
		if (value == "*") return value;

		throw new CliException($"Invalid --scope=[{scope}]. Use 'realm', 'org', or 'public'.");
	}
}

/// <summary>
/// Helpers for parsing a bundle reference of the form <c>[@&lt;namespace&gt;/]&lt;bundle-name&gt;</c> or
/// <c>[@&lt;namespace&gt;/]&lt;bundle-name&gt;@&lt;selector&gt;</c> (where <c>selector</c> is a tag or a
/// <c>sha256:&lt;checksum&gt;</c>). The namespace is optional: when the reference is written in its
/// fully-qualified <c>@&lt;namespace&gt;/&lt;bundle-name&gt;</c> form the namespace is honored as-is (so you
/// can address a bundle owned by another customer); otherwise it is derived at runtime from your
/// customer alias (see <see cref="cli.Services.Bundles.BundleNamespace"/>).
/// </summary>
public static class BundleRef
{
	/// <summary>
	/// Split <c>[@&lt;namespace&gt;/]&lt;bundle-name&gt;@&lt;selector&gt;</c> into its optional namespace, short
	/// bundle name, and trailing selector. When the reference carries no <c>@&lt;namespace&gt;/</c> prefix
	/// <c>ns</c> is null; when it carries no trailing <c>@selector</c>, <c>selector</c> is null.
	/// </summary>
	public static (string ns, string name, string selector) Split(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
			throw new CliException("A bundle reference is required, e.g. <bundle-name> or <bundle-name>@sha256:<checksum>");

		// Peel off a trailing @selector first. A leading '@' (the namespace prefix) is at index 0, so
		// LastIndexOf > 0 only matches a selector separator, never the namespace marker.
		var at = raw.LastIndexOf('@');
		var (head, selector) = at > 0 ? (raw.Substring(0, at), raw.Substring(at + 1)) : (raw, null);
		var (ns, name) = BundleNamespace.SplitName(head);
		BundleWorkspace.ValidateName(name);
		return (ns, name, selector);
	}

	/// <summary>Require a <c>sha256:</c> checksum selector on the reference, throwing otherwise.</summary>
	public static (string ns, string name, string checksum) RequireChecksum(string raw)
	{
		var (ns, name, selector) = Split(raw);
		if (string.IsNullOrEmpty(selector) || !selector.StartsWith("sha256:"))
			throw new CliException($"Reference=[{raw}] must include a content checksum, e.g. <bundle-name>@sha256:<checksum>");
		return (ns, name, selector);
	}
}
