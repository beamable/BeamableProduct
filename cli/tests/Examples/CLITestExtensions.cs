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
	protected string alias = "sample-alias";
	protected string userName = "user@test.com";
	protected string password = "password";
	private string cid = "123";
	private string pid = "456";

	protected void SetupMocks()
	{
		base.Setup();
		_serilogLevel.MinimumLevel = LogEventLevel.Verbose;

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
	}
}