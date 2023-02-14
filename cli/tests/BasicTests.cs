
using Beamable.Common.Api;
using cli;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
	public void CheckNaming()
	{
		const string KEBAB_CASE_PATTERN = "^[a-z]+(?:[-][a-z]+)*$";
		var baseType = typeof(Command);
		var allTypes = Assembly.GetAssembly(typeof(App))!.GetTypes();
		var commandsList = new List<Command>();
		var app = new App();
		app.Configure();
		app.Build();
		
		foreach (Type type in allTypes.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(baseType)))
		{
			var command = app.CommandProvider.GetService(type);
			if(command != null)
			{
				commandsList.Add((Command)command);
			}
		}

		foreach (var command in commandsList)
		{
			var sameDescriptionCommand = commandsList.FirstOrDefault(c =>
				c.Name != command.Name &&
				c.Description != null &&
				c.Description.Equals(command.Description, StringComparison.InvariantCultureIgnoreCase));

			if (sameDescriptionCommand != null)
			{
				Assert.Fail($"{command.Name} and {sameDescriptionCommand.Name} have the same description.");
			}
			
			var match = Regex.Match(command.Name, KEBAB_CASE_PATTERN);
			Assert.AreEqual(match.Success, true, $"{command.Name} does not match kebab case naming.");
		}
	}

	// // use this test to help identify live issues
	// [Test]
	// public async Task TestBrokenOrder()
	// {
	// 	var status = await Cli.RunAsyncWithParams("--host", "https://dev.api.beamable.com", "oapi", "generate", "--conflict-strategy", "RenameUncommonConflicts", "--engine", "unity");
	// }

	[Test]
	public async Task GenerateStuff() // TODO: better name please
	{
		var status = await Cli.RunAsyncWithParams(builder =>
		{
			var mock = new Mock<ISwaggerStreamDownloader>();
			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("basic") && x.Contains("inventory"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.InventoryBasicOpenApi));

			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("object") && x.Contains("inventory"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.InventoryObjectOpenApi));

			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("basic") && x.Contains("accounts"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.AccountBasicOpenApi));

			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("object") && x.Contains("accounts"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.AccountObjectOpenApi));

			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("object") && x.Contains("event-players"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.EventPlayersObjectApi));

			mock.Setup(x => x.GetStreamAsync(It.Is<string>(x => x.Contains("basic") && x.Contains("social"))))
				.ReturnsAsync(GenerateStreamFromString(OpenApiFixtures.SocialBasicOpenApi));


			builder.AddSingleton<ISwaggerStreamDownloader>(mock.Object);

		}, "oapi", "generate", "--filter", "social,t:basic");
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
