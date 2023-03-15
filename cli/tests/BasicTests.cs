
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
	public void NamingPass()
	{
		void CheckNaming(string commandName, string description, string optionName = null)
		{
			const string KEBAB_CASE_PATTERN = "^([a-z]|[0-9])+(?:[-]([a-z]|[0-9])+)*$";
			var isOption = !string.IsNullOrWhiteSpace(optionName);
			var logPrefix = isOption ?
				$"{optionName} argument for command {commandName}" :
				$"{commandName} command";
			if (string.IsNullOrWhiteSpace(description))
			{
				Assert.Fail($"{logPrefix} description should be provided.");
			}
			if (!char.IsUpper(description[0]))
			{
				Assert.Fail($"{logPrefix} description should start with upper letter.");
			}
			if (description.TrimEnd()[^1] == '.')
			{
				Assert.Fail($"{logPrefix} description should not end with dot.");
			}

			var valueToCheck = isOption ? optionName : commandName;
			var match = Regex.Match(valueToCheck, KEBAB_CASE_PATTERN);
			Assert.AreEqual(match.Success, true, $"{valueToCheck} does not match kebab case naming.");
		}
		var commandTypes = Assembly.GetAssembly(typeof(App))!.GetTypes()
			.Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Command)));
		var commandsList = new List<Command>();
		var app = new App();
		app.Configure();
		app.Build();

		foreach (Type type in commandTypes)
		{
			var command = app.CommandProvider.GetService(type);
			if (command != null)
			{
				commandsList.Add((Command)command);
			}
		}

		foreach (var command in commandsList)
		{
			CheckNaming(command.Name, command.Description);

			var sameDescriptionCommand = commandsList.FirstOrDefault(c =>
				c.Name != command.Name &&
				c.Description != null &&
				c.Description.Equals(command.Description, StringComparison.InvariantCultureIgnoreCase));

			if (sameDescriptionCommand != null)
			{
				Assert.Fail($"{command.Name} and {sameDescriptionCommand.Name} have the same description.", command, sameDescriptionCommand);
			}

			foreach (Option option in command.Options)
			{
				CheckNaming(command.Name, option.Description, option.Name);
			}
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
