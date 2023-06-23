using NUnit.Framework;
using System.IO;

namespace tests.Examples;

public class FileIO : CLITest
{
	[Test]
	public void Simple()
	{
		var content = "hello world";
		File.WriteAllText("test.txt", content);

		var latest = File.ReadAllText("test.txt");
		Assert.AreEqual(content, latest);
	}
	[Test]
	public void Simple2()
	{
		var content = "hello world2";
		File.WriteAllText("test.txt", content);

		var latest = File.ReadAllText("test.txt");
		Assert.AreEqual(content, latest);
	}
}
