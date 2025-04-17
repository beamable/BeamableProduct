using Beamable.Microservice.SourceGen.Analyzers;
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
/// This class is responsible for Fixing Federations IDS that are missing or when it needs to be default
/// The fix message will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FederationDefaultIDFixer)), Shared]
public class FederationDefaultIDFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		Diagnostics.Fed.FEDERATION_ID_MISSING_ATTRIBUTE_DIAGNOSTIC_ID,
		Diagnostics.Fed.FEDERATION_ID_MUST_BE_DEFAULT_DIAGNOSTIC_ID
	);

	public sealed override FixAllProvider GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		
		var rootNode = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
		
		// Using the diagnosticSpan (code range from the Diagnostic Location) we can find the ClassDeclaration by searching
		// From self or Ancestors nodes.
		var classDecl = rootNode.FindNode(diagnosticSpan).AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
		if (classDecl != null)
		{
			context.RegisterCodeFix(
				Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
					title: $"[{diagnostic.Descriptor.Title}] Set FederationId attribute to \"default\"",
					createChangedDocument: c => AddOrFixFederationIdAttribute(context.Document, classDecl, c),
					equivalenceKey: "FixFederationIdDefault"),
				diagnostic);
		}
	}
	
	private async Task<Document> AddOrFixFederationIdAttribute(Document document, ClassDeclarationSyntax classDecl, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var existingAttrs = classDecl.AttributeLists.SelectMany(a => a.Attributes);
		var hasFederationID = existingAttrs.Count(item => item.Name.ToString().Contains(FederationAnalyzer.FEDERATION_ATTRIBUTE_NAME)) > 0;

		
		AttributeSyntax attribute = GenerateCustomAttributeWithArgument(FederationAnalyzer.FEDERATION_ATTRIBUTE_NAME);
		AttributeListSyntax newAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));

		ClassDeclarationSyntax newClassDecl;
		 
		if (!hasFederationID)
		{
			newClassDecl = classDecl.AddAttributeLists(newAttributeList);
		}
		else
		{
			var updatedLists = classDecl.AttributeLists.Select(attrList =>
			{
				var newAttrs = attrList.Attributes.Select(attr =>
				{
					string attributeName = attr.Name.ToString();
					return attributeName.Contains(FederationAnalyzer.FEDERATION_ATTRIBUTE_NAME) ? attribute : attr;
				}).ToArray();
				return SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(newAttrs));
			});

			newClassDecl = classDecl.WithAttributeLists(SyntaxFactory.List(updatedLists));
		}

		var newRoot = root.ReplaceNode(classDecl, newClassDecl);
		return document.WithSyntaxRoot(newRoot);
	}

	public static AttributeSyntax GenerateCustomAttributeWithArgument(string attributeName, string attributeValue = "default")
	{
		var defaultLiteralSyntax = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(attributeValue));
		var federationIdAttributeSyntax = SyntaxFactory.IdentifierName(attributeName);
		var attributeArgumentSyntaxes = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.AttributeArgument(defaultLiteralSyntax));
		var attributeArguments = SyntaxFactory.AttributeArgumentList(attributeArgumentSyntaxes);
		var attribute = SyntaxFactory.Attribute(federationIdAttributeSyntax).WithArgumentList(attributeArguments);
		return attribute;
	}
}
