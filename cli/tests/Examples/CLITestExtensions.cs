using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Moq;
using Serilog.Events;
using System;
using System.Collections.Generic;
using tests.MoqExtensions;

namespace tests.Examples;

public class CLITestExtensions : CLITest
{
	public void PrepareEnvironment()
	{
		base.Setup();
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

		// Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(cid); // enter cid
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password

		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm
	}
}
