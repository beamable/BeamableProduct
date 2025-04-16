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

		var sourceConfigText = context.AdditionalTextsProvider
			.Where(text => text.Path.EndsWith(MicroserviceFederationsConfig.CONFIG_FILE_NAME, StringComparison.OrdinalIgnoreCase))
			.Select((text, token) => (Path: text.Path, Text: text.GetText(token)?.ToString()))
			.Where(text => text.Item2 is not null)!
			.Collect<ValueTuple<string, string>>();

		context.RegisterSourceOutput(microserviceTypes.Combine(sourceConfigText), GenerateCode);
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

	private static void GenerateCode(SourceProductionContext context, (ImmutableArray<MicroserviceInfo> Left, ImmutableArray<(string, string)> Right) args)
	{

	}

	
}
