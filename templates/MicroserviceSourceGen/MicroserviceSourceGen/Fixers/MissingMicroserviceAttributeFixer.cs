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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingMicroserviceAttributeFixer)), Shared]
public class MissingMicroserviceAttributeFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => 
		ImmutableArray.Create(Diagnostics.Srv.MISSING_MICROSERVICE_ID_DIAGNOSTIC_ID);

	public sealed override FixAllProvider GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		
		var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
		
		var classDecl = rootNode.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
		if (classDecl != null)
		{
			context.RegisterCodeFix(
				Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
					title: $"[{diagnostic.Descriptor.Title}] Add Microservice Attribute",
					createChangedDocument: c => AddMicroserviceAttribute(context.Document, classDecl, c),
					equivalenceKey: "FixFederationIdDefault"),
				diagnostic);
		}
	}
	
	private async Task<Document> AddMicroserviceAttribute(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken);

		AttributeSyntax attribute =
			FederationDefaultIDFixer.GenerateCustomAttributeWithArgument(nameof(Server.Microservice),
				classDecl.Identifier.ToString());
		AttributeListSyntax newAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
		
		var newClassDecl = classDecl.AddAttributeLists(newAttributeList);
		
		var newRoot = root.ReplaceNode(classDecl, newClassDecl);
		return document.WithSyntaxRoot(newRoot);
	}
}
