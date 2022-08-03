
using Beamable.Common.Api;
using cli;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

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


	[Test]
	public async Task GenerateStuff()
	{
		var status = await Cli.RunAsyncWithParams(builder =>
		{
			var mock = new Mock<ISwaggerStreamDownloader>();
			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("basic"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.InventoryBasicOpenApi));

			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("object"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.InventoryObjectOpenApi));
			builder.AddSingleton<ISwaggerStreamDownloader>(mock.Object);
		}, "generate");
		Assert.AreEqual(0, status);
	}

	public static Stream GenerateStreamFromString(string s)
	{
		var stream = new MemoryStream();
		var writer = new StreamWriter(stream);
		writer.Write(s);
		writer.Flush();
		stream.Position = 0;
		return stream;
	}
}
