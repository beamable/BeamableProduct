using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace cli.Unreal;

public class UnrealSourceGenerator : SwaggerService.ISourceGenerator
{
	public const string UNREAL_STRING = "FString";
	public const string UNREAL_BYTE = "int8";
	public const string UNREAL_SHORT = "int16";
	public const string UNREAL_INT = "int32";
	public const string UNREAL_LONG = "int64";
	public const string UNREAL_BOOL = "bool";
	public const string UNREAL_FLOAT = "float";
	public const string UNREAL_DOUBLE = "double";
	public const string UNREAL_GUID = "FGuid";
	public const string UNREAL_OPTIONAL = "FOptional";
	public const string UNREAL_OPTIONAL_STRING = $"{UNREAL_OPTIONAL}String";
	public const string UNREAL_OPTIONAL_BYTE = $"{UNREAL_OPTIONAL}Int8";
	public const string UNREAL_OPTIONAL_SHORT = $"{UNREAL_OPTIONAL}Int16";
	public const string UNREAL_OPTIONAL_INT = $"{UNREAL_OPTIONAL}Int32";
	public const string UNREAL_OPTIONAL_LONG = $"{UNREAL_OPTIONAL}Int64";
	public const string UNREAL_OPTIONAL_BOOL = $"{UNREAL_OPTIONAL}Bool";
	public const string UNREAL_OPTIONAL_FLOAT = $"{UNREAL_OPTIONAL}Float";
	public const string UNREAL_OPTIONAL_DOUBLE = $"{UNREAL_OPTIONAL}Double";
	public const string UNREAL_OPTIONAL_GUID = $"{UNREAL_OPTIONAL}Guid";
	public const string UNREAL_ARRAY = "TArray";
	public const string UNREAL_OPTIONAL_ARRAY = $"{UNREAL_OPTIONAL}ArrayOf";
	public const string UNREAL_WRAPPER_ARRAY = "FArrayOf";
	public const string UNREAL_MAP = $"TMap";
	public const string UNREAL_OPTIONAL_MAP = $"{UNREAL_OPTIONAL}MapOf";
	public const string UNREAL_WRAPPER_MAP = "FMapOf";
	public const string UNREAL_U_ENUM_PREFIX = "E";
	public const string UNREAL_U_OBJECT_PREFIX = "U";
	public const string UNREAL_U_STRUCT_PREFIX = "F";

	// Start of Semantic Types
	public const string UNREAL_U_SEMTYPE_CID = "FBeamCid";
	public const string UNREAL_U_SEMTYPE_PID = "FBeamPid";
	public const string UNREAL_U_SEMTYPE_ACCOUNTID = "FBeamAccountId";
	public const string UNREAL_U_SEMTYPE_GAMERTAG = "FBeamGamerTag";
	public const string UNREAL_U_SEMTYPE_CONTENTMANIFESTID = "FBeamContentManifestId";
	public const string UNREAL_U_SEMTYPE_CONTENTID = "FBeamContentId";
	public const string UNREAL_U_SEMTYPE_STATSTYPE = "FBeamStatsType";

	public const string UNREAL_OPTIONAL_U_SEMTYPE_CID = $"{UNREAL_OPTIONAL}BeamCid";
	public const string UNREAL_OPTIONAL_U_SEMTYPE_PID = $"{UNREAL_OPTIONAL}BeamPid";
	public const string UNREAL_OPTIONAL_U_SEMTYPE_ACCOUNTID = $"{UNREAL_OPTIONAL}BeamAccountId";
	public const string UNREAL_OPTIONAL_U_SEMTYPE_GAMERTAG = $"{UNREAL_OPTIONAL}BeamGamerTag";
	public const string UNREAL_OPTIONAL_U_SEMTYPE_CONTENTMANIFESTID = $"{UNREAL_OPTIONAL}BeamContentManifestId";
	public const string UNREAL_OPTIONAL_U_SEMTYPE_CONTENTID = $"{UNREAL_OPTIONAL}BeamContentId";
	public const string UNREAL_OPTIONAL_U_SEMTYPE_STATSTYPE = $"{UNREAL_OPTIONAL}BeamStatsType";

	public static readonly List<string> UNREAL_ALL_SEMTYPES = new()
	{
		UNREAL_U_SEMTYPE_CID,
		UNREAL_U_SEMTYPE_PID,
		UNREAL_U_SEMTYPE_ACCOUNTID,
		UNREAL_U_SEMTYPE_GAMERTAG,
		UNREAL_U_SEMTYPE_CONTENTMANIFESTID,
		UNREAL_U_SEMTYPE_CONTENTID,
		UNREAL_U_SEMTYPE_STATSTYPE,
	};

