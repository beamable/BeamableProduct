using Beamable.Common;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Content;
using Beamable.Server;
using beamable.tooling.common.Microservice;
using Beamable.Tooling.Common.OpenAPI;
using microserviceTests.microservice;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using microserviceTests.microservice.Util;

namespace microserviceTests.OpenAPITests;

[Serializable]
public class DocTests
{
	public class ExampleAttrs : ITelemetryAttributeProvider
	{
		public List<TelemetryAttributeDescriptor> GetDescriptors()
		{
			return new List<TelemetryAttributeDescriptor>
			{
				new TelemetryAttributeDescriptor
				{
					name = "tuna",
					description = "blah blah blah",
					level = TelemetryImportance.ESSENTIAL,
					type = TelemetryAttributeType.LONG,
					source = TelemetryAttributeSource.RESOURCE
				}
			};
		}

		public void CreateDefaultAttributes(IDefaultAttributeContext ctx)
		{
		}

		public void CreateConnectionAttributes(IConnectionAttributeContext ctx)
		{
		}

		public void CreateRequestAttributes(IRequestAttributeContext ctx)
		{
		}
	}
	
	[Test]
	public void TestMethodScanning()
	{
		LoggingUtil.InitTestCorrelator();
		var gen = new ServiceDocGenerator();

		var builder = new DependencyBuilder();
		
		builder.AddSingleton<BeamStandardTelemetryAttributeProvider>();
		builder.AddSingleton<SingletonDependencyList<ITelemetryAttributeProvider>>();
		builder.AddSingleton<IMicroserviceArgs, TestArgs>();
		builder.AddSingleton<ExampleAttrs>();
		var provider = builder.Build();
		var doc = gen.Generate<DocService>(provider);
		
		// Assert.AreEqual("docs", doc.Info.Title);
		// Assert.AreEqual(1, doc.Paths.Count);

		// var reqSchema = doc.Paths["/Add"].Operations[OperationType.Post].RequestBody.Content["application/json"].Schema;
		// Assert.AreEqual(1, reqSchema.Required.Count);
		
		var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		Console.WriteLine(outputString);
	}

	[Microservice("docs")]
	public class DocService : Microservice
	{
		[ClientCallable]
		public void Nothing(){ }
		
		/// <summary>
		/// This method will add some stuff together
		/// </summary>
		/// <param name="req">the req object</param>
		/// <returns>a sum response</returns>
		[ClientCallable]
		public Response Add(Request req) => new Response { sum = new Number{ num = req.a.num + req.b.num} };
		
		/// <summary>
		/// This method will add some stuff together
		/// </summary>
		/// <param name="a">the first number</param>
		/// <param name="b">the second number</param>
		/// <returns>an int</returns>
		[ClientCallable]
		public Promise<int> ReturnsTypedPromise(int a, long b) => Promise<int>.Successful(3);
		
		[ClientCallable]
		public Task<int> ReturnsTypedTask(int a, long b) => Task.FromResult(3);
		
		[ClientCallable]
		public Promise ReturnedUntypedPromise(int a, long b) => Promise.Success;
		
		[ClientCallable]
		public Task ReturnedUntypedTask(int a, long b) => Task.CompletedTask;

		[ClientCallable]
		public int[] ReturnsArrayOfPrimitive() => new[] { 1, 2, 3 };
		
		[ClientCallable]
		public List<int> ReturnsListOfPrimitive() => null;
		
		[ClientCallable]
		public Promise<List<int>> ReturnsPromiseOfListOfPrim() => null;

		[ClientCallable]
		public Number[] ReturnsArrayOfComplex() => null;
		
		[ClientCallable]
		public List<Number> ReturnsListOfComplex() => null;
		
		[ClientCallable]
		public Promise<List<Number>> ReturnsPromiseOfListOfComplex() => null;
		
		[Callable]
		public void NoReturnType_Callable() {}

		[Callable]
		public FISHY ReturnsEnum() => FISHY.Haddock;
		
		[Callable]
		public FISHY ReturnsAndAccepts(FISHY fishy) => fishy;

		[ClientCallable]
		[SwaggerCategory("test")]
		public bool ReturnsBoolWithTag() => true;
		
		[ClientCallable]
		public Dictionary<string, int> ReturnsMapOfInt() => null;

		[ClientCallable]
		public Dictionary<string, Number> ReturnsMapOfComplex(Dictionary<string, int> props) => null;
		
		[ClientCallable]
		public ObjectWithAMap ReturnsComplex(ObjectWithAMap map) => null;

		[ClientCallable]
		public InventoryResponse ReturnsInventoryStuff() => null;

		[ClientCallable]
		public InventoryUpdateBuilder ReturnsInventoryUpdateBuilder() => null;
		
		[ClientCallable]
		public ObjectWithOptional ReturnsOptions(ObjectWithOptional req, OptionalInt num) => null;
	}

	/// <summary>
	/// a request
	/// </summary>
	[Serializable]
	public class Request
	{
		/// <summary>
		/// the first number
		/// </summary>
		public Number a;
		
		/// <summary>
		/// the second number
		/// </summary>
		public Number b;
	}

	/// <summary>
	/// a response
	/// </summary>
	[Serializable]
	public class Response
	{
		/// <summary>
		/// the sum
		/// </summary>
		public Number sum;
	}

	/// <summary>
	/// a number
	/// </summary>
	[Serializable]
	public class Number
	{
		/// <summary>
		/// the number
		/// </summary>
		public int num;
	}

	[Serializable]
	public class ObjectWithAMap
	{
		public Dictionary<string, Number> nums;
	}

	[Serializable]
	public class ObjectWithOptional
	{
		public Optional<int> genInt;
		public OptionalInt num;
	}

	/// <summary>
	/// a fishy enum
	/// </summary>
	public enum FISHY
	{
		/// <summary>
		/// tuna value
		/// </summary>
		Tuna,
		
		/// <summary>
		/// salmon value
		/// </summary>
		Salmon, 
		Haddock
	}
}
