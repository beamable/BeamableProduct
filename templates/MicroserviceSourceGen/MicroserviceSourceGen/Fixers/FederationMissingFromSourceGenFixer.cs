using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beamable.Microservice.SourceGen.Fixers;

/// <summary>
/// This class is responsible for removing federation that exists on code but not on federations.json file.
/// The fix will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FederationMissingFromSourceGenFixer)), Shared]
public class FederationMissingFromSourceGenFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds =>
		ImmutableArray.Create(Diagnostics.Fed.DECLARED_FEDERATION_MISSING_FROM_SOURCE_GEN_CONFIG_DIAGNOSTIC_ID);

	public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics.First();
		context.RegisterCodeFix(
			Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
				$"[{diagnostic.Descriptor.Title}] Remove invalid Federation interface reference",
				ct => RemoveFederationFromMicroservices(context.Document, diagnostic, ct),
				$"{diagnostic.Descriptor.Id}"),
			diagnostic);
	}

	private async Task<Document> RemoveFederationFromMicroservices(Document document, Diagnostic diagnostic,
		CancellationToken cancellationToken)
	{
		var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
		var editor = await DocumentEditor.CreateAsync(document, cancellationToken);

		var diagnosticProperties = diagnostic.Properties;
		if (!diagnosticProperties.TryGetValue(Diagnostics.Fed.PROP_FEDERATION_INTERFACE, out var fedInterface) ||
		    !diagnosticProperties.TryGetValue(Diagnostics.Fed.PROP_FEDERATION_CLASS_NAME, out var className))
		{
			return document;
		}

		var interfaceImplValue = $"{fedInterface}<{className}>";

		var microserviceClasses = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
			.Where(cls => cls.BaseList != null)
			.ToList();

		var changed = false;

		foreach (var cls in microserviceClasses)
		{
			var matchingTypes = cls.BaseList.Types
				.Where(typeSyntax =>
					interfaceImplValue == NormalizeInterfaceImplementation(typeSyntax.Type.ToFullString()))
				.ToList();

			if (!matchingTypes.Any())
			{
				continue;
			}

			var updatedClass = cls.RemoveNodes(matchingTypes, SyntaxRemoveOptions.AddElasticMarker | SyntaxRemoveOptions.KeepNoTrivia | SyntaxRemoveOptions.KeepEndOfLine);
			editor.ReplaceNode(cls, updatedClass);
			changed = true;
		}

		return changed ? editor.GetChangedDocument() : document;

		string NormalizeInterfaceImplementation(string interfaceImplementation)
		{
			return new string(interfaceImplementation.Where(c => !char.IsWhiteSpace(c)).ToArray());
		}
	}
}
	
