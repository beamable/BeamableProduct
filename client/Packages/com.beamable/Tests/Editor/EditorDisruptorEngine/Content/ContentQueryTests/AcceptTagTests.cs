using Beamable.Common.Content;
using Beamable.Editor.Tests.Beamable.Content;
using NUnit.Framework;
using System.Collections.Generic;

namespace BeamableEditor.Tests.Content
{
	public class AcceptTagTests
	{
		[Test]
		public void AcceptsOneTag()
		{
			var query = new ContentQuery
			{
				TagConstraints = new HashSet<string> { "a" }
			};

			var content = new ExampleContent { Tags = new[] { "a", "b" } };

			Assert.IsTrue(query.AcceptTag(content));
		}

		[Test]
		public void AcceptsManyTags()
		{
			var query = new ContentQuery
			{
				TagConstraints = new HashSet<string> { "a", "b", "c" }
			};

			var content = new ExampleContent { Tags = new[] { "a", "b", "c" } };

			Assert.IsTrue(query.AcceptTag(content));
		}

		[Test]
		public void Rejects()
		{
			var query = new ContentQuery
			{
				TagConstraints = new HashSet<string> { "a", "b", "c" }
			};

			var content = new ExampleContent { Tags = new[] { "a", "c" } };

			Assert.IsFalse(query.AcceptTag(content));
		}
	}
}
