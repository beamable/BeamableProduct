using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Builders;
using Moq;
using NUnit.Framework;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using tests.MoqExtensions;

namespace tests.Examples.Services;

[TestFixture]
public class BeamServicesFlows : CLITest
{
	private const string ServiceName = "Example";
	private DockerClient _dockerClient = null!;

	[SetUp]
	public new void Setup()
	{
		base.Setup();
		_dockerClient = new DockerClientConfiguration(new AnonymousCredentials()).CreateClient();
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
		// Dispose of the Docker client
		_dockerClient.Dispose();
	}

	[Test]
	public void CanCreateNewBeamableSolution()
	{
		#region Arrange

		Ansi.Input.PushTextWithEnter("n"); // don't link unity project
		Ansi.Input.PushTextWithEnter("n"); // don't link unreal project

		#endregion

		#region Act

		Run("project", "new", ServiceName);

		#endregion

		#region Assert

		// there should a .sln file
		Assert.That(File.Exists($"{ServiceName}/{ServiceName}.sln"),
			$"There must be an Example.sln file after beam project new {ServiceName}");

		// there should be a beamoLocalManifest.json file
		Assert.That(File.Exists($"{ServiceName}/.beamable/beamoLocalManifest.json"),
			$"There must be a beamo local manifest file after beam project new {ServiceName}");

		// the contents of the file beamoId should be equal to the name of the service created
		string localManifestTextContent = File.ReadAllText($"{ServiceName}/.beamable/beamoLocalManifest.json");
		Assert.That(localManifestTextContent.Contains($"\"BeamoId\":\"{ServiceName}\""));

		#endregion
	}

	[Test]
	public async Task CanResetBeamableServiceContainer()
	{
		#region Arrange

		CanCreateNewBeamableSolution();

		// Create a new instance of a container
		var container = new ContainerBuilder()
			.WithName(ServiceName)
			// Set the image for the container to "testcontainers/helloworld:1.1.0"
			.WithImage("testcontainers/helloworld:1.1.0")
			// Build the container configuration
			.Build();

		// Start the container.
		await container.StartAsync()
			.ConfigureAwait(false);

		// Get list of all containers
		var containers = await _dockerClient.Containers.ListContainersAsync(
			new ContainersListParameters { All = true });

		#endregion

		#region Act + Assert

		// Check if the container with the specified name is running
		bool isRunning = containers.Any(c => c.Names.Contains($"/{ServiceName}"));
		Assert.IsTrue(isRunning, $"Container '{ServiceName}' should be running.");

		string workingDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory($"{workingDirectory}/{ServiceName}");
		Run("services", "reset", "container", "--ids", ServiceName);

		// Check if the container with the specified name is not running
		bool isNotRunning = containers.Any(c => c.Names.Contains($"/{ServiceName}"));
		Assert.IsTrue(isNotRunning, $"Container '{ServiceName}' should not be running.");

		#endregion
	}

	[Test]
	public async Task CanStopBeamableServiceContainer()
	{
		#region Arrange

		CanCreateNewBeamableSolution();

		// Create a new instance of a container
		var container = new ContainerBuilder()
			.WithName(ServiceName)
			// Set the image for the container to "testcontainers/helloworld:1.1.0"
			.WithImage("testcontainers/helloworld:1.1.0")
			// Build the container configuration
			.Build();

		// Start the container.
		await container.StartAsync()
			.ConfigureAwait(false);

		// Get list of all containers
		var containers = await _dockerClient.Containers.ListContainersAsync(
			new ContainersListParameters { All = true });

		#endregion

		#region Act + Assert

		// Check if the container with the specified name is running
		bool isRunning = containers.Any(c => c.Names.Contains($"/{ServiceName}"));
		Assert.IsTrue(isRunning, $"Container '{ServiceName}' should be running.");

		string workingDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory($"{workingDirectory}/{ServiceName}");
		Run("services", "stop", "--ids", ServiceName);

		// Check if the container with the specified name is not running
		bool isNotRunning = containers.Any(c => c.Names.Contains($"/{ServiceName}"));
		Assert.IsTrue(isNotRunning, $"Container '{ServiceName}' should not be running.");

		#endregion
	}
}
