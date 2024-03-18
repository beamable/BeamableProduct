using cli.Services;
using Newtonsoft.Json;
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
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm

		const string serviceName = "Example";

		#endregion

		#region Act

		Run("project", "new", "microservice", serviceName, "--auto-init", "--quiet");

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(File.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an Example.sln file after beam project new {serviceName}");

		// there should be a local-services-manifest.json file
		Assert.That(File.Exists(".beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = File.ReadAllText(".beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));

		#endregion
	}
}
