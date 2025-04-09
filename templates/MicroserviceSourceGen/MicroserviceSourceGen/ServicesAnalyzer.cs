using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Beamable.Server;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ServicesAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Diagnostics.Srv.InvalidAsyncVoidCallableMethod);
	
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeForAsyncVoidMethod, SyntaxKind.MethodDeclaration);
	}
	
	private static void AnalyzeForAsyncVoidMethod(SyntaxNodeAnalysisContext context)
	{
		var method = (MethodDeclarationSyntax)context.Node;

		// Check if Method is async, if not skip analyze
		if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
		{
			return;
		}
		
		// Check if method has the CallableAttribute
		IMethodSymbol symbol = context.SemanticModel.GetDeclaredSymbol(method);
		if (symbol == null || !symbol.GetAttributes().Any(BeamableSourceGenerator.IsCallableAttribute))
		{
			return;
		}
		
		// Check if Return type is null
		ITypeSymbol returnType = ModelExtensions.GetTypeInfo(context.SemanticModel, method.ReturnType).ConvertedType;
		if (returnType?.SpecialType != SpecialType.System_Void)
		{
			return;
		}
		
		var diagnostic = Diagnostic.Create(Diagnostics.Srv.InvalidAsyncVoidCallableMethod, method.ReturnType.GetLocation(), method.Identifier.Text);
		context.ReportDiagnostic(diagnostic);
	}
	
}
