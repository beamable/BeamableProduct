using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Microservice.SourceGen.Fixers;

/// <summary>
/// This class is responsible for Fixing Non Readonly Static Filed on <see cref="Microservice"/> classes
/// The fix will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MicroserviceNonReadonlyStaticFieldFixer)), Shared]
public class MicroserviceNonReadonlyStaticFieldFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Diagnostics.Srv.STATIC_FIELD_FOUND_IN_MICROSERVICE_ID);
	
	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
			.ConfigureAwait(false);
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;

		var fieldDeclaration = root.FindToken(diagnosticSpan.Start).Parent
			.FirstAncestorOrSelf<FieldDeclarationSyntax>();

		if (fieldDeclaration != null && diagnostic.Properties.TryGetValue(Diagnostics.Srv.PROP_FIELD_NAME, out var fieldName))
		{
			context.RegisterCodeFix(
				Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
					title: $"[{diagnostic.Descriptor.Id}] Add readonly modifier to `{fieldName}`",
					createChangedDocument: c => AddReadonlyModifier(context.Document, fieldDeclaration, c),
					equivalenceKey: $"{diagnostic.Descriptor.Id}"),
				diagnostic);
		}
	}
	
	private async Task<Document> AddReadonlyModifier(Document document, FieldDeclarationSyntax fieldDeclaration, CancellationToken cancellationToken)
	{
		var newModifiers = fieldDeclaration.Modifiers.Add(
			SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)
				.WithTrailingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")));
		
		var newFieldDeclaration = fieldDeclaration
			.WithModifiers(newModifiers)
			.WithAdditionalAnnotations(Formatter.Annotation);
		
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var newRoot = root.ReplaceNode(fieldDeclaration, newFieldDeclaration);
		
		return document.WithSyntaxRoot(newRoot);
	}
}
