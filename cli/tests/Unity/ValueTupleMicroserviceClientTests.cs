using Beamable.Common.Dependencies;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Generator;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Text;
using ZLogger;

namespace tests.Unity;

public class ValueTupleMicroserviceClientTests
{
	[Test]
	public void ServerClientGenerator_PreservesTupleCollectionTypes()
	{
		InitializeLogging();
		var output = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
		try
		{
			new OpenApiServerCodeGenerator(GenerateServiceDocument()).GenerateCSharpCode(output);
			Assert.That(File.ReadAllText(output), Does.Contain("System.Collections.Generic.List<System.ValueTuple<int, string>>"));
		}
		finally { File.Delete(output); }
	}

	[Test]
	public void TupleSchemas_SurviveDiskRoundTrip_AndClientCompilation()
	{
		InitializeLogging();
		var directory = Path.Combine(Path.GetTempPath(), "beam-tuple-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			var document = GenerateServiceDocument();
			var schemaPath = Path.Combine(directory, "beam_openApi.json");
			File.WriteAllText(schemaPath, document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json));
			var parsed = new OpenApiStringReader().Read(File.ReadAllText(schemaPath), out var diagnostics);
			Assert.That(diagnostics.Errors, Is.Empty, string.Join("\n", diagnostics.Errors));
			Assert.That(diagnostics.Warnings, Is.Empty);
			new OpenApiClientCodeGenerator(parsed).GenerateCSharpCode(Path.Combine(directory, "Client.cs"));
			var client = File.ReadAllText(Path.Combine(directory, "Client.cs"));
			Assert.That(client, Does.Contain("System.Collections.Generic.List<System.ValueTuple<int, string>>"));
			Assert.That(client, Does.Contain("System.ValueTuple<int, System.ValueTuple<int, string>>"));
			Assert.That(client, Does.Contain("System.ValueTuple<int, System.ValueTuple<int, string>[]>"));

			// Compile against the shared SDK and minimal Unity-only infrastructure. This checks actual
			// C# syntax/types without requiring an installed Unity editor on the CLI test runner.
			File.WriteAllText(Path.Combine(directory, "Client.csproj"), $"""
				<Project Sdk="Microsoft.NET.Sdk">
				<PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>
				<ItemGroup><Reference Include="Beamable.Common"><HintPath>{System.Security.SecurityElement.Escape(typeof(Beamable.Common.Promise).Assembly.Location)}</HintPath></Reference></ItemGroup>
				</Project>
				""");
			File.WriteAllText(Path.Combine(directory, "UnityInfrastructure.cs"), """
				namespace Beamable.Platform.SDK { }
				namespace Beamable {
				  public class BeamContext { }
				  public class BeamContextSystemAttribute : System.Attribute { }
				}
				namespace Beamable.Server {
				  public class MicroserviceClient {
				    public MicroserviceClient(Beamable.BeamContext context) { }
				    public MicroserviceClient(Beamable.Common.Dependencies.IDependencyProvider provider) { }
				    protected Beamable.Common.Promise<T> Request<T>(string service, string method, System.Collections.Generic.Dictionary<string, object> fields) => default;
				  }
				  public class MicroserviceClientDataWrapper<T> { }
				  public class MicroserviceClients { public T GetClient<T>() => default; }
				}
				""");
			var start = new ProcessStartInfo("dotnet", "build Client.csproj --nologo --verbosity quiet")
			{
				WorkingDirectory = directory, RedirectStandardOutput = true, RedirectStandardError = true,
				UseShellExecute = false, CreateNoWindow = true
			};
			using var process = Process.Start(start)!;
			var stdout = process.StandardOutput.ReadToEndAsync();
			var stderr = process.StandardError.ReadToEndAsync();
			if (!process.WaitForExit(120000)) { process.Kill(true); Assert.Fail("Client compilation timed out"); }
			Assert.That(process.ExitCode, Is.Zero, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
		}
		finally { Directory.Delete(directory, true); }
	}

	[Test]
	public void ClientGenerator_PreservesValueTupleCallableTypes()
	{
		InitializeLogging();
		var document = GenerateServiceDocument();
		var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.cs");

		try
		{
			new OpenApiClientCodeGenerator(document).GenerateCSharpCode(outputPath);
			var generatedClient = File.ReadAllText(outputPath);

			Assert.That(generatedClient,
				Does.Contain("System.ValueTuple<int, int> value"));
			Assert.That(generatedClient,
				Does.Contain("serializedFields.Add(\"value\", raw_value)"));
			Assert.That(generatedClient,
				Does.Contain("Beamable.Common.Promise<System.ValueTuple<int, int>> GetTuple()"));
		}
		finally
		{
			File.Delete(outputPath);
		}
	}

	[Test]
	public void RequestSerialization_RoundTripsValueTupleParameter()
	{
		var requestFields = new Dictionary<string, object>
		{
			["value"] = (1, 3)
		};

		var requestJson = Json.Serialize(requestFields, new StringBuilder());
		var tupleJson = JObject.Parse(requestJson)["value"]!.ToString(Formatting.None);
		var deserialized = JsonConvert.DeserializeObject<(int, int)>(
			tupleJson,
			UnitySerializationSettings.Instance);

		Assert.That(deserialized.Item1, Is.EqualTo(1));
		Assert.That(deserialized.Item2, Is.EqualTo(3));
	}

	[Test]
	public void ResponseDeserialization_RoundTripsValueTupleReturn()
	{
		const string ResponseJson = "{\"Item1\":1,\"Item2\":3}";

		var deserialized = Json.Deserialize<(int, int)>(ResponseJson);

		Assert.That(deserialized.Item1, Is.EqualTo(1));
		Assert.That(deserialized.Item2, Is.EqualTo(3));
	}

	private static Microsoft.OpenApi.Models.OpenApiDocument GenerateServiceDocument()
	{
		var builder = new DependencyBuilder();
		builder.AddSingleton<BeamStandardTelemetryAttributeProvider>();
		builder.AddSingleton<SingletonDependencyList<ITelemetryAttributeProvider>>();
		builder.AddSingleton<IMicroserviceArgs>(new MicroserviceArgs());

		return new ServiceDocGenerator().Generate<ValueTupleParameterService>(builder.Build());
	}

	private static void InitializeLogging()
	{
		BeamableZLoggerProvider.Provider = new BeamableZLoggerProvider();
		BeamableZLoggerProvider.LogContext.Value = LoggerFactory.Create(builder =>
		{
			builder.AddZLoggerConsole();
		}).CreateLogger<ValueTupleMicroserviceClientTests>();
	}

	[Microservice("tuple_parameter_tests")]
	private class ValueTupleParameterService : Microservice
	{
		[ClientCallable]
		public int AddTuple((int, int) value) => value.Item1 + value.Item2;

		[ClientCallable]
		public (int, int) GetTuple() => (1, 3);

		[ClientCallable]
		public string Nested((int, (int, string)) value) => value.ToString();

		[ClientCallable]
		public List<(int, string)> ListTuples(List<(int, string)> value) => value;

		[ClientCallable]
		public Dictionary<string, (int, string)> MapTuples(Dictionary<string, (int, string)> value) => value;

		[ClientCallable]
		public (int, string)[] ArrayTuples((int, string)[] value) => value;

		[ClientCallable]
		public (int, (int, string)[]) NestedArray((int, (int, string)[]) value) => value;
	}
}
