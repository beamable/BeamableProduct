using Beamable.Microservice.SourceGen;
using Beamable.Microservice.SourceGen.Analyzers;
using Beamable.Microservice.SourceGen.Fixers;
using Beamable.Server;
using Microservice.SourceGen.Tests.Dep;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests
{
	[Fact]
	public async Task Test_Diagnostic_Srv_UsesFedFromAnotherProject()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using Microservice.SourceGen.Tests.Dep;

namespace TestNamespace;

[Microservice(""TunaService"")]
public partial class TunaService : Beamable.Server.Microservice, IFederatedLogin<ExampleFederationId>
{		
	public Promise<FederatedAuthenticationResponse> Authenticate(string token, string challenge, string solution)
	{
		throw new System.NotImplementedException();
	}
}";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		// Microsoft.CodeAnalysis.Testing v1.1.1 used on tests don't have the Net9 Reference, so we need to manually create it
		ctx.ReferenceAssemblies = new ReferenceAssemblies("net9.0",
			new PackageIdentity("Microsoft.NETCore.App.Ref", "9.0.0"), Path.Combine("ref", "net9.0"));
		PrepareForRun(ctx, cfg, UserCode);
		
		ctx.TestState.AdditionalReferences.Add(Assembly.GetAssembly(typeof(ExampleFederationId))!);
		
		await ctx.RunAsync();
		
	}

	
	[Fact]
	public async Task Test_Diagnostic_Srv_NoMicroserviceClassesDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public class SomeUserMicroservice
{		
}";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.NoMicroserviceClassesDetected));
		
		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Srv_MultipleMicroserviceClassesDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""id"")]
public partial class SomeUserMicroservice : Microservice
{		
}

[Microservice(""id2"")]
public partial class {|#0:SomeOtherUserMicroservice|} : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() };

		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.MultipleMicroserviceClassesDetected)
			.WithLocation(0)
			.WithArguments("SomeUserMicroservice")
			.WithOptions(DiagnosticOptions.IgnoreAdditionalLocations));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_MultipleMicroserviceClassesDetected_PartialCompatibility()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""someid"")]
public partial class {|#0:SomeUserMicroservice|} : Microservice
{		
}

public partial class SomeUserMicroservice : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() };

		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);
		
		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Srv_NonPartialMicroserviceClassDetected()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

[Microservice(""id"")]
public class {|#0:SomeUserMicroservice|} : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() };

		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.NonPartialMicroserviceClassDetected).WithLocation(0));
		
		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Srv_MissingMicroserviceId()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;

namespace TestNamespace;

public partial class {|#0:SomeUserMicroservice|} : Microservice
{		
}
";

		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.MissingMicroserviceId).WithLocation(0));
		
		await ctx.RunAsync();
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
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_ReturnTypeInsideServerScope()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;



[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{

	[Beamable.BeamGenerateSchema]
	[Serializable]
	public class {|#0:DTO_Attribute|}
	{
		public int x;
	}

	[Serializable]
	public class DTO_Nested
	{
		public int x;
	}

	[ClientCallable]
	public {|#1:DTO_Nested|} CallService_Nested() 
	{
		return new DTO_Nested{ x = 1 };
	}


	[ClientCallable]
	public async {|#2:Task<DTO_MicroserviceScope>|} CallServiceAsync(DTO_MicroserviceScope {|#3:param|}) 
	{
	return new DTO_MicroserviceScope{ x = 1 };
	}
}

[Serializable]
public class DTO_MicroserviceScope 
{
    public int x;
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.ClassBeamGenerateSchemaAttributeIsNested)
				.WithLocation(0)
				.WithArguments("DTO_Attribute"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableMethodTypeIsNested)
				.WithLocation(1)
				.WithArguments("DTO_Nested", "CallService_Nested"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableTypeInsideMicroserviceScope)
				.WithLocation(1)
				.WithArguments("CallService_Nested", "DTO_Nested"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableTypeInsideMicroserviceScope)
				.WithLocation(2)
				.WithArguments("CallServiceAsync", "DTO_MicroserviceScope"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.CallableTypeInsideMicroserviceScope)
				.WithLocation(3)
				.WithArguments("CallServiceAsync", "DTO_MicroserviceScope"));

		string config = $@"
is_global = true
build_property.BeamValidateCallableTypesExistInSharedLibraries = true
";
		ctx.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_ReturnTypeNonCallableMethod()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[Serializable]
	public class DTO_Nested
	{
		public int x;
	}

	public DTO_Nested CallService_Nested() 
	{
		return new DTO_Nested{ x = 1 };
	}
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);

		string config = $@"
is_global = true
build_property.BeamValidateCallableTypesExistInSharedLibraries = true
";
		ctx.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_NonReadonlyStaticField()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	static string {|#0:TestField|} = ""Hello"";
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.StaticFieldFoundInMicroservice)
				.WithLocation(0)
				.WithArguments("TestField"));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Srv_MissingSerializableOnType()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;
using System;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	[ClientCallable]
	public async Task<DTO_AsyncTest> CallServiceAsync() 
	{
		return new DTO_AsyncTest{ x = 1 };
	}

	[ClientCallable]
	public async Task<DTO_Test> CallService() 
	{
		return new DTO_Test{ x = 1 };
	}
}


public class {|#0:DTO_AsyncTest|}
{
    public int x;
}

public class {|#1:DTO_Test|}
{
    public int x;
}

[Beamable.BeamGenerateSchema]
public class {|#2:DTO_BeamGenSchemaAttribute|}
{
	public int x;
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		
		var ctx = new CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier>();
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.MissingSerializableAttributeOnType)
				.WithLocation(0)
				.WithArguments("DTO_AsyncTest"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.MissingSerializableAttributeOnType)
				.WithLocation(1)
				.WithArguments("DTO_Test"));
		
		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.MissingSerializableAttributeOnType)
				.WithLocation(2)
				.WithArguments("DTO_BeamGenSchemaAttribute"));
		
		PrepareForRun(ctx, cfg, UserCode);
		
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
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_CodeFixer_Srv_InvalidMicroserviceID_NonMatchBeamIdProperty()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class {|#0:SomeUserMicroservice|} : Microservice
{		
}
";
		
		const string FixedCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""OtherBeamID"")]
public partial class SomeUserMicroservice : Microservice
{		
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		var ctx = new CSharpCodeFixTest<ServicesAnalyzer, InvalidMicroserviceAttributeFixer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode, FixedCode, false);

		ctx.TestState.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Srv.MicroserviceIdInvalidFromCsProj)
			.WithLocation(0)
			.WithArguments("MyMicroservice", "OtherBeamID"));
		
		string config = $@"
is_global = true
build_property.beamid = OtherBeamID
";
		ctx.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", config));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_CodeFixer_Srv_NonReadonlyStaticField()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
	static string {|#0:TestField|} = ""Hello"";
}
";
		
		const string FixedCode = @"
using Beamable.Server;
using Beamable.Common;
using System.Threading.Tasks;

namespace TestNamespace;

[Microservice(""MyMicroservice"")]
public partial class MyMicroservice : Microservice 
{
    static readonly string TestField = ""Hello"";
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() };
		
		var ctx = new CSharpCodeFixTest<ServicesAnalyzer, MicroserviceNonReadonlyStaticFieldFixer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode, FixedCode, false);
		
		ctx.TestState.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Srv.StaticFieldFoundInMicroservice)
				.WithLocation(0)
				.WithArguments("TestField"));
		
		await ctx.RunAsync();
	}
}
