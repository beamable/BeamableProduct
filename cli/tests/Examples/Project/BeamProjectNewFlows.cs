using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;

namespace tests.Examples.Project;

public class BeamProjectNewFlows : CLITestExtensions
{
	[Test]
	[TestCase("Example")]
	public void NewProject_AutoInit_NoSlnConfig(string serviceName)
	{
		#region Arrange

		SetupMocks();
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm


		#endregion

		#region Act

		Run("project", "new", "service", serviceName, "-i", "--quiet");

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an Example.sln file after beam project new {serviceName}");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($"{serviceName}/.beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($"{serviceName}/.beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, Is.EqualTo($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"services/{serviceName}/Dockerfile"));

		string dockerfileContent = BFile.ReadAllText($"{serviceName}/services/{serviceName}/Dockerfile");
		var hasCorrectCopyFragment = dockerfileContent.Contains($"COPY services/{serviceName} ");
		Assert.IsTrue(hasCorrectCopyFragment, "the docker file needs to have a copy line relative to the services folder.\n" + dockerfileContent);
		
		#endregion
	}

	[Test]
	public void NewProject_AutoInit_NoSlnConfig_PassFlags()
	{
		#region Arrange

		SetupMocks(mockAlias: false, mockRealms: false);
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		const string serviceName = "Example";

		#endregion

		#region Act

		Run("project", "new", "service", serviceName, "-i", "--quiet", "--cid", cid, "--pid", pid);

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an Example.sln file after beam project new {serviceName}");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($"{serviceName}/.beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($"{serviceName}/.beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, Is.EqualTo($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"services/{serviceName}/Dockerfile"));

		#endregion
	}

	[Test]
	public void NewProject_AutoInit_SlnPathInLocalFolder()
	{
		#region Arrange

		SetupMocks();
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm

		const string serviceName = "Example";
		const string slnName = "fake";

		#endregion

		#region Act

		Run("project", "new", "service", serviceName, "-i", "--quiet", "--sln", $"{slnName}.sln");

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{slnName}.sln"),
			$"There must be an {slnName}.sln file after beam project new --sln {slnName}");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($".beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($".beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, Is.EqualTo($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"services/{serviceName}/Dockerfile"));

		#endregion
	}


	[Test]
	public void NewProject_AutoInit_NewSlnPathInSubFolder()
	{
		#region Arrange

		SetupMocks();
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm

		const string serviceName = "Example";
		const string slnPath = "nested/folder/over";
		const string slnName = $"{slnPath}/here";

		#endregion

		#region Act

		Run("project", "new", "service", serviceName, "-i", "--quiet", "--sln", $"{slnName}.sln");

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{slnName}.sln"),
			$"There must be an {slnName}.sln file after beam project new --sln {slnName}");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($"{slnPath}/.beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($"{slnPath}/.beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, Is.EqualTo($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"services/{serviceName}/Dockerfile"));

		#endregion
	}

	[Test]
	public void NewProject_UsingExistingInit_NoSlnConfig()
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

		Run("init", "--save-to-file");
		ResetConfigurator();

		Run("project", "new", "service", serviceName, "--quiet");

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an {serviceName}/{serviceName}.sln file ");

		// there should a .beamable folder
		Assert.That(BFile.Exists(".beamable/connection-configuration.json"), "there must be a config defaults file after beam init.");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($".beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($".beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"{serviceName}/services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, BIs.Path($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"{serviceName}/services/{serviceName}/Dockerfile"));

		#endregion
	}

