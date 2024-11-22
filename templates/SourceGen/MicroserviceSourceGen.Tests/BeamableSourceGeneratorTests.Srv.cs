using Beamable.Server;
using Microsoft.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests
{
	[Fact]
	public void Test_Diagnostic_Srv_UsesFedFromAnotherProject()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using Microservice.SourceGen.Tests.Dep;

namespace TestNamespace;

[Microservice(""TunaService"")]
public partial class TunaService : Microservice, IFederatedLogin<ExampleFederationId>
{		
	public Promise<FederatedAuthenticationResponse> Authenticate(string token, string challenge, string solution)
	{
		throw new System.NotImplementedException();
	}
}";

		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "example", [new() { Interface = "IFederatedLogin" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have no errors
		Assert.Empty(runResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
	}

	
	[Fact]
	public void Test_Diagnostic_Srv_NoMicroserviceClassesDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class SomeUserMicroservice
{		
}";

		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.NoMicroserviceClassesDetected));
	}

	[Fact]
	public void Test_Diagnostic_Srv_MultipleMicroserviceClassesDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class SomeUserMicroservice : Microservice
{		
}

public class SomeOtherUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.MultipleMicroserviceClassesDetected));
	}

	[Fact]
	public void Test_Diagnostic_Srv_NonPartialMicroserviceClassDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class SomeUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.NonPartialMicroserviceClassDetected));
	}

	[Fact]
	public void Test_Diagnostic_Srv_MissingMicroserviceId()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public partial class SomeUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.MissingMicroserviceId));
	}

	[Fact]
	public void Test_Diagnostic_Srv_InvalidMicroserviceId()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""2-n%t-v#l!d-i&"")]
public partial class SomeUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceSourceGenConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.InvalidMicroserviceId));
	}
}
