using Beamable.Common.Api;

namespace Tests.Runtime.Beamable
{
	public class MockFileAccessor : IBeamableFilesystemAccessor
	{
		public string TestPath { get; }

		// PlatformFilesystemAccessor
		public MockFileAccessor(string testPath)
		{
			TestPath = testPath;
		}
		public string GetPersistentDataPathWithoutTrailingSlash()
		{
			return TestPath;
		}
	}
}
