using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.Content;
using Beamable.Server;
using Newtonsoft.Json;
using System.CommandLine;
using System.Reflection;
using System.Xml.Linq;

namespace cli.Docs;

public class GenerateBeamableTypesSchemaCommandArgs : CommandArgs
{
	public string outputPath;
	public string outputDir;
}

[CliContractType]
public class BeamableTypesSchema
{
	public string GeneratedAt;
	public string AssemblyVersion;
	public ContentTypeEntry[] ContentTypes;
	public FederationTypeEntry[] FederationTypes;
	public UtilityTypeEntry[] UtilityTypes;
	public UnrealTypeMappingEntry[] UnrealTypeMappings;
}

[CliContractType]
public class FederationTypeEntry
{
	public string InterfaceName;
	public string Namespace;
	public string Summary;
	public string GenericConstraint;
	public string Platform;
	public FederationMethodEntry[] Methods;
}

[CliContractType]
public class FederationMethodEntry
{
	public string Name;
	public string ReturnType;
	public string Summary;
	public FederationMethodParam[] Parameters;
}

[CliContractType]
public class FederationMethodParam
{
	public string Name;
	public string Type;
}

[CliContractType]
public class UtilityTypeEntry
{
	public string TypeName;
	public string Namespace;
	public string Kind;  // "class", "abstract class", "static class", "struct", "interface", "enum"
	public string Summary;
	public string Platform;
	public UtilityMemberEntry[] Members;   // public fields + properties
	public UtilityMethodEntry[] Methods;   // public declared methods
	public string[] EnumValues;
}

[CliContractType]
public class UtilityMemberEntry
{
	public string Name;
	public string Type;
	public bool IsStatic;
	public string MemberKind;  // "field" | "property"
	public string Summary;
}

[CliContractType]
public class UtilityMethodEntry
{
	public string Name;
	public string ReturnType;
	public bool IsStatic;
	public string Summary;
	public UtilityParam[] Parameters;
}

[CliContractType]
public class UtilityParam
{
	public string Name;
	public string Type;
}

[CliContractType]
public class ContentTypeEntry
{
	public string TypeName;
	public string ClassName;
	public string Namespace;
	public string Summary;
	public string Platform;
	public string[] FormerlyKnownAs;
	public ContentFieldEntry[] Fields;
}

[CliContractType]
public class ContentFieldEntry
{
	public string Name;
	public string Type;
	public string Summary;
}

[CliContractType]
public class UnrealTypeMappingEntry
{
	public string CppType;
	public string CSharpEquivalent;
	public string Notes;
}

[CliContractType]
public class TypeSectionIndex
{
	public string Name;
	public string Description;
	public int TypeCount;
}

