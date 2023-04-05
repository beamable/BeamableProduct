using System;
using System.Linq;

namespace microservice.Extensions
{
    public static class StringExtensions
    {
        public static string FirstCharToUpperRestToLower(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => input.ToLower().First().ToString().ToUpper() + input.Substring(1).ToLower()
            };
    }
}