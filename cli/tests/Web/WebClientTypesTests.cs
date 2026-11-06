using Beamable.Server;
using cli.Portal;
using cli.Services;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceConstants = Beamable.Common.Constants.Features.Services;

namespace tests.Web;

/// <summary>
///   Every generated client imports its DTO types from the shared <c>beamable/clients/types/index.ts</c>. The
///   portal extension commands used to write only the client files, so the import pointed at a file that was
///   missing (new extensions) or stale (existing ones), and the extension failed type checking.
/// </summary>
public class WebClientTypesTests
{
	private string _dir;

	[SetUp]
	public void SetUp()
	{
		_dir = Path.Combine(Path.GetTempPath(), "beam-webclient-types-" + Path.GetRandomFileName());
		Directory.CreateDirectory(_dir);
	}

	[TearDown]
	public void TearDown()
	{
		if (Directory.Exists(_dir))
		{
			Directory.Delete(_dir, true);
		}
	}

	/// <summary>A service with one callable. With <paramref name="dtoName"/>, it returns that DTO; otherwise a string.</summary>
	private static OpenApiDocument Doc(string serviceName, string dtoName = null)
	{
		var responseSchema = dtoName == null
			? new OpenApiSchema { Type = "string" }
			: new OpenApiSchema
			{
				Type = "object",
				Reference = new OpenApiReference { Id = dtoName, Type = ReferenceType.Schema },
				Properties = new Dictionary<string, OpenApiSchema> { ["value"] = new() { Type = "string" } },
				Required = new HashSet<string> { "value" }
			};

		var operation = new OpenApiOperation
		{
			Responses = new OpenApiResponses
			{
				["200"] = new OpenApiResponse
				{
					Content = new Dictionary<string, OpenApiMediaType> { ["application/json"] = new() { Schema = responseSchema } }
				}
			}
		};
		operation.Extensions[ServiceConstants.OPERATION_CALLABLE_METHOD_TYPE_KEY] = new OpenApiString("ClientCallableAttribute");

		var pathItem = new OpenApiPathItem();
		pathItem.Operations[OperationType.Post] = operation;

		return new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = serviceName },
			Paths = new OpenApiPaths { ["/Get"] = pathItem }
		};
	}

	private static BeamoLocalManifest Manifest(params (string beamoId, OpenApiDocument doc)[] services)
	{
		var manifest = new BeamoLocalManifest
		{
			ServiceDefinitions = new List<BeamoServiceDefinition>(),
			HttpMicroserviceLocalProtocols = new BeamoLocalProtocolMap<HttpMicroserviceLocalProtocol>()
		};
		foreach (var (beamoId, doc) in services)
		{
			manifest.ServiceDefinitions.Add(new BeamoServiceDefinition { BeamoId = beamoId, Protocol = BeamoProtocolType.HttpMicroservice });
			manifest.HttpMicroserviceLocalProtocols[beamoId] = new HttpMicroserviceLocalProtocol { OpenApiDoc = doc };
		}
		return manifest;
	}

	private string Extension(string name, params string[] dependencies)
	{
		var path = Path.Combine(_dir, name);
		Directory.CreateDirectory(path);
		var deps = string.Join(", ", dependencies.Select(d => $"\"{d}\""));
		File.WriteAllText(Path.Combine(path, "package.json"),
			$"{{ \"name\": \"{name}\", \"beamable\": {{ \"portalExtension\": true, \"microserviceDependencies\": [ {deps} ] }} }}");
		return path;
	}

	private static string Client(string extensionPath, string serviceName) =>
		File.ReadAllText(Path.Combine(extensionPath, "beamable", "clients", $"{serviceName}Client.ts"));

	private static string TypesPath(string extensionPath) =>
		Path.Combine(extensionPath, "beamable", "clients", "types", "index.ts");

	[Test]
	public void AnExtensionGetsTheTypesOfAllItsClients()
	{
		var ext = Extension("ext", "SvcA", "SvcB", "NoDtoSvc");
		var manifest = Manifest(("SvcA", Doc("SvcA", "AResult")), ("SvcB", Doc("SvcB", "BResult")), ("NoDtoSvc", Doc("NoDtoSvc")));

		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(ext, manifest);

		var types = File.ReadAllText(TypesPath(ext));
		StringAssert.Contains("export type AResult", types);
		StringAssert.Contains("export type BResult", types);

		StringAssert.Contains("from './types'", Client(ext, "SvcA"));
		StringAssert.Contains("Promise<Types.AResult>", Client(ext, "SvcA"));
		StringAssert.Contains("Promise<Types.BResult>", Client(ext, "SvcB"));

		// Generated after services with DTOs, but has none of its own, so it must not import the types module.
		StringAssert.DoesNotContain("./types", Client(ext, "NoDtoSvc"));
	}

	[Test]
	public void TypesDoNotLeakBetweenExtensions()
	{
		var manifest = Manifest(("SvcA", Doc("SvcA", "AResult")), ("SvcB", Doc("SvcB", "BResult")));
		var extA = Extension("ext-a", "SvcA");
		var extB = Extension("ext-b", "SvcB");

		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(extA, manifest);
		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(extB, manifest);

		var typesB = File.ReadAllText(TypesPath(extB));
		StringAssert.Contains("export type BResult", typesB);
		StringAssert.DoesNotContain("AResult", typesB);
	}

	[Test]
	public void ADuplicatedTypeIsDeclaredOnce()
	{
		var ext = Extension("ext", "SvcA", "SvcB");
		var manifest = Manifest(("SvcA", Doc("SvcA", "Shared")), ("SvcB", Doc("SvcB", "Shared")));

		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(ext, manifest);

		var types = File.ReadAllText(TypesPath(ext));
		Assert.That(types.Split("export type Shared").Length - 1, Is.EqualTo(1));
	}

	[Test]
	public void ExistingTypesAreKeptWhenADependencyHasNoApiDoc()
	{
		// SvcB's previous client may still import its types, so index.ts must not be rewritten without them.
		var ext = Extension("ext", "SvcA", "SvcB");
		Directory.CreateDirectory(Path.GetDirectoryName(TypesPath(ext))!);
		File.WriteAllText(TypesPath(ext), "export type BResult = { value: string; };");
		var manifest = Manifest(("SvcA", Doc("SvcA", "AResult")), ("SvcB", null));

		PortalExtensionAddDependencyCommand.GenerateDependenciesClients(ext, manifest);

		Assert.That(File.ReadAllText(TypesPath(ext)), Is.EqualTo("export type BResult = { value: string; };"));
		StringAssert.Contains("Promise<Types.AResult>", Client(ext, "SvcA"));
	}
}