public class GenerateBeamableTypesSchemaCommand
	: AtomicCommand<GenerateBeamableTypesSchemaCommandArgs, BeamableTypesSchema>, IStandaloneCommand
{
	public override bool IsForInternalUse => true;

	public GenerateBeamableTypesSchemaCommand()
		: base("generate-type-schema", "Generate a JSON snapshot of Beamable Common types and their fields for MCP type discovery")
	{
	}

	public override void Configure()
	{
		AddOption(
			new Option<string>("--output", "File path to write the JSON schema to; omit to return as command output"),
			(args, v) => args.outputPath = v);
		AddOption(
			new Option<string>("--output-dir", "Directory to write split JSON schema files to; generates one file per section plus an index"),
			(args, v) => args.outputDir = v);
	}

	public static BeamableTypesSchema GenerateLive()
	{
		var commonAssembly = typeof(ContentObject).Assembly;
		var commonDocs = LoadXmlDocs(commonAssembly);
		var schema = BuildSchema(commonAssembly, commonDocs);

		foreach (var ct in schema.ContentTypes) ct.Platform = "Shared";
		foreach (var ft in schema.FederationTypes) ft.Platform = "Shared";
		foreach (var ut in schema.UtilityTypes) ut.Platform = ClassifyPlatform(ut.Namespace);

		// Collect types already covered so we don't duplicate them
		var coveredTypeNames = new HashSet<string>(
			schema.UtilityTypes.Select(u => $"{u.Namespace}.{u.TypeName}")
				.Concat(schema.ContentTypes.Select(c => $"{c.Namespace}.{c.ClassName}"))
				.Concat(schema.FederationTypes.Select(f => $"{f.Namespace}.{f.InterfaceName}"))
		);

		// Scan all loaded Beamable assemblies beyond the common assembly
		var scannedAssemblies = new HashSet<string> { commonAssembly.FullName };
		var anchorTypes = new Type[] { typeof(Microservice) };
		foreach (var anchor in anchorTypes)
		{
			try { _ = anchor.Assembly; }
			catch { /* force load */ }
		}

		var beamableAssemblies = AppDomain.CurrentDomain.GetAssemblies()
			.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
			.Where(a =>
			{
				var name = a.GetName().Name ?? "";
				return (name.Contains("beamable", StringComparison.OrdinalIgnoreCase) ||
				        name.Contains("Beamable", StringComparison.OrdinalIgnoreCase) ||
				        name == "BeamableMicroserviceBase") &&
				       !scannedAssemblies.Contains(a.FullName);
			})
			.ToList();

		foreach (var assembly in beamableAssemblies)
		{
			scannedAssemblies.Add(assembly.FullName);
			try
			{
				var docs = LoadXmlDocs(assembly);
				var alreadyCovered = new HashSet<Type>(
					assembly.GetTypes().Where(t => coveredTypeNames.Contains($"{t.Namespace}.{t.Name}"))
				);
				var utilityTypes = GetUtilityTypes(assembly, docs, alreadyCovered);
				foreach (var ut in utilityTypes)
				{
					ut.Platform = ClassifyPlatform(ut.Namespace);
					coveredTypeNames.Add($"{ut.Namespace}.{ut.TypeName}");
				}
				schema.UtilityTypes = schema.UtilityTypes.Concat(utilityTypes)
					.OrderBy(t => t.Namespace).ThenBy(t => t.TypeName).ToArray();
			}
			catch { /* Assembly may not be scannable in all contexts */ }
		}

		schema.UnrealTypeMappings = BuildUnrealTypeMappings();
		return schema;
	}

	static string ClassifyPlatform(string ns)
	{
		if (string.IsNullOrEmpty(ns)) return "Shared";
		if (ns.StartsWith("Beamable.Server")) return "MicroserviceOnly";
		return "Shared";
	}

	public override async Task<BeamableTypesSchema> GetResult(GenerateBeamableTypesSchemaCommandArgs args)
	{
		var schema = GenerateLive();

		if (!string.IsNullOrWhiteSpace(args.outputDir))
		{
			Directory.CreateDirectory(args.outputDir);
			await WriteSectionFiles(schema, args.outputDir);
		}
		else if (!string.IsNullOrWhiteSpace(args.outputPath))
		{
			var dir = Path.GetDirectoryName(args.outputPath);
			if (!string.IsNullOrEmpty(dir))
				Directory.CreateDirectory(dir);
			await File.WriteAllTextAsync(args.outputPath, JsonConvert.SerializeObject(schema, Formatting.Indented));
			Log.Information($"Wrote Beamable type schema to {args.outputPath}");
		}

		return schema;
	}

	static async Task WriteSectionFiles(BeamableTypesSchema schema, string outputDir)
	{
		var settings = new JsonSerializerSettings { Formatting = Formatting.Indented };

		// Split utility types by platform
		var sharedUtility = schema.UtilityTypes?.Where(t => t.Platform != "MicroserviceOnly").ToArray() ?? Array.Empty<UtilityTypeEntry>();
		var serverUtility = schema.UtilityTypes?.Where(t => t.Platform == "MicroserviceOnly").ToArray() ?? Array.Empty<UtilityTypeEntry>();

		var sections = new Dictionary<string, (string description, object data, int count)>
		{
			["content"] = ("C# content object types with [ContentType] attribute", schema.ContentTypes, schema.ContentTypes?.Length ?? 0),
			["federation"] = ("Federation interfaces (IFederatedLogin, IFederatedInventory, etc.)", schema.FederationTypes, schema.FederationTypes?.Length ?? 0),
			["utility-shared"] = ("Shared C# utility types from Beamable SDK (usable by Unity and Microservices)", sharedUtility, sharedUtility.Length),
			["utility-server"] = ("Microservice-only C# types from Beamable SDK server assemblies", serverUtility, serverUtility.Length),
			["unreal"] = ("Unreal C++ to C# type mapping table", schema.UnrealTypeMappings, schema.UnrealTypeMappings?.Length ?? 0),
		};

		var index = new List<TypeSectionIndex>();

		foreach (var (name, (description, data, count)) in sections)
		{
			var filePath = Path.Combine(outputDir, $"beamable-types-{name}.json");
			await File.WriteAllTextAsync(filePath, JsonConvert.SerializeObject(data, settings));
			index.Add(new TypeSectionIndex { Name = name, Description = description, TypeCount = count });
		}

		var indexPath = Path.Combine(outputDir, "beamable-types-index.json");
		await File.WriteAllTextAsync(indexPath, JsonConvert.SerializeObject(index.ToArray(), settings));

		// Also write combined file for backward compatibility
		var combinedPath = Path.Combine(outputDir, "beamable-types.json");
		await File.WriteAllTextAsync(combinedPath, JsonConvert.SerializeObject(schema, settings));

		Log.Information($"Wrote {sections.Count} section files + index to {outputDir}");
	}

	static UnrealTypeMappingEntry[] BuildUnrealTypeMappings()
	{
		return new[]
		{
			new UnrealTypeMappingEntry { CppType = "uint8", CSharpEquivalent = "byte", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "int16", CSharpEquivalent = "short", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "int32", CSharpEquivalent = "int", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "int64", CSharpEquivalent = "long", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "bool", CSharpEquivalent = "bool", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "float", CSharpEquivalent = "float", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "double", CSharpEquivalent = "double", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "FString", CSharpEquivalent = "string", Notes = "Serialized as JSON strings" },
			new UnrealTypeMappingEntry { CppType = "FText", CSharpEquivalent = "string", Notes = "Serialized as JSON strings" },
			new UnrealTypeMappingEntry { CppType = "FName", CSharpEquivalent = "string", Notes = "Serialized as JSON strings" },
			new UnrealTypeMappingEntry { CppType = "FGuid", CSharpEquivalent = "Guid", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "FDateTime", CSharpEquivalent = "DateTime", Notes = "" },
			new UnrealTypeMappingEntry { CppType = "FGameplayTag", CSharpEquivalent = "string", Notes = "Use FGameplayTag::RequestGameplayTag for deserialization" },
			new UnrealTypeMappingEntry { CppType = "FGameplayTagContainer", CSharpEquivalent = "string", Notes = "Use FGameplayTagContainer::FromExportString for deserialization" },
			new UnrealTypeMappingEntry { CppType = "TSoftObjectPtr<>", CSharpEquivalent = "string", Notes = "Serializes as FSoftObjectPath; empty string when None" },
			new UnrealTypeMappingEntry { CppType = "TArray<T>", CSharpEquivalent = "List<T> or T[]", Notes = "Works for any supported element type" },
			new UnrealTypeMappingEntry { CppType = "TMap<FString, V>", CSharpEquivalent = "Dictionary<string, V>", Notes = "Only FString keys supported" },
			new UnrealTypeMappingEntry { CppType = "FOptional<T>", CSharpEquivalent = "Optional<T>", Notes = "Not serialized when IsSet==false" },
			new UnrealTypeMappingEntry { CppType = "FBeamArray", CSharpEquivalent = "ArrayOf", Notes = "For nested TArray<TArray<>> with Blueprint support" },
			new UnrealTypeMappingEntry { CppType = "FBeamMap", CSharpEquivalent = "MapOf", Notes = "For nested TMap<,TMap<>> with Blueprint support" },
			new UnrealTypeMappingEntry { CppType = "FBeamJsonSerializableUStruct", CSharpEquivalent = "C# class", Notes = "Serialized as JSON object" },
			new UnrealTypeMappingEntry { CppType = "IBeamJsonSerializableUObject", CSharpEquivalent = "C# class", Notes = "Use DefaultToInstanced, EditInlineNew for content UObjects" },
		};
	}

	static BeamableTypesSchema BuildSchema(Assembly assembly, Dictionary<string, string> xmlDocs)
	{
		var contentTypeAttrType = typeof(ContentTypeAttribute);
		var formerlyAttrType = typeof(ContentFormerlySerializedAsAttribute);

		var allTypes = assembly.GetTypes();

		var entries = allTypes
			.Select(t => new
			{
				Type = t,
				ContentAttr = t.GetCustomAttribute<ContentTypeAttribute>(),
				FormerlyAttrs = t.GetCustomAttributes(formerlyAttrType)
					.Cast<ContentFormerlySerializedAsAttribute>()
					.Select(a => a.OldTypeName)
					.ToArray()
			})
			.Where(x => x.ContentAttr != null)
			.OrderBy(x => x.ContentAttr.TypeName)
			.Select(x => new ContentTypeEntry
			{
				TypeName = x.ContentAttr.TypeName,
				ClassName = x.Type.Name,
				Namespace = x.Type.Namespace ?? "",
				Summary = CleanSummary(xmlDocs, "T:" + x.Type.FullName),
				FormerlyKnownAs = x.FormerlyAttrs,
				Fields = GetFields(x.Type, xmlDocs),
			})
			.ToArray();

		// Types already captured in ContentTypes or FederationTypes — skip in UtilityTypes
		var coveredTypes = new HashSet<Type>(allTypes.Where(t => t.GetCustomAttribute<ContentTypeAttribute>() != null));
		coveredTypes.Add(typeof(IFederation));
		coveredTypes.Add(typeof(IFederationId));
		foreach (var ft in allTypes.Where(t => t.IsInterface && t.GetInterfaces().Contains(typeof(IFederation))))
			coveredTypes.Add(ft);

		var version = assembly.GetName().Version?.ToString() ?? "unknown";

		return new BeamableTypesSchema
		{
			GeneratedAt = DateTimeOffset.UtcNow.ToString("o"),
			AssemblyVersion = version,
			ContentTypes = entries,
			FederationTypes = GetFederationTypes(assembly, xmlDocs),
			UtilityTypes = GetUtilityTypes(assembly, xmlDocs, coveredTypes),
		};
	}

	static ContentFieldEntry[] GetFields(Type type, Dictionary<string, string> xmlDocs)
	{
		// Walk the type hierarchy up to (but not including) ContentObject so we
		// capture fields defined in intermediate abstract base classes too.
		var stopAt = typeof(ContentObject);
		var result = new List<ContentFieldEntry>();
		var current = type;

		while (current != null && current != stopAt && current != typeof(object))
		{
			var declared = current
				.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.Where(f => f.GetCustomAttribute<NonSerializedAttribute>() == null)
				.Select(f => new ContentFieldEntry
				{
					Name = f.Name,
					Type = FriendlyTypeName(f.FieldType),
					Summary = CleanSummary(xmlDocs, $"F:{f.DeclaringType!.FullName}.{f.Name}"),
				});

			result.InsertRange(0, declared); // keep declaration order, base fields first
			current = current.BaseType;
		}

		return result.ToArray();
	}

	static FederationTypeEntry[] GetFederationTypes(Assembly assembly, Dictionary<string, string> xmlDocs)
	{
		var federationInterface = typeof(IFederation);
		var federationIdInterface = typeof(IFederationId);

		var entries = assembly.GetTypes()
			.Where(t => t.IsInterface && t != federationInterface &&
			            t.GetInterfaces().Contains(federationInterface))
			.OrderBy(t => t.Name)
			.Select(t => BuildFederationEntry(t, xmlDocs))
			.ToList();

		// Prepend IFederationId — it is the type parameter constraint for all federation interfaces
		entries.Insert(0, new FederationTypeEntry
		{
			InterfaceName = federationIdInterface.Name,
			Namespace = federationIdInterface.Namespace ?? "",
			Summary = CleanSummary(xmlDocs, "T:" + federationIdInterface.FullName) +
			          " Implement this on a concrete class and annotate it with [FederationId(\"yourId\")] and a parameterless constructor to serve as the T type argument for any IFederated* interface.",
			GenericConstraint = "",
			Methods = Array.Empty<FederationMethodEntry>()
		});

		return entries.ToArray();
	}

	static FederationTypeEntry BuildFederationEntry(Type t, Dictionary<string, string> xmlDocs)
	{
		var methods = t
			.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
			.Select(m => new FederationMethodEntry
			{
				Name = m.Name,
				ReturnType = FriendlyTypeName(m.ReturnType),
				Summary = FindMethodSummary(xmlDocs, t.FullName!, m.Name),
				Parameters = m.GetParameters()
					.Select(p => new FederationMethodParam { Name = p.Name!, Type = FriendlyTypeName(p.ParameterType) })
					.ToArray()
			})
			.ToArray();

		return new FederationTypeEntry
		{
			InterfaceName = FriendlyTypeName(t),
			Namespace = t.Namespace ?? "",
			Summary = CleanSummary(xmlDocs, "T:" + t.FullName),
			GenericConstraint = GetGenericConstraint(t),
			Methods = methods
		};
	}

	static string GetGenericConstraint(Type type)
	{
		if (!type.IsGenericTypeDefinition) return "";
		return string.Join(" ", type.GetGenericArguments()
			.Select(tp =>
			{
				var constraints = tp.GetGenericParameterConstraints()
					.Select(c => FriendlyTypeName(c))
					.ToList();
				if (tp.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
					constraints.Add("new()");
				return constraints.Count > 0 ? $"where {tp.Name} : {string.Join(", ", constraints)}" : "";
			})
			.Where(s => s.Length > 0));
	}

	// Fuzzy method-summary lookup: matches any key that starts with "M:TypeName.MethodName("
	// so we don't need to reconstruct the full XML parameter signature.
	static string FindMethodSummary(Dictionary<string, string> xmlDocs, string typeFullName, string methodName)
	{
		var prefix = $"M:{typeFullName}.{methodName}(";
		foreach (var kvp in xmlDocs)
			if (kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
				return kvp.Value;
		return "";
	}

	// Auto-generated OpenAPI model types — too numerous and internal to be useful in the schema
	static readonly string[] SkippedNamespacePrefixes = { "Beamable.Api.Autogenerated" };

	static UtilityTypeEntry[] GetUtilityTypes(Assembly assembly, Dictionary<string, string> xmlDocs, HashSet<Type> alreadyCovered)
	{
		return assembly.GetTypes()
			.Where(t =>
				(t.IsPublic || (t.IsNested && t.IsNestedPublic)) &&
				!t.Name.Contains('<') &&
				!SkippedNamespacePrefixes.Any(prefix => (t.Namespace ?? "").StartsWith(prefix)) &&
				!alreadyCovered.Contains(t))
			.OrderBy(t => t.Namespace)
			.ThenBy(t => t.Name)
			.Select(t =>
			{
				try { return BuildUtilityEntry(t, xmlDocs); }
				catch { return null; }
			})
			.Where(e => e != null)
			.ToArray();
	}

	static UtilityTypeEntry BuildUtilityEntry(Type t, Dictionary<string, string> xmlDocs)
	{
		var typeName = t.IsNested && t.DeclaringType != null
			? t.DeclaringType.Name + "." + t.Name
			: FriendlyTypeName(t);

		string[] enumValues = Array.Empty<string>();
		UtilityMemberEntry[] members = Array.Empty<UtilityMemberEntry>();
		UtilityMethodEntry[] methods = Array.Empty<UtilityMethodEntry>();

		if (t.IsEnum)
		{
			enumValues = Enum.GetNames(t);
		}
		else
		{
			members = GetUtilityMembers(t, xmlDocs);
			methods = GetUtilityMethods(t, xmlDocs);
		}

		return new UtilityTypeEntry
		{
			TypeName = typeName,
			Namespace = t.Namespace ?? "",
			Kind = GetTypeKind(t),
			Summary = CleanSummary(xmlDocs, "T:" + t.FullName),
			Members = members,
			Methods = methods,
			EnumValues = enumValues
		};
	}

	static string GetTypeKind(Type t)
	{
		if (t.IsEnum) return "enum";
		if (t.IsInterface) return "interface";
		if (t.IsValueType) return "struct";
		if (t.IsAbstract && t.IsSealed) return "static class";
		if (t.IsAbstract) return "abstract class";
		return "class";
	}

	static UtilityMemberEntry[] GetUtilityMembers(Type t, Dictionary<string, string> xmlDocs)
	{
		var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		var fields = t.GetFields(flags)
			.Where(f => !f.Name.Contains('<') && !f.IsLiteral)
			.Select(f => new UtilityMemberEntry
			{
				Name = f.Name,
				Type = FriendlyTypeName(f.FieldType),
				IsStatic = f.IsStatic,
				MemberKind = "field",
				Summary = CleanSummary(xmlDocs, $"F:{f.DeclaringType!.FullName}.{f.Name}")
			});

		var properties = t.GetProperties(flags)
			.Where(p => p.GetMethod?.IsPublic == true && !p.Name.Contains('<'))
			.Select(p => new UtilityMemberEntry
			{
				Name = p.Name,
				Type = FriendlyTypeName(p.PropertyType),
				IsStatic = p.GetMethod?.IsStatic ?? false,
				MemberKind = "property",
				Summary = CleanSummary(xmlDocs, $"P:{p.DeclaringType!.FullName}.{p.Name}")
			});

		return fields.Concat(properties).OrderBy(m => m.Name).ToArray();
	}

	static UtilityMethodEntry[] GetUtilityMethods(Type t, Dictionary<string, string> xmlDocs)
	{
		var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

		return t.GetMethods(flags)
			.Where(m => !m.IsSpecialName && !m.Name.Contains('<'))
			.Select(m => new UtilityMethodEntry
			{
				Name = m.Name,
				ReturnType = FriendlyTypeName(m.ReturnType),
				IsStatic = m.IsStatic,
				Summary = FindMethodSummary(xmlDocs, t.FullName!, m.Name),
				Parameters = m.GetParameters()
					.Select(p => new UtilityParam { Name = p.Name ?? "_", Type = FriendlyTypeName(p.ParameterType) })
					.ToArray()
			})
			.ToArray();
	}

	static string FriendlyTypeName(Type type)
	{
		if (!type.IsGenericType)
		{
			return type.Name switch
			{
				"String" => "string",
				"Int32" => "int",
				"Int64" => "long",
				"Single" => "float",
				"Double" => "double",
				"Boolean" => "bool",
				_ => type.Name,
			};
		}

		var tick = type.Name.IndexOf('`');
		var baseName = tick >= 0 ? type.Name[..tick] : type.Name;
		var args = string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName));
		return $"{baseName}<{args}>";
	}

	static Dictionary<string, string> LoadXmlDocs(Assembly assembly)
	{
		var dict = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(assembly.Location))
			return dict;

		var xmlPath = Path.ChangeExtension(assembly.Location, ".xml");
		if (!File.Exists(xmlPath))
			return dict;

		try
		{
			var doc = XDocument.Load(xmlPath);
			foreach (var member in doc.Descendants("member"))
			{
				var name = member.Attribute("name")?.Value;
				if (name == null) continue;
				var summary = member.Element("summary");
				if (summary == null) continue;
				dict[name] = CleanXmlText(summary);
			}
		}
		catch
		{
			// XML doc is best-effort — don't fail the command if it can't be read
		}

		return dict;
	}

	static string CleanSummary(Dictionary<string, string> xmlDocs, string memberKey)
		=> xmlDocs.TryGetValue(memberKey, out var s) ? s : "";

	static string CleanXmlText(XElement element)
	{
		// Strip child XML elements (inheritdoc, see, para, etc.), keep inner text only
		var text = string.Concat(element.Nodes()
			.Select(n => n is XText t ? t.Value : n is XElement e ? e.Value : ""));

		// Collapse whitespace runs and trim
		return System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\s{2,}", " ");
	}
}
