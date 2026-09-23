using cli;
using cli.Commands.Project;
using cli.Services;
using cli.Services.Web.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace tests.Web;

[TestFixture]
public class GenerateWebClientCommandTests
{
	private string _outputDirectory;

	[SetUp]
	public void SetUp()
	{
		WebClientTestDocuments.InitializeLogging();
		_outputDirectory = Path.Combine(Path.GetTempPath(), "beam-web-client-command-tests", Guid.NewGuid().ToString("N"));
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, true);
		}
	}

	[TestCase(null)]
	[TestCase("")]
	public void ValidateArgs_MissingOutputDirectory_Throws(string outputDirectory)
	{
		var args = new GenerateWebClientCommandArgs { outputDirectory = outputDirectory, lang = "ts" };

		var ex = Assert.Throws<CliException>(() => GenerateWebClientCommand.ValidateArgs(args));
		Assert.That(ex.Message, Does.Contain("--output-dir"));
	}

	[Test]
	public void ValidateArgs_UnsupportedLanguage_Throws()
	{
		var args = new GenerateWebClientCommandArgs { outputDirectory = _outputDirectory, lang = "python" };

		var ex = Assert.Throws<CliException>(() => GenerateWebClientCommand.ValidateArgs(args));
		Assert.That(ex.Message, Does.Contain("python"));
		Assert.That(ex.Message, Does.Contain("typescript"));
	}

	[Test]
	public void ValidateArgs_UnsupportedInt64As_Throws()
	{
		var args = new GenerateWebClientCommandArgs { outputDirectory = _outputDirectory, lang = "ts", int64As = "long" };

		var ex = Assert.Throws<CliException>(() => GenerateWebClientCommand.ValidateArgs(args));
		Assert.That(ex.Message, Does.Contain("--int64-as"));
		Assert.That(ex.Message, Does.Contain("bigint-union"));
	}

	[TestCase(null, TsInt64Mapping.BigIntUnion)]
	[TestCase("bigint-union", TsInt64Mapping.BigIntUnion)]
	[TestCase("number", TsInt64Mapping.Number)]
	[TestCase("string", TsInt64Mapping.String)]
	public void ValidateArgs_ValidArgs_ReturnsInt64Mapping(string int64As, TsInt64Mapping expected)
	{
		var args = new GenerateWebClientCommandArgs { outputDirectory = _outputDirectory, lang = "JS", int64As = int64As };

		Assert.AreEqual(expected, GenerateWebClientCommand.ValidateArgs(args));
	}

	[Test]
	public void Generate_SkipsServicesWithoutOpenApiDocument()
	{
		var services = new Dictionary<string, HttpMicroserviceLocalProtocol>
		{
			["UnbuiltService"] = new() { ExpectedOpenApiDocPath = "/nowhere/beam_openApi.json" },
			["MatchService"] = new() { OpenApiDoc = WebClientTestDocuments.MatchServiceDocument() },
		};

		var result = GenerateWebClientCommand.Generate(services, _outputDirectory, "ts", TsInt64Mapping.Number);

		Assert.That(result.Clients, Has.Count.EqualTo(1));
		Assert.That(Path.GetFileName(result.Clients[0]), Is.EqualTo("MatchServiceClient.ts"));
		Assert.That(result.Types, Has.Count.EqualTo(1));
		Assert.That(File.ReadAllText(result.Types[0]), Does.Contain("createdAt: number;"));
		Assert.That(Directory.GetFiles(Path.Combine(_outputDirectory, "beamable", "clients")),
			Has.Length.EqualTo(1));
	}

	[Test]
	public void Generate_JavaScript_WritesNoTypesFile()
	{
		var services = new Dictionary<string, HttpMicroserviceLocalProtocol>
		{
			["MatchService"] = new() { OpenApiDoc = WebClientTestDocuments.MatchServiceDocument() },
		};

		var result = GenerateWebClientCommand.Generate(services, _outputDirectory, "js", TsInt64Mapping.BigIntUnion);

		Assert.That(result.Clients, Has.Count.EqualTo(1));
		Assert.That(Path.GetExtension(result.Clients[0]), Is.EqualTo(".js"));
		Assert.That(result.Types, Is.Empty);
	}

	[Test]
	public void Generate_NoServiceHasOpenApiDocument_Throws()
	{
		var services = new Dictionary<string, HttpMicroserviceLocalProtocol>
		{
			["UnbuiltService"] = new() { ExpectedOpenApiDocPath = "/nowhere/beam_openApi.json" },
		};

		var ex = Assert.Throws<CliException>(() =>
			GenerateWebClientCommand.Generate(services, _outputDirectory, "ts", TsInt64Mapping.BigIntUnion));
		Assert.That(ex.Message, Does.Contain("UnbuiltService"));
		Assert.That(ex.Message, Does.Contain("beam project build"));
		Assert.That(Directory.Exists(_outputDirectory), Is.False);
	}

	[Test]
	public void Generate_NoServices_Throws()
	{
		var ex = Assert.Throws<CliException>(() => GenerateWebClientCommand.Generate(
			new Dictionary<string, HttpMicroserviceLocalProtocol>(), _outputDirectory, "ts", TsInt64Mapping.BigIntUnion));
		Assert.That(ex.Message, Does.Contain("no microservices"));
	}
}
