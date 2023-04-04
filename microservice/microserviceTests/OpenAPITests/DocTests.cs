using Beamable.Server;
using beamable.tooling.common.Microservice;
using Beamable.Tooling.Common.OpenAPI;
using microserviceTests.microservice;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using NUnit.Framework;
using System;

namespace microserviceTests.OpenAPITests;

[Serializable]
public class DocTests
{
	[Test]
	public void TestMethodScanning()
	{
		var gen = new ServiceDocGenerator();
		var doc = gen.Generate<DocService>();
		
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
		public Response Add(Request req) => new Response { sum = new Number{ num = req.a.num + req.b.num} };

		[ClientCallable]
		public int Add2(int a, long b) => a + (int)b;
		// [ClientCallable]
		// public Response Add2(Number a, Number b) => new Response { sum = new Number{ num = a.num + b.num} };
	}

	[Serializable]
	public class Request
	{
		public Number a, b;
	}

	[Serializable]
	public class Response
	{
		public Number sum;
	}

	[Serializable]
	public class Number
	{
		public int num;
	}
}
