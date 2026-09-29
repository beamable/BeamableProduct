using Beamable.Server;
using cli.Services.Web;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using ServiceConstants = Beamable.Common.Constants.Features.Services;

namespace tests.PortalExtensionTests;

/// <summary>
/// `generate pe-client` writes one `types/index.ts` beside every extension's clients directory. The type
/// accumulator is process-wide and drained once the types are written, so each directory has to get the
/// same file from a single render rather than a per-directory call.
/// </summary>
[NonParallelizable]
public class PortalExtensionClientTypesTests
{
	private string _root;

	[SetUp]
	public void SetUp()
	{
		_root = Path.Combine(Path.GetTempPath(), "pe-client-types-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(_root);

		// Drain anything a previous test in this process left in the static accumulators.
		WebClientCodeGenerator.GenerateClientTypes(Array.Empty<string>());
	}

	[TearDown]
	public void TearDown()
	{
		WebClientCodeGenerator.GenerateClientTypes(Array.Empty<string>());
		if (Directory.Exists(_root))
		{
			Directory.Delete(_root, true);
		}
	}

	private static OpenApiDocument DocumentWithCallableReturning(string dtoName)
	{
		var dto = new OpenApiSchema
		{
			Type = "object",
			Reference = new OpenApiReference { Id = dtoName, Type = ReferenceType.Schema },
			Properties = new Dictionary<string, OpenApiSchema>
			{
				["name"] = new OpenApiSchema { Type = "string" },
			},
			Required = new HashSet<string> { "name" },
		};

		var operation = new OpenApiOperation
		{
			Responses = new OpenApiResponses
			{
				["200"] = new OpenApiResponse
				{
					Content = new Dictionary<string, OpenApiMediaType>
					{
						["application/json"] = new OpenApiMediaType { Schema = dto },
					},
				},
			},
		};
		operation.Extensions[ServiceConstants.OPERATION_CALLABLE_METHOD_TYPE_KEY] =
			new OpenApiString(nameof(ClientCallableAttribute));

		var path = new OpenApiPathItem();
		path.Operations[OperationType.Post] = operation;

		return new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = "Svc" },
			Paths = new OpenApiPaths { ["/GetDto"] = path },
		};
	}

	[Test]
	public void EveryDirectory_GetsTheSameTypesFile()
	{
		var generator = new WebClientCodeGenerator(DocumentWithCallableReturning("MyDto"), "ts");
		var clientsA = Path.Combine(_root, "extA", "beamable", "clients");
		var clientsB = Path.Combine(_root, "extB", "beamable", "clients");
		generator.GenerateClientCode(clientsA);
		generator.GenerateClientCode(clientsB);

		var written = WebClientCodeGenerator.GenerateClientTypes(new[]
		{
			Path.Combine(clientsA, "types"),
			Path.Combine(clientsB, "types"),
		});

		Assert.That(written, Has.Count.EqualTo(2));
		var typesA = Path.Combine(clientsA, "types", "index.ts");
		var typesB = Path.Combine(clientsB, "types", "index.ts");
		Assert.That(File.Exists(typesA), Is.True, "first directory must get types/index.ts");
		Assert.That(File.Exists(typesB), Is.True, "second directory must get types/index.ts too");

		var contentA = File.ReadAllText(typesA);
		Assert.That(contentA, Does.Contain("MyDto"));
		Assert.That(File.ReadAllText(typesB), Is.EqualTo(contentA));
	}

	[Test]
	public void AccumulatorIsClearedAfterWriting()
	{
		var generator = new WebClientCodeGenerator(DocumentWithCallableReturning("MyDto"), "ts");
		generator.GenerateClientCode(Path.Combine(_root, "ext", "beamable", "clients"));
		WebClientCodeGenerator.GenerateClientTypes(new[] { Path.Combine(_root, "ext", "beamable", "clients", "types") });

		var again = Path.Combine(_root, "again", "types");
		var written = WebClientCodeGenerator.GenerateClientTypes(new[] { again });

		Assert.That(written, Is.Empty);
		Assert.That(File.Exists(Path.Combine(again, "index.ts")), Is.False);
	}
}
