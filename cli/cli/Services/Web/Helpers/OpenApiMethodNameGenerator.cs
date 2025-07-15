using System.Text.RegularExpressions;
using static cli.Services.Web.Helpers.StringHelper;

namespace cli.Services.Web.Helpers;

/// <summary>
/// Generates method names for Web SDK based on API endpoints.
/// </summary>
public static class OpenApiMethodNameGenerator
{
	// ─── Constants ───

	private static readonly string[] SkippablePrefixes = { "api", "basic", "object", "internal" };

	private static readonly char[] Separator = { '/' };

	// ─── Public API ───

	/// <summary>
	/// Generate a camel-cased Web SDK method name for the given endpoint.
	/// </summary>
	public static string GenerateMethodName(string apiEndpoint, string httpMethod)
	{
		var isInternalEndpoint = apiEndpoint.StartsWith("/api/internal", StringComparison.OrdinalIgnoreCase);
		var isBasicEndpoint = apiEndpoint.StartsWith("/basic", StringComparison.OrdinalIgnoreCase);

		// 1) Split raw segments
		var rawSegments = apiEndpoint
			.Split(Separator, StringSplitOptions.RemoveEmptyEntries) // Split by '/'
			.Where(s => !SkippablePrefixes.Contains(s, StringComparer.OrdinalIgnoreCase)) // Skip skippable prefixes
			.Select(RemoveNonAlphanumericExceptBraces)
			.ToList();

		if (isBasicEndpoint)
			rawSegments = rawSegments.Append("basic").ToList();

		// 2) Detect param vs static in original segments
		var rawParamSegments = rawSegments.Where(IsParameter).ToList();
		var rawStaticSegments = rawSegments.Where(s => !IsParameter(s)).ToList();

		// 3) Sanitize them (remove hyphens/underscores, camel-case each piece)
		var staticSegments = rawStaticSegments
			.Select(SanitizeSegment)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
		var paramNames = rawParamSegments
			.Select(p => ParamName(SanitizeSegment(p)))
			.ToList();

		var verbUpper = httpMethod.Trim().ToUpperInvariant();

		// 4) Prepare singular/plural forms of the resource
		if (staticSegments.Count == 0)
			return verbUpper == "GET" ? "list" : HttpVerbPrefix(verbUpper);

		var resources = staticSegments.First();

		string methodName;

		// 5) GET logic: list / get by id / list nested
		if (verbUpper == "GET")
		{
			var excludeWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "get" };
			var filteredStaticSegments = staticSegments.Where(word => !excludeWords.Contains(word)).ToList();

			methodName = paramNames.Count switch
			{
				1 when filteredStaticSegments.Count == 1 =>
					// GET /resources/{id}
					resources + HttpVerbPrefix(verbUpper) + "By" + paramNames[0],
				1 when filteredStaticSegments.Count > 1 =>
					// GET /resources/subresources/{id} or /resources/{id}/subresources
					resources + HttpVerbPrefix(verbUpper) + ConcatCapitalize(filteredStaticSegments[1..]) + "By" +
					paramNames[0],
				// _ when apiEndpoint.Equals("/basic/auth/token", StringComparison.OrdinalIgnoreCase) =>
				// 	// special-case for auth token endpoint
				// 	"get" + ConcatCapitalize(filteredStaticSegments),
				_ =>
					// fallback: list everything e.g GET /resources | /resources/subresources
					resources + HttpVerbPrefix(verbUpper) + (filteredStaticSegments.Count > 1
						? ConcatCapitalize(filteredStaticSegments[1..])
						: string.Empty)
			};
		}
		else
		{
			// 6) Non-GET: CRUD + ById for single-resource param
			methodName = paramNames.Count switch
			{
				1 when staticSegments.Count == 1 => resources + HttpVerbPrefix(verbUpper) + "By" + paramNames[0],
				1 when staticSegments.Count > 1 => resources + HttpVerbPrefix(verbUpper) +
				                                   ConcatCapitalize(staticSegments[1..]) + "By" + paramNames[0],
				_ => resources + HttpVerbPrefix(verbUpper) + (staticSegments.Count > 1
					? ConcatCapitalize(staticSegments[1..])
					: string.Empty)
			};
		}

		// 7) Final camel-case
		methodName = ToCamel(methodName);
		return isInternalEndpoint ? $"{methodName}Internal" : methodName;
	}

	// ─── Helper Methods ───

	private static bool IsParameter(string word)
		=> word.StartsWith('{') && word.EndsWith('}');

	private static string SanitizeSegment(string segment)
	{
		segment = segment.Trim('{', '}');
		var parts = Regex.Split(segment, "[^A-Za-z0-9]+")
			.Where(p => p.Length > 0)
			.ToArray();
		if (parts.Length == 0) return "";
		var first = parts[0];
		var rest = string.Concat(parts.Skip(1).Select(Capitalize));
		return first + rest;
	}

	private static string ParamName(string segment)
	{
		// already sanitized; just uppercase first letter
		return Capitalize(segment);
	}

	private static string RemoveNonAlphanumericExceptBraces(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;

		// Upper-case any letter preceded by one or more non-alphanumeric (excluding braces)
		var result = Regex.Replace(input, "[^A-Za-z0-9{}]+([A-Za-z0-9])",
			m => m.Groups[1].Value.ToUpperInvariant());
		// Remove all remaining (leading and trailing) non-alphanumeric except braces
		result = Regex.Replace(result, "[^A-Za-z0-9{}]", string.Empty);
		return result;
	}

	private static string ToCamel(string word)
		=> string.IsNullOrEmpty(word) ? word : char.ToLowerInvariant(word[0]) + word[1..];

	private static string HttpVerbPrefix(string verb)
		=> verb switch
		{
			"POST" => "Post",
			"PUT" => "Put",
			"DELETE" => "Delete",
			_ => "Get"
		};
}
