
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
	public async Task Add()
	{
		var status = await Cli.RunAsyncWithParams( "--dryrun", "--cid", "asdf", "add", "1", "2");
		Assert.AreEqual(0, status);
	}

	[Test]
	public async Task AddWithMock()
	{
		var status = await Cli.RunAsyncWithParams(services =>
		{
			var mockFake = new Mock<IFakeService>();
			mockFake
				.Setup(x => x.AddAsync(It.IsAny<int>(), It.IsAny<int>()))
				.Returns(Task.FromResult(12));

			services.AddSingleton(mockFake.Object);
		},"--dryrun", "--cid", "asdf", "add", "1", "2");
		Assert.AreEqual(0, status);
	}

	[Test]
	public void PrintHelp()
	{
		var status = Cli.RunWithParams("add", "--help");
		Assert.AreEqual(0, status);
	}

	[Test]
	public void PrintVersion()
	{
		var status = Cli.RunWithParams("--version");
		Assert.AreEqual(0, status);
	}
}
