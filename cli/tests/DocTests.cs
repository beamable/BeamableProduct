using NUnit.Framework;
using tests.Examples;

namespace tests;

public class DocTests : CLITest
{
	[Test]
	public void WriteDocs()
	{
		var status = Run("docs");
		Assert.AreEqual(0, status);
	}

}
