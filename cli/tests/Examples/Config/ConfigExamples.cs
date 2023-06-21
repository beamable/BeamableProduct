using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace tests.Examples.Config;

public class ConfigExamples : ExampleTest
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
