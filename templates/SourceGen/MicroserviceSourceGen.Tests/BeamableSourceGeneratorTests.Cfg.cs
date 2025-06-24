using Beamable.Microservice.SourceGen;
using Beamable.Microservice.SourceGen.Analyzers;
using Beamable.Server;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests
{
	[Fact]
	public async Task Test_Diagnostic_Cfg_NoSourceGenConfigFound()
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
		var ctx = new CSharpAnalyzerTest<FederationAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, null, UserCode);

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Cfg.NoSourceGenConfigFound));
		
		await ctx.RunAsync();
	}
	
	[Fact]
	public async Task Test_Diagnostic_Cfg_MultipleSourceGenConfigsFound()
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
		var ctx = new CSharpAnalyzerTest<FederationAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, null, UserCode);
		
		ctx.TestState.AdditionalFiles.Add((MicroserviceFederationsConfig.CONFIG_FILE_NAME, "{ }"));
		ctx.TestState.AdditionalFiles.Add((MicroserviceFederationsConfig.CONFIG_FILE_NAME, "{ }"));

		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Cfg.MultipleSourceGenConfigsFound).WithArguments(
				"federations.json, federations.json"));
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Fed.FederationCodeGeneratedProperly));
		
		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Cfg_FailedToDeserializeSourceGenConfig()
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
		var ctx = new CSharpAnalyzerTest<FederationAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, null, UserCode);
		
		ctx.TestState.AdditionalFiles.Add((MicroserviceFederationsConfig.CONFIG_FILE_NAME, "{ invalid json }"));

		ctx.ExpectedDiagnostics.Add(
			new DiagnosticResult(Diagnostics.Cfg.FailedToDeserializeSourceGenConfig).WithArguments(
				@"'i' is an invalid start of a property name. Expected a '""'. Path: $ | LineNumber: 0 | BytePositionInLine: 2."));
		
		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Cfg_SuccessfullyDeserializeSourceGenConfig()
	{
		const string UserCode = @"
using Beamable.Server;
using Beamable.Experimental.Api.Lobbies;
using Beamable.Common;

namespace TestNamespace;

[FederationId(""hathora"")]
public class HathoraFederation : IFederationId
{
}

[Microservice(""some_user_service"")]
public partial class SomeUserMicroservice : Microservice, IFederatedGameServer<HathoraFederation>
{
    public Promise<ServerInfo> CreateGameServer(Lobby lobby)
    {
        throw new System.NotImplementedException();
    }
}
";
		var cfg = new MicroserviceFederationsConfig() { Federations = new() { { "hathora", [new() { Interface = "IFederatedGameServer" }] } } };

		var ctx = new CSharpAnalyzerTest<FederationAnalyzer, DefaultVerifier>();
		
		PrepareForRun(ctx, cfg, UserCode);
		
		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Fed.FederationCodeGeneratedProperly));
		
		await ctx.RunAsync();
	}
}
