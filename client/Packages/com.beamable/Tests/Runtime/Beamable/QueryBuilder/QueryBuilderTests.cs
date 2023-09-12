using Beamable.Api;
using Beamable.Common.Api;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Beamable.Tests.Runtime.QueryBuilderTests
{
	public class QueryBuilderTests
	{
		public class SimpleUrlEscaper : IUrlEscaper
		{
			public string EscapeURL(string url)
			{
				return UnityWebRequest.EscapeURL(url);
			}
		}
		
		
		[Test]
		public void Simple()
		{
			var args = new Dictionary<string, object> {{"x", "hello"}};

			var qb = new QueryBuilder(new SimpleUrlEscaper(), args);
			var str = qb.ToString();
			
			Assert.AreEqual(str, "?x=hello");
		}
		
		[Test]
		public void PhoneNumber()
		{
			var args = new Dictionary<string, object> {{"x", "+15081234567"}};

			var qb = new QueryBuilder(new SimpleUrlEscaper(), args);
			var str = qb.ToString();
			
			Assert.AreEqual(str, "?x=%2b15081234567");
		}
		
		[Test]
		public void Multiple()
		{
			var args = new Dictionary<string, object>
			{
				{"x", "hello"},
				{"y", "world"}
			};
			
			var qb = new QueryBuilder(new SimpleUrlEscaper(), args);
			var str = qb.ToString();
			
			Assert.AreEqual(str, "?x=hello&y=world");
		}

		[Test]
		public void Empty()
		{
			var args = new Dictionary<string, object>();
			var qb = new QueryBuilder(new SimpleUrlEscaper(), args);
			var str = qb.ToString();
			Assert.AreEqual(str, "");
		}
	}
}
