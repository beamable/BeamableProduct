using Beamable.Common;
using Beamable.Server;
using Microservice.SourceGen.Tests.Dep;
using Microservice.SourceGen.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests : IDisposable
{
	public BeamableSourceGenerator Generator { get; set; }
	public GeneratorDriver Driver { get; set; }
	public CSharpCompilation Compilation { get; set; }

	/// <summary>
	/// The in-memory source generator can add the wrong version of dotnet when looking for system types.
	/// This means that it can fail to load attribute values.
	/// https://stackoverflow.com/questions/68231332/in-memory-csharpcompilation-cannot-resolve-attributes
	/// https://stackoverflow.com/questions/23907305/roslyn-has-no-reference-to-system-runtime/72618941#72618941
	/// 
	/// </summary>
	public static readonly List<PortableExecutableReference> References = 
		AppDomain.CurrentDomain.GetAssemblies()
			.Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
			.Select(_ => MetadataReference.CreateFromFile(_.Location))
			.Concat(new[]
			{
				// add your app/lib specifics, e.g.:                      
				MetadataReference.CreateFromFile(typeof(IFederation).Assembly.Location), 
				MetadataReference.CreateFromFile(typeof(Beamable.Server.Microservice).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(MicroserviceAttribute).Assembly.Location), 
				MetadataReference.CreateFromFile(typeof(ExampleFederationId).Assembly.Location)
			})
			.ToList();
	
	public BeamableSourceGeneratorTests()
	{
		Generator = new();
		Driver = CSharpGeneratorDriver.Create(Generator);
		
		Compilation = CSharpCompilation.Create(
			nameof(BeamableSourceGenerator) + "Tests",
			Array.Empty<SyntaxTree>(),
			References);
	}

	private void PrepareForRun(IEnumerable<MicroserviceFederationsConfig?> configs, string[] csharpText, bool failConfig = false)
	{
		// If we have the source-gen file, let's add it.
		foreach (var config in configs)
		{
			var sourceGenConfigFile = failConfig ? "{ invalid json }" : JsonSerializer.Serialize(config, new JsonSerializerOptions { IncludeFields = true });
			var additionalTexts = new List<AdditionalText>();
			additionalTexts.Add(new TestAdditionalFile(MicroserviceFederationsConfig.CONFIG_FILE_NAME, sourceGenConfigFile));
			Driver = Driver.AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts));
		}

		// We need to update the compilation with all the syntax trees
		Compilation = Compilation.AddSyntaxTrees(csharpText.Select(s => CSharpSyntaxTree.ParseText(s)));
	}
	
	private static void PrepareForRun(CSharpAnalyzerTest<ServicesAnalyzer, DefaultVerifier> ctx, MicroserviceFederationsConfig? cfg, string userCode)
	{
		// Needs Beamable Runtime and Server Assemblies so it can properly find Interfaces and Classes
		var serverCommonAssembly = Assembly.Load("Beamable.Server.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
		var runtimeCommonAssembly = Assembly.Load("Unity.Beamable.Runtime.Common, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
		ctx.TestState.AdditionalReferences.Add(serverCommonAssembly);
		ctx.TestState.AdditionalReferences.Add(runtimeCommonAssembly);

		ctx.TestCode = userCode;
		if (cfg != null)
		{
			string serialize = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { IncludeFields = true });
			ctx.TestState.AdditionalFiles.Add((MicroserviceFederationsConfig.CONFIG_FILE_NAME, serialize));
		}
	}

	private static void PrepareForRun<T>(CSharpCodeFixTest<ServicesAnalyzer, T, DefaultVerifier> ctx,
		MicroserviceFederationsConfig? cfg, string userCode, string fixedCode, bool runOnce = true)
		where T : CodeFixProvider, new()
	{
		// Needs Beamable Runtime and Server Assemblies so it can properly find Interfaces and Classes
		var serverAssembly = Assembly.GetAssembly(typeof(ClientCallableAttribute));
		var runtimeAssembly = Assembly.GetAssembly(typeof(IFederationId));
		ctx.TestState.AdditionalReferences.Add(serverAssembly);
		ctx.TestState.AdditionalReferences.Add(runtimeAssembly);

		ctx.TestCode = userCode;
		ctx.FixedCode = fixedCode;
		if (cfg != null)
		{
			string serialize = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { IncludeFields = true });
			ctx.TestState.AdditionalFiles.Add((MicroserviceFederationsConfig.CONFIG_FILE_NAME, serialize));
		}

		if (runOnce)
		{
			ctx.NumberOfFixAllIterations = 0;
			ctx.NumberOfFixAllInDocumentIterations = 0;
			ctx.NumberOfFixAllInProjectIterations = 0;
			ctx.NumberOfIncrementalIterations = 1;
		}
	}

	public void Dispose()
	{
	}
}
