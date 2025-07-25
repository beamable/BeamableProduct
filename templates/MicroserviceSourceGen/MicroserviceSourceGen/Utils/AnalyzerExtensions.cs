using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;



namespace Beamable.Microservice.SourceGen.Utils
{

	public static class AnalyzerExtensions
	{
		public static List<string> GetAllBaseTypes(this ITypeSymbol symbol, bool includeType = true)
		{
			// CSharpErrorMessageFormat generate the Class name with the <Namespace>.<ClassName>
			List<string> allTypes = new List<string>();
			if (includeType)
			{
				allTypes.Add(symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
			}
			var lastSymbol = symbol;
			while (lastSymbol.BaseType != null)
			{
				lastSymbol = lastSymbol.BaseType;
				allTypes.Add(lastSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
			}
			return allTypes;
		}

		public static string GetDataString(this ImmutableArray<AttributeData> attributes)
		{
			return string.Join(" | ", attributes.Select(a =>
					$"Name: {a.AttributeClass?.Name ?? string.Empty}, " +
					$"MetadataName: {a.AttributeClass?.MetadataName ?? string.Empty}, " +
					$"ClassToDisplayString: {a.AttributeClass?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty}, " +
					$"NamespaceName: {a.AttributeClass?.ContainingNamespace.Name ?? string.Empty}, " +
					$"NamespaceToDisplay: {a.AttributeClass?.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty}, "+
					$"AssemblyToDisplay: {a.AttributeClass?.ContainingAssembly?.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat) ?? string.Empty}")
				.ToList());
		}
		
	}
}
