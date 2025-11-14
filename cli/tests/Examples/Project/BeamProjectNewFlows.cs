using cli;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.IO;
using tests.MoqExtensions;

namespace tests.Examples.Project;

public partial class BeamProjectNewFlows : CLITestExtensions
{
	[Test]
	[TestCase("Example", ".")]
	[TestCase("Example", "toast")]
	[TestCase("Example", "with a space")]
	public void NewProject_Init_NoSlnConfig(string serviceName, string dir)
	{
		#region Arrange

		SetupMocks(mockAdminMe: false);
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm


		#endregion

		#region Act

		Run("init", dir);
		var old = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(dir);
		Run("project", "new", "service", serviceName, "--quiet");
		Directory.SetCurrentDirectory(old);
		_mockObjects.Clear();

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{dir}/BeamableServices.sln"),
			$"There must be an Example.sln file after beam project new {serviceName}");

		// there should be a local-services-manifest.json file
		Assert.That(BFile.Exists($"{dir}/services/{serviceName}/{serviceName}.csproj"),
			$"There must be a csproj file after beam project new {serviceName}");

		Assert.That(BFile.Exists($"{dir}/services/{serviceName}/Dockerfile"),
			$"There must be a dockerfile");
		Assert.That(!BFile.Exists($"{dir}/services/{serviceName}/Dockerfile-BeamableDev"),
			$"There must not be a dev dockerfile");
		
		// There should be a .config/dotnet-tools.json file
		Assert.That(BFile.Exists($"{dir}/.config/dotnet-tools.json"), "There must be .config/dotnet-tools.json file");

		
		// the contents of the file beamoId should be equal to the name of the service created
		
		
		string localCsProjContent = BFile.ReadAllText($"{dir}/services/{serviceName}/{serviceName}.csproj");
		// var manifest = JsonConvert.DeserializeObject<BeamoLocalManifest>(localManifestTextContent);
		// Assert.That(manifest!.ServiceDefinitions.Count, Is.EqualTo(1));

		var hasProjectType = localCsProjContent.Contains("<BeamProjectType>service</BeamProjectType>");
		var hasNugetReference = localCsProjContent.Contains("<PackageReference Include=\"Beamable.Microservice.Runtime\" Version=\"$(BeamableVersion)\" />");
		
		Assert.That(hasProjectType, "csproj must have project type fragment\n" + localCsProjContent);
		Assert.That(hasNugetReference, "csproj must have nuget reference fragment\n" + localCsProjContent);
		
		string dockerfileContent = BFile.ReadAllText($"{dir}/services/{serviceName}/Dockerfile");
		var hasCorrectCopyFragment = dockerfileContent.Contains("# <beamReserved>") &&
		                             dockerfileContent.Contains("# </beamReserved>");
		Assert.IsTrue(hasCorrectCopyFragment, "the docker file needs to have a copy line relative to the services folder.\n" + dockerfileContent);
		
