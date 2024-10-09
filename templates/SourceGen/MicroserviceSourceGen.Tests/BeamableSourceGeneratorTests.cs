using Beamable.Common;
using Beamable.Server;
using Microservice.SourceGen.Tests.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Microservice.SourceGen.Tests;

public partial class BeamableSourceGeneratorTests : IDisposable
{
	public BeamableSourceGenerator Generator { get; set; }
	public GeneratorDriver Driver { get; set; }
	public CSharpCompilation Compilation { get; set; }

	public BeamableSourceGeneratorTests()
	{
		Generator = new();
		Driver = CSharpGeneratorDriver.Create(Generator);
		Compilation = CSharpCompilation.Create(
			nameof(BeamableSourceGenerator) + "Tests",
			Array.Empty<SyntaxTree>(),
			new[]
			{
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(IFederation).Assembly.Location), MetadataReference.CreateFromFile(typeof(Beamable.Server.Microservice).Assembly.Location),
				MetadataReference.CreateFromFile(typeof(MicroserviceAttribute).Assembly.Location), MetadataReference.CreateFromFile(typeof(Constants).Assembly.Location),
			});
	}

	private void PrepareForRun(IEnumerable<MicroserviceSourceGenConfig?> configs, string[] csharpText, bool failConfig = false)
	{
		// If we have the source-gen file, let's add it.
		foreach (var config in configs)
		{
			var sourceGenConfigFile = failConfig ? "{ invalid json }" : JsonSerializer.Serialize(config, new JsonSerializerOptions { IncludeFields = true });
			var additionalTexts = new List<AdditionalText>();
			additionalTexts.Add(new TestAdditionalFile(MicroserviceSourceGenConfig.CONFIG_FILE_NAME, sourceGenConfigFile));
			Driver = Driver.AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts));
		}

		// We need to update the compilation with all the syntax trees
		Compilation = Compilation.AddSyntaxTrees(csharpText.Select(s => CSharpSyntaxTree.ParseText(s)));
	}

	public void Dispose()
	{
	}
}
