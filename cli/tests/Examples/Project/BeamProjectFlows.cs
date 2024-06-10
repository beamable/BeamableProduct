using NUnit.Framework;
using System;
using System.IO;

namespace tests.Examples.Project;

public class BeamProjectFlows : CLITestExtensions
{
	[Test]
	public void CanCreateNewBeamableSolution()
	{
		#region Arrange

		SetupMocks();
		Ansi.Input.PushKey(ConsoleKey.Enter);
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm
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
		Assert.That(localManifestTextContent.Contains($"\"BeamoId\": \"{serviceName}\""));

		#endregion
	}
}
