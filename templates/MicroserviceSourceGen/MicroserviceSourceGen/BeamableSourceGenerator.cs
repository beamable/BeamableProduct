using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Threading;
using SourceProductionContext = Microsoft.CodeAnalysis.SourceProductionContext;

namespace Beamable.Microservice.SourceGen;

/// <summary>
/// A sample source generator that creates C# classes based on the text file (in this case, Domain Driven Design ubiquitous language registry).
/// When using a simple text file as a baseline, we can create a non-incremental source generator.
/// </summary>
[Generator]
public class BeamableSourceGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var microserviceTypes = context.SyntaxProvider
			.CreateSyntaxProvider(CouldBeMicroserviceAsync, GetMicroserviceInfo)
			.Where(type => type != default)!
			.Collect<MicroserviceInfo>();

		context.RegisterSourceOutput(microserviceTypes, GenerateCode);
		return;

		static bool CouldBeMicroserviceAsync(SyntaxNode syntaxNode, CancellationToken cancellationToken)
		{
			if (syntaxNode is not ClassDeclarationSyntax c)
				return false;

			return c.BaseList?.Types.Count > 0;
		}

		static MicroserviceInfo GetMicroserviceInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
		{
			var classDeclaration = (ClassDeclarationSyntax)context.Node;
			var type = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclaration) as INamedTypeSymbol;

			return type is null || type.BaseType?.Name != "Microservice" ? default : new MicroserviceInfo(type);
		}
	}

	private static void GenerateCode(SourceProductionContext context, ImmutableArray<MicroserviceInfo> args)
	{
		// All the validations from this method were moved to their specific DiagnosticAnalyzer, which are the FederationAnalyzer and ServicesAnalyzer.
		// This method isn't removed just to keep the reference from the IncrementalGenerator, but at the moment this class isn't doing much
		// In the future we will use this method to generate the code
	}

	
}
