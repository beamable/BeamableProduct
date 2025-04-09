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

namespace Beamable.Server;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ServicesFixer)), Shared]
public class ServicesFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Diagnostics.Srv.DIAG_INVALID_ASYNC_VOID_ID);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
			.ConfigureAwait(false);
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		var methodDecl = root.FindToken(diagnosticSpan.Start).Parent
			.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

		context.RegisterCodeFix(
			Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
				title: "Change async void to async Task",
				createChangedDocument: c => FixAsyncCallable(context.Document, methodDecl, c),
				equivalenceKey: "ChangeAsyncVoidToTask"),
			diagnostic);
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
