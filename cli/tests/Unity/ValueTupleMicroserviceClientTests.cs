using Beamable.Common.Dependencies;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Generator;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZLogger;

namespace tests.Unity;

public class ValueTupleMicroserviceClientTests
{
	[Test]
	public void ClientGenerator_PreservesValueTupleCallableParameterType()
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
	}
}
