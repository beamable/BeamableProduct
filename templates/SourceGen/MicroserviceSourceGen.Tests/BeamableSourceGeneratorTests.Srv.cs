using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Linq;
using System.Threading.Tasks;
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

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "example", [new() { Interface = "IFederatedLogin" }] } } };

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

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

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

[Microservice(""id"")]
public class SomeUserMicroservice : Microservice
{		
}

[Microservice(""id2"")]
public class SomeOtherUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.MultipleMicroserviceClassesDetected));
	}
	
	[Fact]
	public void Test_Diagnostic_Srv_MultipleMicroserviceClassesDetected_PartialCompatibility()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""someid"")]
public partial class SomeUserMicroservice : Microservice
{		
}

public partial class SomeUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.DoesNotContain(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.MultipleMicroserviceClassesDetected));
	}

	[Fact]
	public void Test_Diagnostic_Srv_NonPartialMicroserviceClassDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""id"")]
public class SomeUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() };

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

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		// We are testing the detection
		PrepareForRun(new[] { cfg }, new[] { UserCode });

		// Run generators and retrieve all results.
		var runResult = Driver.RunGenerators(Compilation).GetRunResult();

		// Ensure we have a single diagnostic error.
		Assert.Contains(runResult.Diagnostics, d => d.Descriptor.Equals(Diagnostics.Srv.MissingMicroserviceId));
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_InvalidAsyncVoidCallableMethod()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{
	[ClientCallable]
	public async {|#0:void|} ClientTestAsyncCallable()
	{
	}

	[ServerCallable]
	public async {|#1:void|} ServerTestAsyncCallable()
	{
	}

	[Callable]
	public async {|#2:void|} TestAsyncCallable()
	{
	}
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(0)
			.WithArguments("ClientTestAsyncCallable"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(1)
			.WithArguments("ServerTestAsyncCallable"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(2)
			.WithArguments("TestAsyncCallable"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Fed.FederationCodeGeneratedProperly));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_CodeFixer_Srv_InvalidAsyncVoidCallableMethod()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{
	[ClientCallable]
	public async {|#0:void|} ClientTestAsyncCallable()
	{
	}

	[ServerCallable]
	public async {|#1:void|} ServerTestAsyncCallable()
	{
	}

	[Callable]
	public async {|#2:void|} TestAsyncCallable()
	{
	}
}
";
		
		const string FixedCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice
{
	[ClientCallable]
	public async Task ClientTestAsyncCallable()
	{
	}

	[ServerCallable]
	public async Task ServerTestAsyncCallable()
	{
	}

	[Callable]
	public async Task TestAsyncCallable()
	{
	}
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		var ctx = new CSharpCodeFixTest<ServicesAnalyzer, AsyncVoidCallableFixer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode, FixedCode, false);

		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(0)
			.WithArguments("ClientTestAsyncCallable"));
		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(1)
			.WithArguments("ServerTestAsyncCallable"));
		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.InvalidAsyncVoidCallableMethod)
			.WithLocation(2)
			.WithArguments("TestAsyncCallable"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Fed.FederationCodeGeneratedProperly));
		
		await ctx.RunAsync();
	}
}
