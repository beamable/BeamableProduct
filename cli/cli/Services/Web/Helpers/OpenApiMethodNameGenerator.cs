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

	private static readonly char[] Separator = { '/', '-', '_' };

	// ─── Public API ───

	/// <summary>
	/// Generate a camel-cased Web SDK method name for the given endpoint.
	/// </summary>
	public static string GenerateMethodName(string apiEndpoint, string httpMethod)
	{
		var isInternalApi = apiEndpoint.StartsWith("/api/internal", StringComparison.OrdinalIgnoreCase);

		// 1) Split raw segments
		var rawSegments = apiEndpoint
			.Split(Separator, StringSplitOptions.RemoveEmptyEntries)
			.Where(s => !SkippablePrefixes.Contains(s, StringComparer.OrdinalIgnoreCase))
			.ToList();

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

		// 4) Special-case payments endpoints
		var payIdx = staticSegments.FindIndex(s => s.Equals("payments", StringComparison.OrdinalIgnoreCase));
		if (payIdx >= 0)
		{
			var after = staticSegments.Skip(payIdx + 1).ToList();
			var name = BuildPaymentsName(after, verbUpper);
			return name;
		}

		// 5) Prepare singular/plural forms of the resource
		if (staticSegments.Count == 0)
			return verbUpper == "GET" ? "list" : HttpVerbPrefix(verbUpper);

		var firstStatic = staticSegments.First();
		var resourceSingularize = Singularize(firstStatic);

		string methodName;

		// 6) GET logic: list / get by id / list nested
		if (verbUpper == "GET")
		{
			var excludeWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "get" };
			var filteredStaticSegments = staticSegments.Where(word => !excludeWords.Contains(word)).ToList();

			methodName = paramNames.Count switch
			{
				1 when filteredStaticSegments.Count == 1 =>
					// GET /resources/{id}
					"get" + Capitalize(resourceSingularize) + "By" + paramNames[0],
				1 when filteredStaticSegments.Count > 1 =>
					// GE /resources/subresources/{id} or /resources/{id}/subresources
					"get" + Capitalize(resourceSingularize) + ConcatCapitalize(filteredStaticSegments[1..]) +
					"By" + paramNames[0],
				_ when apiEndpoint.Equals("/basic/auth/token", StringComparison.OrdinalIgnoreCase) =>
					// special-case for auth token endpoint
					"get" + ConcatCapitalize(filteredStaticSegments),
				_ =>
					// fallback: list everything e.g GET /resources | /resources/subresources
					"get" + ConcatCapitalize(filteredStaticSegments)
			};
		}
		else
		{
			// 7) Non-GET: CRUD + ById for single-resource param
			methodName = paramNames.Count switch
			{
				1 when staticSegments.Count == 1 => HttpVerbPrefix(verbUpper) + Capitalize(resourceSingularize) +
				                                    "By" + paramNames[0],
				1 when staticSegments.Count > 1 => HttpVerbPrefix(verbUpper) + Capitalize(resourceSingularize) +
				                                   ConcatCapitalize(staticSegments[1..]) + "By" + paramNames[0],
				_ => HttpVerbPrefix(verbUpper) + Capitalize(resourceSingularize) + (staticSegments.Count > 1
					? ConcatCapitalize(staticSegments[1..])
					: string.Empty)
			};
		}

		// 8) Special-case tokens endpoints
		if (staticSegments.Contains("tokens", StringComparer.OrdinalIgnoreCase) && staticSegments.Count >= 1)
		{
			if (apiEndpoint == "/api/auth/tokens/refresh-token")
			{
				methodName = $"{HttpVerbPrefix(verbUpper)}AuthRefreshTokenV2";
			}
			else
			{
				var kind = Capitalize(staticSegments[^1].Replace("token", "", StringComparison.OrdinalIgnoreCase));
				methodName = $"{HttpVerbPrefix(verbUpper)}{kind}Token";
			}
		}

		// 9) Final camel-case
		methodName = ToCamel(methodName);
		return isInternalApi ? $"{methodName}Internal" : methodName;
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

	private static string BuildPaymentsName(List<string> segments, string verbUpper)
	{
		var provider = segments.ElementAtOrDefault(0);
		var subject = segments.ElementAtOrDefault(1);
		var action = segments.ElementAtOrDefault(2) ?? segments.ElementAtOrDefault(0);

		string format = action switch
		{
			"track" => $"track{Capitalize(provider)}{Capitalize(subject)}",
			"complete" => $"complete{Capitalize(provider)}{Capitalize(subject)}",
			"begin" => $"begin{Capitalize(provider)}{Capitalize(subject)}",
			"verify" => $"verify{Capitalize(provider)}{Capitalize(subject)}",
			"cancel" => $"cancel{Capitalize(provider)}{Capitalize(subject)}",
			"fail" => $"fail{Capitalize(provider)}{Capitalize(subject)}",
			_ when verbUpper == "GET" && segments.Count == 1
				=> $"getPayments{Capitalize(action)}",
			_ => $"{HttpVerbPrefix(verbUpper)}Payment{Capitalize(provider)}{Capitalize(subject)}"
		};

		return ToCamel(format);
	}

	private static string Singularize(string word)
	{
		if (Regex.IsMatch(word, "ies$", RegexOptions.IgnoreCase))
			return Regex.Replace(word, "ies$", "y", RegexOptions.IgnoreCase);
		if (word.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
		    !word.EndsWith("ss", StringComparison.OrdinalIgnoreCase))
			return word[..^1];
		return word;
	}

	private static string ToCamel(string word)
		=> string.IsNullOrEmpty(word) ? word : char.ToLower(word[0]) + word[1..];

	private static string HttpVerbPrefix(string verb)
		=> verb switch
		{
			"POST" => "post",
			"PUT" => "put",
			"DELETE" => "delete",
			_ => "get"
		};
}
