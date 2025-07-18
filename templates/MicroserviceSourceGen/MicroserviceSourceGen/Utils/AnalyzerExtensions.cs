using Microsoft.CodeAnalysis;
using System.Collections.Generic;

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
		
	}
}
