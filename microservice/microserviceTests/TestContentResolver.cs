using Beamable.Common;
using Beamable.Server;
using System;
using System.Threading.Tasks;

namespace microserviceTests
{
	public class TestContentResolver : IContentResolver
	{
		private readonly ContentUriResolver _idToContent;

		public delegate Task<string> ContentUriResolver(string uri);

		public TestContentResolver()
		{
			_idToContent = uri => throw new NotImplementedException("Cannot fetch content from test code.");
		}
		public TestContentResolver(ContentUriResolver idToContent)
		{
			_idToContent = idToContent;
		}

		public Task<string> RequestContent(string uri)
		{
			return _idToContent(uri);
		}
	}
}
