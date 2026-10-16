using Beamable.Common.Api.Realms;

namespace cli.Services;

/// <summary>
/// Picks a game and realm by name or pid, so `init` and `config realm use` can target a realm without prompts.
/// </summary>
public static class RealmSelection
{
	/// <summary>The name a game is listed under in prompts (its display name without the [PROD] tag).</summary>
	public static string GameLabel(RealmView game) => game.DisplayName.Replace("[PROD]", "").Trim();

	/// <summary>The label a realm is listed under in prompts: "name - pid".</summary>
	public static string RealmLabel(RealmView realm) =>
		$"{realm.DisplayName.Replace("[", "").Replace("]", "")} - {realm.Pid}";

	/// <summary>Finds a game by pid or name (case-insensitive), or throws listing the available games.</summary>
	public static RealmView MatchGame(IReadOnlyCollection<RealmView> games, string nameOrPid)
	{
		var wanted = nameOrPid.Trim();
		var byPid = games.FirstOrDefault(g => string.Equals(g.Pid, wanted, StringComparison.OrdinalIgnoreCase)
		                                      || string.Equals(g.GamePid, wanted, StringComparison.OrdinalIgnoreCase));
		if (byPid != null) return byPid;

		var byName = games.Where(g => string.Equals(GameLabel(g), wanted, StringComparison.OrdinalIgnoreCase)
		                              || string.Equals(g.ProjectName, wanted, StringComparison.OrdinalIgnoreCase)).ToList();
		if (byName.Count == 1) return byName[0];
		if (byName.Count > 1)
			throw new CliException($"More than one game is named '{wanted}'. Pass its pid instead: {string.Join(", ", byName.Select(g => $"{GameLabel(g)} ({g.Pid})"))}");

		throw new CliException($"No game named '{wanted}'. Available games: {string.Join(", ", games.Select(g => $"{GameLabel(g)} ({g.Pid})"))}");
	}

	/// <summary>
	/// Finds a non-archived realm by pid or name (case-insensitive), or returns null.
	/// Throws when a name matches several realms.
	/// </summary>
	public static RealmView TryMatchRealm(IEnumerable<RealmView> realms, string nameOrPid)
	{
		var wanted = nameOrPid.Trim();
		var live = realms.Where(r => !r.Archived).ToList();
		var byPid = live.FirstOrDefault(r => string.Equals(r.Pid, wanted, StringComparison.OrdinalIgnoreCase));
		if (byPid != null) return byPid;

		var byName = live.Where(r => string.Equals(r.ProjectName, wanted, StringComparison.OrdinalIgnoreCase)
		                             || string.Equals(r.DisplayName, wanted, StringComparison.OrdinalIgnoreCase)).ToList();
		if (byName.Count > 1)
			throw new CliException($"More than one realm is named '{wanted}'. Pass its pid instead: {string.Join(", ", byName.Select(RealmLabel))}");
		return byName.FirstOrDefault();
	}

	/// <summary>
	/// Resolves the game and realm for the given filters. With a realm but no game, every game is searched and the
	/// realm name must be unique across them.
	/// </summary>
	public static async Task<(RealmView game, RealmView realm)> Resolve(IRealmsApi realmsApi, string gameNameOrPid, string realmNameOrPid)
	{
		var games = await realmsApi.GetGames();
		if (games.Count == 0)
			throw new CliException("This organization has no games.");

		if (!string.IsNullOrWhiteSpace(gameNameOrPid))
		{
			var game = MatchGame(games, gameNameOrPid);
			var realms = await realmsApi.GetRealms(game);
			var realm = TryMatchRealm(realms, realmNameOrPid)
			            ?? throw new CliException($"No realm named '{realmNameOrPid.Trim()}' in game '{GameLabel(game)}'. Available realms: {string.Join(", ", realms.Where(r => !r.Archived).Select(RealmLabel))}");
			return (game, realm);
		}

		var matches = new List<(RealmView game, RealmView realm)>();
		var available = new List<string>();
		foreach (var game in games)
		{
			var realms = await realmsApi.GetRealms(game);
			available.AddRange(realms.Where(r => !r.Archived).Select(r => $"{RealmLabel(r)} (game {GameLabel(game)})"));
			var realm = TryMatchRealm(realms, realmNameOrPid);
			if (realm != null) matches.Add((game, realm));
		}

		if (matches.Count == 1) return matches[0];
		if (matches.Count > 1)
			throw new CliException($"Realm '{realmNameOrPid.Trim()}' exists in more than one game; pass --game: {string.Join(", ", matches.Select(m => GameLabel(m.game)))}");
		throw new CliException($"No realm named '{realmNameOrPid.Trim()}'. Available realms: {string.Join(", ", available)}");
	}
}
