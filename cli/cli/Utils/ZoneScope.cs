using Beamable.Server;
using cli.Services;
using System.CommandLine;

namespace cli;

/// <summary>
/// Args of a command that takes the <c>--scope realm|zone</c> option (see <see cref="ZoneScope"/>).
/// </summary>
public interface IHasZoneScopeArgs
{
	public bool IsZoneScope { get; set; }
}

/// <summary>
/// Shared <c>--scope realm|zone</c> handling for commands that can target either the realm (<c>cid.pid</c>) or the
/// zone bound to it (<c>cid.zid</c>). Zone targeting pins the <see cref="CliRequester"/> BEAM_SCOPE to
/// <c>{cid}.{zid}</c>, so every API call made inside the returned handle hits the zone.
/// </summary>
public static class ZoneScope
{
	public const string REALM = "realm";
	public const string ZONE = "zone";

	public const string DEFAULT_DESCRIPTION =
		"Which scope to target: 'realm' (default) for the selected realm (cid.pid); 'zone' for the zone bound to it (cid.zid)";

	/// <summary>
	/// Adds a <c>--scope</c> option that sets <see cref="IHasZoneScopeArgs.IsZoneScope"/>. Typed as <c>string</c> on
	/// purpose: the Unity BeamCli command-wrapper generator emits an option's ValueType verbatim, and a
	/// <c>cli</c>-namespaced enum does not resolve inside the Unity package.
	/// </summary>
	public static void AddScopeOption<TArgs>(AppCommand<TArgs> command, string description = DEFAULT_DESCRIPTION)
		where TArgs : CommandArgs, IHasZoneScopeArgs
	{
		var scopeOption = new Option<string>(new string[] { "--scope" }, () => REALM, description);
		scopeOption.FromAmong(REALM, ZONE);
		command.AddOption(scopeOption,
			(args, i) => args.IsZoneScope = string.Equals(i, ZONE, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Resolves the effective zone id (a selected realm's zone binding wins; otherwise the local <c>.beamable</c>
	/// zid) and throws when there is none.
	/// </summary>
	public static async Task<string> ResolveRequiredZid(CommandArgs args)
	{
		var cid = args.AppContext.Cid;
		var pid = args.AppContext.Pid;
		var localZid = args.ConfigService.GetConfigString(ConfigService.CFG_JSON_FIELD_ZID);
		var zid = await ZoneResolver.ResolveZid(args.DependencyProvider, cid, pid, localZid);
		if (string.IsNullOrEmpty(zid))
		{
			var reason = string.IsNullOrEmpty(pid)
				? "no realm is selected and no 'zid' is set in the .beamable config"
				: $"the selected realm [{pid}] is not bound to a zone";
			throw new CliException(
				$"`--scope zone` was requested, but {reason}. Bind the realm to a zone (Portal > Zone Management) " +
				$"or set a 'zid' in the .beamable config.");
		}

		return zid;
	}

	/// <summary>
	/// For <paramref name="isZone"/>, pins the requester's BEAM_SCOPE to <c>{cid}.{zid}</c> and returns a handle that
	/// restores the previous scope on dispose; otherwise a no-op handle (default <c>{cid}.{pid}</c> scope).
	/// </summary>
	public static async Task<IDisposable> ApplyAsync(CommandArgs args, bool isZone)
	{
		if (!isZone)
		{
			return NoopScope.Instance;
		}

		var cid = args.AppContext.Cid;
		var zid = await ResolveRequiredZid(args);
		Log.Information($"Targeting zone [{zid}] with scope [{cid}.{zid}]");
		var requester = args.DependencyProvider.GetService<CliRequester>();
		return requester.WithBeamScope($"{cid}.{zid}");
	}

	/// <inheritdoc cref="ApplyAsync(CommandArgs, bool)"/>
	public static Task<IDisposable> ApplyAsync<TArgs>(TArgs args)
		where TArgs : CommandArgs, IHasZoneScopeArgs
		=> ApplyAsync(args, args.IsZoneScope);

	private sealed class NoopScope : IDisposable
	{
		public static readonly NoopScope Instance = new NoopScope();
		public void Dispose() { }
	}
}
