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

	    /// <summary>
	    /// Ensure that a path has quotes around it.
	    /// If the path already has the <paramref name="quoteChar"/> at the start AND end, then the string is unchanged. 
	    /// </summary>
	    /// <param name="path">A path string</param>
	    /// <param name="openChar">Optional, the quote character. By default, it is a double quote. </param>
	    /// <param name="closeChar">Optional, the quote character. By default, it is a double quote. </param>
	    /// <returns></returns>
	    public static string EnquotePath(this string path, char openChar='"', char closeChar='"')
	    {
		    if (string.IsNullOrEmpty(path)) return path;

		    var isFirstCharMatch = path[0] == openChar;
		    var isLastCharMatch = path[path.Length - 1] == closeChar;

		    if (isFirstCharMatch && isLastCharMatch) return path;

		    return openChar + path + closeChar;
	    }
	    
    }
}
