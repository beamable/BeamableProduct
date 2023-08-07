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
using tests.Examples.Project;
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
	}

	[TearDown]
	public new void Teardown()
	{
		// Dispose of the Docker client
		_dockerClient.Dispose();
	}

	[Test]
	public async Task CanResetBeamableServiceContainer()
	{
		#region Arrange

		new BeamProjectFlows().CanCreateNewBeamableSolution();

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

		new BeamProjectFlows().CanCreateNewBeamableSolution();

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
