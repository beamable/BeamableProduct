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
using UnityEngine;

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

	private static void PrepareForRun<T>(CSharpAnalyzerTest<T, DefaultVerifier> ctx,
		string userCode, string[]? extraGlobalConfigs = null) where T : DiagnosticAnalyzer, new()
	{
		ctx.DisabledDiagnostics.Add("BEAM_DBG_0001");
		AddAssemblyReferences(ctx.TestState);

		ctx.TestCode = userCode;

		string globalConfig = @"
is_global = true 
build_property.EnableUnrealBlueprintCompatibility = true";
		if (extraGlobalConfigs != null)
		{
			foreach (string extraGlobalConfig in extraGlobalConfigs)
			{
				globalConfig += $@"
{extraGlobalConfig}";

			}
		}

		ctx.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", globalConfig));
	}

	private static void PrepareForRun<TAnalyzer,TFixProvider>(CSharpCodeFixTest<TAnalyzer, TFixProvider, DefaultVerifier> ctx, string userCode, string fixedCode, bool runOnce = true)
		where TFixProvider : CodeFixProvider, new() where TAnalyzer : DiagnosticAnalyzer, new()
	{
		AddAssemblyReferences(ctx.TestState);
		
		ctx.CodeActionValidationMode = CodeActionValidationMode.SemanticStructure;
		ctx.TestCode = NormalizeLineEndings(userCode);
		ctx.FixedCode = NormalizeLineEndings(fixedCode);
		
		ctx.DisabledDiagnostics.Add("BEAM_DBG_0001");
		
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
		if (string.IsNullOrEmpty(input))
			return input;
		
		string normalized = input
			.Replace("\r\n", "\n")
			.Replace("\n\r", "\n")
			.Replace("\r", "\n");
		
		normalized = normalized.Replace("\n", Environment.NewLine);

		return normalized;
	}
	
	private static void AddAssemblyReferences(SolutionState state)
	{
		// Needs Beamable Runtime and Server Assemblies so it can properly find Interfaces and Classes
		var serverAssembly = Assembly.GetAssembly(typeof(ClientCallableAttribute));
		var runtimeAssembly = Assembly.GetAssembly(typeof(IFederationId));
		var unityAssembly = Assembly.GetAssembly(typeof(ScriptableObject));
		state.AdditionalReferences.Add(serverAssembly!);
		state.AdditionalReferences.Add(runtimeAssembly!);
		state.AdditionalReferences.Add(unityAssembly!);
	}

	public void Dispose()
	{
	}
}
