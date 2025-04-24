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
/// This class is responsible for Fixing async void <see cref="CallableAttribute"/> methods
/// The fix will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AsyncVoidCallableFixer)), Shared]
public class AsyncVoidCallableFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Diagnostics.Srv.INVALID_ASYNC_VOID_CALLABLE_DIAGNOSTIC_ID);
	
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
			.ConfigureAwait(false);
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		// Using the diagnosticSpan (code range from the Diagnostic Location) we can find the MethodDeclaration by searching
		// From self or Ancestors nodes.
		var methodDecl = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
			.OfType<MethodDeclarationSyntax>().FirstOrDefault();

		if (methodDecl != null)
		{
			context.RegisterCodeFix(
				Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
					title: $"[{diagnostic.Descriptor.Id}] async void to async Task",
					createChangedDocument: c => FixAsyncCallable(context.Document, methodDecl, c),
					equivalenceKey: $"{diagnostic.Descriptor.Id}"),
				diagnostic);
		}
		
	}
	
	private async Task<Document> FixAsyncCallable(Document document, MethodDeclarationSyntax methodDecl, CancellationToken cancellationToken)
	{
		var taskType = SyntaxFactory.ParseTypeName("Task")
			.WithTriviaFrom(methodDecl.ReturnType);

		var newMethod = methodDecl.WithReturnType(taskType);

		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var newRoot = root.ReplaceNode(methodDecl, newMethod);

		return document.WithSyntaxRoot(newRoot);
	}
}
