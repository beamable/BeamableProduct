using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Server;

/// <summary>
/// This class is responsible for Fixing Federations issues which we cannot automatically apply the fixes
/// The fix message will appear on the IDE, inserting a comment line with a pragma warning disable for that diagnostics.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FederationDefaultFixer)), Shared]
public class FederationDefaultFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		
		Diagnostics.Fed.DECLARED_FEDERATION_MISSING_FROM_SOURCE_GEN_CONFIG_DIAGNOSTIC_ID,
		Diagnostics.Fed.CONFIGURED_FEDERATION_MISSING_FROM_CODE_DIAGNOSTIC_ID,
		Diagnostics.Fed.FEDERATION_CODE_GENERATED_PROPERLY_DIAGNOSTIC_ID,
		Diagnostics.Fed.FEDERATION_ID_INVALID_CONFIG_FILE_ID
	);

	public sealed override FixAllProvider GetFixAllProvider() => null;
	
	public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		
		context.RegisterCodeFix(
			Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
				title: $"[{diagnostic.Id}] No Code/Solution code for this, a Comment will be added to Guide the next steps.",
				createChangedDocument: c => AddCommentToGuideUser(context.Document, diagnosticSpan, diagnostic, c),
				equivalenceKey: "AddGuideComment"),
			diagnostic);
		
		return Task.CompletedTask;
	}
	
	private async Task<Document> AddCommentToGuideUser(Document document, TextSpan span, Diagnostic diagnostic, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var node = root.FindNode(span);
		
		var targetNode = node.AncestorsAndSelf()
			.OfType<ClassDeclarationSyntax>()
			.FirstOrDefault() ?? root.ChildNodes().FirstOrDefault();
		
		if (targetNode == null)
			return document;

		var comments = SyntaxFactory.TriviaList(
			SyntaxFactory.Comment($"#pragma warning disable {diagnostic.Id}"),
			SyntaxFactory.ElasticCarriageReturnLineFeed,
			SyntaxFactory.Comment($"// TODO: {diagnostic.GetMessage()}"),
			SyntaxFactory.ElasticCarriageReturnLineFeed
		).AddRange(targetNode.GetLeadingTrivia());
		
		var newNode = node.WithLeadingTrivia(comments);
		var newRoot = root.ReplaceNode(targetNode, newNode);
		return document.WithSyntaxRoot(newRoot);
	}
}
