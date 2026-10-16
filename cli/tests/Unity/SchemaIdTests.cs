using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Beamable.Server;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using NUnit.Framework;

namespace tests.Unity;

public class SchemaIdTests
{
	[Test]
	public void SchemaIds_AreLegalDistinctAndStable()
	{
		var types = new[] { typeof((int, string)), typeof((string, int)), typeof((int, (int, string))),
			typeof(ValueTuple<int[]>), typeof(ValueTuple<int[,]>), typeof(Outer<int>.First), typeof(Outer<int>.Second) };
		var ids = types.Select(SchemaGenerator.GetSchemaId).ToArray();
		Assert.That(ids, Is.Unique);
		foreach (var id in ids) Assert.That(id, Does.Match(@"^[a-zA-Z0-9\.\-_]+$"));
		Assert.That(types.Reverse().Select(SchemaGenerator.GetSchemaId).Reverse(), Is.EqualTo(ids));
		Assert.That(SchemaGenerator.GetSchemaId(typeof(System.Text.StringBuilder)), Is.EqualTo("System.Text.StringBuilder"));
	}

	[Test]
	public void SchemaId_EscapesLiteralEscapeMarkers()
	{
		var module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("SchemaIdNames"), AssemblyBuilderAccess.Run)
			.DefineDynamicModule("Types");
		var punctuated = module.DefineType("Names.A<B>").CreateType()!;
		var encoded = module.DefineType(SchemaGenerator.GetSchemaId(punctuated)).CreateType()!;
		Assert.That(SchemaGenerator.GetSchemaId(encoded), Is.Not.EqualTo(SchemaGenerator.GetSchemaId(punctuated)));
	}

	[Test]
	public void RequiredEnumSchema_IsNotSkippedAsAPrimitive()
	{
		var required = new HashSet<Type>();
		var reference = SchemaGenerator.Convert(typeof(SampleEnum), ref required, 0);
		var doc = new OpenApiDocument { Components = new OpenApiComponents() };
		SchemaGenerator.TryAddMissingSchemaTypes(ref doc, required);
		Assert.That(doc.Components.Schemas.ContainsKey(reference.Reference.Id), Is.True);
	}

	[Test]
	public void AssemblyDocument_RoundTripIncludesTransitiveDtoAndEnumSchemas()
	{
		BeamableZLoggerProvider.Provider = new BeamableZLoggerProvider();
		BeamableZLoggerProvider.LogContext.Value = NullLogger<SchemaIdTests>.Instance;
		var document = new ServiceDocGenerator().Generate(typeof(SchemaIdTests).Assembly,
			new[] { typeof(AssemblyRoot) });
		var json = document.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		var parsed = new OpenApiStringReader().Read(json, out var diagnostics);

		Assert.That(diagnostics.Errors, Is.Empty, string.Join("\n", diagnostics.Errors));
		Assert.That(diagnostics.Warnings, Is.Empty);
		Assert.That(parsed.Components.Schemas.Keys, Is.EquivalentTo(new[]
		{
			SchemaGenerator.GetSchemaId(typeof(AssemblyRoot)),
			SchemaGenerator.GetSchemaId(typeof(AssemblyChild)),
			SchemaGenerator.GetSchemaId(typeof(SampleEnum))
		}));
		var root = parsed.Components.Schemas[SchemaGenerator.GetSchemaId(typeof(AssemblyRoot))];
		Assert.That(root.Properties[nameof(AssemblyRoot.Child)].Reference.Id,
			Is.EqualTo(SchemaGenerator.GetSchemaId(typeof(AssemblyChild))));
		var child = parsed.Components.Schemas[root.Properties[nameof(AssemblyRoot.Child)].Reference.Id];
		Assert.That(child.Properties[nameof(AssemblyChild.State)].Reference.Id,
			Is.EqualTo(SchemaGenerator.GetSchemaId(typeof(SampleEnum))));
		var enumSchema = parsed.Components.Schemas[child.Properties[nameof(AssemblyChild.State)].Reference.Id];
		Assert.That(enumSchema.Enum.Cast<OpenApiString>().Select(value => value.Value),
			Is.EqualTo(new[] { nameof(SampleEnum.First), nameof(SampleEnum.Second) }));
	}

	[Test]
	public void InvalidGeneratedDocument_ThrowsWithParserDiagnosticAndPointer()
	{
		const string invalidDocument = """
			{
			  "openapi": "3.0.1",
			  "info": { "title": "Invalid schema", "version": "1.0" },
			  "paths": {},
			  "components": {
			    "schemas": {
			      "Broken": { "type": "object", "properties": "not-an-object" }
			    }
			  }
			}
			""";
		new OpenApiStringReader().Read(invalidDocument, out var diagnostics);
		Assert.That(diagnostics.Errors, Is.Not.Empty);
		var diagnostic = diagnostics.Errors.First(error => !string.IsNullOrEmpty(error.Pointer));
		var readGeneratedDocument = typeof(ServiceDocGenerator).GetMethod("ReadGeneratedDocument",
			BindingFlags.Static | BindingFlags.NonPublic);
		Assert.That(readGeneratedDocument, Is.Not.Null);

		var invocation = Assert.Throws<TargetInvocationException>(() =>
			readGeneratedDocument!.Invoke(null, new object[] { invalidDocument }));
		Assert.That(invocation!.InnerException, Is.TypeOf<InvalidOperationException>());
		Assert.That(invocation.InnerException!.Message, Does.StartWith("Generated invalid OpenAPI: "));
		Assert.That(invocation.InnerException.Message, Does.Contain(diagnostic.Message));
		Assert.That(invocation.InnerException.Message, Does.Contain($"({diagnostic.Pointer})"));
	}

	private class AssemblyRoot { public AssemblyChild Child = new(); }
	private class AssemblyChild { public SampleEnum State = SampleEnum.First; }
	private enum SampleEnum { First, Second }
	private class Outer<T> { public class First { } public class Second { } }
}
