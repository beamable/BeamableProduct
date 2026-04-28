using Beamable.Common.BeamCli;
using Beamable.Common.Content;
using cli.Docs;
using Newtonsoft.Json;
using System.CommandLine;
using System.Reflection;

namespace cli.Mcp;

public class McpListTypesCommandArgs : CommandArgs
{
	public string section;
	public string filter;
}

public class McpListTypesCommand : AtomicCommand<McpListTypesCommandArgs, BeamableTypesSchema>,
	IStandaloneCommand, ISkipManifest
{
	public McpListTypesCommand()
		: base("list-types", "Return the Beamable Common type schema snapshot — content objects, federation types, microservice types, and other customer-facing types with their fields and summaries")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--section",
			"Which section to return: 'content', 'federation', or 'utility' — leave empty to return all sections"),
			(args, v) => args.section = v);
		AddOption(new Option<string>("--filter",
			"For section='utility': narrow results to types whose namespace or name contains this string (case-insensitive)"),
			(args, v) => args.filter = v);
	}

	public override Task<BeamableTypesSchema> GetResult(McpListTypesCommandArgs args)
	{
		var schema = ReadEmbeddedSchema();
		if (schema == null || schema.ContentTypes is not { Length: > 0 })
			schema = GenerateBeamableTypesSchemaCommand.GenerateLive();

		return Task.FromResult(ApplySectionFilter(schema, args.section, args.filter));
	}

	internal static BeamableTypesSchema ApplySectionFilter(BeamableTypesSchema schema, string section, string filter)
	{
		var norm = section?.Trim().ToLowerInvariant() ?? "";
		if (string.IsNullOrEmpty(norm))
			return schema;

		var f = filter?.Trim() ?? "";
		return norm switch
		{
			"content" => new BeamableTypesSchema
			{
				GeneratedAt = schema.GeneratedAt,
				AssemblyVersion = schema.AssemblyVersion,
				ContentTypes = schema.ContentTypes,
				FederationTypes = System.Array.Empty<FederationTypeEntry>(),
				UtilityTypes = System.Array.Empty<UtilityTypeEntry>(),
				UnrealTypeMappings = System.Array.Empty<UnrealTypeMappingEntry>()
			},
			"federation" => new BeamableTypesSchema
			{
				GeneratedAt = schema.GeneratedAt,
				AssemblyVersion = schema.AssemblyVersion,
				ContentTypes = System.Array.Empty<ContentTypeEntry>(),
				FederationTypes = schema.FederationTypes,
				UtilityTypes = System.Array.Empty<UtilityTypeEntry>(),
				UnrealTypeMappings = System.Array.Empty<UnrealTypeMappingEntry>()
			},
			"utility" or "utility-shared" => new BeamableTypesSchema
			{
				GeneratedAt = schema.GeneratedAt,
				AssemblyVersion = schema.AssemblyVersion,
				ContentTypes = System.Array.Empty<ContentTypeEntry>(),
				FederationTypes = System.Array.Empty<FederationTypeEntry>(),
				UtilityTypes = FilterUtility(
					schema.UtilityTypes?.Where(t => t.Platform != "MicroserviceOnly").ToArray() ?? System.Array.Empty<UtilityTypeEntry>(), f),
				UnrealTypeMappings = System.Array.Empty<UnrealTypeMappingEntry>()
			},
			"utility-server" => new BeamableTypesSchema
			{
				GeneratedAt = schema.GeneratedAt,
				AssemblyVersion = schema.AssemblyVersion,
				ContentTypes = System.Array.Empty<ContentTypeEntry>(),
				FederationTypes = System.Array.Empty<FederationTypeEntry>(),
				UtilityTypes = FilterUtility(
					schema.UtilityTypes?.Where(t => t.Platform == "MicroserviceOnly").ToArray() ?? System.Array.Empty<UtilityTypeEntry>(), f),
				UnrealTypeMappings = System.Array.Empty<UnrealTypeMappingEntry>()
			},
			"unreal" => new BeamableTypesSchema
			{
				GeneratedAt = schema.GeneratedAt,
				AssemblyVersion = schema.AssemblyVersion,
				ContentTypes = System.Array.Empty<ContentTypeEntry>(),
				FederationTypes = System.Array.Empty<FederationTypeEntry>(),
				UtilityTypes = System.Array.Empty<UtilityTypeEntry>(),
				UnrealTypeMappings = schema.UnrealTypeMappings
			},
			_ => schema
		};
	}

	internal static UtilityTypeEntry[] FilterUtility(UtilityTypeEntry[] types, string filter)
	{
		if (string.IsNullOrEmpty(filter)) return types;
		return System.Array.FindAll(types, t =>
			(t.Namespace?.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
			(t.TypeName?.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
	}

	internal static BeamableTypesSchema? ReadEmbeddedSchema()
	{
		var stream = typeof(McpListTypesCommand).Assembly
			.GetManifestResourceStream("cli.Resources.beamable-types.json");
		if (stream == null) return null;
		using var reader = new StreamReader(stream);
		var json = reader.ReadToEnd();
		if (string.IsNullOrWhiteSpace(json) || json.Trim() == "{}") return null;
		try { return JsonConvert.DeserializeObject<BeamableTypesSchema>(json); }
		catch { return null; }
	}
}
