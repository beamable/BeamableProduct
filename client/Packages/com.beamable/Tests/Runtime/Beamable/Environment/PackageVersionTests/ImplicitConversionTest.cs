using Beamable.Common;
using NUnit.Framework;

namespace Beamable.Tests.Runtime.Environment.PackageVersionTests
{
	public class ImplicitConversionTest
	{
		[Test]
		public void StringToVersion()
		{
			var versionStr = "1.0.1";
			var version = PackageVersion.FromSemanticVersionString("1.0.2");
			Assert.AreEqual(true, versionStr < version);
			Assert.AreEqual(true, version > versionStr);
		}

		[Test]
		public void NullEquality()
		{
			var version = PackageVersion.FromSemanticVersionString("1.0.2");
			Assert.IsFalse(version == null, "the version should not think it is equal to null, and an exception should not be thrown.");
		}
	}
}
