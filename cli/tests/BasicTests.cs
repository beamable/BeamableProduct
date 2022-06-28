
using System.Threading.Tasks;
using cli;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace tests;

public class Tests
{
	[SetUp]
	public void Setup()
	{
	}

	[Test]
	public void PrintVersion()
	{
		var status = Cli.RunWithParams("--version");
		Assert.AreEqual(0, status);
	}
}
