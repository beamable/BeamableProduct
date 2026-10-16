using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using cli;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using tests.MoqExtensions;

namespace tests.Examples.Init;

/// <summary>
/// `init --realm` / `--game` and `config realm use` pick a realm by name or pid without prompts. In quiet mode
/// without them, init used to silently pick the oldest dev realm of the first game.
/// </summary>
public class BeamInitRealmSelectionFlows : CLITest
{
	private const string Cid = "123";
	private const string UserName = "user@test.com";
	private const string Password = "password";

	private static readonly RealmView HeroGame = new() { Cid = Cid, Pid = "DE_100", GamePid = "DE_100", ProjectName = "Hero", Depth = 0 };
	private static readonly RealmView SandboxGame = new() { Cid = Cid, Pid = "DE_200", GamePid = "DE_200", ProjectName = "ClaudeSandbox", Depth = 0 };

	private static List<RealmView> HeroRealms() => new()
	{
		HeroGame,
		new RealmView { Cid = Cid, Pid = "DE_101", GamePid = "DE_100", ProjectName = "Hero-staging", Depth = 1 },
		new RealmView { Cid = Cid, Pid = "DE_102", GamePid = "DE_100", ProjectName = "Hero-dev", Depth = 2 },
	};

	private static List<RealmView> SandboxRealms() => new()
	{
		SandboxGame,
		new RealmView { Cid = Cid, Pid = "DE_201", GamePid = "DE_200", ProjectName = "ClaudeSandbox-staging", Depth = 1 },
		new RealmView { Cid = Cid, Pid = "DE_202", GamePid = "DE_200", ProjectName = "ClaudeSandbox-dev", Depth = 2 },
		new RealmView { Cid = Cid, Pid = "DE_203", GamePid = "DE_200", ProjectName = "Old-dev", Depth = 2, Archived = true },
	};

	private Mock<IAuthApi> _auth = null!;
	private Mock<IRealmsApi> _realms = null!;

	[SetUp]
	public void SetUpMocks()
	{
		_auth = new Mock<IAuthApi>();
		_auth.Setup(x => x.Login(UserName, Password, false, true))
			.ReturnsPromise(new TokenResponse { refresh_token = "refresh", access_token = "access", token_type = "token" });
		_realms = new Mock<IRealmsApi>();
		_realms.Setup(x => x.GetGames()).ReturnsPromise(new List<RealmView> { HeroGame, SandboxGame });
		_realms.Setup(x => x.GetRealms(It.Is<RealmView>(g => g.Pid == HeroGame.Pid))).ReturnsPromise(HeroRealms());
		_realms.Setup(x => x.GetRealms(It.Is<RealmView>(g => g.Pid == SandboxGame.Pid))).ReturnsPromise(SandboxRealms());
	}

	// The mocks go through RunFull's configurator rather than CLITest.Mock, whose VerifyAll would also check the
	// copies made each time the DI container is rebuilt during init.
	private int RunWithMocks(bool assertExitCode, params string[] args) =>
		RunFull(args, assertExitCode, builder =>
		{
			builder.ReplaceSingleton<IAuthApi, IAuthApi>(() => _auth.Object);
			builder.ReplaceSingleton<IRealmsApi, IRealmsApi>(() => _realms.Object);
		});

	private void Init(params string[] extraArgs) =>
		RunWithMocks(true, new[] { "init", "--cid", Cid, "--username", UserName, "--password", Password, "-q" }.Concat(extraArgs).ToArray());

	private static string ConfiguredPid() =>
		JObject.Parse(File.ReadAllText(Path.Combine(".beamable", "config.beam.json")))["pid"]!.Value<string>()!;

	[Test]
	public void Init_WithRealmName_PicksThatRealmAcrossGames()
	{
		Init("--realm", "claudesandbox-dev");
		Assert.AreEqual("DE_202", ConfiguredPid());
	}

	[Test]
	public void Init_WithGameAndRealmPid()
	{
		Init("--game", "ClaudeSandbox", "--realm", "DE_201");
		Assert.AreEqual("DE_201", ConfiguredPid());
		_realms.Verify(x => x.GetRealms(It.Is<RealmView>(g => g.Pid == HeroGame.Pid)), Times.Never, "--game limits the search to one game");
	}

	[Test]
	public void Init_WithGameOnly_PicksThatGamesDevRealmInQuietMode()
	{
		Init("--game", "ClaudeSandbox");
		Assert.AreEqual("DE_202", ConfiguredPid(), "archived realms are skipped");
	}

	[Test]
	public void Init_WithUnknownRealm_Fails()
	{
		var exitCode = RunWithMocks(false, "init", "--cid", Cid, "--username", UserName, "--password", Password, "--realm", "Nope-dev", "-q");
		Assert.AreNotEqual(0, exitCode);
	}

	[Test]
	public void ConfigRealmUse_SwitchesPidByName()
	{
		Init("--realm", "Hero-dev");
		Assert.AreEqual("DE_102", ConfiguredPid());

		RunWithMocks(true, "config", "realm", "use", "ClaudeSandbox-dev", "-q");
		Assert.AreEqual("DE_202", ConfiguredPid());
	}
}
