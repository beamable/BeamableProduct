using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Moq;
using NUnit.Framework;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using tests.MoqExtensions;

namespace tests.Examples.Init;

public class BeamInitFlows : CLITest
{
	[Test]
	public void InEmptyDirectory()
	{
		_serilogLevel.MinimumLevel = LogEventLevel.Verbose;
		var alias = "sample-alias";
		var userName = "user@test.com";
		var password = "password";
		var cid = "123";
		var pid = "456";

		Mock<IAliasService>(mock =>
		{
			mock.Setup(x => x.Resolve(alias))
				.ReturnsPromise(new AliasResolve
				{
					Alias = new OptionalString(alias),
					Cid = new OptionalString("123")
				})
				.Verifiable();
		});

		Mock<IAuthApi>(mock =>
		{
			mock.Setup(x => x.Login(userName, password, false, false))
				.ReturnsPromise(new TokenResponse
				{
					refresh_token = "refresh",
					access_token = "access",
					token_type = "token"
				})
				.Verifiable();
		});

		Mock<IRealmsApi>(mock =>
		{

			mock.Setup(x => x.GetGames())
				.ReturnsPromise(new List<RealmView>
				{
					new RealmView
					{
						Cid = cid, Pid = pid, ProjectName = pid, GamePid = pid,
					}
				})
				.Verifiable();

			mock.Setup(x => x.GetRealms(It.IsAny<RealmView>()))
				.ReturnsPromise(new List<RealmView>
				{
					new RealmView { Cid = cid, Pid = pid, ProjectName = pid, GamePid = pid }
				})
				.Verifiable();
		});

		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password

		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm

		// before we start, the directory should not exist...
		Assert.That(!Directory.Exists(".beamable"), "there should not be a .beamable folder to start");

		Run("init");

		// there should a .beamable folder
		Assert.That(File.Exists(".beamable/config-defaults.json"), "there must be a config defaults file after beam init.");

		// the contents of the file should contain the given cid and pid.
		var configDefaultsStr = File.ReadAllText(".beamable/config-defaults.json");
		var expectedJson = $@"{{""host"":""https://api.beamable.com"",""cid"":""{cid}"",""pid"":""{pid}""}}";
		Assert.AreEqual(expectedJson, configDefaultsStr, "The config-defaults file should contain the cid and pid.");

	}
}
