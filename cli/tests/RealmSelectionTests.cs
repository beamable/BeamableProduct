using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common.Api.Realms;
using cli;
using cli.Services;
using Moq;
using NUnit.Framework;
using tests.MoqExtensions;

namespace tests;

public class RealmSelectionTests
{
	private static RealmView Game(string pid, string name) => new() { Pid = pid, GamePid = pid, ProjectName = name, Depth = 0 };
	private static RealmView Realm(string pid, string gamePid, string name, int depth = 2, bool archived = false) =>
		new() { Pid = pid, GamePid = gamePid, ProjectName = name, Depth = depth, Archived = archived };

	[TestCase("Hero", "DE_1")]
	[TestCase("hero", "DE_1")]
	[TestCase("DE_2", "DE_2")]
	[TestCase("de_2", "DE_2")]
	public void MatchGame_ByNameOrPid(string wanted, string expectedPid)
	{
		var games = new List<RealmView> { Game("DE_1", "Hero"), Game("DE_2", "Sandbox") };
		Assert.AreEqual(expectedPid, RealmSelection.MatchGame(games, wanted).Pid);
	}

	[Test]
	public void MatchGame_Unknown_ListsAvailableGames()
	{
		var games = new List<RealmView> { Game("DE_1", "Hero") };
		var ex = Assert.Throws<CliException>(() => RealmSelection.MatchGame(games, "Nope"));
		Assert.That(ex!.Message, Does.Contain("Hero (DE_1)"));
	}

	[Test]
	public void TryMatchRealm_SkipsArchived_AndRejectsAmbiguousNames()
	{
		var realms = new List<RealmView>
		{
			Realm("DE_11", "DE_1", "dev", archived: true),
			Realm("DE_12", "DE_1", "dev"),
			Realm("DE_13", "DE_1", "twin"),
			Realm("DE_14", "DE_1", "Twin"),
		};
		Assert.AreEqual("DE_12", RealmSelection.TryMatchRealm(realms, "DEV")!.Pid);
		Assert.IsNull(RealmSelection.TryMatchRealm(realms, "DE_11"), "archived realms can't be selected");
		Assert.Throws<CliException>(() => RealmSelection.TryMatchRealm(realms, "twin"));
		Assert.AreEqual("DE_14", RealmSelection.TryMatchRealm(realms, "DE_14")!.Pid, "a pid is never ambiguous");
	}

	private static Mock<IRealmsApi> Api()
	{
		var hero = Game("DE_1", "Hero");
		var sandbox = Game("DE_2", "Sandbox");
		var api = new Mock<IRealmsApi>();
		api.Setup(x => x.GetGames()).ReturnsPromise(new List<RealmView> { hero, sandbox });
		api.Setup(x => x.GetRealms(It.Is<RealmView>(g => g.Pid == "DE_1")))
			.ReturnsPromise(new List<RealmView> { hero, Realm("DE_12", "DE_1", "Hero-dev"), Realm("DE_13", "DE_1", "shared-dev") });
		api.Setup(x => x.GetRealms(It.Is<RealmView>(g => g.Pid == "DE_2")))
			.ReturnsPromise(new List<RealmView> { sandbox, Realm("DE_22", "DE_2", "Sandbox-dev"), Realm("DE_23", "DE_2", "shared-dev") });
		return api;
	}

	[Test]
	public async Task Resolve_FindsRealmAcrossGames()
	{
		var (game, realm) = await RealmSelection.Resolve(Api().Object, null, "sandbox-dev");
		Assert.AreEqual("DE_2", game.Pid);
		Assert.AreEqual("DE_22", realm.Pid);
	}

	[Test]
	public void Resolve_NameInSeveralGames_AsksForGame()
	{
		var ex = Assert.ThrowsAsync<CliException>(() => RealmSelection.Resolve(Api().Object, null, "shared-dev"));
		Assert.That(ex!.Message, Does.Contain("--game"));
	}

	[Test]
	public async Task Resolve_WithGame_DisambiguatesName()
	{
		var (_, realm) = await RealmSelection.Resolve(Api().Object, "Sandbox", "shared-dev");
		Assert.AreEqual("DE_23", realm.Pid);
	}

	[Test]
	public void Resolve_Unknown_ListsAvailableRealms()
	{
		var ex = Assert.ThrowsAsync<CliException>(() => RealmSelection.Resolve(Api().Object, null, "nope"));
		Assert.That(ex!.Message, Does.Contain("Hero-dev - DE_12"));
	}
}
