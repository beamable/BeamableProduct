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
/// This class is responsible for Fixing Federations IDS that are invalid (starts with number or if it contains special characters)
/// The fix message will appear on the IDE, allowing the user to Fix it automatically.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FederationIdNameFixer)), Shared]
public class FederationIdNameFixer : CodeFixProvider
{
	public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(
		Diagnostics.Fed.DECLARED_FEDERATION_INVALID_FEDERATION_ID_DIAGNOSTIC_ID
	);

	public sealed override FixAllProvider GetFixAllProvider() => null;

	public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		var diagnostic = context.Diagnostics[0];
		var diagnosticSpan = diagnostic.Location.SourceSpan;
		await RegisterFederationIdNameFixer(context, diagnosticSpan, diagnostic);
	}
	
	private async Task RegisterFederationIdNameFixer(CodeFixContext context, TextSpan diagnosticSpan, Diagnostic diagnostic)
{
	// As the diagnostic location is the Federation Class and not the Attribute, we first need to find the class and 
	// get the attributes that matches the FederationAttribute name
	var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
	
	// Using the diagnosticSpan (code range from the Diagnostic Location) we can find the ClassDeclaration by searching
	// From self or Ancestors nodes. 
	var classNode = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
	
	if (classNode == null)
		return;
	
	var federationAttribute = classNode.AttributeLists
		.SelectMany(list => list.Attributes)
		.FirstOrDefault(attr => attr.Name.ToString().Contains(ServicesAnalyzer.FEDERATION_ATTRIBUTE_NAME));

	if (federationAttribute?.ArgumentList?.Arguments.Count > 0 &&
	    federationAttribute.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal)
	{
		context.RegisterCodeFix(
			Microsoft.CodeAnalysis.CodeActions.CodeAction.Create(
				title: $"[{diagnostic.Descriptor.Title}] Fix Federation ID name",
				createChangedDocument: c => FixFederationIdName(context.Document, literal, c),
				equivalenceKey: "FixFederationIdName"),
			diagnostic);
	}
}
	
	private async Task<Document> FixFederationIdName(Document document, LiteralExpressionSyntax literal, CancellationToken cancellationToken)
	{
		var currentValue = literal.Token.ValueText;

		string newValue = FixFederationIdName(currentValue);

		var newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(newValue));

		var root = await document.GetSyntaxRootAsync(cancellationToken);
		var newRoot = root.ReplaceNode(literal, newLiteral);

		return document.WithSyntaxRoot(newRoot);
	}

	public static string FixFederationIdName(string currentValue)
	{
		var sanitizedName = new string(currentValue.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
		
		var newValue = new string(sanitizedName.SkipWhile(c => !char.IsLetter(c)).ToArray());
		if (string.IsNullOrWhiteSpace(newValue)) newValue = "default";
		return newValue;
	}
}