	public static readonly List<string> UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES = new()
	{
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_CID),
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_PID),
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_ACCOUNTID),
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_GAMERTAG),
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_CONTENTMANIFESTID),
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_CONTENTID),
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_STATSTYPE),
	};
	// End of Semantic Types

	public const string UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE = "UBeamPlainTextResponseBody*";
	private const string EXTENSION_BEAMABLE_SEMANTIC_TYPE = "x-beamable-semantic-type";

	/// <summary>
	/// Exists so we don't keep reallocating while building the field names.
	/// </summary>
	private static readonly StringBuilder kSchemaGenerationBuilder = new(2048);

	/// <summary>
	/// Exists so we don't keep reallocating while building the field names.
	/// </summary>
	private static readonly StringBuilder kSemTypeDeclarationPointsLog = new(4096);

	public static readonly Dictionary<string, string> NAMESPACED_TYPES_OVERRIDES = new() { { "Player", "PlayerId" }, { "DeleteRole", "DeleteRoleRequestBody" } };
	public static readonly Dictionary<string, string> NAMESPACED_ENDPOINT_OVERRIDES = new() { { "PostToken", "Authenticate" } };

	public List<GeneratedFileDescriptor> Generate(IGenerationContext context)
	{
		var outputFiles = new List<GeneratedFileDescriptor>(16);

		kSemTypeDeclarationPointsLog.Clear();
		kSemTypeDeclarationPointsLog.AppendLine("Handle,UProperty,SerializationType");

		// Build a list of dictionaries of schemas whose names appear in the list more than once.
		IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata = context.OrderedSchemas;
		var schemaNameOverlaps = new Dictionary<string, List<NamedOpenApiSchema>>(namedOpenApiSchemata.Count);
		BuildSchemaNameOverlapsMap(namedOpenApiSchemata, schemaNameOverlaps);

		// Build a list of dictionaries of endpoint names whose that are declared in more than one service.
		BuildEndpointNameCollidedMaps(namedOpenApiSchemata, context.Documents, out var globalEndpointNameCollisions, out var perSubsystemCollisions);

		// Go through all properties of all schemas and see if they are required or not
		var fieldSchemaRequiredMap = new Dictionary<string, bool>(namedOpenApiSchemata.Count);
		var fieldSemanticTypesUnderlyingTypeMap = new Dictionary<string, string>(namedOpenApiSchemata.Count);
		BuildRequiredFieldMaps(namedOpenApiSchemata, context.Documents, schemaNameOverlaps, globalEndpointNameCollisions, fieldSchemaRequiredMap);
		BuildSemanticTypesUnderlyingTypeMaps(namedOpenApiSchemata, context.Documents, schemaNameOverlaps, globalEndpointNameCollisions, fieldSemanticTypesUnderlyingTypeMap);


		// Build the data required to generate all subsystems and their respective endpoints
		var subsystemDeclarations = new List<UnrealApiSubsystemDeclaration>(context.Documents.Count);
		var unrealTypesUsedAsResponses = new HashSet<string>();
		BuildSubsystemDeclarations(context.Documents, globalEndpointNameCollisions, perSubsystemCollisions, schemaNameOverlaps, fieldSchemaRequiredMap, fieldSemanticTypesUnderlyingTypeMap,
			subsystemDeclarations,
			unrealTypesUsedAsResponses);

		// Build the data required to generate all serializable types, enums, optionals, array and map wrapper types.
		// Array and Map Wrapper types are required due to UE's TMap and TArray not supporting nested data structures. As in, TArray<TArray<int>> doesn't work --- but TArray<FArrayOfInt> does. 
		BuildSerializableTypeDeclarations(namedOpenApiSchemata, schemaNameOverlaps, fieldSchemaRequiredMap, fieldSemanticTypesUnderlyingTypeMap, unrealTypesUsedAsResponses,
			out var serializableTypes,
			out var enumTypes,
			out var optionalTypes,
			out var arrayWrapperTypes,
			out var mapWrapperTypes);

		// Generate the actual files we'll need from the data we've built.
		var processDictionary = new Dictionary<string, string>(16);

		// Generate all Optional Type Files
		var optionalDeclarations = optionalTypes.Select(ot =>
		{
			ot.BakeIntoProcessMap(processDictionary);
			var headerDeclaration = UnrealOptionalDeclaration.OPTIONAL_HEADER_DECL.ProcessReplacement(processDictionary);
			var cppDeclaration = UnrealOptionalDeclaration.OPTIONAL_CPP_DECL.ProcessReplacement(processDictionary);
			var bpLibraryHeader = UnrealOptionalDeclaration.OPTIONAL_LIBRARY_HEADER_DECL.ProcessReplacement(processDictionary);
			var bpLibraryCpp = UnrealOptionalDeclaration.OPTIONAL_LIBRARY_CPP_DECL.ProcessReplacement(processDictionary);

			processDictionary.Clear();
			return (headerDeclaration, cppDeclaration, bpLibraryHeader, bpLibraryCpp);
		});
		outputFiles.AddRange(optionalDeclarations.SelectMany((s, idx) =>
		{
			var optionalType = optionalTypes[idx];
			return new[]
			{
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/Optionals/{optionalType.NamespacedTypeName}.h", Content = s.headerDeclaration },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/Optionals/{optionalType.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/Optionals/{optionalType.NamespacedTypeName}Library.h", Content = s.bpLibraryHeader },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/Optionals/{optionalType.NamespacedTypeName}Library.cpp", Content = s.bpLibraryCpp },
			};
		}));

		// Generate Array Wrapper Type Files
		var arrayWrapperDeclarations = arrayWrapperTypes.Select(ot =>
		{
			ot.BakeIntoProcessMap(processDictionary);
			var headerDeclaration = UnrealWrapperContainerDeclaration.ARRAY_WRAPPER_HEADER_DECL.ProcessReplacement(processDictionary);
			var cppDeclaration = UnrealWrapperContainerDeclaration.ARRAY_WRAPPER_CPP_DECL.ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return (headerDeclaration, cppDeclaration);
		});
		outputFiles.AddRange(arrayWrapperDeclarations.SelectMany((s, idx) =>
		{
			var arrayWrapperType = arrayWrapperTypes[idx];
			return new[]
			{
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/Arrays/{arrayWrapperType.NamespacedTypeName}.h", Content = s.headerDeclaration },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/Arrays/{arrayWrapperType.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
			};
		}));

		// Generate Map Wrapper Type Files
		var mapWrapperDeclarations = mapWrapperTypes.Select(ot =>
		{
			ot.BakeIntoProcessMap(processDictionary);
			var headerDeclaration = UnrealWrapperContainerDeclaration.MAP_WRAPPER_HEADER_DECL.ProcessReplacement(processDictionary);
			var cppDeclaration = UnrealWrapperContainerDeclaration.MAP_WRAPPER_CPP_DECL.ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return (headerDeclaration, cppDeclaration);
		});
		outputFiles.AddRange(mapWrapperDeclarations.SelectMany((s, idx) =>
		{
			var mapWrapperType = mapWrapperTypes[idx];
			return new[]
			{
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/Maps/{mapWrapperType.NamespacedTypeName}.h", Content = s.headerDeclaration },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/Maps/{mapWrapperType.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
			};
		}));

		// Generate all enum type declarations
		var enumTypesCode = enumTypes.Select(d =>
		{
			d.BakeIntoProcessMap(processDictionary);
			var header = UnrealEnumDeclaration.U_ENUM_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return header;
		});
		outputFiles.AddRange(enumTypesCode.SelectMany((s, idx) =>
		{
			var enumType = enumTypes[idx];
			return new[] { new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/Enums/{enumType.NamespacedTypeName}.h", Content = s, }, };
		}));

		// Generate all serializable types
		var serializableTypesCode = serializableTypes.Select(d =>
		{
			d.IntoProcessMap(processDictionary);

			var serializableHeader = UnrealSerializableTypeDeclaration.SERIALIZABLE_TYPE_HEADER.ProcessReplacement(processDictionary);
			var serializableCpp = UnrealSerializableTypeDeclaration.SERIALIZABLE_TYPE_CPP.ProcessReplacement(processDictionary);
			var serializableTypeLibraryHeader = UnrealSerializableTypeDeclaration.SERIALIZABLE_TYPES_LIBRARY_HEADER.ProcessReplacement(processDictionary);
			var serializableTypeLibraryCpp = UnrealSerializableTypeDeclaration.SERIALIZABLE_TYPES_LIBRARY_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			return (serializableHeader, serializableCpp, serializableTypeLibraryHeader, serializableTypeLibraryCpp);
		});
		outputFiles.AddRange(serializableTypesCode.SelectMany((s, idx) =>
		{
			var serializedTypeName = serializableTypes[idx];
			return new[]
			{
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/{serializedTypeName.NamespacedTypeName}.h", Content = s.serializableHeader, },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/{serializedTypeName.NamespacedTypeName}.cpp", Content = s.serializableCpp, },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/{serializedTypeName.NamespacedTypeName}Library.h", Content = s.serializableTypeLibraryHeader, },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/{serializedTypeName.NamespacedTypeName}Library.cpp", Content = s.serializableTypeLibraryCpp, },
			};
		}));


		// Subsystem Declarations
		var subsystemsCode = subsystemDeclarations.Select(sd =>
		{
			sd.IntoProcessMapHeader(processDictionary);
			var subsystemHeader = UnrealApiSubsystemDeclaration.U_SUBSYSTEM_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			sd.IntoProcessMapCpp(processDictionary);
			var subsystemCpp = UnrealApiSubsystemDeclaration.U_SUBSYSTEM_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			return (subsystemHeader, subsystemCpp);
		});
		outputFiles.AddRange(subsystemsCode.SelectMany((sc, i) =>
		{
			var decl = subsystemDeclarations[i];
			return new[]
			{
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Public/AutoGen/SubSystems/Beam{decl.SubsystemName}Api.h", Content = sc.subsystemHeader },
				new GeneratedFileDescriptor() { FileName = $"BeamableCore/Private/AutoGen/SubSystems/Beam{decl.SubsystemName}Api.cpp", Content = sc.subsystemCpp },
			};
		}));

		var subsystemEndpoints = subsystemDeclarations.SelectMany(sd => sd.GetAllEndpoints()).ToList();
		var subsystemEndpointsCode = subsystemEndpoints.Select(se =>
		{
			se.IntoProcessMap(processDictionary, serializableTypes);
			var endpointHeader = UnrealEndpointDeclaration.U_ENDPOINT_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			se.IntoProcessMap(processDictionary, serializableTypes);
			var endpointCpp = UnrealEndpointDeclaration.U_ENDPOINT_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			se.IntoProcessMap(processDictionary, serializableTypes);
			var beamFlowNodeHeader = UnrealEndpointDeclaration.BEAM_FLOW_BP_NODE_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			se.IntoProcessMap(processDictionary, serializableTypes);
			var beamFlowNodeCpp = UnrealEndpointDeclaration.BEAM_FLOW_BP_NODE_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();


			return (endpointHeader, endpointCpp, beamFlowNodeHeader, beamFlowNodeCpp);
		});
		outputFiles.AddRange(subsystemEndpointsCode.SelectMany((sc, i) =>
		{
			var decl = subsystemEndpoints[i];
			return new[]
			{
				new GeneratedFileDescriptor()
				{
					FileName = $"BeamableCore/Public/AutoGen/SubSystems/{decl.NamespacedOwnerServiceName}/{decl.GlobalNamespacedEndpointName}Request.h", Content = sc.endpointHeader
				},
				new GeneratedFileDescriptor()
				{
					FileName = $"BeamableCore/Private/AutoGen/SubSystems/{decl.NamespacedOwnerServiceName}/{decl.GlobalNamespacedEndpointName}Request.cpp", Content = sc.endpointCpp
				},
				new GeneratedFileDescriptor()
				{
					FileName = $"BeamableCoreBlueprintNodes/Public/BeamFlow/ApiRequest/AutoGen/{decl.NamespacedOwnerServiceName}/K2BeamNode_ApiRequest_{decl.GlobalNamespacedEndpointName}.h",
					Content = sc.beamFlowNodeHeader
				},
				new GeneratedFileDescriptor()
				{
					FileName =
						$"BeamableCoreBlueprintNodes/Private/BeamFlow/ApiRequest/AutoGen/{decl.NamespacedOwnerServiceName}/K2BeamNode_ApiRequest_{decl.GlobalNamespacedEndpointName}.cpp",
					Content = sc.beamFlowNodeCpp
				},
			};
		}));

		// Generate Template Specializations file ---
		// We need this file so that we can reduce the size of the header for the BeamBackend class (by separating the implementation of it's templated methods into its cpp file).
		// The thinking is that this is preferable to adding a ~3k LOC header to every class that wants to make a request due to compile-time bloat.
		// This is less of a problem for our users since we should aim for most of their Beamable-related work in UE to happen in BP land --- however, it is a big problem for us.
		// This is only a decent solution because we ONLY support making requests to beamable via our BeamBackend class.
		// TODO: In the future, this may change. If that happens, we need to allow users to declare their own template specializations and have a Pre-Processor flag to include that file as well.
		var templateSpecializationsCode = new StringBuilder(4096);
		templateSpecializationsCode.Append("#include \"BeamBackend/BeamBackend.h\"\n");
		templateSpecializationsCode.Append(string.Join("\n",
			subsystemEndpoints.Select(e => $@"#include ""AutoGen/SubSystems/{e.NamespacedOwnerServiceName}/{e.GlobalNamespacedEndpointName}Request.h""")));
		foreach (var unrealEndpointDeclaration in subsystemEndpoints)
		{
			unrealEndpointDeclaration.IntoProcessMap(processDictionary);
			templateSpecializationsCode.Append(UnrealEndpointDeclaration.U_ENDPOINT_TEMPLATE_SPECIALIZATIONS.ProcessReplacement(processDictionary));
			processDictionary.Clear();
		}

		outputFiles.Add(new GeneratedFileDescriptor() { FileName = "BeamableCore/Private/AutoGen/Special/TemplateSpecializations.cpp", Content = templateSpecializationsCode.ToString() });

		// Prints out all the identified semtype declarations
		Console.WriteLine(kSemTypeDeclarationPointsLog.ToString());
		return outputFiles;
	}

	private static void BuildSerializableTypeDeclarations(IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata, Dictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps,
		Dictionary<string, bool> fieldSchemaRequiredMap, Dictionary<string, string> fieldSemanticTypesUnderlyingTypeMap, HashSet<string> unrealTypesUsedAsResponses,
		out List<UnrealSerializableTypeDeclaration> serializableTypes,
		out List<UnrealEnumDeclaration> enumTypes, out List<UnrealOptionalDeclaration> optionalTypes, out List<UnrealWrapperContainerDeclaration> arrayWrapperTypes,
		out List<UnrealWrapperContainerDeclaration> mapWrapperTypes)
	{
		serializableTypes = new List<UnrealSerializableTypeDeclaration>(namedOpenApiSchemata.Count);
		enumTypes = new List<UnrealEnumDeclaration>(namedOpenApiSchemata.Count);
		optionalTypes = new List<UnrealOptionalDeclaration>(namedOpenApiSchemata.Count);
		arrayWrapperTypes = new List<UnrealWrapperContainerDeclaration>(namedOpenApiSchemata.Count);
		mapWrapperTypes = new List<UnrealWrapperContainerDeclaration>(namedOpenApiSchemata.Count);

		// Allocate a list to keep track of all Schema types that we have already declared.
		var listOfDeclaredTypes = new List<string>(namedOpenApiSchemata.Count);
		foreach (var namedOpenApiSchema in namedOpenApiSchemata)
		{
			// We need to decide on whether we'll name the type simply or if we'll use their service title to augment the name.
			string schemaUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, namedOpenApiSchema.Document, namedOpenApiSchema.Schema);
			string schemaNamespacedType = GetNamespacedTypeNameFromUnrealType(schemaUnrealType);
			// Make sure we don't declare two types with the same name
			if (listOfDeclaredTypes.Contains(schemaNamespacedType)) continue;
			listOfDeclaredTypes.Add(schemaNamespacedType);

			// Find Enum declarations even within arrays and maps 
			// TODO: Declare this instead of serialized type
			if (schemaUnrealType.StartsWith(UNREAL_U_ENUM_PREFIX))
			{
				var enumDecl = new UnrealEnumDeclaration
				{
					UnrealTypeName = schemaUnrealType,
					NamespacedTypeName = schemaNamespacedType,
					EnumValues = namedOpenApiSchema.Schema.Enum.OfType<OpenApiString>().Select(v => v.Value).ToList()
				};

				enumTypes.Add(enumDecl);
			}
			else
			{
				// Prepare the data for injection in the template string.
				var serializableTypeDeclaration = new UnrealSerializableTypeDeclaration
				{
					NamespacedTypeName = schemaNamespacedType,
					PropertyIncludes = new List<string>(8),
					UPropertyDeclarations = new List<UnrealPropertyDeclaration>(8),
					JsonUtilsInclude = "",
					IsSomeRequestsResponseBody = unrealTypesUsedAsResponses.Contains(schemaUnrealType),
				};

				foreach ((string fieldName, OpenApiSchema fieldSchema) in namedOpenApiSchema.Schema.Properties)
				{
					var handle = GetFieldDeclarationHandle(schemaNamespacedType, fieldName);
					// see schema type and format
					var unrealType = GetUnrealTypeFromSchema(schemaNameOverlaps, fieldSchemaRequiredMap, namedOpenApiSchema.Document, handle, fieldSchema);
					if (string.IsNullOrEmpty(unrealType))
					{
						Console.WriteLine($"Skipping unreal type for {handle} cause not supported yet!");
						continue;
					}

					var propertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealType, fieldName, kSchemaGenerationBuilder);
					var nonOptionalUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, namedOpenApiSchema.Document, fieldSchema);
					var propertyDisplayName = propertyName;
					var uPropertyDeclarationData = new UnrealPropertyDeclaration
					{
						PropertyUnrealType = unrealType,
						PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealType),
						PropertyName = propertyName,
						PropertyDisplayName = propertyDisplayName.SpaceOutOnUpperCase(),
						RawFieldName = fieldName,
						NonOptionalTypeName = nonOptionalUnrealType,
					};

					if (fieldSemanticTypesUnderlyingTypeMap.TryGetValue(handle, out uPropertyDeclarationData.SemTypeSerializationType))
						kSemTypeDeclarationPointsLog.AppendLine($"{handle},{uPropertyDeclarationData.PropertyUnrealType},{uPropertyDeclarationData.SemTypeSerializationType}");

					if (unrealType.StartsWith(UNREAL_OPTIONAL))
					{
						optionalTypes.Add(new UnrealOptionalDeclaration()
						{
							UnrealTypeName = unrealType,
							NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(unrealType),
							UnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(unrealType),
							ValueUnrealTypeName = nonOptionalUnrealType,
							ValueNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(nonOptionalUnrealType),
							ValueUnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(nonOptionalUnrealType)
						});
					}

					if (nonOptionalUnrealType.StartsWith(UNREAL_MAP))
						uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOptionalUnrealType);
					if (nonOptionalUnrealType.StartsWith(UNREAL_ARRAY))
						uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOptionalUnrealType);

					if (unrealType.StartsWith(UNREAL_ARRAY) || unrealType.StartsWith(UNREAL_MAP) || unrealType.StartsWith(UNREAL_OPTIONAL) || unrealType.StartsWith(UNREAL_U_OBJECT_PREFIX) ||
						UNREAL_ALL_SEMTYPES.Contains(unrealType))
					{
						// We only need this include if we have any array, wrapper or optional types --- since this is a template it's worth not including it to keep compile times as small as we can have them.
						serializableTypeDeclaration.JsonUtilsInclude = string.IsNullOrEmpty(serializableTypeDeclaration.JsonUtilsInclude)
							? "#include \"Serialization/BeamJsonUtils.h\""
							: serializableTypeDeclaration.JsonUtilsInclude;
					}

					// Decide if we need to add the default value helper in order to parse primitive numeric types
					if (unrealType.StartsWith(UNREAL_BYTE) || unrealType.StartsWith(UNREAL_SHORT) || unrealType.StartsWith(UNREAL_INT) || unrealType.StartsWith(UNREAL_LONG) ||
						unrealType.StartsWith(UNREAL_FLOAT) || unrealType.StartsWith(UNREAL_DOUBLE))
					{
						serializableTypeDeclaration.DefaultValueHelpersInclude = string.IsNullOrEmpty(serializableTypeDeclaration.DefaultValueHelpersInclude)
							? "#include \"Misc/DefaultValueHelper.h\""
							: serializableTypeDeclaration.DefaultValueHelpersInclude;
					}

					// Wrapper types can only appear inside Non-Optional declarations of TMap/TArray ---
					// as such, we can find all of them by checking them against the NonOptionalUnrealType.
					if (nonOptionalUnrealType.Contains(UNREAL_WRAPPER_ARRAY))
					{
						var wrapper = new UnrealWrapperContainerDeclaration();

						// If it's a TMap we want the second parameter, if it's an array we want the first template parameter.
						wrapper.UnrealTypeName = nonOptionalUnrealType.StartsWith(UNREAL_MAP)
							? UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOptionalUnrealType)
							: UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOptionalUnrealType);
						wrapper.ValueUnrealTypeName = GetWrappedUnrealTypeFromUnrealWrapperType(wrapper.UnrealTypeName);

						wrapper.NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(wrapper.UnrealTypeName);
						wrapper.UnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(wrapper.UnrealTypeName);

						wrapper.ValueNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(wrapper.ValueUnrealTypeName);
						wrapper.ValueUnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(wrapper.ValueUnrealTypeName);

						arrayWrapperTypes.Add(wrapper);
					}

					// Wrapper types can only appear inside Non-Optional declarations of TMap/TArray ---
					// as such, we can find all of them by checking them against the NonOptionalUnrealType.
					if (nonOptionalUnrealType.Contains(UNREAL_WRAPPER_MAP))
					{
						var wrapper = new UnrealWrapperContainerDeclaration();

						// If it's a TMap we want the second parameter, if it's an array we want the first template parameter.
						wrapper.UnrealTypeName = nonOptionalUnrealType.StartsWith(UNREAL_MAP)
							? UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOptionalUnrealType)
							: UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOptionalUnrealType);

						wrapper.ValueUnrealTypeName = GetWrappedUnrealTypeFromUnrealWrapperType(wrapper.UnrealTypeName);

						wrapper.NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(wrapper.UnrealTypeName);
						wrapper.UnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(wrapper.UnrealTypeName);

						wrapper.ValueNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(wrapper.ValueUnrealTypeName);
						wrapper.ValueUnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(wrapper.ValueUnrealTypeName);

						mapWrapperTypes.Add(wrapper);
					}

					serializableTypeDeclaration.PropertyIncludes.Add(GetIncludeStatementForUnrealType(unrealType));
					serializableTypeDeclaration.UPropertyDeclarations.Add(uPropertyDeclarationData);

					kSchemaGenerationBuilder.Clear();
				}

				// Remove any includes to yourself to guarantee no cyclical dependencies
				serializableTypeDeclaration.PropertyIncludes.Remove(GetIncludeStatementForUnrealType(schemaUnrealType));

				serializableTypes.Add(serializableTypeDeclaration);
			}
		}
	}


	/*
	 * GENERATION HELPER FUNCTIONS ---- THESE ARE SIMPLY HERE TO MAKE IT EASIER TO GO THROUGH THE GENERATION ALGORITHM AND TO DOCUMENT IMPORTANT CONCEPTS OF THE ALGORITHM.
	 * These could all be inlined and it's unlikely they would ever be used outside of the main generation algorithm's flow.
	 */

	/// <summary>
	/// Fills the given <paramref name="schemaNameOverlaps"/> dictionary with lists containing the named schema collisions across all services we are generating for.
	/// We use this dictionary to make sure we have the correct names for each named schema and also their property declarations. 
	/// </summary>
	private static void BuildSchemaNameOverlapsMap(IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata, Dictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps)
	{
		foreach (var namedOpenApiSchema in namedOpenApiSchemata)
		{
			if (!schemaNameOverlaps.TryGetValue(namedOpenApiSchema.Name, out var list))
			{
				list = new List<NamedOpenApiSchema>(1);
				schemaNameOverlaps.Add(namedOpenApiSchema.Name, list);
			}

			list.Add(namedOpenApiSchema);
		}
	}

	/// <summary>
	/// Generates two separate name collision maps related to each endpoint.
	/// <paramref name="globalEndpointNameCollisions"/> answers the question: "Does this endpoint name collides with any other endpoint from any other service?"
	/// <paramref name="perSubsystemCollisions"/> answers the question: "Does this endpoint name collide with any other endpoint from within the subsystem it'll exist inside of?"
	///
	/// The reason we have the first one is that UE does not support namespaces yet. 
	/// </summary>
	private static void BuildEndpointNameCollidedMaps(IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata, IReadOnlyList<OpenApiDocument> openApiDocuments,
		out Dictionary<string, bool> globalEndpointNameCollisions,
		out Dictionary<string, Dictionary<string, bool>> perSubsystemCollisions)
	{
		globalEndpointNameCollisions = new Dictionary<string, bool>(namedOpenApiSchemata.Count);
		var perNameDocuments = openApiDocuments.GroupBy(d =>
		{
			GetNamespacedServiceNameFromApiDoc(d.Info, out _, out var serviceName);
			return serviceName;
		}).ToDictionary(g => g.Key, g => g.ToList());

		perSubsystemCollisions = new Dictionary<string, Dictionary<string, bool>>(perNameDocuments.Count);
		foreach ((string serviceName, List<OpenApiDocument> documents) in perNameDocuments)
		{
			perSubsystemCollisions.Add(serviceName, new Dictionary<string, bool>(16));
			foreach (OpenApiDocument openApiDocument in documents)
			{
				GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out _);
				var isObjectService = serviceTitle.Contains("object", StringComparison.OrdinalIgnoreCase);

				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var endpointName = GetSubsystemNamespacedEndpointName(serviceName, isObjectService, operationType, endpointPath);

						// If it collides with another endpoint globally...
						if (globalEndpointNameCollisions.ContainsKey(endpointName))
							globalEndpointNameCollisions[endpointName] = true;
						else
							globalEndpointNameCollisions.Add(endpointName, false);

						// If it collides with another endpoint in this service...
						if (perSubsystemCollisions[serviceName].ContainsKey(endpointName))
							perSubsystemCollisions[serviceName][endpointName] = true;
						else
							perSubsystemCollisions[serviceName].Add(endpointName, false);
					}
				}
			}
		}
	}


	/// <summary>
	/// Goes through the <paramref name="namedOpenApiSchemata"/> and <paramref name="openApiDocuments"/> and, for each field of all relevant types in them, saves a flag that answers:
	/// "Is this type's field required?"
	///
	/// This flag uses <see cref="GetNamespacedSerializableTypeFromSchema"/> and <see cref="GetEndpointFieldOwner"/> in order to keep track which fields of which types are tied to each flag.
	/// </summary>
	private static void BuildRequiredFieldMaps(IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata, IReadOnlyList<OpenApiDocument> openApiDocuments,
		Dictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps, Dictionary<string, bool> globalEndpointNameCollisions, Dictionary<string, bool> outFieldSchemaRequiredMap)
	{
		foreach (var ns in namedOpenApiSchemata)
		{
			var properties = ns.Schema.Properties;
			foreach ((string fieldName, OpenApiSchema _) in properties)
			{
				var handle = GetFieldDeclarationHandle(GetNamespacedSerializableTypeFromSchema(schemaNameOverlaps, ns.Document, ns.Name, false), fieldName);
				outFieldSchemaRequiredMap.TryAdd(handle, ns.Schema.Required.Contains(fieldName));
			}

			foreach (var openApiDocument in openApiDocuments)
			{
				GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var isObjectService = serviceTitle.Contains("object", StringComparison.OrdinalIgnoreCase);

						foreach (var param in value.Parameters)
						{
							var paramOwnerId = GetEndpointFieldOwner(globalEndpointNameCollisions, serviceName, isObjectService, operationType, endpointPath);
							var handle = GetFieldDeclarationHandle(paramOwnerId, $"{param.Name}");
							outFieldSchemaRequiredMap.TryAdd(handle, param.Required);
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Goes through the <paramref name="namedOpenApiSchemata"/> and <paramref name="openApiDocuments"/> and, for each field of all relevant types in them, saves a flag that answers:
	/// "Is this type's field required?"
	///
	/// This flag uses <see cref="GetNamespacedSerializableTypeFromSchema"/> and <see cref="GetEndpointFieldOwner"/> in order to keep track which fields of which types are tied to each flag.
	/// </summary>
	private static void BuildSemanticTypesUnderlyingTypeMaps(IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata, IReadOnlyList<OpenApiDocument> openApiDocuments,
		Dictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps, Dictionary<string, bool> globalEndpointNameCollisions, Dictionary<string, string> outFieldSemTypeUnderlyingTypeMap)
	{
		foreach (var ns in namedOpenApiSchemata)
		{
			var properties = ns.Schema.Properties;
			foreach ((string fieldName, OpenApiSchema fieldSchema) in properties)
			{
				var handle = GetFieldDeclarationHandle(GetNamespacedSerializableTypeFromSchema(schemaNameOverlaps, ns.Document, ns.Name, false), fieldName);

				// Array fields
				if (fieldSchema.Type == "array")
				{
					var isReference = fieldSchema.Items.Reference != null;
					var arrayTypeSchema = isReference ? fieldSchema.Items.GetEffective(ns.Document) : fieldSchema.Items;
					if (arrayTypeSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
					{
						var arraySerializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, ns.Document, arrayTypeSchema, UnrealTypeGetFlags.NeverSemanticType);
						outFieldSemTypeUnderlyingTypeMap.TryAdd(handle, arraySerializationUnrealType);
						continue;
					}
				}

				// Map case
				if (fieldSchema.Type == "object" && fieldSchema.Reference == null && fieldSchema.AdditionalPropertiesAllowed)
				{
					if (fieldSchema.AdditionalProperties.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
					{
						var mapSerializationUnrealType =
							GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, ns.Document, fieldSchema.AdditionalProperties, UnrealTypeGetFlags.NeverSemanticType);
						outFieldSemTypeUnderlyingTypeMap.TryAdd(handle, mapSerializationUnrealType);
						continue;
					}
				}

				// Raw Semantic Type case
				if (fieldSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var extension) && extension is OpenApiString)
				{
					var serializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, ns.Document, fieldSchema, UnrealTypeGetFlags.NeverSemanticType);
					outFieldSemTypeUnderlyingTypeMap.TryAdd(handle, serializationUnrealType);
				}
			}

			foreach (var openApiDocument in openApiDocuments)
			{
				GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var isObjectService = serviceTitle.Contains("object", StringComparison.OrdinalIgnoreCase);

						foreach (var param in value.Parameters)
						{
							var paramOwnerId = GetEndpointFieldOwner(globalEndpointNameCollisions, serviceName, isObjectService, operationType, endpointPath);
							var handle = GetFieldDeclarationHandle(paramOwnerId, $"{param.Name}");

							var fieldSchema = param.Schema;

							// Array fields
							if (fieldSchema is { Type: "array" })
							{
								var isReference = fieldSchema.Items.Reference != null;
								var arrayTypeSchema = isReference ? fieldSchema.Items.GetEffective(openApiDocument) : fieldSchema.Items;
								if (arrayTypeSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
								{
									var arraySerializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, ns.Document, arrayTypeSchema, UnrealTypeGetFlags.NeverSemanticType);
									outFieldSemTypeUnderlyingTypeMap.TryAdd(handle, arraySerializationUnrealType);
									continue;
								}
							}

							// Map case
							if (fieldSchema is { Type: "object", Reference: null, AdditionalPropertiesAllowed: true })
							{
								if (fieldSchema.AdditionalProperties.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
								{
									var mapSerializationUnrealType =
										GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, ns.Document, fieldSchema.AdditionalProperties, UnrealTypeGetFlags.NeverSemanticType);
									outFieldSemTypeUnderlyingTypeMap.TryAdd(handle, mapSerializationUnrealType);
									continue;
								}
							}

							// Raw Semantic Type case
							if (fieldSchema != null && fieldSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var extension) && extension is OpenApiString)
							{
								var serializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, ns.Document, fieldSchema, UnrealTypeGetFlags.NeverSemanticType);
								outFieldSemTypeUnderlyingTypeMap.TryAdd(handle, serializationUnrealType);
							}
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Builds the necessary data we'll need to generate the code for each subsystem, their Request Type declarations and the relevant helper implementations to improve Blueprint UX.
	/// It takes in all the helper data structures built by <see cref="BuildSchemaNameOverlapsMap"/>, <see cref="BuildEndpointNameCollidedMaps"/> and <see cref="BuildRequiredFieldMaps"/>.
	/// </summary>
	private static void BuildSubsystemDeclarations(IReadOnlyList<OpenApiDocument> openApiDocuments, Dictionary<string, bool> globalEndpointNameCollisions,
		Dictionary<string, Dictionary<string, bool>> perSubsystemCollisions, Dictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps,
		Dictionary<string, bool> fieldSchemaRequiredMap, Dictionary<string, string> fieldSemanticTypesUnderlyingTypeMap, List<UnrealApiSubsystemDeclaration> outSubsystemDeclarations,
		HashSet<string> outUnrealTypesUsedAsResponses)
	{
		foreach (var openApiDocument in openApiDocuments)
		{
			GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);

			var unrealServiceDecl = new UnrealApiSubsystemDeclaration { SubsystemName = serviceName.Capitalize(), };

			// check and see if we already declared this subsystem (but an object/basic version of it)
			var alreadyDeclared = outSubsystemDeclarations.Any(d => d.SubsystemName == unrealServiceDecl.SubsystemName);
			if (alreadyDeclared)
				unrealServiceDecl = outSubsystemDeclarations.First(d => d.SubsystemName == unrealServiceDecl.SubsystemName);

			// Get the number of endpoints so we can pre-allocate the correct list sizes.
			var endpointCount = openApiDocument.Paths.SelectMany(endpoint => endpoint.Value.Operations).Count();

			unrealServiceDecl.IncludeStatements = new List<string>(endpointCount);

			unrealServiceDecl.EndpointRawFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);
			unrealServiceDecl.AuthenticatedEndpointRawFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);

			unrealServiceDecl.EndpointLambdaBindableFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);
			unrealServiceDecl.AuthenticatedEndpointLambdaBindableFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);

			unrealServiceDecl.EndpointUFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);
			unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);

			foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
			{
				foreach ((OperationType operationType, OpenApiOperation endpointData) in endpoint.Operations)
				{
					var unrealEndpoint = new UnrealEndpointDeclaration();

					var isObjectService = serviceTitle.Contains("object", StringComparison.OrdinalIgnoreCase);

					unrealEndpoint.GlobalNamespacedEndpointName =
						GetSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, isObjectService, operationType, endpointPath, globalEndpointNameCollisions);
					unrealEndpoint.SubsystemNamespacedEndpointName =
						GetSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, isObjectService, operationType, endpointPath, perSubsystemCollisions[serviceName]);
					unrealEndpoint.NamespacedOwnerServiceName = unrealServiceDecl.SubsystemName;
					unrealEndpoint.IsAuth = endpointData.Security[0].Any(kvp => kvp.Key.Reference.Id == "user");
					unrealEndpoint.EndpointPath = endpointPath;
					unrealEndpoint.EndpointVerb = operationType switch
					{
						OperationType.Get => "Get",
						OperationType.Put => "Put",
						OperationType.Post => "Post",
						OperationType.Delete => "Delete",
						_ => throw new ArgumentOutOfRangeException()
					};

					unrealEndpoint.RequestQueryParameters = new List<UnrealPropertyDeclaration>(4);
					unrealEndpoint.RequestPathParameters = new List<UnrealPropertyDeclaration>(4);
					foreach (var param in endpointData.Parameters)
					{
						var paramSchema = param.Schema.Reference != null ? param.Schema.GetEffective(openApiDocument) : param.Schema;
						var paramOwnerId = $"{serviceName}_{GetSubsystemNamespacedEndpointName(serviceName, isObjectService, operationType, endpointPath, globalEndpointNameCollisions)}";
						var paramFieldHandle = GetFieldDeclarationHandle(paramOwnerId, param.Name);

						// Handle Object Ids so that we can generate the correct type.
						if (param.Name == "objectId")
						{
							if (param.Extensions.TryGetValue("x-beamable-object-id", out var ext) && ext is OpenApiObject obj)
							{
								if (obj.TryGetValue("type", out var customType) && customType is OpenApiString typeStr)
								{
									var customSchema = new OpenApiSchema { Type = typeStr.Value, Extensions = param.Schema.Extensions };
									if (obj.TryGetValue("format", out var customFormat) && customFormat is OpenApiString formatStr)
									{
										customSchema.Format = formatStr.Value;
									}

									paramSchema = customSchema;
								}
							}
						}

						var unrealProperty = new UnrealPropertyDeclaration();
						unrealProperty.PropertyUnrealType = GetUnrealTypeFromSchema(schemaNameOverlaps, fieldSchemaRequiredMap, openApiDocument, paramFieldHandle, paramSchema);
						unrealProperty.PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealProperty.PropertyUnrealType);
						unrealProperty.PropertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealProperty.PropertyUnrealType, param.Name, kSchemaGenerationBuilder);
						unrealProperty.RawFieldName = param.Name;
						unrealProperty.PropertyDisplayName = unrealProperty.PropertyName.SpaceOutOnUpperCase();
						unrealProperty.NonOptionalTypeName = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, openApiDocument, paramSchema);
						unrealProperty.BriefCommentString = $"{param.Description}";

						// Semantic type serialization for Query and Path Parameters is always FString
						if (fieldSemanticTypesUnderlyingTypeMap.TryGetValue(paramFieldHandle, out unrealProperty.SemTypeSerializationType))
						{
							kSemTypeDeclarationPointsLog.AppendLine($"{paramFieldHandle},{unrealProperty.PropertyUnrealType},{unrealProperty.SemTypeSerializationType}");
							unrealProperty.SemTypeSerializationType = UNREAL_STRING;
						}

						switch (param.In)
						{
							case ParameterLocation.Query:
								unrealEndpoint.RequestQueryParameters.Add(unrealProperty);
								break;
							case ParameterLocation.Path:
								unrealEndpoint.RequestPathParameters.Add(unrealProperty);
								break;
							default:
								Console.WriteLine($"Skipping Endpoint Param. ENDPOINT={unrealEndpoint.GlobalNamespacedEndpointName}, PARAM={param.Name}, PARAM.IN={param.In.ToString()}");
								break;
						}
					}

					// Find and declare all request body properties
					unrealEndpoint.RequestBodyParameters = new List<UnrealPropertyDeclaration>(1);
					if (endpointData.RequestBody?.Content?.TryGetValue("application/json", out var requestMediaType) ?? false)
					{
						var bodySchema = requestMediaType.Schema.GetEffective(openApiDocument);

						var unrealProperty = new UnrealPropertyDeclaration();
						unrealProperty.PropertyUnrealType = GetUnrealTypeFromSchema(schemaNameOverlaps, fieldSchemaRequiredMap, openApiDocument, "", bodySchema, UnrealTypeGetFlags.NeverOptional);
						unrealProperty.PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealProperty.PropertyUnrealType);
						unrealProperty.PropertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealProperty.PropertyUnrealType, "Body", kSchemaGenerationBuilder);
						unrealProperty.BriefCommentString = $"The \"{unrealProperty.PropertyUnrealType}\" instance to use for the request.";

						unrealEndpoint.RequestBodyParameters.Add(unrealProperty);
					}


					if (endpointData.Responses.TryGetValue("200", out var response))
					{
						if (response.Content.TryGetValue("application/json", out var jsonResponse))
						{
							var bodySchema = jsonResponse.Schema.GetEffective(openApiDocument);
							var ueType = unrealEndpoint.ResponseBodyUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, openApiDocument, bodySchema);
							unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
							unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

							// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
							outUnrealTypesUsedAsResponses.Add(ueType);

							using var sw = new StringWriter();
							var writer = new OpenApiJsonWriter(sw);
							bodySchema.SerializeAsV3WithoutReference(writer);
							Console.WriteLine($"{serviceTitle}-{serviceName}-{unrealEndpoint.GlobalNamespacedEndpointName} FROM {operationType.ToString()} {endpointPath}\n" +
											  string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
											  "\n" + string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
											  "\n" + string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
											  $"\n{unrealEndpoint.ResponseBodyUnrealType}" +
											  $"\n{sw.ToString()}");
						}
						else if (response.Content.TryGetValue("text/plain", out jsonResponse))
						{
							var ueType = unrealEndpoint.ResponseBodyUnrealType = UnrealSourceGenerator.UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE;
							unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
							unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

							// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
							outUnrealTypesUsedAsResponses.Add(ueType);
						}
					}


					unrealEndpoint.IncludeStatementsUnrealTypes = string.Join("\n", unrealEndpoint.GetAllUnrealTypes().Select(GetIncludeStatementForUnrealType));

					if (unrealEndpoint.IsAuth)
					{
						unrealServiceDecl.AuthenticatedEndpointRawFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.AuthenticatedEndpointLambdaBindableFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations.Add(unrealEndpoint);
					}
					else
					{
						unrealServiceDecl.EndpointRawFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.EndpointLambdaBindableFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.EndpointUFunctionDeclarations.Add(unrealEndpoint);
					}
				}
			}

			unrealServiceDecl.IncludeStatements.AddRange(
				unrealServiceDecl.GetAllEndpoints().Select(e => $"#include \"{e.NamespacedOwnerServiceName}/{e.GlobalNamespacedEndpointName}Request.h\"")
			);

			// If we had declared it already, replace that old declaration with the new one.
			if (alreadyDeclared) outSubsystemDeclarations.RemoveAll(d => d.SubsystemName == unrealServiceDecl.SubsystemName);

			unrealServiceDecl.EndpointUFunctionWithRetryDeclarations = unrealServiceDecl.EndpointUFunctionDeclarations;
			unrealServiceDecl.AuthenticatedEndpointUFunctionWithRetryDeclarations = unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations;
			outSubsystemDeclarations.Add(unrealServiceDecl);
		}
	}


	/*
	 * NAMESPACE CONFLICT RESOLUTION FUNCTIONS ---- THESE ARE MEANT TO RESOLVE NAME CONFLICTS AND PRODUCE A TYPE NAME (WITHOUT THE UNREAL PREFIXES) THAT IS UNIQUE ACROSS ALL THE GENERATION SPACE.
	 * We use these to control what the "final name" of any give type will look like. The other use relies on UNREAL_TYPES_OVERRIDES and NAMESPACED_ENDPOINT_OVERRIDES to manually enforce a change
	 * between a "auto-magically generated name" and a "manually defined one". This is important due to some of our types conflicting with each other as well as with Unreal's own types.  
	 */

	/// <summary>
	/// Makes a declaration handle string. This string represents a unique declaration (member field) in the space of all declarations in the code. 
	/// We use this for building helper maps that we use during the generation:
	///  - Whether or not a field is required at that specific declaration point; 
	///  - Type for the serialization of a Semantic Type field.
	///  
	/// <see cref="GetUnrealTypeFromSchema"/> expects to receive a FieldDeclaration handle when generating the Unreal Type declaration for a field or parameter. Passing a non-existent field handle will result in the type being non-optional.
	/// </summary>
	public static string GetFieldDeclarationHandle(string owner, string fieldOrParamName) => $"{owner}.{fieldOrParamName}";

	/// <summary>
	/// Basically generates the <see cref="GetFieldDeclarationHandle"/>'s 'Owner' parameter for endpoint fields.
	/// </summary>
	private static string GetEndpointFieldOwner(Dictionary<string, bool> globalEndpointNameCollisions, string serviceName, bool isObjectService, OperationType operationType, string endpointPath)
	{
		return $"{serviceName}_{GetSubsystemNamespacedEndpointName(serviceName, isObjectService, operationType, endpointPath, globalEndpointNameCollisions)}";
	}

	/// <summary>
	/// Generates the service title (basic/object) and the service name (auth, realms, etc...) from the OpenApiInfo struct.
	/// </summary>
	public static void GetNamespacedServiceNameFromApiDoc(OpenApiInfo parentDocInfo, out string serviceTitle, out string serviceName)
	{
		var serviceNames = parentDocInfo.Title.Split(" ");
		serviceTitle = serviceNames[1].Sanitize().Capitalize();
		serviceName = serviceNames[0].Sanitize().Capitalize();
	}

	/// <summary>
	/// Returns the namespaced type for the given schema of the given document.
	/// This either returns <paramref name="schemaName"/> (in the case of only one schema across all documents having this name or if all the repeated schema declarations have the same properties) 
	/// OR 
	/// it returns the type name compounded with the it's owner service's name and type (derived from the <paramref name="parentDoc"/>'s <see cref="OpenApiDocument.Info.Title"/>).
	///
	/// If we are asking for the Optional version of the type, we add optional to it's name.
	/// 
	/// This GetNamespacedTypeSchemaName gets the correct schema name for the purposes of the Unreal Code Gen. It solves the problem of NamedSchemas not being unique in global scope.
	/// As in, there can be two Account NamedSchemas but they will always be from different documents.
	/// </summary>
	public static string GetNamespacedSerializableTypeFromSchema(IReadOnlyDictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps, OpenApiDocument parentDoc, string schemaName,
		bool isOptional)
	{
		var namedOpenApiSchemata = schemaNameOverlaps[schemaName];
		if (namedOpenApiSchemata.Count > 1)
		{
			// We then check if the properties on all declarations are the same, if they are we'll consider them the same type for purposes of the SDK.
			var allPropertiesAreEqual = true;
			for (int i = 0; i < namedOpenApiSchemata.Count - 1; i++)
			{
				var schemaProperties1 = namedOpenApiSchemata[i].Schema.Properties.Keys.ToImmutableSortedSet();
				var schemaProperties2 = namedOpenApiSchemata[i + 1].Schema.Properties.Keys.ToImmutableSortedSet();
				allPropertiesAreEqual &= schemaProperties1.SequenceEqual(schemaProperties2);
			}

			// If they aren't, we'll define the type for this schema based on the service it's declared into.
			if (!allPropertiesAreEqual)
			{
				var parentDocInfo = parentDoc.Info;
				GetNamespacedServiceNameFromApiDoc(parentDocInfo, out string serviceTitle, out var serviceName);

				schemaName = $"{serviceName.Capitalize()}{serviceTitle.Capitalize()}{schemaName}";
			}
		}

		if (schemaName.EndsWith("Request"))
			schemaName += "Body";

		if (NAMESPACED_TYPES_OVERRIDES.TryGetValue(schemaName, out var overridenName))
			schemaName = overridenName;

		return isOptional ? $"Optional{schemaName}" : schemaName;
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="isObjectService"></param>
	/// <param name="httpVerb"></param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	public static string GetSubsystemNamespacedEndpointName(string serviceName, bool isObjectService, OperationType httpVerb, string endpointPath, Dictionary<string, bool> endpointNameOverlaps = null)
	{
		// If an object service, we need to skip 4 '/' to get what we want (/object/mail/{objectId}/whatWeWant)
		var skipsLeft = isObjectService ? 4 : 3;

		// Find the 3rd/4th index and split out that substring (whatWeWant OR WhatWeWant/SomeOtherThing)
		var index = 0;
		for (var i = 0; i < endpointPath.Length; i++)
		{
			if (endpointPath[i] == '/')
			{
				skipsLeft--;
			}

			if (skipsLeft == 0)
			{
				index = i + 1;
				break;
			}
		}

		// Capitalize the name
		var methodName = endpointPath.Substring(index);
		methodName = methodName.Length > 1 ? methodName.Capitalize() : methodName;

		// Ensure we don't have '-', '/' and others in the name
		methodName = methodName.Sanitize();

		// If the name here is empty (happens in routes like this "/object/mail/{objectId}/"), we'll use the service's own name as the method name.
		methodName = string.IsNullOrEmpty(methodName) ? serviceName : methodName;

		// Attach a prefix based on the HttpVerb to the start of the method name.
		methodName = httpVerb switch
		{
			OperationType.Get => $"Get{methodName}",
			OperationType.Put => $"Put{methodName}",
			OperationType.Post => $"Post{methodName}",
			OperationType.Delete => $"Delete{methodName}",
			_ => throw new ArgumentOutOfRangeException(nameof(httpVerb), httpVerb, null)
		};


		// Check and see if we have an name collision with some other service endpoint
		var doesConflict = endpointNameOverlaps != null && endpointNameOverlaps.TryGetValue(methodName, out var conflicts) && conflicts;
		if (doesConflict)
		{
			var conflictResolutionPrefix = isObjectService ? $"Object{serviceName}" : $"Basic{serviceName}";
			methodName = conflictResolutionPrefix + methodName;
		}

		// In case we want to manually override an endpoint's name...
		return NAMESPACED_ENDPOINT_OVERRIDES.ContainsKey(methodName) ? NAMESPACED_ENDPOINT_OVERRIDES[methodName] : methodName;
	}


	/*
	 * UNREAL TYPE FUNCTIONS ---- THESE ARE MEANT TO CONVERT SCHEMA DECLARATIONS INTO THEIR FINAL TYPE IN UNREAL. THESE USES THE NAMESPACED FUNCTIONS ABOVE IN ORDER TO ENSURE NO NAME CONFLICTS HAPPEN.
	 * From an Unreal Type, we can find the namespaced type, check if it's an array, optional array, wrapper array, map and so on and so forth --- this allows us to specialize the code generation at
	 * each point using UE's own prefixes and code patterns. This is a good thing since we need to enforce this standard anyway to remain 100% BP compatible.
	 */

	[System.Flags]
	public enum UnrealTypeGetFlags { None, NeverOptional, NeverSemanticType }

	/// <summary>
	/// Gets the Unreal type name for a given field declared in a <see cref="OpenApiSchema.Properties"/> dictionary.
	/// </summary>
	/// <param name="schemaNameOverlaps">A schema name overlap dictionary built from the generation context's <see cref="NamedOpenApiSchema"/>.</param>
	/// <param name="fieldRequireMaps">A field require dictionary built from all the declared <see cref="OpenApiSchema.Properties"/> of the generation context's <see cref="NamedOpenApiSchema"/>.</param>
	/// <param name="parentDoc">The <see cref="OpenApiDocument"/> containing the schema that owns the given field.</param>
	/// <param name="fieldDeclarationHandle">A string created by <see cref="GetFieldDeclarationHandle"/>. Should null or empty, if generating the type name instead of a field/parameter's declaration.</param>
	/// <param name="schema">The field schema (value of <see cref="OpenApiSchema.Properties"/>).</param>
	/// <returns>The correct Unreal-land type as a string.</returns>
	public static string GetUnrealTypeFromSchema([NotNull] IReadOnlyDictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps, [NotNull] IReadOnlyDictionary<string, bool> fieldRequireMaps,
		[NotNull] OpenApiDocument parentDoc, [NotNull] string fieldDeclarationHandle, [NotNull] OpenApiSchema schema, UnrealTypeGetFlags Flags = UnrealTypeGetFlags.None)
	{
		// The field is considered an optional type ONLY if it is in the dictionary AND it's value in the dictionary is false.
		// This dictionary must be built from all NamedSchemas's properties (fields) and contain true/false for whether or not that field of that type is required.
		var isOptional = !Flags.HasFlag(UnrealTypeGetFlags.NeverOptional) && fieldRequireMaps.TryGetValue(fieldDeclarationHandle, out var isRequired) && !isRequired;
		var isEnum = schema.GetEffective(parentDoc).Enum.Count > 0;

		var semType = "";
		if (!Flags.HasFlag(UnrealTypeGetFlags.NeverSemanticType) && schema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var ext) && ext is OpenApiString s)
			semType = s.Value;

		switch (schema.Type, schema.Format, schema.Reference?.Id, semType)
		{
			// Handles any field of any existing Schema Types
			case var (_, _, referenceId, _) when !string.IsNullOrEmpty(referenceId):
			{
				referenceId = GetNamespacedSerializableTypeFromSchema(schemaNameOverlaps, parentDoc, referenceId, isOptional);
				string unrealType;
				if (isOptional)
				{
					unrealType = $"F{referenceId}";

					if (isEnum)
						Console.WriteLine($"ENUM ={unrealType}, {referenceId}, {string.Join("-", schema.GetEffective(parentDoc).Enum.OfType<OpenApiString>().Select(s => s.Value))}\n");
				}
				else if (isEnum)
				{
					unrealType = $"E{referenceId}";
				}
				else
				{
					unrealType = $"U{referenceId}*";
				}


				// if (UNREAL_TYPES_OVERRIDES.TryGetValue(unrealType, out var overwrittenUnrealType))
				// 	return overwrittenUnrealType;
				return unrealType;
			}
			// Handles any dictionary/map fields
			case ("object", _, _, _) when schema.Reference == null && schema.AdditionalPropertiesAllowed:
			{
				if (schema.AdditionalProperties == null)
					return UNREAL_MAP + $"<{UNREAL_STRING}, {UNREAL_STRING}>";

				// Get the data type but force it to not be an optional by passing in a blank field name!
				// We do this as it makes no sense to have an map of optionals --- the semantics for optional and maps are that the entire map is optional, instead.
				var dataType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, parentDoc, schema.AdditionalProperties);

				// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
				// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
				if (dataType.StartsWith(UNREAL_MAP))
				{
					dataType = dataType.Remove(0, dataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
					dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F' from the type.
					dataType = GetNamespacedTypeNameFromUnrealType(dataType);
					dataType = UNREAL_WRAPPER_MAP + dataType;
				}
				else if (dataType.StartsWith(UNREAL_ARRAY))
				{
					dataType = dataType.Remove(0, dataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
					dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
					dataType = GetNamespacedTypeNameFromUnrealType(dataType);
					dataType = UNREAL_WRAPPER_ARRAY + dataType;
				}

				// Depending on whether or not this is an optional map or not, we generate a different Unreal Type for this field's declaration.
				return isOptional
					? UNREAL_OPTIONAL_MAP + $"{GetNamespacedTypeNameFromUnrealType(dataType)}" // Remove the "F" from the Unreal type when declaring an optional map
					: UNREAL_MAP + $"<{UNREAL_STRING}, {dataType}>";
			}
			case ("object", _, _, _) when schema.Reference == null && !schema.AdditionalPropertiesAllowed:
				throw new Exception("Object fields must either reference some other schema or must be a map/dictionary!");
			case ("array", _, _, _):
			{
				var isReference = schema.Items.Reference != null;
				var arrayTypeSchema = isReference ? schema.Items.GetEffective(parentDoc) : schema.Items;

				if (isOptional)
				{
					// Get the data type but force it to not be an optional by passing in a blank field map!
					// We do this as it makes no sense to have an array of optionals --- the semantics for optional and arrays are that the entire array is optional, instead.
					var dataType = GetNonOptionalUnrealTypeFromFieldSchema(schemaNameOverlaps, parentDoc, arrayTypeSchema);
					dataType = GetNamespacedTypeNameFromUnrealType(dataType);
					// Remove the "F" from the Unreal type when declaring an optional array
					return UNREAL_OPTIONAL_ARRAY + $"{dataType}";
				}
				else
				{
					var dataType = GetUnrealTypeFromSchema(schemaNameOverlaps, fieldRequireMaps, parentDoc, fieldDeclarationHandle, arrayTypeSchema);
					return UNREAL_ARRAY + $"<{dataType}>";
				}
			}
			// Handle semantic types
			case (_, _, _, "Cid"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CID : UNREAL_U_SEMTYPE_CID;
			case (_, _, _, "Pid"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_PID : UNREAL_U_SEMTYPE_PID;
			case (_, _, _, "AccountId"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_ACCOUNTID : UNREAL_U_SEMTYPE_ACCOUNTID;
			case (_, _, _, "Gamertag"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_GAMERTAG : UNREAL_U_SEMTYPE_GAMERTAG;
			case (_, _, _, "ContentManifestId"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CONTENTMANIFESTID : UNREAL_U_SEMTYPE_CONTENTMANIFESTID;
			case (_, _, _, "ContentId"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CONTENTID : UNREAL_U_SEMTYPE_CONTENTID;
			case (_, _, _, "StatsType"):
				return isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_STATSTYPE : UNREAL_U_SEMTYPE_STATSTYPE;

			// Handle Primitive Types 
			case ("number", "float", _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_FLOAT : UNREAL_FLOAT;
			}
			case ("number", "double", _, _):
			case ("number", _, _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_DOUBLE : UNREAL_DOUBLE;
			}
			case ("boolean", _, _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_BOOL : UNREAL_BOOL;
			}
			case ("string", "uuid", _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_GUID : UNREAL_GUID;
			}
			case ("string", "byte", _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_BYTE : UNREAL_BYTE;
			}
			case ("string", _, _, _) when (schema?.Extensions.TryGetValue("x-beamable-object-id", out _) ?? false):
			{
				return isOptional ? UNREAL_OPTIONAL_STRING : UNREAL_STRING;
			}
			case ("System.String", _, _, _):
			case ("string", _, _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_STRING : UNREAL_STRING;
			}
			case ("integer", "int16", _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_SHORT : UNREAL_SHORT;
			}
			case ("integer", "int32", _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_INT : UNREAL_INT;
			}
			case ("integer", "int64", _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_LONG : UNREAL_LONG;
			}
			case ("integer", _, _, _):
			{
				return isOptional ? UNREAL_OPTIONAL_INT : UNREAL_INT;
			}
			default:
				return "";
		}
	}

	/// <summary>
	/// Gets the header file name for any given UnrealType.
	/// </summary>
	public static string GetIncludeStatementForUnrealType(string unrealType)
	{
		// First go over all non-generated first-class types
		{
			if (unrealType.StartsWith(UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE))
				return $@"#include ""Serialization/BeamPlainTextResponseBody.h""";

			// TODO: Add sem type includes here... 
			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_CID))
				return $@"#include ""BeamBackend/SemanticTypes/BeamCid.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_PID))
				return $@"#include ""BeamBackend/SemanticTypes/BeamPid.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_ACCOUNTID))
				return $@"#include ""BeamBackend/SemanticTypes/BeamAccountId.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_GAMERTAG))
				return $@"#include ""BeamBackend/SemanticTypes/BeamGamerTag.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_CONTENTID))
				return $@"#include ""BeamBackend/SemanticTypes/BeamContentId.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_CONTENTMANIFESTID))
				return $@"#include ""BeamBackend/SemanticTypes/BeamContentManifestId.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_STATSTYPE))
				return $@"#include ""BeamBackend/SemanticTypes/BeamStatsType.h""";
		}

		// Then, go over all generated types
		{
			if (unrealType.StartsWith(UNREAL_U_ENUM_PREFIX))
			{
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"AutoGen/Enums/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_OPTIONAL))
			{
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"AutoGen/Optionals/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_WRAPPER_ARRAY))
			{
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"AutoGen/Arrays/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_WRAPPER_MAP))
			{
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"AutoGen/Maps/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_ARRAY))
			{
				var firstTemplate = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(unrealType);
				if (MustInclude(firstTemplate))
				{
					return GetIncludeStatementForUnrealType(firstTemplate);
				}
			}

			if (unrealType.StartsWith(UNREAL_MAP))
			{
				var secondTemplate = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(unrealType);
				if (MustInclude(secondTemplate))
				{
					return GetIncludeStatementForUnrealType(secondTemplate);
				}
			}

			if (MustInclude(unrealType))
			{
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"AutoGen/{header}\"";
			}
		}

		return "";

		bool MustInclude(string s)
		{
			return s.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX) || s.StartsWith(UnrealSourceGenerator.UNREAL_U_STRUCT_PREFIX) && s != UNREAL_GUID && s != UNREAL_STRING;
		}
	}

	/// <summary>
	/// Gets a guaranteed non-optional type for the given field schema, even if the field schema is in-fact Optional.
	/// We use this in order to get the correct type to pass into the serialization/deserialization templated functions that work with FBeamOptionals in Unreal-land.
	/// </summary>
	public static string GetNonOptionalUnrealTypeFromFieldSchema([NotNull] IReadOnlyDictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps, [NotNull] OpenApiDocument parentDoc,
		[NotNull] OpenApiSchema fieldSchema, UnrealTypeGetFlags flags = UnrealTypeGetFlags.NeverOptional)
	{
		// This passes in a blank field map which means it's not possible for it to found in the fieldRequiredMaps. This means we will get the Required Version of it. 
		return GetUnrealTypeFromSchema(schemaNameOverlaps, new Dictionary<string, bool>(), parentDoc, "", fieldSchema, UnrealTypeGetFlags.NeverOptional | flags);
	}


	/// <summary>
	/// Given an unreal string created with <see cref="GetUnrealTypeFromSchema"/> turns it into a valid Namespaced Type Name (<see cref="GetNamespacedSerializableTypeFromSchema"/>).
	/// </summary>
	public static string GetNamespacedTypeNameFromUnrealType(string unrealTypeName)
	{
		if (unrealTypeName == UNREAL_BYTE)
			return "Int8";

		if (unrealTypeName == UNREAL_SHORT)
			return "Int16";
		if (unrealTypeName == UNREAL_INT)
			return "Int32";

		if (unrealTypeName == UNREAL_LONG)
			return "Int64";

		if (unrealTypeName == UNREAL_BOOL)
			return "Bool";

		if (unrealTypeName == UNREAL_MAP || unrealTypeName == UNREAL_ARRAY)
			throw new Exception("There's no namespaced type declaration for unreal maps!");

		if (unrealTypeName == UNREAL_FLOAT)
			return "Float";

		if (unrealTypeName == UNREAL_DOUBLE)
			return "Double";

		// F"AnyTypes" we just remove the F's
		if (unrealTypeName.StartsWith(UnrealSourceGenerator.UNREAL_U_STRUCT_PREFIX) || unrealTypeName.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
			return unrealTypeName.AsSpan(1).ToString();

		// U"AnyTypes"* we just remove the U's and *'s
		if (unrealTypeName.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
			return unrealTypeName.AsSpan(1, unrealTypeName.Length - 2).ToString();

		return unrealTypeName;
	}

	/// <summary>
	/// Given an unreal string created with <see cref="GetUnrealTypeFromSchema"/> turns it into a valid Namespaced Type Name (<see cref="GetNamespacedSerializableTypeFromSchema"/>).
	/// </summary>
	public static string GetWrappedUnrealTypeFromUnrealWrapperType(string unrealWrapperType)
	{
		// unrealWrapperType = FArrayOfSomethingArrayey  | FMapOfSomethingMappy             | FOptionalMapOfSomethingOptional
		// these wrap        = TArray<USomethingArrayey*> | TMap<FString, USomethingMappy*>   | Optional of TMap<FString, USomethingOptional*>

		var indexOf = unrealWrapperType.IndexOf("Of", StringComparison.Ordinal);
		var namespacedWrappedType = unrealWrapperType.Substring(indexOf + 2);

		// namespacedWrappedType = SomethingArrayey | SomethingMappy | SomethingOptional
		if (namespacedWrappedType == "Int8")
			return UNREAL_BYTE;
		if (namespacedWrappedType == "Int16")
			return UNREAL_SHORT;
		if (namespacedWrappedType == "Int32")
			return UNREAL_INT;
		if (namespacedWrappedType == "Int64")
			return UNREAL_LONG;
		if (namespacedWrappedType == "Bool")
			return UNREAL_BOOL;
		if (namespacedWrappedType == "Float")
			return UNREAL_FLOAT;
		if (namespacedWrappedType == "Double")
			return UNREAL_DOUBLE;
		if (namespacedWrappedType == "String")
			return UNREAL_STRING;
		if (namespacedWrappedType == "Guid")
			return UNREAL_GUID;

		// If (SomethingArrayey | SomethingMappy | SomethingOptional) aren't any of the raw cases above, we just prepend an 'U' and '*' to it.
		return $"U{namespacedWrappedType}*";
	}

	/// <summary>
	/// If the given <paramref name="ueType"/> ends with a '*' (as in, is a pointer declaration), we remove it. 
	/// </summary>
	public static string RemovePtrFromUnrealTypeIfAny(string ueType)
	{
		return ueType.EndsWith("*") ? ueType.Substring(0, ueType.Length - 1) : ueType;
	}
}

public static class StringExtensions
{
	public static string SpaceOutOnUpperCase(this string word) => Regex.Replace(word.Capitalize(), @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");

	public static string Capitalize(this string word)
	{
		return string.Concat(char.ToUpper(word[0]).ToString(), word.AsSpan(1));
	}

	public static string ProcessReplacement(this string src,
		Dictionary<string, string> replacements,
		string replacementKeyStart = "₢",
		string replacementKeyEnd = "₢")
	{
		var replacementChainValues = replacements.Values.ToArray();
		var replacementChain = replacements.Keys.Select(r => replacementKeyStart + r + replacementKeyEnd).ToArray();

		var res = src;
		for (var i = 0; i < replacementChain.Length; i++)
		{
			var key = replacementChain[i];
			var value = replacementChainValues[i];

			// Replace doesn't fail if key is not found, so we can just ask it to try to replace...
			res = res.Replace(key, value);
		}

		return res;
	}

	/// <summary>
	/// Ensures
	/// </summary>
	/// <param name="propKey"></param>
	/// <returns></returns>
	public static string Sanitize(this string propKey)
	{
		string AppendKey(string str) => str + "Key";
		var protectedKeys = new HashSet<string>
		{
			// TODO: add all other C++ and Unreal keywords...
			"class",
			"struct",
			"const",
			"namespace",
			"if",
			"switch",
			"do",
			"for",
			"while",
			"fixed",
			"int",
			"long",
			"auto",
			"void",
			"public",
			"protected",
			"private",
		};

		for (var i = propKey.Length - 2; i >= 0; i--)
		{
			if (propKey[i] == '-' || propKey[i] == '/' || propKey[i] == '$')
			{
				if (i + 2 >= propKey.Length)
				{
					propKey = propKey[..i] + char.ToUpper(propKey[i + 1]);
				}
				else
				{
					propKey = propKey[..i] + char.ToUpper(propKey[i + 1]) + propKey[(i + 2)..];
				}
			}
		}

		return protectedKeys.Contains(propKey)
			? AppendKey(propKey)
			: propKey;
	}
}
