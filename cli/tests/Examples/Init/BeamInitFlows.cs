using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using cli.Services;
using Microsoft.Extensions.Logging;
using tests.MoqExtensions;

namespace tests.Examples.Init;

public partial class BeamInitFlows : CLITest
{

	[TestCase("")]
	[TestCase("toast")]
	[TestCase("with a space")]
	[TestCase("with a space/Iglued")]
	public void UpdateFlow_LocalDevVersion(string initArg)
	{
		InEmptyDirectory(initArg);
		
		// and now run an update command!
		//  it should be on 0.0.123.x where x is the build number, 
		//  so updating should just re-import everything.

		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
		if (!string.IsNullOrEmpty(initArg))
		{
			Directory.SetCurrentDirectory(initArg);
		}
		Run("version", "update", "-q");
		_mockObjects.Clear();
	}
	
	[TestCase("")]
	[TestCase("toast")]
	[TestCase("with a space")]
	public void InEmptyDirectory(string initArg)
	{
		_logSwitch.Level = LogLevel.Trace;
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
					Cid = new OptionalString(cid)
				})
				.Verifiable();
			mock.Setup(x => x.Resolve(cid))
				.ReturnsPromise(new AliasResolve
				{
					Alias = new OptionalString(alias),
					Cid = new OptionalString(cid)
				})
				.Verifiable();
		});

		Mock<IAuthApi>(mock =>
		{
			mock.Setup(x => x.Login(userName, password, false, true))
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

		if (string.IsNullOrEmpty(initArg))
		{
			Run("init");
		}
		else
		{
			Run("init", initArg);
		}

		// there should a .beamable folder
		Assert.That(File.Exists( Path.Combine(initArg, ".beamable/config.beam.json")), "there must be a config defaults file after beam init.");

		// the contents of the file should contain the given cid and pid.
		var configDefaultsStr = File.ReadAllText(Path.Combine(initArg, ".beamable/config.beam.json"));
		var expectedJson = 
$@"{{
  ""additionalProjectPaths"" : [ ],
  ""ignoredProjectPaths"" : [ ],
  ""host"" : ""https://api.beamable.com"",
  ""cid"" : ""{cid}"",
  ""pid"" : ""{pid}"",
  ""cliVersion"" : ""0.0.123"",
}}";

		var actual = JToken.Parse(configDefaultsStr);
		var expected = JToken.Parse(expectedJson);
		
		Assert.AreEqual(expected["cid"].Value<string>(), actual["cid"].Value<string>());
		Assert.AreEqual(expected["pid"].Value<string>(), actual["pid"].Value<string>());
		Assert.AreEqual(expected["host"].Value<string>(), actual["host"].Value<string>());
	}
}
