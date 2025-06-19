using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharpExtensions;

namespace Beamable.Microservice.SourceGen;

public readonly record struct MicroserviceInfo : IEquatable<MicroserviceInfo>
{
	public string Namespace { get; }
	public string Name { get; }
	public Location MicroserviceClassLocation { get; }
	public string ServiceId { get; }
	public bool HasMicroserviceAttribute { get; }
	public Location MicroserviceAttributeLocation { get; }
	public bool IsPartial { get; }
	
	public MicroserviceInfo(INamedTypeSymbol type)
	{
		Namespace = type.ContainingNamespace.IsGlobalNamespace
			? null
			: type.ContainingNamespace.ToString();
		Name = type.Name;

		MicroserviceClassLocation = type.Locations.FirstOrDefault();
		
		// Check if this is a partial class
		IsPartial = type.DeclaringSyntaxReferences
			.Any(syntax => syntax.GetSyntax() is ClassDeclarationSyntax declaration && declaration.Modifiers.Any(modifier => CSharpExtensions.IsKind((SyntaxToken)modifier, SyntaxKind.PartialKeyword)));
			
		// Check for the microservice attribute so we can validate its name does not have any invalid characters.
		var serviceId = "";

		var microserviceAttr = type.GetAttributes().FirstOrDefault(a => a?.AttributeClass?.Name == nameof(MicroserviceAttribute));

		HasMicroserviceAttribute = microserviceAttr != null;
		if (microserviceAttr != null)
		{
			MicroserviceAttributeLocation = microserviceAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation();
		}

		if (microserviceAttr?.ConstructorArguments.Length > 0)
		{
			serviceId = microserviceAttr.ConstructorArguments[0].Value?.ToString();
		}

		ServiceId = serviceId;
	}

	public bool Equals(MicroserviceInfo other)
	{
		return Namespace == other.Namespace
		       && Name == other.Name
		       && HasMicroserviceAttribute == other.HasMicroserviceAttribute;
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = (Namespace != null ? Namespace.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ Name.GetHashCode();
			hashCode = (hashCode * 397) ^ HasMicroserviceAttribute.GetHashCode();

			return hashCode;
		}
	}
}
