using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Microservice.SourceGen.Fixers;

/// <summary>
/// This class is responsible for Fixing Federations issues which we cannot automatically apply the fixes
/// The fix message will appear on the IDE, inserting a comment line with a pragma warning disable for that diagnostics.
/// This class is not being used at the moment as some of the Diagnostics that was using it were removed as we don't have Federation.json anymore.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FederationDefaultFixer)), Shared]
public class FederationDefaultFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray<string>.Empty;

	public sealed override FixAllProvider GetFixAllProvider() => null;
	
	public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		
		context.RegisterCodeFix(
			Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
				title: $"[{diagnostic.Descriptor.Title}] No Code/Solution auto-fix for this, a Comment will be added to Guide the next steps.",
				createChangedDocument: c => AddCommentToGuideUser(context.Document, diagnosticSpan, diagnostic, c),
				equivalenceKey: $"{diagnostic.Descriptor.Id}"),
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

		List<SyntaxTrivia> triviaComments = new();
		triviaComments.Add(SyntaxFactory.Comment($"#pragma warning disable {diagnostic.Id}"));
		triviaComments.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
		triviaComments.Add(SyntaxFactory.Comment($"// TODO: {GetFixMessages(diagnostic)}"));
		triviaComments.Add(SyntaxFactory.ElasticCarriageReturnLineFeed);
		
		var comments = SyntaxFactory.TriviaList(triviaComments)
			.AddRange(targetNode.GetLeadingTrivia());
		
		var newNode = node.WithLeadingTrivia(comments);
		var newRoot = root.ReplaceNode(targetNode, newNode);
		return document.WithSyntaxRoot(newRoot);
	}

	private string GetFixMessages(Diagnostic diagnostic)
	{
		return diagnostic.GetMessage();
	}
}
