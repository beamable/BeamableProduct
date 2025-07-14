using Beamable.Common;
using Beamable.Microservice.SourceGen;
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
using System.Linq;
using System.Reflection;
using System.Text.Json;

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

	private static void PrepareForRun<T>(CSharpAnalyzerTest<T, DefaultVerifier> ctx, MicroserviceFederationsConfig? cfg,
		string userCode) where T : DiagnosticAnalyzer, new()
	{
		AddAssemblyReferences(ctx.TestState);

		ctx.TestCode = userCode;
		if (cfg != null)
		{
			string serialize = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { IncludeFields = true });
			ctx.TestState.AdditionalFiles.Add((MicroserviceFederationsConfig.CONFIG_FILE_NAME, serialize));
		}
	}

	private static void PrepareForRun<TAnalyzer,TFixProvider>(CSharpCodeFixTest<TAnalyzer, TFixProvider, DefaultVerifier> ctx,
		MicroserviceFederationsConfig? cfg, string userCode, string fixedCode, bool runOnce = true)
		where TFixProvider : CodeFixProvider, new() where TAnalyzer : DiagnosticAnalyzer, new()
	{
		AddAssemblyReferences(ctx.TestState);
		
		ctx.CodeActionValidationMode = CodeActionValidationMode.SemanticStructure;
		ctx.TestCode = NormalizeLineEndings(userCode);
		ctx.FixedCode = NormalizeLineEndings(fixedCode);
		
		ctx.SolutionTransforms.Add((solution, projectId) =>
		{
			var project = solution.GetProject(projectId)!;

			foreach (var doc in project.Documents)
			{
				var text = doc.GetTextAsync().Result;
				var normalizedText = Microsoft.CodeAnalysis.Text.SourceText.From(
					NormalizeLineEndings(text.ToString()),
					text.Encoding
				);
				solution = solution.WithDocumentText(doc.Id, normalizedText);
			}

			return solution;
		});
		
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

	private static string NormalizeLineEndings(string input)
	{
		return input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n\r", "\n").Replace("\n", Environment.NewLine);
	}
	
	private static void AddAssemblyReferences(SolutionState state)
	{
		// Needs Beamable Runtime and Server Assemblies so it can properly find Interfaces and Classes
		var serverAssembly = Assembly.GetAssembly(typeof(ClientCallableAttribute));
		var runtimeAssembly = Assembly.GetAssembly(typeof(IFederationId));
		state.AdditionalReferences.Add(serverAssembly!);
		state.AdditionalReferences.Add(runtimeAssembly!);
	}

	public void Dispose()
	{
	}
}
