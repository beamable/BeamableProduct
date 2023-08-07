using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Moq;
using NUnit.Framework;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using tests.Examples.Init;
using tests.MoqExtensions;

namespace tests.Examples.Project;

public class BeamProjectFlows : CLITest
{
	[SetUp]
	public new void Setup()
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

		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password

		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm
	}
	
	[TearDown]
	public override void Teardown()
	{
		Directory.SetCurrentDirectory(OriginalWorkingDir);
		Directory.Delete(WorkingDir, true);
	}

	[Test]
	public void CanCreateNewBeamableSolution()
	{
		#region Arrange

		new BeamInitFlows().InEmptyDirectory();
		Ansi.Input.PushTextWithEnter("n"); // don't link unity project
		Ansi.Input.PushTextWithEnter("n"); // don't link unreal project

		const string serviceName = "Example";

		#endregion

		#region Act

		Run("project", "new", serviceName);

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(File.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an Example.sln file after beam project new {serviceName}");

		// there should be a beamoLocalManifest.json file
		Assert.That(File.Exists($"{serviceName}/.beamable/beamoLocalManifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = File.ReadAllText($"{serviceName}/.beamable/beamoLocalManifest.json");
		Assert.That(localManifestTextContent.Contains($"\"BeamoId\":\"{serviceName}\""));

		#endregion
	}
}
