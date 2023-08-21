using System;
using System.Linq;

namespace microservice.Extensions
{
	/// <summary>
	/// Provides extension methods for manipulating strings.
	/// </summary>
    public static class StringExtensions
    {
	    /// <summary>
	    /// Converts the first character of the input string to uppercase and the remaining characters to lowercase.
	    /// </summary>
	    /// <param name="input">The input string to be processed.</param>
	    /// <returns>A new string with the first character in uppercase and the rest in lowercase.</returns>
        public static string FirstCharToUpperRestToLower(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input.ToLower().First().ToString().ToUpper() + input.Substring(1).ToLower()
            };
    }
}
