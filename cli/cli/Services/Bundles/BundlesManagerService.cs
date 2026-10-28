using cli.Services.LocalWeb;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace cli.Services.Bundles;

/// <summary>
/// The API of the bundles web app `beam bundles management` serves (<see cref="LocalWebServer"/>). Every operation
/// runs one of the existing `bundles` / `deploy` commands against this workspace as a job (<see cref="CliJobRunner"/>),
/// with this CLI's credentials.
/// </summary>
public class BundlesApi : ILocalWebApi
{
	readonly BundlesManagementContext _ctx;

	public BundlesApi(BundlesManagementContext ctx)
	{
		_ctx = ctx;
	}

	public object Dispatch(string method, string[] parts, NameValueCollection query, string body)
	{
		switch (method, parts.Length > 0 ? parts[0] : "", parts.Length)
		{
			case ("GET", "workspace", 1):
				return Workspace();
			case (_, "jobs", _):
				return _ctx.Jobs.Dispatch(method, parts, query, body, request =>
				{
					string Field(string name) => request[name]?.Type == JTokenType.String ? request[name]!.Value<string>() : "";
					var op = Field("op");
					return (op, BuildArgs(op, Field("bundle"), Field("tag"), Field("checksum"), Field("comment"), Field("acl"),
						Field("scope"), request["flag"]?.Type == JTokenType.Boolean && request["flag"]!.Value<bool>()));
				});
			default:
				throw new LocalWebRequestException(404, $"No route {method} /api/{string.Join('/', parts)}.");
		}
	}

	BundlesWorkspaceView Workspace()
	{
		var pins = _ctx.ConfigService.LoadManifestReferences() ?? new ManifestReferences();
		return new BundlesWorkspaceView
		{
			cid = _ctx.Cid,
			pid = _ctx.Pid,
			zid = _ctx.Zid,
			host = _ctx.Host,
			ownerAccountId = _ctx.OwnerAccountId.ToString(),
			ownerEmail = _ctx.OwnerEmail,
			workingDirectory = _ctx.ConfigService.BeamableWorkspace,
			realmPins = pins.realm.Select(kv => new BundlePinView { name = kv.Key, checksum = kv.Value }).ToArray(),
			zonePins = pins.zone.Select(kv => new BundlePinView { name = kv.Key, checksum = kv.Value }).ToArray(),
		};
	}

	// Values must start alphanumeric so nothing the page sends can parse as an option.
	static readonly Regex BundleRefPattern = new(@"^@?[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*)?(@[A-Za-z0-9][A-Za-z0-9._:-]*)?$", RegexOptions.Compiled);
	static readonly Regex TagPattern = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.Compiled);
	static readonly Regex ChecksumPattern = new(@"^sha256:[0-9a-fA-F]{8,128}$", RegexOptions.Compiled);
	static readonly string[] AclValues = { "private", "org", "public" };
	static readonly string[] ScopeValues = { "realm", "zone" };

	/// <summary>Maps a whitelisted operation to the CLI arguments that perform it.</summary>
	public static string[] BuildArgs(string op, string bundle, string tag, string checksum, string comment,
		string acl, string scope, bool flag)
	{
		string Bundle()
		{
			if (string.IsNullOrEmpty(bundle) || !BundleRefPattern.IsMatch(bundle))
				throw new LocalWebRequestException(400, $"Invalid bundle reference [{bundle}].");
			return bundle;
		}

		string Tag()
		{
			if (!TagPattern.IsMatch(tag ?? ""))
				throw new LocalWebRequestException(400, $"Invalid tag [{tag}].");
			return tag;
		}

		string OneOf(string value, string[] allowed, string what)
		{
			if (!allowed.Contains(value))
				throw new LocalWebRequestException(400, $"Invalid {what} [{value}]. Expected one of: {string.Join(", ", allowed)}.");
			return value;
		}

		var args = new List<string>();
		switch (op)
		{
			case "list":
				args.AddRange(new[] { "bundles", "list" });
				break;
			case "get":
				args.AddRange(new[] { "bundles", "get", Bundle() });
				break;
			case "releases":
				args.AddRange(new[] { "bundles", "releases", Bundle() });
				break;
			case "install":
				args.AddRange(new[] { "bundles", "install", Bundle() });
				if (!string.IsNullOrEmpty(checksum))
				{
					if (!ChecksumPattern.IsMatch(checksum))
						throw new LocalWebRequestException(400, $"Invalid checksum [{checksum}].");
					args.AddRange(new[] { "--checksum", checksum });
				}
				else if (!string.IsNullOrEmpty(tag))
				{
					args.AddRange(new[] { "--tag", Tag() });
				}
				break;
			case "uninstall":
				args.AddRange(new[] { "bundles", "uninstall", Bundle() });
				break;
			case "prune-yanked":
				args.AddRange(new[] { "bundles", "prune-yanked" });
				if (flag) args.Add("--remove");
				break;
			case "plan":
				args.AddRange(new[] { "bundles", "plan", Bundle() });
				break;
			case "publish":
				args.AddRange(new[] { "bundles", "publish", Bundle() });
				// flag = publish exactly what the last `plan` job showed.
				if (flag) args.Add("--from-latest-plan");
				if (!string.IsNullOrEmpty(tag)) args.AddRange(new[] { "--tag", Tag() });
				if (!string.IsNullOrEmpty(acl)) args.AddRange(new[] { "--acl", OneOf(acl, AclValues, "acl") });
				if (!string.IsNullOrWhiteSpace(comment)) args.Add(CommentOption(comment));
				break;
			case "tag":
				args.AddRange(new[] { "bundles", "tag", Bundle(), Tag() });
				break;
			case "yank":
				args.AddRange(new[] { "bundles", "yank", Bundle() });
				break;
			case "acl":
				args.AddRange(new[] { "bundles", "acl", Bundle(), "--scope", OneOf(acl, AclValues, "acl") });
				break;
			case "deploy-plan":
				args.AddRange(new[] { "deploy", "plan", "--scope", OneOf(scope, ScopeValues, "scope") });
				if (!string.IsNullOrWhiteSpace(comment)) args.Add(CommentOption(comment));
				break;
			case "deploy-release":
				args.AddRange(new[] { "deploy", "release", "--from-latest-plan", "--scope", OneOf(scope, ScopeValues, "scope") });
				// Release clears the plan's comments and applies its own, so the comment goes here too.
				if (!string.IsNullOrWhiteSpace(comment)) args.Add(CommentOption(comment));
				break;
			default:
				throw new LocalWebRequestException(400, $"Unknown operation [{op}].");
		}

		return args.ToArray();
	}

	/// <summary>One token, so a comment that starts with a dash is not parsed as an option.</summary>
	static string CommentOption(string comment) => "--comment=" + comment.Trim();
}

/// <summary>The CLI state <see cref="BundlesApi"/> works with.</summary>
public class BundlesManagementContext
{
	public string Cid;
	public string Pid;
	public string Zid;
	public string Host;
	public long OwnerAccountId;
	public string OwnerEmail;
	public ConfigService ConfigService;
	public CliJobRunner Jobs;
}

[Serializable]
public class BundlesWorkspaceView
{
	public string cid;
	public string pid;
	public string zid;
	public string host;
	/// <summary>A string: account ids exceed what a JavaScript number holds exactly.</summary>
	public string ownerAccountId;
	public string ownerEmail;
	public string workingDirectory;
	public BundlePinView[] realmPins;
	public BundlePinView[] zonePins;
}

[Serializable]
public class BundlePinView
{
	public string name;
	public string checksum;
}
