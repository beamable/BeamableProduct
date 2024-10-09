using Beamable.Server;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests
{
	[Fact]
	public void Test_Diagnostic_Fed_DeclaredFederationMissingFromSourceConfig()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class MyFederation : IFederationId {
	public string UniqueName => ""my_federation"";
}

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice, IFederatedLogin<MyFederation>
{		
}
";
		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Fed.DeclaredFederationMissingFromSourceGenConfig));
	}
	
	[Fact]
	public void Test_Diagnostic_Fed_DeclaredFederationInvalidFederationId()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{		
}
";
		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { @"my_f!*&@¨&*¨@!*&", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Fed.DeclaredFederationInvalidFederationId));
	}
	
	[Fact]
	public void Test_Diagnostic_Fed_DeclaredFederationInvalidFederationId_WithHandwritten()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class MyFederation : IFederationId {
	public string UniqueName => ""my_f!*&@¨&*¨@!*&"";
}

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice, IFederatedLogin<MyFederation>
{		
}
";
		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { @"my_f!*&@¨&*¨@!*&", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Fed.DeclaredFederationInvalidFederationId));
	}

	[Fact]
	public void Test_Diagnostic_Fed_FederationCodeGeneratedProperly()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{		
}
";
		var cfg = new MicroserviceSourceGenConfig()
		{
			Federations = new()
			{
				{ "hathora", [new() { Interface = "IFederatedGameServer" }] },
				{ "steam", [new() { Interface = "IFederatedInventory" }, new() { Interface = "IFederatedGameServer" }] },
				{ "discord", [new() { Interface = "IFederatedLogin" }] }
			}
		};

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Fed.FederationCodeGeneratedProperly));
	}
	
	[Fact]
	public void Test_Diagnostic_Fed_FederationCodeGeneratedProperly_WithFullHandwrittenOverride()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class HandwrittenHathoraId : IFederationId {
	public string UniqueName => ""hathora"";
}

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice, IFederatedGameServer<HandwrittenHathoraId>
{		
}
";
		var cfg = new MicroserviceSourceGenConfig()
		{
			Federations = new()
			{
				{ "hathora", [new() { Interface = "IFederatedGameServer" }] },
				{ "steam", [new() { Interface = "IFederatedInventory" }, new() { Interface = "IFederatedGameServer" }] },
				{ "discord", [new() { Interface = "IFederatedLogin" }] }
			}
		};

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Assert that we didn't generate the HathoraId IFederationId class
		Assert.All(runResult.Results[0].GeneratedSources, sr => Assert.DoesNotContain("HathoraId", sr.SourceText.ToString()));
		
		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Fed.FederationCodeGeneratedProperly));
	}
	
	[Fact]
	public void Test_Diagnostic_Fed_FederationCodeGeneratedProperly_WithPartialHandwrittenOverride()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class HandwrittenSteamId : IFederationId {
	public string UniqueName => ""steam"";
}

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice, IFederatedGameServer<HandwrittenSteamId>
{		
}
";
		var cfg = new MicroserviceSourceGenConfig()
		{
			Federations = new()
			{
				{ "hathora", [new() { Interface = "IFederatedGameServer" }] },
				{ "steam", [new() { Interface = "IFederatedInventory" }, new() { Interface = "IFederatedGameServer" }] },
				{ "discord", [new() { Interface = "IFederatedLogin" }] }
			}
		};

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Assert that we didn't generate the HathoraId IFederationId class
		foreach (GeneratedSourceResult sr in runResult.Results[0].GeneratedSources)
		{
			// Assert we did use the handwritten steam id for any interface declarations we have.
			if (sr.HintName.Contains("steam"))
			{
				Assert.Contains("HandwrittenSteamId", sr.SourceText.ToString());
			}
			
			// Assert we didn't create an id class for the HandwrittenSteamId.
			if (sr.HintName.Contains("FederationIds"))
			{
				Assert.DoesNotContain("SteamId", sr.SourceText.ToString());
			}
		}
		
		
		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Fed.FederationCodeGeneratedProperly));
	}
}
