using Beamable.Server;
using System;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests
{
	[Fact]
	public void Test_Diagnostic_Cfg_NoSourceGenConfigFound()
	{
		// We are testing the detection
		PrepareForRun(Array.Empty<MicroserviceSourceGenConfig>(), new[] { "" });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Cfg.NoSourceGenConfigFound));
	}
	
	[Fact]
	public void Test_Diagnostic_Cfg_MultipleSourceGenConfigsFound()
	{
		// We are testing the detection
		PrepareForRun(new MicroserviceSourceGenConfig[] { new(), new() }, new[] { "" });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Cfg.MultipleSourceGenConfigsFound));
	}

	[Fact]
	public void Test_Diagnostic_Cfg_FailedToDeserializeSourceGenConfig()
	{
		// We are testing the detection
		PrepareForRun(new MicroserviceSourceGenConfig?[] { new() }, new[] { "" }, true);

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Cfg.FailedToDeserializeSourceGenConfig));
	}

	[Fact]
	public void Test_Diagnostic_Cfg_SuccessfullyDeserializeSourceGenConfig()
	{
		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { "" });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Cfg.DeserializedSourceGenConfig));
	}
}