	[Test]
	public void NewProject_UsingExistingInit_NoSlnConfig_AddSecondWithExistingSln()
	{
		#region Arrange

		SetupMocks();
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm

		const string serviceName = "Example";
		const string secondServiceName = "Tuna";

		#endregion

		#region Act

		Run("init", "--save-to-file");
		ResetConfigurator();

		Run("project", "new", "service", serviceName, "--quiet");
		ResetConfigurator();

		Run("project", "new", "service", secondServiceName, "--quiet", "--sln", $"{serviceName}/{serviceName}.sln");

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an {serviceName}/{serviceName}.sln file ");

		Assert.That(BFile.Exists($"{serviceName}/services/{serviceName}/{serviceName}.csproj"),
			"the first service needs to have a csproj");
		Assert.That(BFile.Exists($"{serviceName}/services/{secondServiceName}/{secondServiceName}.csproj"),
			"the second service needs to have a csproj");

		// there should a .beamable folder
		Assert.That(BFile.Exists(".beamable/connection-configuration.json"), "there must be a config defaults file after beam init.");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($".beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($".beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(2));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"{serviceName}/services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, BIs.Path($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"{serviceName}/services/{serviceName}/Dockerfile"));

		Assert.That(manifest.ServiceDefinitions[1].BeamoId, Is.EqualTo(secondServiceName));
		Assert.That(manifest.ServiceDefinitions[1].ProjectDirectory, BIs.Path($"{serviceName}/services/{secondServiceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[secondServiceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[secondServiceName].DockerBuildContextPath, BIs.Path($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[secondServiceName].RelativeDockerfilePath, BIs.Path($"{serviceName}/services/{secondServiceName}/Dockerfile"));

		#endregion
	}


	[TestCase("Example", "Data")]
	public void NewProject_UsingExistingInit_NoSlnConfig_AddStorageToExistingSln(string serviceName, string storageName)
	{
		#region Arrange

		SetupMocks();
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm


		#endregion

		#region Act

		Run("init", "--save-to-file");
		ResetConfigurator();

		Run("project", "new", "service", serviceName, "--quiet");
		ResetConfigurator();

		Run("project", "new", "storage", storageName, "--quiet", "--sln", $"{serviceName}/{serviceName}.sln", "--link-to", serviceName);

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{serviceName}/{serviceName}.sln"),
			$"There must be an {serviceName}/{serviceName}.sln file ");

		Assert.That(BFile.Exists($"{serviceName}/services/{serviceName}/{serviceName}.csproj"),
			"the first service needs to have a csproj");
		Assert.That(BFile.Exists($"{serviceName}/services/{storageName}/{storageName}.csproj"),
			"the second service needs to have a csproj");

		// there should a .beamable folder
		Assert.That(BFile.Exists(".beamable/connection-configuration.json"), "there must be a config defaults file after beam init.");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($".beamable/local-services-manifest.json"),
			$"There must be a beamo local manifest file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = BFile.ReadAllText($".beamable/local-services-manifest.json");
		var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(2));
		Assert.That(manifest.ServiceDefinitions[0].BeamoId, Is.EqualTo(serviceName));
		Assert.That(manifest.ServiceDefinitions[0].ProjectDirectory, BIs.Path($"{serviceName}/services/{serviceName}"));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName], Is.Not.Null);
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].DockerBuildContextPath, BIs.Path($"."));
		Assert.That(manifest.HttpMicroserviceLocalProtocols[serviceName].RelativeDockerfilePath, BIs.Path($"{serviceName}/services/{serviceName}/Dockerfile"));

		Assert.That(manifest.ServiceDefinitions[1].BeamoId, Is.EqualTo(storageName));
		Assert.That(manifest.ServiceDefinitions[1].ProjectDirectory, BIs.Path($"{serviceName}/services/{storageName}"));
		Assert.That(manifest.EmbeddedMongoDbLocalProtocols[storageName], Is.Not.Null);

		// the service should have a reference to the storage
		var csProjPath = $"{serviceName}/services/{serviceName}/{serviceName}.csproj";
		Assert.That(BFile.Exists(csProjPath),
			$"there should be a csproj at {csProjPath}");
		var csProjContent = BFile.ReadAllText(csProjPath);
		Assert.That(csProjContent.Contains($"<ProjectReference Include=\"..\\{storageName}\\{storageName}.csproj\" />"),
			"There should be a reference to the storage in the csproj"
			);
		// <ProjectReference Include="..\Data\Data.csproj" />

		#endregion
	}

}
