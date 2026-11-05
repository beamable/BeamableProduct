using cli.Services.LocalWeb;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace cli.Services.Bundles;

/// <summary>
/// The API of the Bundles tab of the page `beam local management` serves, under <c>/api/bundles</c>
/// (<see cref="cli.Commands.LocalStack.LocalManagementApi"/>). Every operation
/// runs one of the existing `bundles` commands against this workspace as a job (<see cref="CliJobRunner"/>),
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
				return _ctx.Jobs.Dispatch(method, parts, query, body, BuildArgs);
			default:
				throw new LocalWebRequestException(404, $"No route {method} /api/{string.Join('/', parts)}.");
		}
	}

	BundlesWorkspaceView Workspace()
	{
		return new BundlesWorkspaceView
		{
			cid = _ctx.Cid,
			pid = _ctx.Pid,
			zid = _ctx.Zid,
			host = _ctx.Host,
			ownerAccountId = _ctx.OwnerAccountId.ToString(),
			ownerEmail = _ctx.OwnerEmail,
			workingDirectory = _ctx.ConfigService.BeamableWorkspace,
		};
	}

	// Values must start alphanumeric so nothing the page sends can parse as an option.
	static readonly Regex BundleRefPattern = new(@"^@?[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*)?$", RegexOptions.Compiled);
	static readonly Regex NamePattern = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.Compiled);
	static readonly Regex VersionPattern = new(@"^[1-9][0-9]{0,17}$", RegexOptions.Compiled);
	static readonly Regex HashPattern = new(@"^[A-Za-z0-9][A-Za-z0-9+/=_-]{0,255}$", RegexOptions.Compiled);
	static readonly string[] VisibilityValues = { "private", "public" };
	static readonly string[] RealmsValues = { "all", "none" };

	/// <summary>
	/// Maps a whitelisted operation to the CLI arguments that perform it. Checked values follow their option as the
	/// next token, and none can start with a dash.
	/// <c>install</c> and <c>uninstall</c> either stop at the plan (<c>planOnly</c>) or apply the plan the page showed
	/// (<c>planHash</c>), never one nobody saw.
	/// </summary>
	public static (string op, string[] args) BuildArgs(JObject request)
	{
		string Field(string name) => request[name]?.Type == JTokenType.String ? request[name]!.Value<string>() : "";
		bool Flag(string name) => request[name]?.Type == JTokenType.Boolean && request[name]!.Value<bool>();

		string Checked(string name, Regex pattern)
		{
			var value = Field(name);
			if (!pattern.IsMatch(value))
				throw new LocalWebRequestException(400, $"Invalid {name} [{value}].");
			return value;
		}

		string OneOf(string name, string[] allowed)
		{
			var value = Field(name);
			if (!allowed.Contains(value))
				throw new LocalWebRequestException(400, $"Invalid {name} [{value}]. Expected one of: {string.Join(", ", allowed)}.");
			return value;
		}

		string Bundle() => Checked("bundle", BundleRefPattern);
		string Version() => Checked("version", VersionPattern);
		bool Has(string name) => !string.IsNullOrEmpty(Field(name));

		var op = Field("op");
		var args = new List<string>();
		switch (op)
		{
			case "list":
				args.AddRange(new[] { "bundles", "list" });
				break;
			case "releases":
				args.AddRange(new[] { "bundles", "releases", Bundle() });
				if (Has("version")) args.AddRange(new[] { "--version", Version() });
				break;
			case "create":
				args.AddRange(new[] { "bundles", "create", Bundle() });
				if (Has("visibility")) args.AddRange(new[] { "--visibility", OneOf("visibility", VisibilityValues) });
				break;
			case "visibility":
				args.AddRange(new[] { "bundles", "update", Bundle(), "--visibility", OneOf("visibility", VisibilityValues) });
				break;
			case "plan":
				args.AddRange(new[] { "bundles", "plan", Bundle() });
				break;
			case "publish":
				args.AddRange(new[] { "bundles", "publish", Bundle() });
				// flag = publish exactly what the last `plan` job showed.
				if (Flag("flag")) args.Add("--from-latest-plan");
				if (Has("tag")) args.AddRange(new[] { "--tag", Checked("tag", NamePattern) });
				break;
			case "tag":
				args.AddRange(new[] { "bundles", "tag", Bundle(), Checked("tag", NamePattern), Version() });
				break;
			case "yank":
				args.AddRange(new[] { "bundles", "yank", Bundle(), Version() });
				break;
			case "library":
				args.AddRange(new[] { "bundles", "library" });
				break;
			case "acquire":
			case "remove":
				args.AddRange(new[] { "bundles", op, Bundle() });
				break;
			case "installations":
				args.AddRange(new[] { "bundles", "installations" });
				break;
			case "history":
				args.AddRange(new[] { "bundles", "history", Bundle() });
				break;
			case "install":
			case "uninstall":
			{
				args.AddRange(new[] { "bundles", op, Bundle() });
				if (op == "install")
				{
					if (Has("version")) args.AddRange(new[] { "--version", Version() });
					if (Has("tag")) args.AddRange(new[] { "--tag", Checked("tag", NamePattern) });
					if (request["componentGroups"] is JArray groups)
					{
						var names = groups.Select(g => g.Type == JTokenType.String && NamePattern.IsMatch(g.Value<string>()!)
							? g.Value<string>()
							: throw new LocalWebRequestException(400, $"Invalid component group [{g}].")).ToList();
						// An empty list turns every component group off.
						args.Add("--component-groups=" + string.Join(",", names));
					}
					if (Has("realms")) args.AddRange(new[] { "--realms", OneOf("realms", RealmsValues) });
					if (request["autoUpdate"]?.Type == JTokenType.Boolean)
						args.Add("--auto-update=" + (Flag("autoUpdate") ? "true" : "false"));
				}

				if (Flag("planOnly")) args.Add("--plan-only");
				else args.AddRange(new[] { "--plan-hash", Checked("planHash", HashPattern) });
				break;
			}
			default:
				throw new LocalWebRequestException(400, $"Unknown operation [{op}].");
		}

		return (op, args.ToArray());
	}
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
}
