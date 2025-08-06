using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Beamable.Common;
using cli;
using cli.Services;
using Microsoft.Extensions.Logging;
using tests.MoqExtensions;

namespace tests.Examples.Init;

public class BeamInitFlows : CLITest
{

	[TestCase("")]
	[TestCase("toast")]
	[TestCase("with a space")]
	[TestCase("with a space/Iglued")]
	public void Config_HasLinkedProjectInNestedFolder(string initArg)
	{
		InEmptyDirectory(initArg);

		// save the linked-projects json file
		var path = Path.Combine(initArg, ".beamable", cli.Constants.CONFIG_LINKED_PROJECTS);
		File.WriteAllText(path, @"{
  ""unityProjectsPaths"": [
    "".""
  ],
  ""unrealProjectsPaths"": []
}");
		
		// set the working dir to the initArg folder (where the init ran)
		if (!string.IsNullOrEmpty(initArg))
		{
			Directory.SetCurrentDirectory(initArg);
		}
		
		// and run the config command...
		var report = new TestDataReporter();
		RunFull(new string[]{"config"}, true, builder =>
		{
			// override the data extraction...
			builder.RemoveIfExists<IDataReporterService>();
			builder.AddSingleton<IDataReporterService>(report);
		});
		_mockObjects.Clear();
		
		Assert.That(report.reports.Count, Is.EqualTo(1));
		var result = report.reports["stream"][0] as ConfigCommandResult;
		Assert.That(result, Is.Not.Null);
		
		Assert.That(result.linkedUnityProjects.Count, Is.EqualTo(1));
	}

	class TestDataReporter : IDataReporterService
	{
		public Dictionary<string, List<object>> reports = new Dictionary<string, List<object>>();
		
		
		public void Report<T>(string type, T data)
		{
			if (!reports.TryGetValue(type, out var obj))
			{
				reports[type] = obj = new List<object>();
			}
			obj.Add(data);
		}
	}
	
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
		Assert.That(File.Exists( Path.Combine(initArg,".beamable/connection-configuration.json")), "there must be a config defaults file after beam init.");

		// the contents of the file should contain the given cid and pid.
		var configDefaultsStr = File.ReadAllText(Path.Combine(initArg, ".beamable/connection-configuration.json"));
		var expectedJson = $@"{{""host"":""https://api.beamable.com"",""cid"":""{cid}"",""pid"":""{pid}""}}";

		bool areEqual = JToken.DeepEquals(JToken.Parse(configDefaultsStr), JToken.Parse(expectedJson));

		Assert.IsTrue(areEqual, "The connection-configuration file should contain the cid and pid.");


	}
}
