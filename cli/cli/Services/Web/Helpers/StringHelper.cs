using System.Text.RegularExpressions;

namespace cli.Services.Web.Helpers;

public static class StringHelper
{
	/// <summary>
	/// Sanitizes a raw string to a valid TypeScript identifier.
	/// Replaces invalid characters with underscores and prefixes leading digits.
	/// <param name="input">The raw string to sanitize.</param>
	/// <returns>A sanitized string that can be used as a TypeScript identifier.</returns>
	/// </summary>
	public static string ToSafeIdentifier(string input)
	{
		var sanitized = Regex.Replace(input, "[^A-Za-z0-9_]", "_");
		if (!string.IsNullOrEmpty(sanitized) && char.IsDigit(sanitized[0]))
			sanitized = "_" + sanitized;
		return sanitized;
	}

	/// <summary>
	/// Converts a string to a PascalCase identifier.
	/// </summary>
	/// <param name="input">The input string to convert.</param>
	/// <returns>A PascalCase identifier formed by capitalizing each segment of the input string.</returns>
	public static string ToPascalCaseIdentifier(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
			return string.Empty;

		var parts = Regex
			.Split(input, "[^A-Za-z0-9_]+")
			.Where(p => p.Length > 0)
			.Select(p =>
			{
				// If the part contains underscores, preserve the internal casing
				if (p.Contains('_'))
				{
					// Split by underscore and capitalize each part
					return string.Join("_", p.Split('_')
						.Select(sub => sub.Length > 0 ? Capitalize(sub) : ""));
				}

				return Capitalize(p);
			});

		return string.Concat(parts);
	}

	/// <summary>
	/// Concatenates a collection of strings into a single string with each segment capitalized.
	/// </summary>
	/// <param name="segments">The collection of string segments to concatenate.</param>
	/// <returns>
	/// A single string formed by concatenating the capitalized segments.
	/// </returns>
	public static string ConcatCapitalize(IEnumerable<string> segments)
		=> string.Concat(segments.Select(Capitalize));

	/// <summary>
	/// Capitalizes the first letter of a given word.
	/// <param name="word">
	/// The word to capitalize.
	/// </param>
	/// <returns>
	/// A new string with the first letter capitalized.
	/// </returns>
	/// </summary>
	public static string Capitalize(string word)
		=> string.IsNullOrEmpty(word) ? word : char.ToUpperInvariant(word[0]) + word[1..];

	/// <summary>
	/// Converts a string to a camelCase identifier.
	/// </summary>
	/// <param name="input">The input string to convert.</param>
	/// <returns>A camelCase identifier formed by converting the input to PascalCase and then lowercasing the first character.</returns>
	public static string ToCamelCaseIdentifier(string input)
	{
		if (string.IsNullOrEmpty(input))
			return string.Empty;

		var pascalCase = ToPascalCaseIdentifier(input);
		return char.ToLowerInvariant(pascalCase[0]) + pascalCase[1..];
	}
}