		#endregion
	}

	[TestCase(".")]
	[TestCase("toast")]
	[TestCase("with a space")]
	public void NewProject_AutoInit_SlnPathInLocalFolder(string dir)
	{
		#region Arrange

		SetupMocks(mockAdminMe: false);
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm

		const string serviceName = "Example";
		const string slnName = "fake";

		#endregion

		#region Act


		Run("init", dir);
		var old = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(dir);
		Run("project", "new", "service", serviceName, "--quiet", "--sln", $"{slnName}.sln");
		Directory.SetCurrentDirectory(old);
		_mockObjects.Clear();

		
		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{dir}/{slnName}.sln"),
			$"There must be an {slnName}.sln file after beam project new --sln {slnName}");

		// there should be a csproj file
		Assert.That(BFile.Exists($"{dir}/services/{serviceName}/{serviceName}.csproj"),
			$"There must be a beamo csproj file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localCsProjContent = BFile.ReadAllText($"{dir}/services/{serviceName}/{serviceName}.csproj");

		var hasProjectTypeFragment = localCsProjContent.Contains("<BeamProjectType>service</BeamProjectType>");
		Assert.That(hasProjectTypeFragment, "csproj must have project type\n" + hasProjectTypeFragment);

		#endregion
	}


	[TestCase(".")]
	[TestCase("toast")]
	[TestCase("with a space")]
	public void NewProject_AutoInit_NewSlnPathInSubFolder(string dir)
	{
		#region Arrange

		SetupMocks(mockAdminMe: false);
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

		Run("init", dir);
		var old = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(dir);
		Run("project", "new", "service", serviceName, "--quiet", "--sln", $"{slnName}.sln");
		Directory.SetCurrentDirectory(old);
		_mockObjects.Clear();
		
		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists($"{dir}/{slnName}.sln"),
			$"There must be an {slnName}.sln file after beam project new --sln {slnName}");

		// there should be a csproj file
		Assert.That(BFile.Exists($"{dir}/{slnPath}/services/{serviceName}/{serviceName}.csproj"),
			$"There must be a beamo csproj file after beam project new service.\n" );

		// the contents of the file beamoId should be equal to the name of the service created
		string localCsProjContent = BFile.ReadAllText($"{dir}/{slnPath}/services/{serviceName}/{serviceName}.csproj");

		var hasProjectTypeFragment = localCsProjContent.Contains("<BeamProjectType>service</BeamProjectType>");
		Assert.That(hasProjectTypeFragment, "csproj must have project type\n" + hasProjectTypeFragment);

		#endregion
	}

	[Test]
	public void NewProject_UsingExistingInit_NoSlnConfig()
	{
		#region Arrange

		SetupMocks(mockBeamoManifest:false, mockAdminMe: false);
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

		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
		Run("project", "new", "service", serviceName, "--quiet", "--service-directory", Path.Combine(serviceName, "services"));

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists(Constants.DEFAULT_SLN_NAME),
			$"There must be an {serviceName}/{serviceName}.sln file ");

		// there should a .beamable folder
		Assert.That(BFile.Exists(".beamable/config.beam.json"), "there must be a config defaults file after beam init.");

		// there should be a csproj file
		Assert.That(BFile.Exists($"{serviceName}/services/{serviceName}/{serviceName}.csproj"),
			$"There must be a beamo csproj file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localCsProjContent = BFile.ReadAllText($"{serviceName}/services/{serviceName}/{serviceName}.csproj");

		var hasProjectTypeFragment = localCsProjContent.Contains("<BeamProjectType>service</BeamProjectType>");
		Assert.That(hasProjectTypeFragment, "csproj must have project type\n" + hasProjectTypeFragment);

		#endregion
	}

	[Test]
	public void NewProject_UsingExistingInit_NoSlnConfig_AddSecondWithExistingSln()
	{
		#region Arrange

		SetupMocks(mockBeamoManifest:false, mockAdminMe: false);
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
		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
		
		Run("project", "new", "service", serviceName, "--quiet", "--service-directory", Path.Combine(serviceName, "services"));
		ResetConfigurator();
		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
		
		Run("project", "new", "service", secondServiceName, "--quiet", "--sln", Constants.DEFAULT_SLN_NAME, "--service-directory", Path.Combine(serviceName, "services"));

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists(Constants.DEFAULT_SLN_NAME),
			$"There must be an {serviceName}/{serviceName}.sln file ");

		Assert.That(BFile.Exists($"{serviceName}/services/{serviceName}/{serviceName}.csproj"),
			"the first service needs to have a csproj");
		Assert.That(BFile.Exists($"{serviceName}/services/{secondServiceName}/{secondServiceName}.csproj"),
			"the second service needs to have a csproj");

		// there should a .beamable folder
		Assert.That(BFile.Exists(".beamable/config.beam.json"), "there must be a config defaults file after beam init.");

		// there should be a csproj file
		Assert.That(BFile.Exists($"{serviceName}/services/{serviceName}/{serviceName}.csproj"),
			$"There must be a beamo csproj file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localCsProjContent = BFile.ReadAllText($"{serviceName}/services/{serviceName}/{serviceName}.csproj");

		var hasProjectTypeFragment = localCsProjContent.Contains("<BeamProjectType>service</BeamProjectType>");
		Assert.That(hasProjectTypeFragment, "csproj must have project type\n" + hasProjectTypeFragment);

		#endregion
	}


	[TestCase("Example", "Data")]
	public void NewProject_UsingExistingInit_NoSlnConfig_AddStorageToExistingSln(string serviceName, string storageName)
	{
		#region Arrange

		SetupMocks(mockBeamoManifest: false, mockAdminMe: false);
		Ansi.Input.PushTextWithEnter(alias); // enter alias
		Ansi.Input.PushTextWithEnter(userName); // enter email
		Ansi.Input.PushTextWithEnter(password); // enter password
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the game
		Ansi.Input.PushKey(ConsoleKey.Enter); // hit enter to pick the realm


		#endregion

		#region Act

		Run("init", "--save-to-file");
		ResetConfigurator();

		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
		Run("project", "new", "service", serviceName, "--quiet");
		ResetConfigurator();

		Mock<BeamoService>(mock =>
		{
			mock.Setup(x => x.GetCurrentManifest())
				.ReturnsPromise(new ServiceManifest())
				.Verifiable();
		});
		Run("project", "new", "storage", storageName, "--quiet", "--link-to", serviceName);

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(BFile.Exists(Constants.DEFAULT_SLN_NAME),
			$"There must be an {serviceName}/{serviceName}.sln file ");

		Assert.That(BFile.Exists($"services/{serviceName}/{serviceName}.csproj"),
			"the first service needs to have a csproj");
		Assert.That(BFile.Exists($"services/{storageName}/{storageName}.csproj"),
			"the second service needs to have a csproj");

		// there should a .beamable folder
		Assert.That(BFile.Exists(".beamable/config.beam.json"), "there must be a config defaults file after beam init.");

		// there should be a csproj file
		Assert.That(BFile.Exists($"services/{storageName}/{storageName}.csproj"),
			$"There must be a beamo csproj file after beam project new {serviceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localCsProjContent = BFile.ReadAllText($"services/{storageName}/{storageName}.csproj");

		var hasProjectTypeFragment = localCsProjContent.Contains("<BeamProjectType>storage</BeamProjectType>");
		Assert.That(hasProjectTypeFragment, "csproj must have project type\n" + hasProjectTypeFragment);
		
		// the service should have a reference to the storage
		var csProjPath = $"services/{serviceName}/{serviceName}.csproj";
		Assert.That(BFile.Exists(csProjPath),
			$"there should be a csproj at {csProjPath}");
		var csProjContent = BFile.ReadAllText(csProjPath);
		Assert.That(csProjContent.Contains($"<ProjectReference Include=\"..\\{storageName}\\{storageName}.csproj\" />"),
			"There should be a reference to the storage in the csproj\n" + csProjContent
			);
		// <ProjectReference Include="..\Data\Data.csproj" />

		#endregion
	}

}
