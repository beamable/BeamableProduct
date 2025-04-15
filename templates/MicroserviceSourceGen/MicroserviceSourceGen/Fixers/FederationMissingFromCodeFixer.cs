using Beamable.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Server;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FederationMissingFromCodeFixer)), Shared]
public class FederationMissingFromCodeFixer : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		Diagnostics.Fed.CONFIGURED_FEDERATION_MISSING_FROM_CODE_DIAGNOSTIC_ID);

	public override FixAllProvider GetFixAllProvider() => null;
	
	public override Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First();
		context.RegisterCodeFix(
			Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
				title: $"[{diagnostic.Descriptor.Title}] Create Federation and Implement {diagnostic.Properties[Diagnostics.Fed.PROP_FEDERATION_INTERFACE]}",
				createChangedDocument: cancellationToken => FixMissingConfiguredFederationId(context.Document, diagnostic, cancellationToken),
				equivalenceKey: "FixMissingFederation"),
			diagnostic);
		return Task.CompletedTask;
	}
	
	private async Task<Document> FixMissingConfiguredFederationId(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

		var fedId = diagnostic.Properties[Diagnostics.Fed.PROP_FEDERATION_ID];
		var fedInterface = diagnostic.Properties[Diagnostics.Fed.PROP_FEDERATION_INTERFACE];
		var microserviceName = diagnostic.Properties[Diagnostics.Fed.PROP_MICROSERVICE_NAME];

		// Use the Microservice name to find the microservice class on document.
		var microserviceClass = root.DescendantNodes()
			.OfType<ClassDeclarationSyntax>()
			.FirstOrDefault(cls => cls.Identifier.Text == microserviceName);

		if (microserviceClass == null)
			return document;

		// Adding interface to the Microservice class
		var titleCaseFedID = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(fedId.ToLower());
		string newFederationClassName = $"{titleCaseFedID}Federation";
		
		var newInterfaceType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"{fedInterface}<{newFederationClassName}>").WithTrailingTrivia(SyntaxFactory.Space));
		BaseListSyntax baseList = microserviceClass.BaseList == null
			? SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>().Add(newInterfaceType))
			: microserviceClass.BaseList.AddTypes(newInterfaceType);
		
		var updatedClass = microserviceClass.WithBaseList(baseList);

		editor.ReplaceNode(microserviceClass, updatedClass.WithAdditionalAnnotations(Formatter.Annotation));

		// Creating the new FederationID Class for the new Federation from Config file values.
		var federationIdAttribute = FederationDefaultIDFixer.GenerateFederationIdAttribute(fedId);
		AttributeListSyntax newAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(federationIdAttribute));
		
		var fedIdClass =
			SyntaxFactory.ClassDeclaration(newFederationClassName)
				.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
				.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(nameof(IFederationId))))
				.AddAttributeLists(newAttributeList)
				.NormalizeWhitespace();

		var newRoot = editor.GetChangedRoot();
		var updatedMicroserviceClass = newRoot.DescendantNodes().OfType<ClassDeclarationSyntax>()
			.FirstOrDefault(item => item.Identifier.Text == microserviceName);
		
		var finalRoot = newRoot.InsertNodesBefore(updatedMicroserviceClass, new[] { fedIdClass });
		return document.WithSyntaxRoot(finalRoot);
	}
	
}
