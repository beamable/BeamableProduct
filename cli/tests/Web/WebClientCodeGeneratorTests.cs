using cli.Services.Web;
using cli.Services.Web.Helpers;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace tests.Web;

[TestFixture]
public class WebClientCodeGeneratorTests
{
	private string _outputDirectory;

	[SetUp]
	public void SetUp()
	{
		_outputDirectory = Path.Combine(Path.GetTempPath(), "beam-web-client-tests", Guid.NewGuid().ToString("N"));
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_outputDirectory))
		{
			Directory.Delete(_outputDirectory, true);
		}
	}

	[Test]
	public void SharedSchema_IsEmittedOnceInTypesFile()
	{
		var generator = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts");
		var clientPath = generator.GenerateClientCode(ClientsDir("run"));
		var typesPath = WebClientCodeGenerator.GenerateClientTypes(TypesDir("run"), new[] { generator });

		// the same DTO is collected once per referencing endpoint...
		Assert.That(generator.ClientTypes.Count(t => t.Name == nameof(WebClientTestMatchView)), Is.GreaterThan(1));

		// ...but the types file declares it exactly once, along with its nested DTO
		var types = File.ReadAllText(typesPath);
		Assert.That(CountDeclarations(types, nameof(WebClientTestMatchView)), Is.EqualTo(1), types);
		Assert.That(CountDeclarations(types, nameof(WebClientTestMatchPlayer)), Is.EqualTo(1), types);
		Assert.That(Regex.Matches(types, @"export type (\w+)").Select(m => m.Groups[1].Value),
			Is.Unique, types);

		var client = File.ReadAllText(clientPath);
		Assert.That(Path.GetFileName(clientPath), Is.EqualTo("MatchServiceClient.ts"));
		Assert.That(client, Does.Contain("import type * as Types from './types';"), client);
		Assert.That(client, Does.Contain($"Promise<Types.{nameof(WebClientTestMatchView)}>"), client);
		// an array response references the element type from the types file as well
		Assert.That(client, Does.Contain($"listMatches(): Promise<Types.{nameof(WebClientTestMatchView)}[]>"), client);
		Assert.That(client, Does.Contain("endpoint: \"GetMatch\""), client);
		Assert.That(client, Does.Contain("endpoint: \"CountMatches\""), client);
	}

	[TestCase(TsInt64Mapping.BigIntUnion, "bigint | string")]
	[TestCase(TsInt64Mapping.Number, "number")]
	[TestCase(TsInt64Mapping.String, "string")]
	public void Int64Mapping_IsAppliedToTypesAndClient(TsInt64Mapping mapping, string expected)
	{
		var generator = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts",
			int64Mapping: mapping);
		var clientPath = generator.GenerateClientCode(ClientsDir("run"));
		var typesPath = WebClientCodeGenerator.GenerateClientTypes(TypesDir("run"), new[] { generator });
		var types = File.ReadAllText(typesPath);
		var client = File.ReadAllText(clientPath);

		var arrayOfExpected = expected.Contains('|') ? $"({expected})[]" : $"{expected}[]";

		// DTO fields, including nested DTOs and lists
		Assert.That(types, Does.Contain($"createdAt: {expected};"), types);
		Assert.That(types, Does.Contain($"endedAt: {expected};"), types);
		Assert.That(types, Does.Contain($"playerId: {expected};"), types);
		Assert.That(types, Does.Contain($"roundScores: {arrayOfExpected};"), types);
		// callable parameters: a `long` parameter, and a `long?` parameter keeps its `| null`
		Assert.That(types, Does.Match($@"JoinMatchRequestArgs = \{{[^}}]*playerId: {Regex.Escape(expected)};"), types);
		Assert.That(types, Does.Contain($"since: {expected} | null;"), types);
		// a callable returning `long` uses the same mapping
		Assert.That(client, Does.Contain($"Promise<{expected}>"), client);
		// other integers are unaffected
		Assert.That(types, Does.Contain("score: number;"), types);
	}

	[Test]
	public void Int64Mapping_DefaultsToBigIntUnion()
	{
		var generator = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts");
		var typesPath = WebClientCodeGenerator.GenerateClientTypes(TypesDir("run"), new[] { generator });

		Assert.That(File.ReadAllText(typesPath), Does.Contain("createdAt: bigint | string;"));
	}

	[Test]
	public void RunningTwice_SecondTypesFileOnlyContainsSecondDocumentTypes()
	{
		var first = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts");
		first.GenerateClientCode(ClientsDir("first"));
		var firstTypes = File.ReadAllText(WebClientCodeGenerator.GenerateClientTypes(TypesDir("first"), new[] { first }));
		Assert.That(firstTypes, Does.Contain(nameof(WebClientTestMatchView)));

		var second = new WebClientCodeGenerator(WebClientTestDocuments.InventoryServiceDocument(), "ts");
		second.GenerateClientCode(ClientsDir("second"));
		var secondTypes =
			File.ReadAllText(WebClientCodeGenerator.GenerateClientTypes(TypesDir("second"), new[] { second }));

		AssertOnlyInventoryTypes(secondTypes);
	}

	[Test]
	public void RunningTwice_WithoutGeneratingTypesInBetween_LeavesNoLeftoverState()
	{
		// e.g. portal extension client generation, which never writes a types file, or a JavaScript run
		new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts").GenerateClientCode(
			ClientsDir("extension"));
		new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "js").GenerateClientCode(
			ClientsDir("javascript"));

		var second = new WebClientCodeGenerator(WebClientTestDocuments.InventoryServiceDocument(), "ts");
		second.GenerateClientCode(ClientsDir("second"));
		var secondTypes =
			File.ReadAllText(WebClientCodeGenerator.GenerateClientTypes(TypesDir("second"), new[] { second }));

		AssertOnlyInventoryTypes(secondTypes);
	}

	[Test]
	public void RunningTwice_LanguageOfEarlierRunDoesNotLeak()
	{
		var tsClient = File.ReadAllText(
			new WebClientCodeGenerator(WebClientTestDocuments.InventoryServiceDocument(), "ts").GenerateClientCode(
				ClientsDir("ts")));
		var jsGenerator = new WebClientCodeGenerator(WebClientTestDocuments.InventoryServiceDocument(), "js");
		var jsClientPath = jsGenerator.GenerateClientCode(ClientsDir("js"));
		var tsGenerator = new WebClientCodeGenerator(WebClientTestDocuments.InventoryServiceDocument(), "typescript");

		Assert.That(Path.GetExtension(jsClientPath), Is.EqualTo(".js"));
		Assert.That(File.ReadAllText(jsClientPath), Does.Not.Contain("import type"));
		Assert.That(jsGenerator.IsTypeScript, Is.False);
		Assert.That(tsGenerator.IsTypeScript, Is.True);
		Assert.That(tsClient, Does.Contain("import type * as Types from './types';"));
	}

	[Test]
	public void ClientWithoutObjectTypes_DoesNotImportTypes_EvenAfterAnotherServiceHadTypes()
	{
		var withTypes = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts");
		withTypes.GenerateClientCode(ClientsDir("run"));

		var primitive = new WebClientCodeGenerator(WebClientTestDocuments.PrimitiveServiceDocument(), "ts");
		var primitiveClient = File.ReadAllText(primitive.GenerateClientCode(ClientsDir("run")));

		Assert.That(primitive.ClientTypes, Is.Empty);
		Assert.That(primitiveClient, Does.Not.Contain("./types"), primitiveClient);
		Assert.That(primitiveClient, Does.Contain("getCount(): Promise<number>"), primitiveClient);
	}

	[Test]
	public void GenerateClientTypes_MergesServicesAndCollapsesSharedNames()
	{
		var match = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts");
		var matchAgain = new WebClientCodeGenerator(WebClientTestDocuments.MatchServiceDocument(), "ts");
		var inventory = new WebClientCodeGenerator(WebClientTestDocuments.InventoryServiceDocument(), "ts");

		var types = File.ReadAllText(
			WebClientCodeGenerator.GenerateClientTypes(TypesDir("run"), new[] { match, inventory, matchAgain }));

		Assert.That(CountDeclarations(types, nameof(WebClientTestMatchView)), Is.EqualTo(1), types);
		Assert.That(CountDeclarations(types, nameof(WebClientTestInventoryItem)), Is.EqualTo(1), types);
	}

	[Test]
	public void GenerateClientTypes_WithNoTypes_WritesNothing()
	{
		var primitive = new WebClientCodeGenerator(WebClientTestDocuments.PrimitiveServiceDocument(), "ts");

		var typesPath = WebClientCodeGenerator.GenerateClientTypes(TypesDir("run"), new[] { primitive });

		Assert.That(typesPath, Is.Empty);
		Assert.That(Directory.Exists(TypesDir("run")), Is.False);
	}

	private static void AssertOnlyInventoryTypes(string types)
	{
		Assert.That(CountDeclarations(types, nameof(WebClientTestInventoryItem)), Is.EqualTo(1), types);
		Assert.That(types, Does.Not.Contain(nameof(WebClientTestMatchView)), types);
		Assert.That(types, Does.Not.Contain(nameof(WebClientTestMatchPlayer)), types);
		Assert.That(types, Does.Not.Contain("_GetMatchRequestArgs"), types);
	}

	private static int CountDeclarations(string types, string typeName) =>
		Regex.Matches(types, $@"export type {Regex.Escape(typeName)}\b").Count;

	private string ClientsDir(string run) => Path.Combine(_outputDirectory, run, "beamable", "clients");

	private string TypesDir(string run) => Path.Combine(ClientsDir(run), "types");
}
