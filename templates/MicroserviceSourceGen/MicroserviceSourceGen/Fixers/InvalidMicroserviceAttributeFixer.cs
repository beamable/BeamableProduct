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
/// This class is responsible for Fixing invalid microservice attribute either if it is missing or if it doesn't match BeamId
/// The fix will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InvalidMicroserviceAttributeFixer)), Shared]
public class InvalidMicroserviceAttributeFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
			Diagnostics.Srv.MICROSERVICE_ID_INVALID_FROM_CS_PROJ_ID);

	public sealed override FixAllProvider GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		
		var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
		
		var classDecl = rootNode.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
		if (classDecl != null)
		{
			
			if (!diagnostic.Properties.TryGetValue(Diagnostics.Srv.PROP_BEAM_ID, out string attributeValue))
			{
				attributeValue = classDecl.Identifier.ToString();
			}
			
			context.RegisterCodeFix(
				Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
					title: $"[{diagnostic.Descriptor.Title}] Set Microservice attribute to \"{attributeValue}\"",
					createChangedDocument: c => AddOrSetMicroserviceAttribute(context.Document, classDecl, c, attributeValue),
					equivalenceKey: $"{diagnostic.Descriptor.Id}"),
				diagnostic);
		}
	}
	
	private async Task<Document> AddOrSetMicroserviceAttribute(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken, string attributeValue)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var hasAttribute = classDecl.AttributeLists.Any(attList => attList.Attributes.Any(att => att.Name.ToString().Contains(nameof(Microservice))));
		
		AttributeSyntax attribute =
			FederationDefaultIDFixer.GenerateCustomAttributeWithArgument(nameof(Server.Microservice), attributeValue);
		AttributeListSyntax newAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
		
		ClassDeclarationSyntax newClassDecl = hasAttribute
			? FederationDefaultIDFixer.UpdateClassAttribute(classDecl, nameof(Microservice), attribute)
			: classDecl.AddAttributeLists(newAttributeList);

		var newRoot = root.ReplaceNode(classDecl, newClassDecl);
		return document.WithSyntaxRoot(newRoot);
	}
}
