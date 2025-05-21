using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Microservice.SourceGen.Fixers;

/// <summary>
/// This class is responsible for Fixing missing partial modifier on <see cref="Microservice"/> classes
/// The fix will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingPartialMicroserviceFixer)), Shared]
public class MissingPartialMicroserviceFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(Diagnostics.Srv.NON_PARTIAL_MICROSERVICE_CLASS_DETECTED_DIAGNOSTIC_ID);
	
	public sealed override FixAllProvider GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		
		var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

		var classDeclarationSyntax = rootNode.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();

		if (classDeclarationSyntax != null)
		{
			context.RegisterCodeFix(
				Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
					title: $"[{diagnostic.Descriptor.Title}] Add partial modifier",
					createChangedDocument: c => FixAsyncCallable(context.Document, classDeclarationSyntax, c),
					equivalenceKey: $"{diagnostic.Descriptor.Id}"),
				diagnostic);
		}
		
	}
	
	private async Task<Document> FixAsyncCallable(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
	{
		var newClassDecl = classDecl.AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword));

		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var newRoot = root.ReplaceNode(classDecl, newClassDecl);

		return document.WithSyntaxRoot(newRoot);
	}
}
