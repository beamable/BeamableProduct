using NUnit.Framework;

namespace tests;

public class DocTests
{
	[Test]
	public void WriteDocs()
	{
		var status = Cli.RunWithParams("docs");
		Assert.AreEqual(0, status);
	}

}
