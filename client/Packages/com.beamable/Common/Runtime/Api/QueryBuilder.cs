using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Api
{
	/// <summary>
	/// The query builder is a small utility that manages building a query arg string.
	/// </summary>
	public class QueryBuilder
	{
		private readonly IUrlEscaper _requester;
		private readonly IDictionary<string, object> _dictionary;

		/// <summary>
		/// Create a new query builder.
		/// Later, use the <see cref="ToString"/> method to convert it into a querys tring
		/// </summary>
		/// <param name="requester">Any type that can url-encode parameters</param>
		/// <param name="dictionary">an existing dictionary of parameters to encode</param>
		public QueryBuilder(IUrlEscaper requester, IDictionary<string, object> dictionary=null)
		{
			_requester = requester;
			_dictionary = dictionary ?? new Dictionary<string, object>();
		}

		/// <summary>
		/// Add a parameter to the query arg string
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Add(string key, object value) => _dictionary.Add(key, value);

		/// <summary>
		/// Convert the query arg builder into an a query arg string.
		/// The query arg will include the `?` character if required.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{

			if (_dictionary.Count == 0)
			{
				return "";
			}

			var parts = new string[_dictionary.Count];
			var index = 0;
			foreach (var kvp in _dictionary)
			{
				var part = ($"{kvp.Key}={_requester.EscapeURL(kvp.Value.ToString())}");
				parts[index++] = part;
			}

			var queryArgs = string.Join("&", parts);
			return "?" + queryArgs;
		}
	}

	public static class QueryBuilderExtensions
	{
		/// <summary>
		/// Create a <see cref="QueryBuilder"/> instance from a given set of query parameters in a dictionary.
		/// Later, use the <see cref="QueryBuilder.ToString()"/> method to turn the instance into a query arg string.
		/// </summary>
		/// <param name="escaper"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static QueryBuilder CreateQueryArgBuilder(this IUrlEscaper escaper, Dictionary<string, object> args=null)
		{
			return new QueryBuilder(escaper, args);
		}
	}
}
