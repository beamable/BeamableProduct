using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Concurrent;
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
	public const string UNREAL_MAP = "TMap";
	public const string UNREAL_OPTIONAL_MAP = $"{UNREAL_OPTIONAL}MapOf";
	public const string UNREAL_WRAPPER_MAP = "FMapOf";
	public const string UNREAL_U_ENUM_PREFIX = "E";
	public const string UNREAL_U_OBJECT_PREFIX = "U";
	public const string UNREAL_U_STRUCT_PREFIX = "F";
	public const string UNREAL_U_POLY_WRAPPER_PREFIX = "UOneOf_";
	public const string UNREAL_U_BEAM_NODE_PREFIX = "UK2BeamNode";


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

	// Start of Replacement Types
	public const string UNREAL_U_REPTYPE_CLIENTPERMISSION = "FBeamClientPermission";
	public const string UNREAL_OPTIONAL_U_REPTYPE_CLIENTPERMISSION = $"{UNREAL_OPTIONAL}BeamClientPermission";
	public static readonly List<string> UNREAL_ALL_REPTYPES = new()
	{
		UNREAL_U_REPTYPE_CLIENTPERMISSION
	};
	public static readonly List<string> UNREAL_ALL_REPTYPES_NAMESPACED_NAMES = new()
	{
		GetNamespacedTypeNameFromUnrealType(UNREAL_U_REPTYPE_CLIENTPERMISSION),
	};
	// End of Replacement Types

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

	/// <summary>
	/// These overrides are applied in <see cref="GetNamespacedSerializableTypeFromSchema"/> so that we can override the names of schemas (literal schemas that show up in the content/schemas path of the JSON)
	/// that'll exist in Unreal as a UObject that can be deserialized. Embedded schemas (such as the ones required for polymorphic fields using OneOf) are overriden by <see cref="UNREAL_TYPES_OVERRIDES"/>.
	/// TODO: Over time, we should probably move this into its own partial file of this type.
	/// </summary>
	public static readonly Dictionary<string, string> NAMESPACED_TYPES_OVERRIDES = new() { { "Player", "PlayerId" }, { "DeleteRole", "DeleteRoleRequestBody" } };

	/// <summary>
	/// These overrides are applied in <see cref="GetNamespacedServiceNameFromApiDoc"/> so that we can override specific endpoint names for things that make more sense on the client.
	/// TODO: Over time, we should probably move this into its own partial file of this type.
	/// </summary>
	public static readonly Dictionary<string, string> NAMESPACED_ENDPOINT_OVERRIDES = new() { { "PostToken", "Authenticate" } };

	/// <summary>
	/// These overrides are applied using <see cref="ApplyUnrealTypeOverride"/>. This is used for types that we only discover in the middle of processing the schemas for a document.
	/// Things like Optionals, BeamArray/Map and any polymorphic type (schema containing 'OneOf').
	/// TODO: Over time, we should probably move this into its own partial file of this type.
	/// </summary>
	public static readonly Dictionary<string, string> UNREAL_TYPES_OVERRIDES = new()
	{
		{ "UOneOf_UContentReference_UTextReference_UBinaryReference*", "UBaseContentReference*" },
		{ "UOneOf_UCronTrigger_UExactTrigger*", "UBeamJobTrigger*" },
		{ "UOneOf_UHttpCall_UPublishMessage_UServiceCall*", "UBeamJobType*" }
	};

	/// <summary>
	/// See <see cref="UNREAL_TYPES_OVERRIDES"/>. 
	/// </summary>
	public static string ApplyUnrealTypeOverride(string type) => UNREAL_TYPES_OVERRIDES.TryGetValue(type, out var overriden) ? overriden : type;

	// Helper Data Structures built before starting the actual generation.

	private static IReadOnlyList<NamedOpenApiSchema> namedOpenApiSchemata;
	private static Dictionary<string, List<NamedOpenApiSchema>> schemaNameOverlaps;
	private static Dictionary<string, bool> globalEndpointNameCollisions;
	private static Dictionary<string, Dictionary<string, bool>> perSubsystemCollisions;
	private static Dictionary<string, bool> fieldSchemaRequiredMap;
	private static Dictionary<string, string> fieldSemanticTypesUnderlyingTypeMap;
	private static HashSet<TypeRequestBody> unrealTypesUsedAsResponses;

	/// <summary>
	/// This gets filled set during the parsing of unreal types (<see cref="GetUnrealTypeFromSchema"/>).
	/// The general idea is that, when we find a polymorphic wrapper type there we also find the 'string' value that we can expect its "type" field to have so that we know which wrapped type to deserialize. 
	/// </summary>
	private static ConcurrentDictionary<string, string> polymorphicWrappedSchemaExpectedTypeValues;

	/// <summary>
	/// Set this before calling <see cref="Generate"/> so that you can define what the export macro will be used for generating the core types.
	/// </summary>
	public static string exportMacro = "BEAMABLECORE_API";

	/// <summary>
	/// Set this before calling <see cref="Generate"/> so that you can define what the export macro will be used for generating the core types.
	/// </summary>
	public static string blueprintExportMacro = "BEAMABLECOREBLUEPRINTNODES_API";

	/// <summary>
	/// Set this before calling <see cref="Generate"/> when you want to control where the AutoGen folder will be created.
	/// It can be the same as <see cref="cppFileOutputPath"/>.
	/// </summary>
	public static string headerFileOutputPath = "BeamableCore/Public/";

	/// <summary>
	/// Set this before calling <see cref="Generate"/> to define where AutoGen folder for cpp files will be created.
	/// It can be the same as <see cref="headerFileOutputPath"/>.
	/// </summary>
	public static string cppFileOutputPath = "BeamableCore/Private/";

	/// <summary>
	/// Path to where the AutoGen folder containing the Blueprint headers will be created under the output path folder.
	/// </summary>
	public static string blueprintHeaderFileOutputPath = "BeamableCoreBlueprintNodes/Public/BeamFlow/ApiRequest/";

	/// <summary>
	/// Path to where the AutoGen folder containing the Blueprint cpp files will be created under the output path folder. 
	/// </summary>
	public static string blueprintCppFileOutputPath = "BeamableCoreBlueprintNodes/Private/BeamFlow/ApiRequest/";

	/// <summary>
	/// Path + name, without extension or leading slash, to where in the output folder this the <see cref="PreviousGenerationPassesData"/> will be output.
	/// </summary>
	public static string currentGenerationPassDataFilePath = "BeamableCore_GenerationPass";

	/// <summary>
	/// A set of containers holding data regarding types generated in all previous passes (ie: Our CodeGen API generates a file with this structure that is used for our customers' C#MS codegen).  
	/// </summary>
	public static PreviousGenerationPassesData previousGenerationPassesData = new();

	public enum GenerationType { BasicObject, Microservice }

	public enum ServiceType { Basic, Object, Api }

	public static GenerationType genType = GenerationType.BasicObject;

	public List<GeneratedFileDescriptor> Generate(IGenerationContext context)
	{
		var outputFiles = new List<GeneratedFileDescriptor>(16);

		kSemTypeDeclarationPointsLog.Clear();
		kSemTypeDeclarationPointsLog.AppendLine("Handle,UProperty,SerializationType");

		// Build a list of dictionaries of schemas whose names appear in the list more than once.
		namedOpenApiSchemata = context.OrderedSchemas;
		schemaNameOverlaps = new Dictionary<string, List<NamedOpenApiSchema>>(namedOpenApiSchemata.Count);
		BuildSchemaNameOverlapsMap();

		// Build a list of dictionaries of endpoint names whose that are declared in more than one service.
		BuildEndpointNameCollidedMaps(context.Documents);

		// Go through all properties of all schemas and see if they are required or not
		fieldSchemaRequiredMap = new Dictionary<string, bool>(namedOpenApiSchemata.Count);
		fieldSemanticTypesUnderlyingTypeMap = new Dictionary<string, string>(namedOpenApiSchemata.Count);
		BuildRequiredFieldMaps(context.Documents);
		BuildSemanticTypesUnderlyingTypeMaps(context.Documents);

		// Build the data required to generate all subsystems and their respective endpoints
		unrealTypesUsedAsResponses = new HashSet<TypeRequestBody>(namedOpenApiSchemata.Count);
		BuildSubsystemDeclarations(context.Documents, out var subsystemDeclarations, out var outResponseWrapperTypes);

		// Build the data required to generate all serializable types, enums, optionals, array and map wrapper types.
		// Array and Map Wrapper types are required due to UE's TMap and TArray not supporting nested data structures. As in, TArray<TArray<int>> doesn't work --- but TArray<FArrayOfInt> does.
		polymorphicWrappedSchemaExpectedTypeValues = new ConcurrentDictionary<string, string>(1, namedOpenApiSchemata.Count);
		BuildSerializableTypeDeclarations(out var serializableTypes,
			out var enumTypes,
			out var optionalTypes,
			out var arrayWrapperTypes,
			out var mapWrapperTypes,
			out var csvResponseTypes,
			out var csvRowTypes,
			out var polymorphicTypeWrappers);

		// Generate the actual files we'll need from the data we've built.
		var processDictionary = new Dictionary<string, string>(16);

		// A dictionary that'll be filled with the UnrealType name of ONLY THE TYPES THAT'LL BE CONTAINED IN GENERATED FILES.
		// INFO: UnrealTypes defined in "previousGenerationPassesData" will not be added here.
		var newGeneratedUnrealTypes = new Dictionary<string, string>();

		// Generate all Optional Type Files (except the ones that were already generated in a previous run).
		var optionalDeclarations = optionalTypes
			.Except(optionalTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
			.Select(decl =>
			{
				decl.BakeIntoProcessMap(processDictionary);
				var headerDeclaration = UnrealOptionalDeclaration.OPTIONAL_HEADER_DECL.ProcessReplacement(processDictionary);
				var cppDeclaration = UnrealOptionalDeclaration.OPTIONAL_CPP_DECL.ProcessReplacement(processDictionary);
				var bpLibraryHeader = UnrealOptionalDeclaration.OPTIONAL_LIBRARY_HEADER_DECL.ProcessReplacement(processDictionary);
				var bpLibraryCpp = UnrealOptionalDeclaration.OPTIONAL_LIBRARY_CPP_DECL.ProcessReplacement(processDictionary);

				processDictionary.Clear();
				return (decl, headerDeclaration, cppDeclaration, bpLibraryHeader, bpLibraryCpp);
			}).ToList();
		outputFiles.AddRange(optionalDeclarations.SelectMany((s, idx) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.UnrealTypeName, headerFileName);
			return new[]
			{
				new GeneratedFileDescriptor { FileName = headerFileName, Content = s.headerDeclaration }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
				new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}Library.h", Content = s.bpLibraryHeader }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}Library.cpp", Content = s.bpLibraryCpp },
			};
		}));

		// Generate Array Wrapper Type Files
		var arrayWrapperDeclarations = arrayWrapperTypes
			.Except(arrayWrapperTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
			.Select(decl =>
			{
				decl.BakeIntoProcessMap(processDictionary);
				var headerDeclaration = UnrealWrapperContainerDeclaration.ARRAY_WRAPPER_HEADER_DECL.ProcessReplacement(processDictionary);
				var cppDeclaration = UnrealWrapperContainerDeclaration.ARRAY_WRAPPER_CPP_DECL.ProcessReplacement(processDictionary);
				processDictionary.Clear();
				return (decl, headerDeclaration, cppDeclaration);
			});
		outputFiles.AddRange(arrayWrapperDeclarations.SelectMany((s, idx) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/Arrays/{s.decl.NamespacedTypeName}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.UnrealTypeName, headerFileName);
			return new[] { new GeneratedFileDescriptor { FileName = headerFileName, Content = s.headerDeclaration }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Arrays/{s.decl.NamespacedTypeName}.cpp", Content = s.cppDeclaration }, };
		}));

		// Generate Map Wrapper Type Files
		var mapWrapperDeclarations = mapWrapperTypes
			.Except(mapWrapperTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
			.Select(decl =>
			{
				decl.BakeIntoProcessMap(processDictionary);
				var headerDeclaration = UnrealWrapperContainerDeclaration.MAP_WRAPPER_HEADER_DECL.ProcessReplacement(processDictionary);
				var cppDeclaration = UnrealWrapperContainerDeclaration.MAP_WRAPPER_CPP_DECL.ProcessReplacement(processDictionary);
				processDictionary.Clear();
				return (decl, headerDeclaration, cppDeclaration);
			});
		outputFiles.AddRange(mapWrapperDeclarations.SelectMany((s, idx) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/Maps/{s.decl.NamespacedTypeName}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.UnrealTypeName, headerFileName);
			return new[] { new GeneratedFileDescriptor { FileName = headerFileName, Content = s.headerDeclaration }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Maps/{s.decl.NamespacedTypeName}.cpp", Content = s.cppDeclaration }, };
		}));

		// Generate all enum type declarations
		var enumTypesCode = enumTypes
			.Except(enumTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
			.Select(decl =>
			{
				decl.BakeIntoProcessMap(processDictionary);
				var header = UnrealEnumDeclaration.U_ENUM_HEADER.ProcessReplacement(processDictionary);
				processDictionary.Clear();
				return (decl, header);
			});
		outputFiles.AddRange(enumTypesCode.SelectMany((s, _) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/Enums/{s.decl.NamespacedTypeName}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.UnrealTypeName, headerFileName);
			return new[] { new GeneratedFileDescriptor { FileName = headerFileName, Content = s.header, }, };
		}));

		// Generate all serializable types
		var allSerializableTypes = serializableTypes.Union(polymorphicTypeWrappers).Union(outResponseWrapperTypes).ToList();
		var serializableTypesCode = allSerializableTypes
			.Except(allSerializableTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.NamespacedTypeName)))
			.Select(decl =>
			{
				decl.IntoProcessMap(processDictionary);
				var serializableHeader = UnrealJsonSerializableTypeDeclaration.JSON_SERIALIZABLE_TYPE_HEADER.ProcessReplacement(processDictionary);
				var serializableCpp = UnrealJsonSerializableTypeDeclaration.JSON_SERIALIZABLE_TYPE_CPP.ProcessReplacement(processDictionary);
				var serializableTypeLibraryHeader = UnrealJsonSerializableTypeDeclaration.JSON_SERIALIZABLE_TYPES_LIBRARY_HEADER.ProcessReplacement(processDictionary);
				var serializableTypeLibraryCpp = UnrealJsonSerializableTypeDeclaration.JSON_SERIALIZABLE_TYPES_LIBRARY_CPP.ProcessReplacement(processDictionary);
				processDictionary.Clear();

				return (decl, serializableHeader, serializableCpp, serializableTypeLibraryHeader, serializableTypeLibraryCpp);
			});
		outputFiles.AddRange(serializableTypesCode.SelectMany((s, idx) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.UnrealTypeName, headerFileName);
			return new[]
			{
				new GeneratedFileDescriptor { FileName = headerFileName, Content = s.serializableHeader, }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}.cpp", Content = s.serializableCpp, },
				new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}Library.h", Content = s.serializableTypeLibraryHeader, }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}Library.cpp", Content = s.serializableTypeLibraryCpp, },
			};
		}));


		// Generate all csv response types
		var csvResponseTypesCode = csvResponseTypes.Select(decl =>
		{
			decl.IntoProcessMap(processDictionary);

			var header = UnrealCsvSerializableTypeDeclaration.CSV_SERIALIZABLE_TYPE_HEADER.ProcessReplacement(processDictionary);
			var cpp = UnrealCsvSerializableTypeDeclaration.CSV_SERIALIZABLE_TYPE_CPP.ProcessReplacement(processDictionary);
			return (decl, header, cpp);
		});
		outputFiles.AddRange(csvResponseTypesCode.SelectMany((s, idx) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.UnrealTypeName, headerFileName);
			return new[] { new GeneratedFileDescriptor { FileName = headerFileName, Content = s.header, }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}.cpp", Content = s.cpp, }, };
		}));

		// Generate all csv row types
		var csvRowTypesCode = csvRowTypes.Select(decl =>
		{
			decl.IntoProcessMap(processDictionary);

			var header = UnrealCsvRowTypeDeclaration.CSV_ROW_TYPE_HEADER.ProcessReplacement(processDictionary);
			var cpp = UnrealCsvRowTypeDeclaration.CSV_ROW_TYPE_CPP.ProcessReplacement(processDictionary);

			return (decl, header, cpp);
		});
		outputFiles.AddRange(csvRowTypesCode.SelectMany((s, idx) =>
		{
			var headerFileName = $"{headerFileOutputPath}AutoGen/Rows/{s.decl.RowNamespacedType}.h";
			newGeneratedUnrealTypes.TryAdd(s.decl.RowUnrealType, headerFileName);
			return new[] { new GeneratedFileDescriptor { FileName = headerFileName, Content = s.header, }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Rows/{s.decl.RowNamespacedType}.cpp", Content = s.cpp, }, };
		}));

		// Subsystem Declarations
		var subsystemsCode = subsystemDeclarations.Select(decl =>
		{
			decl.IntoProcessMapHeader(processDictionary);
			var subsystemHeader = UnrealApiSubsystemDeclaration.U_SUBSYSTEM_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMapCpp(processDictionary);
			var subsystemCpp = UnrealApiSubsystemDeclaration.U_SUBSYSTEM_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			return (decl, subsystemHeader, subsystemCpp);
		});
		outputFiles.AddRange(subsystemsCode.SelectMany((s, i) =>
		{
			return new[] { new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/SubSystems/Beam{s.decl.SubsystemName}Api.h", Content = s.subsystemHeader }, new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/SubSystems/Beam{s.decl.SubsystemName}Api.cpp", Content = s.subsystemCpp }, };
		}));

		var subsystemEndpoints = subsystemDeclarations.SelectMany(sd => sd.GetAllEndpoints()).ToList();
		var subsystemEndpointsCode = subsystemEndpoints.Select(decl =>
		{
			decl.IntoProcessMap(processDictionary, serializableTypes);
			var endpointHeader = UnrealEndpointDeclaration.U_ENDPOINT_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMap(processDictionary, serializableTypes);
			var endpointCpp = UnrealEndpointDeclaration.U_ENDPOINT_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMap(processDictionary, serializableTypes);
			var beamFlowNodeHeader = UnrealEndpointDeclaration.BEAM_FLOW_BP_NODE_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMap(processDictionary, serializableTypes);
			var beamFlowNodeCpp = UnrealEndpointDeclaration.BEAM_FLOW_BP_NODE_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();


			return (decl, endpointHeader, endpointCpp, beamFlowNodeHeader, beamFlowNodeCpp);
		});
		outputFiles.AddRange(subsystemEndpointsCode.SelectMany((sc, i) =>
		{
			return new[]
			{
				new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/SubSystems/{sc.decl.NamespacedOwnerServiceName}/{sc.decl.GlobalNamespacedEndpointName}Request.h", Content = sc.endpointHeader },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/SubSystems/{sc.decl.NamespacedOwnerServiceName}/{sc.decl.GlobalNamespacedEndpointName}Request.cpp", Content = sc.endpointCpp },
				new GeneratedFileDescriptor { FileName = $"{blueprintHeaderFileOutputPath}AutoGen/{sc.decl.NamespacedOwnerServiceName}/K2BeamNode_ApiRequest_{sc.decl.GlobalNamespacedEndpointName}.h", Content = sc.beamFlowNodeHeader },
				new GeneratedFileDescriptor { FileName = $"{blueprintCppFileOutputPath}AutoGen/{sc.decl.NamespacedOwnerServiceName}/K2BeamNode_ApiRequest_{sc.decl.GlobalNamespacedEndpointName}.cpp", Content = sc.beamFlowNodeCpp },
			};
		}));

		// Prints out all the identified semtype declarations
		Console.WriteLine(kSemTypeDeclarationPointsLog.ToString());
		foreach ((string key, string value) in newGeneratedUnrealTypes)
		{
			previousGenerationPassesData.InEngineTypeToIncludePaths.Add(key, value);
			Console.WriteLine($"Mapped {key} to {value}");
		}

		outputFiles.Add(new GeneratedFileDescriptor() { FileName = $"{currentGenerationPassDataFilePath}.json", Content = JsonConvert.SerializeObject(previousGenerationPassesData), });
		return outputFiles;
	}

	private static void BuildSerializableTypeDeclarations(out List<UnrealJsonSerializableTypeDeclaration> jsonSerializableTypes,
		out List<UnrealEnumDeclaration> enumTypes, out List<UnrealOptionalDeclaration> optionalTypes,
		out List<UnrealWrapperContainerDeclaration> arrayWrapperTypes, out List<UnrealWrapperContainerDeclaration> mapWrapperTypes,
		out List<UnrealCsvSerializableTypeDeclaration> csvResponseTypes, out List<UnrealCsvRowTypeDeclaration> csvRowTypes,
		out List<UnrealJsonSerializableTypeDeclaration> polymorphicWrappersDeclarations)
	{
		jsonSerializableTypes = new List<UnrealJsonSerializableTypeDeclaration>(namedOpenApiSchemata.Count);
		enumTypes = new List<UnrealEnumDeclaration>(namedOpenApiSchemata.Count);
		optionalTypes = new List<UnrealOptionalDeclaration>(namedOpenApiSchemata.Count);
		arrayWrapperTypes = new List<UnrealWrapperContainerDeclaration>(namedOpenApiSchemata.Count);
		mapWrapperTypes = new List<UnrealWrapperContainerDeclaration>(namedOpenApiSchemata.Count);
		csvResponseTypes = new List<UnrealCsvSerializableTypeDeclaration>(namedOpenApiSchemata.Count);
		csvRowTypes = new List<UnrealCsvRowTypeDeclaration>(namedOpenApiSchemata.Count);
		polymorphicWrappersDeclarations = new List<UnrealJsonSerializableTypeDeclaration>(namedOpenApiSchemata.Count);

		// Allocate a list to keep track of all Schema types that we have already declared.
		var listOfDeclaredTypes = new List<string>(namedOpenApiSchemata.Count);

		// Add replacement types so that we don't generate them when we see them
		listOfDeclaredTypes.AddRange(UNREAL_ALL_REPTYPES_NAMESPACED_NAMES);

		// Convert the schema into the generation format
		foreach (var namedOpenApiSchema in namedOpenApiSchemata)
		{
			// We need to decide on whether we'll name the type simply or if we'll use their service title to augment the name.
			var schema = namedOpenApiSchema.Schema;
			string schemaUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(namedOpenApiSchema.Document, schema, out _);
			string schemaNamespacedType = GetNamespacedTypeNameFromUnrealType(schemaUnrealType);
			// Make sure we don't declare two types with the same name
			if (listOfDeclaredTypes.Contains(schemaNamespacedType)) continue;
			listOfDeclaredTypes.Add(schemaNamespacedType);

			var isResponseBodyType = unrealTypesUsedAsResponses.FirstOrDefault(c => c.Equals(schemaUnrealType));

			// Find Enum declarations even within arrays and maps 
			// TODO: Declare this instead of serialized type
			if (schemaUnrealType.StartsWith(UNREAL_U_ENUM_PREFIX))
			{
				var enumDecl = new UnrealEnumDeclaration { UnrealTypeName = schemaUnrealType, NamespacedTypeName = schemaNamespacedType, EnumValues = schema.Enum.OfType<OpenApiString>().Select(v => v.Value).ToList() };

				enumTypes.Add(enumDecl);
			}
			else if (IsCsvRowSchema(namedOpenApiSchema.Document, schema))
			{
				_ = schema.Extensions.TryGetValue("x-beamable-primary-key", out var keyPropertyNameExt);
				_ = schema.Extensions.TryGetValue("x-beamable-csv-order", out var columnOrderExt);

				var keyProperty = (keyPropertyNameExt as OpenApiString).Value;
				var order = (columnOrderExt as OpenApiString).Value.Split(',');

				var uproperties = order.Select(fieldName =>
				{
					var fieldSchema = schema.Properties[fieldName];
					var unrealType = GetNonOptionalUnrealTypeFromFieldSchema(namedOpenApiSchema.Document, fieldSchema, out _, UnrealTypeGetFlags.NeverSemanticType);

					var prop = new UnrealPropertyDeclaration
					{
						PropertyUnrealType = unrealType,
						PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealType),
						PropertyName = fieldName.Capitalize(),
						PropertyDisplayName = fieldName.Capitalize(),
						RawFieldName = fieldName,

						// We don't support Optional types here.
						NonOptionalTypeName = "",
						SemTypeSerializationType = "",
						NonOptionalTypeNameRelevantTemplateParam = ""
					};
					return prop;
				}).ToList();


				var csvRowType = new UnrealCsvRowTypeDeclaration
				{
					RowUnrealType = schemaUnrealType,
					RowNamespacedType = GetNamespacedTypeNameFromUnrealType(schemaUnrealType),
					PropertyDeclarations = uproperties,
					KeyDeclarationIdx = Array.IndexOf(order, keyProperty),
				};
				csvRowTypes.Add(csvRowType);
			}
			else if (isResponseBodyType.Type is ResponseBodyType.Csv)
			{
				var csvRowForm = schema.Properties["itemsCsv"].Items.GetEffective(namedOpenApiSchema.Document);
				var rowUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(namedOpenApiSchema.Document, csvRowForm, out _);

				var csvResponseType = new UnrealCsvSerializableTypeDeclaration
				{
					UnrealTypeName = schemaUnrealType,
					NamespacedTypeName = schemaNamespacedType,
					RowUnrealType = rowUnrealType,
					RowNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(rowUnrealType),
					NeedsKeyGeneration = !csvRowForm.Extensions.ContainsKey("x-beamable-primary-key"),
					NeedsHeaderRow = true, // the csv we get back from the backend never has headers.
				};
				csvResponseTypes.Add(csvResponseType);
			}
			else
			{
				// Prepare the data for injection in the template string.
				var serializableTypeDeclaration = new UnrealJsonSerializableTypeDeclaration
				{
					UnrealTypeName = schemaUnrealType,
					NamespacedTypeName = schemaNamespacedType,
					PropertyIncludes = new List<string>(8),
					UPropertyDeclarations = new List<UnrealPropertyDeclaration>(8),
					JsonUtilsInclude = "",
					IsResponseBodyType = isResponseBodyType.Type,
				};

				foreach ((string fieldName, OpenApiSchema fieldSchema) in schema.Properties)
				{
					var handle = GetFieldDeclarationHandle(schemaNamespacedType, fieldName);
					// see schema type and format
					var unrealType = GetUnrealTypeFromSchema(namedOpenApiSchema.Document, handle, fieldSchema, out var nonOverridenUnrealType);
					if (string.IsNullOrEmpty(unrealType))
					{
						using var sw = new StringWriter();
						var writer = new OpenApiJsonWriter(sw);
						fieldSchema.SerializeAsV3WithoutReference(writer);
						Console.WriteLine($"Skipping unreal type for {handle} cause not supported yet! Schema: {sw}");
						continue;
					}

					// Check if this field is an poly wrapper field, or polymorphic array/map. If it is, we need to build up a new serializable type for it.
					if (nonOverridenUnrealType.Contains(UNREAL_U_POLY_WRAPPER_PREFIX))
					{
						string nonOverridenPolyWrapperType, overridenWrapperType;
						if (nonOverridenUnrealType.StartsWith(UNREAL_U_POLY_WRAPPER_PREFIX))
						{
							nonOverridenPolyWrapperType = nonOverridenUnrealType;
							overridenWrapperType = unrealType;
						}
						else if (nonOverridenUnrealType.StartsWith(UNREAL_ARRAY))
						{
							nonOverridenPolyWrapperType = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOverridenUnrealType);
							overridenWrapperType = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(unrealType);
						}
						else if (nonOverridenUnrealType.StartsWith(UNREAL_MAP))
						{
							nonOverridenPolyWrapperType = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOverridenUnrealType);
							overridenWrapperType = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(unrealType);
						}
						else
							throw new Exception(
								"Should never see this. If you do, this means someone is using a polymorphic return value in an unsupported way. Figure out which way and add support for it here.");

						var ptrWrappedTypes = nonOverridenPolyWrapperType.Substring(nonOverridenPolyWrapperType.IndexOf('_') + 1).Split("_")
							.Select(nonPtrWrappedTypes => nonPtrWrappedTypes.EndsWith("*") ? nonPtrWrappedTypes : $"{nonPtrWrappedTypes}*")
							.ToArray();
						Console.WriteLine(string.Join(", ", ptrWrappedTypes));

						var polyWrapperDecl = new UnrealJsonSerializableTypeDeclaration
						{
							UnrealTypeName = overridenWrapperType,
							NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(overridenWrapperType),
							PolymorphicWrappedTypes =
								ptrWrappedTypes.Select(s => new PolymorphicWrappedData { UnrealType = s, ExpectedTypeValue = polymorphicWrappedSchemaExpectedTypeValues[s] })
									.ToList(),
							UPropertyDeclarations = ptrWrappedTypes.Select(s => new UnrealPropertyDeclaration
							{
								PropertyUnrealType = s,
								PropertyName = polymorphicWrappedSchemaExpectedTypeValues[s].Capitalize(),
								RawFieldName = polymorphicWrappedSchemaExpectedTypeValues[s],
								PropertyDisplayName = polymorphicWrappedSchemaExpectedTypeValues[s].Capitalize(),
								NonOptionalTypeName = GetNamespacedTypeNameFromUnrealType(s),
							}).ToList(),
							PropertyIncludes = ptrWrappedTypes.Select(GetIncludeStatementForUnrealType).ToList(),
						};

						// We only need this include if we have any array, wrapper or optional types --- since this is a template it's worth not including it to keep compile times as small as we can have them.
						polyWrapperDecl.JsonUtilsInclude = "#include \"Serialization/BeamJsonUtils.h\"";
						polymorphicWrappersDeclarations.Add(polyWrapperDecl);
					}

					// Make the new property declaration for this field.
					var propertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealType, fieldName, kSchemaGenerationBuilder);
					var nonOptionalUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(namedOpenApiSchema.Document, fieldSchema, out _);
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

					// Check if this is an optional type, if it is --- declare it. (We don't support optional arrays of poly wrappers)
					if (unrealType.StartsWith(UNREAL_OPTIONAL))
					{
						optionalTypes.Add(new UnrealOptionalDeclaration
						{
							UnrealTypeName = unrealType,
							NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(unrealType),
							UnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(unrealType),
							ValueUnrealTypeName = nonOptionalUnrealType,
							ValueNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(nonOptionalUnrealType),
							ValueUnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(nonOptionalUnrealType)
						});
					}

					// For Unreal arrays and maps, we store the Relevant Template parameter.
					if (nonOptionalUnrealType.StartsWith(UNREAL_MAP))
						uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOptionalUnrealType);
					if (nonOptionalUnrealType.StartsWith(UNREAL_ARRAY))
						uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOptionalUnrealType);

					AddJsonAndDefaultValueHelperIncludesIfNecessary(unrealType, ref serializableTypeDeclaration);

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

				jsonSerializableTypes.Add(serializableTypeDeclaration);
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
	private static void BuildSchemaNameOverlapsMap()
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
	private static void BuildEndpointNameCollidedMaps(IReadOnlyList<OpenApiDocument> openApiDocuments)
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

				var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var endpointName = GetSubsystemNamespacedEndpointName(serviceName, serviceType, operationType, endpointPath);

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
	private static void BuildRequiredFieldMaps(IReadOnlyList<OpenApiDocument> openApiDocuments)
	{
		foreach (var ns in namedOpenApiSchemata)
		{
			var properties = ns.Schema.Properties;
			foreach ((string fieldName, OpenApiSchema _) in properties)
			{
				var handle = GetFieldDeclarationHandle(GetNamespacedSerializableTypeFromSchema(ns.Document, ns.Name, false), fieldName);
				fieldSchemaRequiredMap.TryAdd(handle, ns.Schema.Required.Contains(fieldName));
			}

			foreach (var openApiDocument in openApiDocuments)
			{
				GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
						foreach (var param in value.Parameters)
						{
							var paramOwnerId = GetEndpointFieldOwner(serviceName, serviceType, operationType, endpointPath);
							var handle = GetFieldDeclarationHandle(paramOwnerId, $"{param.Name}");
							fieldSchemaRequiredMap.TryAdd(handle, param.Required);
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
	private static void BuildSemanticTypesUnderlyingTypeMaps(IReadOnlyList<OpenApiDocument> openApiDocuments)
	{
		foreach (var ns in namedOpenApiSchemata)
		{
			var properties = ns.Schema.Properties;
			foreach ((string fieldName, OpenApiSchema fieldSchema) in properties)
			{
				var handle = GetFieldDeclarationHandle(GetNamespacedSerializableTypeFromSchema(ns.Document, ns.Name, false), fieldName);

				// Array fields
				if (fieldSchema.Type == "array")
				{
					var isReference = fieldSchema.Items.Reference != null;
					var arrayTypeSchema = isReference ? fieldSchema.Items.GetEffective(ns.Document) : fieldSchema.Items;
					if (arrayTypeSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
					{
						var arraySerializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(ns.Document, arrayTypeSchema, out _, UnrealTypeGetFlags.NeverSemanticType);
						fieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, arraySerializationUnrealType);
						continue;
					}
				}

				// Map case
				if (fieldSchema.Type == "object" && fieldSchema.Reference == null && fieldSchema.AdditionalPropertiesAllowed)
				{
					if (fieldSchema.AdditionalProperties.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
					{
						var mapSerializationUnrealType =
							GetNonOptionalUnrealTypeFromFieldSchema(ns.Document, fieldSchema.AdditionalProperties, out _, UnrealTypeGetFlags.NeverSemanticType);
						fieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, mapSerializationUnrealType);
						continue;
					}
				}

				// Raw Semantic Type case
				if (fieldSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var extension) && extension is OpenApiString)
				{
					var serializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(ns.Document, fieldSchema, out _, UnrealTypeGetFlags.NeverSemanticType);
					fieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, serializationUnrealType);
				}
			}

			foreach (var openApiDocument in openApiDocuments)
			{
				GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
						foreach (var param in value.Parameters)
						{
							var paramOwnerId = GetEndpointFieldOwner(serviceName, serviceType, operationType, endpointPath);
							var handle = GetFieldDeclarationHandle(paramOwnerId, $"{param.Name}");

							var fieldSchema = param.Schema;

							// Array fields
							if (fieldSchema is { Type: "array" })
							{
								var isReference = fieldSchema.Items.Reference != null;
								var arrayTypeSchema = isReference ? fieldSchema.Items.GetEffective(openApiDocument) : fieldSchema.Items;
								if (arrayTypeSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
								{
									var arraySerializationUnrealType =
										GetNonOptionalUnrealTypeFromFieldSchema(ns.Document, arrayTypeSchema, out _, UnrealTypeGetFlags.NeverSemanticType);
									fieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, arraySerializationUnrealType);
									continue;
								}
							}

							// Map case
							if (fieldSchema is { Type: "object", Reference: null, AdditionalPropertiesAllowed: true })
							{
								if (fieldSchema.AdditionalProperties.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
								{
									var mapSerializationUnrealType =
										GetNonOptionalUnrealTypeFromFieldSchema(ns.Document, fieldSchema.AdditionalProperties, out _, UnrealTypeGetFlags.NeverSemanticType);
									fieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, mapSerializationUnrealType);
									continue;
								}
							}

							// Raw Semantic Type case
							if (fieldSchema != null && fieldSchema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var extension) && extension is OpenApiString)
							{
								var serializationUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(ns.Document, fieldSchema, out _, UnrealTypeGetFlags.NeverSemanticType);
								fieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, serializationUnrealType);
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
	private static void BuildSubsystemDeclarations(IReadOnlyList<OpenApiDocument> openApiDocuments, out List<UnrealApiSubsystemDeclaration> outSubsystemDeclarations,
		out List<UnrealJsonSerializableTypeDeclaration> outResponseWrapperTypes)
	{
		var isMsGen = UnrealSourceGenerator.genType == GenerationType.Microservice;
		outSubsystemDeclarations = new List<UnrealApiSubsystemDeclaration>(openApiDocuments.Count);
		outResponseWrapperTypes = new List<UnrealJsonSerializableTypeDeclaration>(openApiDocuments.Count);
		foreach (var openApiDocument in openApiDocuments)
		{
			GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);

			var isMSGen = genType == GenerationType.Microservice;
			var unrealServiceDecl = new UnrealApiSubsystemDeclaration { ServiceName = serviceName, SubsystemName = isMSGen ? $"{serviceName.Capitalize()}Ms" : serviceName.Capitalize(), };

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

					// Find the service type from the document
					var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
					if (isMsGen) serviceType = ServiceType.Basic; // We are never an object/api service if we are generating Microservice client code.

					if (genType == GenerationType.BasicObject)
					{
						unrealEndpoint.GlobalNamespacedEndpointName =
							GetSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, serviceType, operationType, endpointPath, globalEndpointNameCollisions);
						unrealEndpoint.SubsystemNamespacedEndpointName =
							GetSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, serviceType, operationType, endpointPath, perSubsystemCollisions[serviceName]);
					}
					else if (genType == GenerationType.Microservice)
					{
						unrealEndpoint.GlobalNamespacedEndpointName =
							GetMicroserviceSubsystemGlobalNamespacedEndpointName(unrealServiceDecl.SubsystemName, endpointPath, globalEndpointNameCollisions);
						unrealEndpoint.SubsystemNamespacedEndpointName =
							GetMicroserviceSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, endpointPath, perSubsystemCollisions[serviceName]);
					}

					unrealEndpoint.SelfUnrealType = $"U{unrealEndpoint.GlobalNamespacedEndpointName}Request*";
					unrealEndpoint.NamespacedOwnerServiceName = unrealServiceDecl.SubsystemName;
					// TODO: For now, we make all non-basic endpoints require auth. This is due to certain endpoints' OpenAPI spec not being correctly generated. We also need to correctly generate the server-only services in UE at a future date.
					unrealEndpoint.IsAuth = serviceType != ServiceType.Basic ||
											serviceTitle.Contains("inventory", StringComparison.InvariantCultureIgnoreCase) ||
											endpointData.Security[0].Any(kvp => kvp.Key.Reference.Id == "user");
					unrealEndpoint.EndpointName = endpointPath;
					unrealEndpoint.EndpointRoute = isMsGen ? $"micro_{openApiDocument.Info.Title}{endpointPath}" : endpointPath;
					unrealEndpoint.EndpointVerb = operationType switch
					{
						OperationType.Get => "Get",
						OperationType.Put => "Put",
						OperationType.Post => "Post",
						OperationType.Delete => "Delete",
						_ => throw new ArgumentOutOfRangeException()
					};

					// Declare Query and Path parameters (not expected to ever show up during C#MS client codegen
					unrealEndpoint.RequestQueryParameters = new List<UnrealPropertyDeclaration>(4);
					unrealEndpoint.RequestPathParameters = new List<UnrealPropertyDeclaration>(4);
					foreach (var param in endpointData.Parameters)
					{
						var paramSchema = param.Schema.Reference != null ? param.Schema.GetEffective(openApiDocument) : param.Schema;
						var paramOwnerId = GetEndpointFieldOwner(serviceName, serviceType, operationType, endpointPath);
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
						unrealProperty.PropertyUnrealType = GetUnrealTypeFromSchema(openApiDocument, paramFieldHandle, paramSchema, out _);
						unrealProperty.PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealProperty.PropertyUnrealType);
						unrealProperty.PropertyName =
							UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealProperty.PropertyUnrealType, param.Name, kSchemaGenerationBuilder);
						unrealProperty.RawFieldName = param.Name;
						unrealProperty.PropertyDisplayName = unrealProperty.PropertyName.SpaceOutOnUpperCase();
						unrealProperty.NonOptionalTypeName = GetNonOptionalUnrealTypeFromFieldSchema(openApiDocument, paramSchema, out _);
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
								Console.WriteLine(
									$"Skipping Endpoint Param. ENDPOINT={unrealEndpoint.GlobalNamespacedEndpointName}, PARAM={param.Name}, PARAM.IN={param.In.ToString()}");
								break;
						}
					}

					// Find and declare all request body properties. Request bodies must always point to a schema reference and can never be individual primitive types.
					unrealEndpoint.RequestBodyParameters = new List<UnrealPropertyDeclaration>(1);
					if (endpointData.RequestBody?.Content?.TryGetValue("application/json", out var requestMediaType) ?? false)
					{
						var bodySchema = requestMediaType.Schema.GetEffective(openApiDocument);

						var unrealProperty = new UnrealPropertyDeclaration();
						unrealProperty.PropertyUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(openApiDocument, bodySchema, out _);
						unrealProperty.PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealProperty.PropertyUnrealType);
						unrealProperty.PropertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealProperty.PropertyUnrealType, "Body", kSchemaGenerationBuilder);
						unrealProperty.BriefCommentString = $"The \"{unrealProperty.PropertyUnrealType}\" instance to use for the request.";

						unrealEndpoint.RequestBodyParameters.Add(unrealProperty);
					}


					// Find and declare the response-body ties.
					// If a single value is returned as a response (such as a "int Add(int a, int b)" client callable in a C#MS), we wrap that around
					// a response wrapper type. We do this because we need all response types to be UObjects in UE.
					if (endpointData.Responses.TryGetValue("200", out var response))
					{
						if (response.Content.TryGetValue("application/json", out var jsonResponse))
						{
							if (jsonResponse.Schema.Reference != null)
							{
								var bodySchema = jsonResponse.Schema.GetEffective(openApiDocument);
								var ueType = unrealEndpoint.ResponseBodyUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(openApiDocument, bodySchema, out _);
								unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
								unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

								// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
								unrealTypesUsedAsResponses.Add(new TypeRequestBody { UnrealType = ueType, Type = ResponseBodyType.Json, });

								using var sw = new StringWriter();
								var writer = new OpenApiJsonWriter(sw);
								bodySchema.SerializeAsV3WithoutReference(writer);
								Console.WriteLine($"{serviceTitle}-{serviceName}-{unrealEndpoint.GlobalNamespacedEndpointName} FROM {operationType.ToString()} {endpointPath}\n" +
												  string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
												  "\n" + string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
												  "\n" + string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
												  $"\n{unrealEndpoint.ResponseBodyUnrealType}" +
												  $"\n{sw}");
							}
							else
							{
								var bodySchema = jsonResponse.Schema.GetEffective(openApiDocument);

								// Prepare the wrapper around the primitive this endpoint returns as a response payload.
								var wrapperBody = new UnrealJsonSerializableTypeDeclaration
								{
									UnrealTypeName = $"U{unrealEndpoint.GlobalNamespacedEndpointName}Response*",
									NamespacedTypeName = $"{unrealEndpoint.GlobalNamespacedEndpointName}Response",
									PropertyIncludes = new List<string>(8),
									UPropertyDeclarations = new List<UnrealPropertyDeclaration>(8),
									JsonUtilsInclude = "",
									DefaultValueHelpersInclude = "",
									IsResponseBodyType = ResponseBodyType.PrimitiveWrapper,
								};

								// Make the new property declaration for this field.
								var unrealType = GetNonOptionalUnrealTypeFromFieldSchema(openApiDocument, bodySchema, out _);
								var fieldName = "Value";
								var propertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealType, fieldName, kSchemaGenerationBuilder);
								var propertyDisplayName = propertyName;
								var wrappedPrimitiveProperty = new UnrealPropertyDeclaration
								{
									PropertyUnrealType = unrealType,
									PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealType),
									PropertyName = propertyName,
									PropertyDisplayName = propertyDisplayName.SpaceOutOnUpperCase(),
									RawFieldName = fieldName,
									NonOptionalTypeName = unrealType,
								};
								wrapperBody.UPropertyDeclarations.Add(wrappedPrimitiveProperty);
								AddJsonAndDefaultValueHelperIncludesIfNecessary(unrealType, ref wrapperBody, true);
								outResponseWrapperTypes.Add(wrapperBody);

								// Configure the endpoint
								var ueType = unrealEndpoint.ResponseBodyUnrealType = MakeUnrealUObjectTypeFromNamespacedType(wrapperBody.NamespacedTypeName);
								unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
								unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

								// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
								unrealTypesUsedAsResponses.Add(new TypeRequestBody { UnrealType = ueType, Type = ResponseBodyType.PrimitiveWrapper, });

								using var sw = new StringWriter();
								var writer = new OpenApiJsonWriter(sw);
								bodySchema.SerializeAsV3WithoutReference(writer);
								Console.WriteLine($"{serviceTitle}-{serviceName}-{unrealEndpoint.GlobalNamespacedEndpointName} FROM {operationType.ToString()} {endpointPath}\n" +
												  string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
												  "\n" + string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
												  "\n" + string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}")) +
												  $"\n{unrealEndpoint.ResponseBodyUnrealType}" +
												  $"\n{sw}");
							}
						}
						else if (response.Content.TryGetValue("text/plain", out jsonResponse))
						{
							var ueType = unrealEndpoint.ResponseBodyUnrealType = UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE;
							unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
							unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

							// We don't add this type to the list of response types as this type is NOT autogenerated.
						}
						else if (response.Content.TryGetValue("text/csv", out var csvResponse))
						{
							var responseType = csvResponse.Schema.GetEffective(openApiDocument);
							var ueType = unrealEndpoint.ResponseBodyUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(openApiDocument, responseType, out _);
							unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
							unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

							// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
							unrealTypesUsedAsResponses.Add(new TypeRequestBody { UnrealType = ueType, Type = ResponseBodyType.Csv, });
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
				unrealServiceDecl.GetAllEndpoints().Select(e =>
					$"#include \"{headerFileOutputPath}AutoGen/SubSystems/{e.NamespacedOwnerServiceName}/{e.GlobalNamespacedEndpointName}Request.h\"")
			);

			// If we had declared it already, replace that old declaration with the new one.
			if (alreadyDeclared) outSubsystemDeclarations.RemoveAll(d => d.SubsystemName == unrealServiceDecl.SubsystemName);

			unrealServiceDecl.EndpointUFunctionWithRetryDeclarations = unrealServiceDecl.EndpointUFunctionDeclarations;
			unrealServiceDecl.AuthenticatedEndpointUFunctionWithRetryDeclarations = unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations;
			outSubsystemDeclarations.Add(unrealServiceDecl);
		}
	}

	private static void AddJsonAndDefaultValueHelperIncludesIfNecessary(string unrealType, ref UnrealJsonSerializableTypeDeclaration serializableTypeData, bool forceJson = false,
		bool forceDefaultHelper = false)
	{
		// If this is a field that will require BeamJsonUtils for deserialization --- add it to the list of includes of this type.
		if (forceJson || IsUnrealContainerOrWrapperType(unrealType))
		{
			// We only need this include if we have any array, wrapper or optional types --- since this is a template it's worth not including it to keep compile times as small as we can have them.
			serializableTypeData.JsonUtilsInclude = string.IsNullOrEmpty(serializableTypeData.JsonUtilsInclude)
				? "#include \"Serialization/BeamJsonUtils.h\""
				: serializableTypeData.JsonUtilsInclude;
		}

		// Decide if we need to add the default value helper in order to parse primitive numeric types
		if (forceDefaultHelper || IsUnrealPrimitiveType(unrealType))
		{
			serializableTypeData.DefaultValueHelpersInclude = string.IsNullOrEmpty(serializableTypeData.DefaultValueHelpersInclude)
				? "#include \"Misc/DefaultValueHelper.h\""
				: serializableTypeData.DefaultValueHelpersInclude;
		}
	}

	private static bool IsUnrealContainerOrWrapperType(string unrealType)
	{
		return unrealType.StartsWith(UNREAL_ARRAY) || unrealType.StartsWith(UNREAL_MAP) || unrealType.StartsWith(UNREAL_OPTIONAL) ||
			   unrealType.StartsWith(UNREAL_U_OBJECT_PREFIX) ||
			   UNREAL_ALL_SEMTYPES.Contains(unrealType);
	}

	private static bool IsUnrealPrimitiveType(string unrealType)
	{
		return unrealType.StartsWith(UNREAL_BYTE) || unrealType.StartsWith(UNREAL_SHORT) || unrealType.StartsWith(UNREAL_INT) || unrealType.StartsWith(UNREAL_LONG) ||
			   unrealType.StartsWith(UNREAL_FLOAT) || unrealType.StartsWith(UNREAL_DOUBLE);
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
	private static string GetEndpointFieldOwner(string serviceName, ServiceType serviceType, OperationType operationType, string endpointPath)
	{
		switch (genType)
		{
			case GenerationType.BasicObject:
				return $"{serviceName}_{GetSubsystemNamespacedEndpointName(serviceName, serviceType, operationType, endpointPath, globalEndpointNameCollisions)}";
			case GenerationType.Microservice:
				return $"{serviceName}_{GetMicroserviceSubsystemGlobalNamespacedEndpointName(serviceName, endpointPath, globalEndpointNameCollisions)}";
			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	/// <summary>
	/// Generates the service title (basic/object) and the service name (auth, realms, etc...) from the OpenApiInfo struct.
	/// </summary>
	public static void GetNamespacedServiceNameFromApiDoc(OpenApiInfo parentDocInfo, out string serviceTitle, out string serviceName)
	{
		var serviceNames = parentDocInfo.Title.Split(" ");
		serviceTitle = serviceNames.Length == 1 ? "Basic" : serviceNames[1].Sanitize().Capitalize();

		serviceName = serviceNames[0].Sanitize().Capitalize();
	}

	public static ServiceType GetServiceTypeFromDocTitle(string serviceTitle)
	{
		if (string.IsNullOrEmpty(serviceTitle)) return ServiceType.Basic;
		if (serviceTitle.Contains("object", StringComparison.OrdinalIgnoreCase)) return ServiceType.Object;
		if (serviceTitle.Contains("actor", StringComparison.OrdinalIgnoreCase)) return ServiceType.Api;
		return ServiceType.Basic;
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
	public static string GetNamespacedSerializableTypeFromSchema(OpenApiDocument parentDoc, string schemaName, bool isOptional, bool isCsvRow = false)
	{
		var hasMappedOverlaps = schemaNameOverlaps.TryGetValue(schemaName, out var overlaps);
		if (hasMappedOverlaps && overlaps.Count > 1)
		{
			// We then check if the properties on all declarations are the same, if they are we'll consider them the same type for purposes of the SDK.
			var allPropertiesAreEqual = true;
			for (int i = 0; i < overlaps.Count - 1; i++)
			{
				var schemaProperties1 = overlaps[i].Schema.Properties.Keys.ToImmutableSortedSet();
				var schemaProperties2 = overlaps[i + 1].Schema.Properties.Keys.ToImmutableSortedSet();
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

		if (isCsvRow)
			schemaName = $"{schemaName}TableRow";

		if (genType == GenerationType.Microservice)
			schemaName = $"{parentDoc.Info.Title.Sanitize()}{schemaName}";

		return isOptional ? $"Optional{schemaName}" : schemaName;
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="serviceType"></param>
	/// <param name="httpVerb"></param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	public static string GetSubsystemNamespacedEndpointName(string serviceName, ServiceType serviceType, OperationType httpVerb, string endpointPath,
		Dictionary<string, bool> endpointNameOverlaps = null)
	{
		// If an object service, we need to skip 4 '/' to get what we want (/object/mail/{objectId}/whatWeWant)
		var nameRelevantPath = endpointPath.Substring(endpointPath.IndexOf('/', 1) + 1);
		nameRelevantPath = nameRelevantPath.Substring(nameRelevantPath.IndexOf('/') + 1);

		var methodName = SwaggerService.FormatPathNameAsMethodName(nameRelevantPath);

		// Capitalize the name
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
			var conflictResolutionPrefix = serviceType switch
			{
				ServiceType.Api => $"Api{serviceName}",
				ServiceType.Basic => $"Basic{serviceName}",
				ServiceType.Object => $"Object{serviceName}",
				_ => throw new Exception($"Should not be possible for service {serviceName} to fall here.")
			};
			methodName = conflictResolutionPrefix + methodName;
		}

		// In case we want to manually override an endpoint's name...
		return NAMESPACED_ENDPOINT_OVERRIDES.ContainsKey(methodName) ? NAMESPACED_ENDPOINT_OVERRIDES[methodName] : methodName;
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="isObjectService"></param>
	/// <param name="httpVerb"></param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	public static string GetMicroserviceSubsystemGlobalNamespacedEndpointName(string serviceName, string endpointPath, Dictionary<string, bool> endpointNameOverlaps = null)
	{
		// Capitalize the name
		var methodName = endpointPath;
		methodName = methodName.Length > 1 ? methodName.Capitalize() : methodName;

		// Ensure we don't have '-', '/' and others in the name
		methodName = methodName.Sanitize();

		// If the name here is empty (happens in routes like this "/object/mail/{objectId}/"), we'll use the service's own name as the method name.
		methodName = string.IsNullOrEmpty(methodName) ? serviceName : methodName;
		methodName = $"{serviceName.Capitalize()}{methodName}";

		// Check and see if we have an name collision with some other service endpoint
		var doesConflict = endpointNameOverlaps != null && endpointNameOverlaps.TryGetValue(methodName, out var conflicts) && conflicts;
		if (doesConflict)
		{
			throw new ArgumentException($"{methodName} was found in more than one service. " +
										$"In this case, this is because you have two microservices with the same name OR because this name clashes with an existing Beamable API. " +
										$"Please change your Microservice name to resolve this.");
		}

		// In case we want to manually override an endpoint's name...
		return methodName;
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	public static string GetMicroserviceSubsystemNamespacedEndpointName(string serviceName, string endpointPath, Dictionary<string, bool> endpointNameOverlaps = null)
	{
		// Capitalize the name
		var methodName = endpointPath;
		methodName = methodName.Length > 1 ? methodName.Capitalize() : methodName;

		// Ensure we don't have '-', '/' and others in the name
		methodName = methodName.Sanitize();

		// If the name here is empty (happens in routes like this "/object/mail/{objectId}/"), we'll use the service's own name as the method name.
		methodName = string.IsNullOrEmpty(methodName) ? serviceName : methodName;
		methodName = $"{methodName}";

		// Check and see if we have an name collision with some other service endpoint
		var doesConflict = endpointNameOverlaps != null && endpointNameOverlaps.TryGetValue(methodName, out var conflicts) && conflicts;
		if (doesConflict)
		{
			throw new ArgumentException($"{methodName} was overloaded in {serviceName}. " +
										$"We do not support overloading Callable/ClientCallable/AdminCallable functions." +
										$"Please rename all overloads to resolve this.");
		}

		// In case we want to manually override an endpoint's name...
		return methodName;
	}


	/*
	 * UNREAL TYPE FUNCTIONS ---- THESE ARE MEANT TO CONVERT SCHEMA DECLARATIONS INTO THEIR FINAL TYPE IN UNREAL. THESE USES THE NAMESPACED FUNCTIONS ABOVE IN ORDER TO ENSURE NO NAME CONFLICTS HAPPEN.
	 * From an Unreal Type, we can find the namespaced type, check if it's an array, optional array, wrapper array, map and so on and so forth --- this allows us to specialize the code generation at
	 * each point using UE's own prefixes and code patterns. This is a good thing since we need to enforce this standard anyway to remain 100% BP compatible.
	 */

	[Flags]
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
	public static string GetUnrealTypeFromSchema([NotNull] OpenApiDocument parentDoc, [NotNull] string fieldDeclarationHandle, [NotNull] OpenApiSchema schema,
		out string nonOverridenUnrealType,
		UnrealTypeGetFlags Flags = UnrealTypeGetFlags.None)
	{
		// The field is considered an optional type ONLY if it is in the dictionary AND it's value in the dictionary is false.
		// This dictionary must be built from all NamedSchemas's properties (fields) and contain true/false for whether or not that field of that type is required.
		var isOptional = !Flags.HasFlag(UnrealTypeGetFlags.NeverOptional) && fieldSchemaRequiredMap.TryGetValue(fieldDeclarationHandle, out var isRequired) && !isRequired;
		var isEnum = schema.GetEffective(parentDoc).Enum.Count > 0;
		var isCsvRow = IsCsvRowSchema(parentDoc, schema);

		var semType = "";
		if (!Flags.HasFlag(UnrealTypeGetFlags.NeverSemanticType) && schema.Extensions.TryGetValue(EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var ext) && ext is OpenApiString s)
			semType = s.Value;

		// Happens in the case where 
		var isPolymorphicWrapper = schema.OneOf.Count > 0;

		switch (schema.Type, schema.Format, schema.Reference?.Id, semType)
		{
			// Handle semantic types
			case (_, _, _, "Cid"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CID : UNREAL_U_SEMTYPE_CID;
			case (_, _, _, "Pid"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_PID : UNREAL_U_SEMTYPE_PID;
			case (_, _, _, "AccountId"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_ACCOUNTID : UNREAL_U_SEMTYPE_ACCOUNTID;
			case (_, _, _, "Gamertag"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_GAMERTAG : UNREAL_U_SEMTYPE_GAMERTAG;
			case (_, _, _, "ContentManifestId"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CONTENTMANIFESTID : UNREAL_U_SEMTYPE_CONTENTMANIFESTID;
			case (_, _, _, "ContentId"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CONTENTID : UNREAL_U_SEMTYPE_CONTENTID;
			case (_, _, _, "StatsType"):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_STATSTYPE : UNREAL_U_SEMTYPE_STATSTYPE;

			// Handle replacement types (types that we replace by hand-crafted types inside the SDK)
			case var (_, _, referenceId, _) when !string.IsNullOrEmpty(referenceId) && referenceId.Equals("ClientPermission", StringComparison.InvariantCultureIgnoreCase):
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_U_REPTYPE_CLIENTPERMISSION : UNREAL_U_REPTYPE_CLIENTPERMISSION;

			// Handles any field of any existing Schema Types
			case var (_, _, _, _) when isPolymorphicWrapper:
			{
				var str = "";
				foreach (OpenApiSchema openApiSchema in schema.OneOf)
				{
					var polyWrappedSchema = openApiSchema.GetEffective(parentDoc);
					var wrappedUnrealType = GetNonOptionalUnrealTypeFromFieldSchema(parentDoc, polyWrappedSchema, out _);
					str += $"_{RemovePtrFromUnrealTypeIfAny(wrappedUnrealType)}";

					if (polyWrappedSchema.Properties.TryGetValue("type", out var defaults))
					{
						var val = defaults.Default as OpenApiString;
						if (polymorphicWrappedSchemaExpectedTypeValues.TryGetValue(wrappedUnrealType, out var existing) &&
							(existing != val?.Value && existing != openApiSchema.Reference.Id.Sanitize()))
							throw new Exception(
								"Found a wrapped type that is currently used in two different ways. We don't support that cause it doesn't make a lot of sense. You should never see this.");

						polymorphicWrappedSchemaExpectedTypeValues.TryAdd(wrappedUnrealType, val?.Value ?? openApiSchema.Reference.Id.Sanitize());
					}
				}

				nonOverridenUnrealType = MakeUnrealUObjectTypeFromNamespacedType($"OneOf{str}");
				return UNREAL_TYPES_OVERRIDES.ContainsKey(nonOverridenUnrealType)
					? UNREAL_TYPES_OVERRIDES[nonOverridenUnrealType]
					: throw new Exception($"Should never see this!!! If you do, add an override to the UNREAL_TYPES_OVERRIDE with this as the key={nonOverridenUnrealType}");
			}
			case var (_, _, referenceId, _) when !string.IsNullOrEmpty(referenceId):
			{
				referenceId = GetNamespacedSerializableTypeFromSchema(parentDoc, referenceId, isOptional, isCsvRow);
				string unrealType;
				if (isOptional)
				{
					unrealType = $"F{referenceId}";

					if (isEnum)
						Console.WriteLine(
							$"ENUM ={unrealType}, {referenceId}, {string.Join("-", schema.GetEffective(parentDoc).Enum.OfType<OpenApiString>().Select(s => s.Value))}\n");
				}
				else if (isEnum)
				{
					unrealType = $"E{referenceId}";
				}
				else if (isCsvRow)
				{
					unrealType = $"F{referenceId}";
				}
				else
				{
					unrealType = MakeUnrealUObjectTypeFromNamespacedType(referenceId);
				}

				return nonOverridenUnrealType = unrealType;
			}
			// Handles any dictionary/map fields
			case ("object", _, _, _) when schema.Reference == null && schema.AdditionalPropertiesAllowed:
			{
				if (schema.AdditionalProperties == null)
					return nonOverridenUnrealType = UNREAL_MAP + $"<{UNREAL_STRING}, {UNREAL_STRING}>";

				// Get the data type but force it to not be an optional by passing in a blank field name!
				// We do this as it makes no sense to have an map of optionals --- the semantics for optional and maps are that the entire map is optional, instead.
				var dataType = GetNonOptionalUnrealTypeFromFieldSchema(parentDoc, schema.AdditionalProperties, out var nonOverridenDataType);

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

				// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
				// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
				if (nonOverridenDataType.StartsWith(UNREAL_MAP))
				{
					nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
					nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F' from the type.
					nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);
					nonOverridenDataType = UNREAL_WRAPPER_MAP + nonOverridenDataType;
				}
				else if (nonOverridenDataType.StartsWith(UNREAL_ARRAY))
				{
					nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
					nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
					nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);
					nonOverridenDataType = UNREAL_WRAPPER_ARRAY + nonOverridenDataType;
				}


				nonOverridenUnrealType = isOptional
					? UNREAL_OPTIONAL_MAP + $"{GetNamespacedTypeNameFromUnrealType(nonOverridenDataType)}" // Remove the "F" from the Unreal type when declaring an optional map
					: UNREAL_MAP + $"<{UNREAL_STRING}, {nonOverridenDataType}>";

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
					var dataType = GetNonOptionalUnrealTypeFromFieldSchema(parentDoc, arrayTypeSchema, out var nonOverridenDataType);
					dataType = GetNamespacedTypeNameFromUnrealType(dataType);
					nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);

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

					// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
					// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
					if (nonOverridenDataType.StartsWith(UNREAL_MAP))
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);
						nonOverridenDataType = UNREAL_WRAPPER_MAP + nonOverridenDataType;
					}
					else if (nonOverridenDataType.StartsWith(UNREAL_ARRAY))
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);
						nonOverridenDataType = UNREAL_WRAPPER_ARRAY + nonOverridenDataType;
					}

					// Remove the "F" from the Unreal type when declaring an optional array
					nonOverridenUnrealType = UNREAL_OPTIONAL_ARRAY + $"{GetNamespacedTypeNameFromUnrealType(nonOverridenDataType)}";
					return UNREAL_OPTIONAL_ARRAY + $"{GetNamespacedTypeNameFromUnrealType(dataType)}";
				}
				else
				{
					var dataType = GetUnrealTypeFromSchema(parentDoc, fieldDeclarationHandle, arrayTypeSchema, out var nonOverridenDataType);

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

					// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
					// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
					if (nonOverridenDataType.StartsWith(UNREAL_MAP))
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);
						nonOverridenDataType = UNREAL_WRAPPER_MAP + nonOverridenDataType;
					}
					else if (nonOverridenDataType.StartsWith(UNREAL_ARRAY))
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(nonOverridenDataType);
						nonOverridenDataType = UNREAL_WRAPPER_ARRAY + nonOverridenDataType;
					}

					nonOverridenUnrealType = UNREAL_ARRAY + $"<{nonOverridenDataType}>";
					return UNREAL_ARRAY + $"<{dataType}>";
				}
			}
			// Handle Primitive Types 
			case ("number", "float", _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_FLOAT : UNREAL_FLOAT;
			}
			case ("number", "double", _, _):
			case ("number", _, _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_DOUBLE : UNREAL_DOUBLE;
			}
			case ("boolean", _, _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_BOOL : UNREAL_BOOL;
			}
			case ("string", "uuid", _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_GUID : UNREAL_GUID;
			}
			case ("string", "byte", _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_BYTE : UNREAL_BYTE;
			}
			case ("string", _, _, _) when (schema?.Extensions.TryGetValue("x-beamable-object-id", out _) ?? false):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_STRING : UNREAL_STRING;
			}
			case ("System.String", _, _, _):
			case ("string", _, _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_STRING : UNREAL_STRING;
			}
			case ("integer", "int16", _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_SHORT : UNREAL_SHORT;
			}
			case ("integer", "int32", _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_INT : UNREAL_INT;
			}
			case ("integer", "int64", _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_LONG : UNREAL_LONG;
			}
			case ("integer", _, _, _):
			{
				return nonOverridenUnrealType = isOptional ? UNREAL_OPTIONAL_INT : UNREAL_INT;
			}
			default:
				return nonOverridenUnrealType = "";
		}
	}

	/// <summary>
	/// Makes a UnrealType from a NamespacedType that the caller knows should become a UObject*.
	/// </summary>
	private static string MakeUnrealUObjectTypeFromNamespacedType(string referenceId) => $"U{referenceId.Capitalize()}*";

	private static bool IsCsvRowSchema(OpenApiDocument parentDoc, OpenApiSchema schema)
	{
		return schema.GetEffective(parentDoc).Extensions.ContainsKey("x-beamable-primary-key");
	}

	/// <summary>
	/// Gets the header file name for any given UnrealType.
	/// </summary>
	public static string GetIncludeStatementForUnrealType(string unrealType)
	{
		if (string.IsNullOrEmpty(unrealType))
			throw new Exception("Investigate this... It should never happen!");

		// First go over all non-generated first-class types
		{
			if (unrealType.StartsWith(UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE))
				return @"#include ""Serialization/BeamPlainTextResponseBody.h""";

			// TODO: Add sem type includes here... 
			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_CID))
				return @"#include ""BeamBackend/SemanticTypes/BeamCid.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_PID))
				return @"#include ""BeamBackend/SemanticTypes/BeamPid.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_ACCOUNTID))
				return @"#include ""BeamBackend/SemanticTypes/BeamAccountId.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_GAMERTAG))
				return @"#include ""BeamBackend/SemanticTypes/BeamGamerTag.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_CONTENTID))
				return @"#include ""BeamBackend/SemanticTypes/BeamContentId.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_CONTENTMANIFESTID))
				return @"#include ""BeamBackend/SemanticTypes/BeamContentManifestId.h""";

			if (unrealType.StartsWith(UNREAL_U_SEMTYPE_STATSTYPE))
				return @"#include ""BeamBackend/SemanticTypes/BeamStatsType.h""";

			if (unrealType.StartsWith(UNREAL_U_REPTYPE_CLIENTPERMISSION))
				return @"#include ""BeamBackend/ReplacementTypes/BeamClientPermission.h""";

		}

		// Then, go over all generated types
		{
			if (unrealType.StartsWith(UNREAL_U_ENUM_PREFIX))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";

				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{headerFileOutputPath}AutoGen/Enums/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_OPTIONAL))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{headerFileOutputPath}AutoGen/Optionals/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_WRAPPER_ARRAY))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{headerFileOutputPath}AutoGen/Arrays/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_WRAPPER_MAP))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{headerFileOutputPath}AutoGen/Maps/{header}\"";
			}

			if (unrealType.StartsWith(UNREAL_ARRAY))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var firstTemplate = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(unrealType);
				if (MustInclude(firstTemplate))
				{
					return GetIncludeStatementForUnrealType(firstTemplate);
				}
			}

			if (unrealType.StartsWith(UNREAL_MAP))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var secondTemplate = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(unrealType);
				if (MustInclude(secondTemplate))
				{
					return GetIncludeStatementForUnrealType(secondTemplate);
				}
			}

			if (MustInclude(unrealType))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";

				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				if (unrealType.StartsWith(UNREAL_U_BEAM_NODE_PREFIX))
					return $"#include \"{blueprintHeaderFileOutputPath}AutoGen/{header}\"";

				return $"#include \"{headerFileOutputPath}AutoGen/{header}\"";
			}
		}

		return "";

		bool MustInclude(string s)
		{
			return s.StartsWith(UNREAL_U_OBJECT_PREFIX) || s.StartsWith(UNREAL_U_STRUCT_PREFIX) && s != UNREAL_GUID && s != UNREAL_STRING;
		}
	}

	/// <summary>
	/// Gets a guaranteed non-optional type for the given field schema, even if the field schema is in-fact Optional.
	/// We use this in order to get the correct type to pass into the serialization/deserialization templated functions that work with FBeamOptionals in Unreal-land.
	/// </summary>
	public static string GetNonOptionalUnrealTypeFromFieldSchema([NotNull] OpenApiDocument parentDoc, [NotNull] OpenApiSchema fieldSchema, out string nonOverridenName,
		UnrealTypeGetFlags flags = UnrealTypeGetFlags.NeverOptional)
	{
		// This passes in a blank field map which means it's not possible for it to found in the fieldRequiredMaps. This means we will get the Required Version of it. 
		return GetUnrealTypeFromSchema(parentDoc, "", fieldSchema, out nonOverridenName, UnrealTypeGetFlags.NeverOptional | flags);
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

		// F"AnyTypes"/E"AnyEnums" we just remove the F's/E's
		if (char.IsUpper(unrealTypeName[1]) && (unrealTypeName.StartsWith(UNREAL_U_STRUCT_PREFIX) || unrealTypeName.StartsWith(UNREAL_U_ENUM_PREFIX)))
			return unrealTypeName.AsSpan(1).ToString();

		// U"AnyTypes"* we just remove the U's and *'s
		if (char.IsUpper(unrealTypeName[1]) && unrealTypeName.StartsWith(UNREAL_U_OBJECT_PREFIX))
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
	/// Gets an Unreal type from a <see cref="System.Type"/>. Used primarily for code generating the CLI interface for invocation from inside Unreal. 
	/// </summary>
	public static string GetUnrealTypeFromReflectionType(Type unrealWrapperType)
	{
		static string GetPrimitive(Type type)
		{
			if (type == typeof(byte))
				return UNREAL_BYTE;
			if (type == typeof(short))
				return UNREAL_SHORT;
			if (type == typeof(int))
				return UNREAL_INT;
			if (type == typeof(long))
				return UNREAL_LONG;
			if (type == typeof(bool))
				return UNREAL_BOOL;
			if (type == typeof(float))
				return UNREAL_FLOAT;
			if (type == typeof(double))
				return UNREAL_DOUBLE;
			if (type == typeof(string))
				return UNREAL_STRING;
			if (type == typeof(Guid))
				return UNREAL_GUID;

			return "";
		}

		var primitiveType = GetPrimitive(unrealWrapperType);

		if (string.IsNullOrEmpty(primitiveType))
		{
			var isList = unrealWrapperType.IsGenericType && typeof(IList).IsAssignableFrom(unrealWrapperType);
			var isArray = unrealWrapperType.IsArray;
			if (isList || isArray)
			{
				var subType = isArray ? unrealWrapperType : unrealWrapperType.GenericTypeArguments[0];
				var primitive = GetPrimitive(subType);
				if (string.IsNullOrEmpty(primitive))
					throw new ArgumentException($"We don't support arrays of non-primitive types here. {unrealWrapperType.FullName}");
				return UNREAL_ARRAY + $"<{primitive}>";
			}

			var isDictionary = unrealWrapperType.IsGenericType && typeof(IDictionary).IsAssignableFrom(unrealWrapperType);
			if (isDictionary)
			{
				if (unrealWrapperType.GenericTypeArguments[0] != typeof(string))
					throw new ArgumentException($"We don't support non-string dictionaries here. {unrealWrapperType.FullName}");

				var subType = unrealWrapperType.GenericTypeArguments[1];
				var primitive = GetPrimitive(subType);
				if (string.IsNullOrEmpty(primitive))
					throw new ArgumentException($"We don't support maps of non-primitive types here. {unrealWrapperType.FullName}");

				return UNREAL_MAP + $"<{UNREAL_STRING}, {primitive}>";
			}
		}
		else
		{
			return primitiveType;
		}


		throw new ArgumentException($"We only support arrays of primitives and string maps of primitives (Dictionary<string, Primitive>, List<Primitive> or Primitive[]). {unrealWrapperType.Name}");
	}

	/// <summary>
	/// If the given <paramref name="ueType"/> ends with a '*' (as in, is a pointer declaration), we remove it. 
	/// </summary>
	public static string RemovePtrFromUnrealTypeIfAny(string ueType)
	{
		return ueType.EndsWith("*") ? ueType.Substring(0, ueType.Length - 1) : ueType;
	}
}

public class PreviousGenerationPassesData
{
	public Dictionary<string, string> InEngineTypeToIncludePaths = new();
}

public static class StringExtensions
{
	public static string SpaceOutOnUpperCase(this string word) => Regex.Replace(word.Capitalize(), @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");

	public static string Capitalize(this string word)
	{
		return string.Concat(char.ToUpper(word[0]).ToString(), word.AsSpan(1));
	}

	public static string UnCapitalize(this string word)
	{
		return string.Concat(char.ToLower(word[0]).ToString(), word.AsSpan(1));
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
			if (propKey[i] == '-' || propKey[i] == '/' || propKey[i] == '$' || propKey[i] == '_')
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
