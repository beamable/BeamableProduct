using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Beamable.Common;
using Beamable.Server;
using Docker.DotNet.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;
using static Beamable.Common.Constants.Features.Services;

namespace cli.Unreal;

public class UnrealSourceGenerator : SwaggerService.ISourceGenerator
{
	// Start of Special-Cased Types
	public static readonly UnrealType UNREAL_STRING = new("FString");
	public static readonly UnrealType UNREAL_BYTE = new("uint8");
	public static readonly UnrealType UNREAL_SHORT = new("int16");
	public static readonly UnrealType UNREAL_INT = new("int32");
	public static readonly UnrealType UNREAL_LONG = new("int64");
	public static readonly UnrealType UNREAL_BOOL = new("bool");
	public static readonly UnrealType UNREAL_FLOAT = new("float");
	public static readonly UnrealType UNREAL_DOUBLE = new("double");
	public static readonly UnrealType UNREAL_GUID = new("FGuid");
	public static readonly UnrealType UNREAL_DATE_TIME = new("FDateTime");
	public static readonly UnrealType UNREAL_JSON = new("TSharedPtr<FJsonObject>");
	public static readonly UnrealType UNREAL_OPTIONAL = new("FOptional");
	public static readonly UnrealType UNREAL_OPTIONAL_STRING = new($"{UNREAL_OPTIONAL}String");
	public static readonly UnrealType UNREAL_OPTIONAL_BYTE = new($"{UNREAL_OPTIONAL}UInt8");
	public static readonly UnrealType UNREAL_OPTIONAL_SHORT = new($"{UNREAL_OPTIONAL}Int16");
	public static readonly UnrealType UNREAL_OPTIONAL_INT = new($"{UNREAL_OPTIONAL}Int32");
	public static readonly UnrealType UNREAL_OPTIONAL_LONG = new($"{UNREAL_OPTIONAL}Int64");
	public static readonly UnrealType UNREAL_OPTIONAL_BOOL = new($"{UNREAL_OPTIONAL}Bool");
	public static readonly UnrealType UNREAL_OPTIONAL_FLOAT = new($"{UNREAL_OPTIONAL}Float");
	public static readonly UnrealType UNREAL_OPTIONAL_DOUBLE = new($"{UNREAL_OPTIONAL}Double");
	public static readonly UnrealType UNREAL_OPTIONAL_GUID = new($"{UNREAL_OPTIONAL}Guid");
	public static readonly UnrealType UNREAL_OPTIONAL_DATE_TIME = new($"{UNREAL_OPTIONAL}DateTime");
	public static readonly UnrealType UNREAL_ARRAY = new("TArray");
	public static readonly UnrealType UNREAL_OPTIONAL_ARRAY = new($"{UNREAL_OPTIONAL}ArrayOf");
	public static readonly UnrealType UNREAL_WRAPPER_ARRAY = new("FArrayOf");
	public static readonly UnrealType UNREAL_MAP = new("TMap");
	public static readonly UnrealType UNREAL_OPTIONAL_MAP = new($"{UNREAL_OPTIONAL}MapOf");
	public static readonly UnrealType UNREAL_WRAPPER_MAP = new("FMapOf");
	public static readonly UnrealType UNREAL_U_ENUM_PREFIX = new("E");
	public static readonly UnrealType UNREAL_U_OBJECT_PREFIX = new("U");
	public static readonly UnrealType UNREAL_U_STRUCT_PREFIX = new("F");
	public static readonly UnrealType UNREAL_U_POLY_WRAPPER_PREFIX = new("UOneOf_");
	public static readonly UnrealType UNREAL_U_HTML_TEXT_RESPONSE_TYPE = new("UHtmlResponse*");

	public static readonly UnrealType UNREAL_U_BEAM_NODE_PREFIX = new("UK2BeamNode");
	public static readonly UnrealType UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE = new("UBeamPlainTextResponseBody*");
	// End of Special Case Types


	/// <summary>
	/// These overrides are applied in <see cref="GetNamespacedServiceNameFromApiDoc"/> so that we can override the names of services
	/// that'll exist in Unreal as a Categories for the BP. 
	/// </summary>
	public static readonly Dictionary<string, string> SERVICE_NAME_OVERRIDES;

	/// <summary>
	/// These overrides are applied in <see cref="GetNamespacedTypeNameFromSchema"/> so that we can override the names of schemas (literal schemas that show up in the content/schemas path of the JSON)
	/// that'll exist in Unreal as a UObject that can be deserialized. Embedded schemas (such as the ones required for polymorphic fields using OneOf) are overriden by <see cref="POLYMORPHIC_WRAPPER_TYPE_OVERRIDES"/>.
	/// TODO: Over time, we should probably move this into its own partial file of this type.
	/// </summary>
	public static readonly Dictionary<string, NamespacedType> NAMESPACED_TYPES_OVERRIDES;

	/// <summary>
	/// These overrides are applied in <see cref="GetNamespacedServiceNameFromApiDoc"/> so that we can override specific endpoint names for things that make more sense on the client.
	/// TODO: Over time, we should probably move this into its own partial file of this type.
	/// </summary>
	public static readonly Dictionary<string, string> NAMESPACED_ENDPOINT_OVERRIDES;

	/// <summary>
	/// These overrides are applied using <see cref="GetUnrealTypeForField"/>. This is used for types that we only discover in the middle of processing the schemas for a document.
	/// Things like Optionals, BeamArray/Map and any polymorphic type (schema containing 'OneOf').
	/// TODO: Over time, we should probably move this into its own partial file of this type.
	/// </summary>
	public static readonly Dictionary<string, UnrealType> POLYMORPHIC_WRAPPER_TYPE_OVERRIDES;


	// Start of Semantic Types
	private static readonly UnrealType UNREAL_U_SEMTYPE_CID = new("FBeamCid");
	private static readonly UnrealType UNREAL_U_SEMTYPE_PID = new("FBeamPid");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ACCOUNTID = new("FBeamAccountId");
	private static readonly UnrealType UNREAL_U_SEMTYPE_GAMERTAG = new("FBeamGamerTag");
	private static readonly UnrealType UNREAL_U_SEMTYPE_CONTENTMANIFESTID = new("FBeamContentManifestId");
	private static readonly UnrealType UNREAL_U_SEMTYPE_CONTENTID = new("FBeamContentId");
	private static readonly UnrealType UNREAL_U_SEMTYPE_STATSTYPE = new("FBeamStatsType");

	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_CID = new("TArray<FBeamCid>");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_PID = new("TArray<FBeamPid>");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_ACCOUNTID = new("TArray<FBeamAccountId>");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_GAMERTAG = new("TArray<FBeamGamerTag>");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_CONTENTMANIFESTID = new("TArray<FBeamContentManifestId>");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_CONTENTID = new("TArray<FBeamContentId>");
	private static readonly UnrealType UNREAL_U_SEMTYPE_ARRAY_STATSTYPE = new("TArray<FBeamStatsType>");

	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_CID = new($"{UNREAL_OPTIONAL}BeamCid");
	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_PID = new($"{UNREAL_OPTIONAL}BeamPid");
	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_ACCOUNTID = new($"{UNREAL_OPTIONAL}BeamAccountId");
	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_GAMERTAG = new($"{UNREAL_OPTIONAL}BeamGamerTag");
	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_CONTENTMANIFESTID = new($"{UNREAL_OPTIONAL}BeamContentManifestId");
	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_CONTENTID = new($"{UNREAL_OPTIONAL}BeamContentId");
	private static readonly UnrealType UNREAL_OPTIONAL_U_SEMTYPE_STATSTYPE = new($"{UNREAL_OPTIONAL}BeamStatsType");

	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_CID = new($"{UNREAL_OPTIONAL_ARRAY}BeamCid");
	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_PID = new($"{UNREAL_OPTIONAL_ARRAY}BeamPid");
	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_ACCOUNTID = new($"{UNREAL_OPTIONAL_ARRAY}BeamAccountId");
	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_GAMERTAG = new($"{UNREAL_OPTIONAL_ARRAY}BeamGamerTag");
	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_CONTENTMANIFESTID = new($"{UNREAL_OPTIONAL_ARRAY}BeamContentManifestId");
	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_CONTENTID = new($"{UNREAL_OPTIONAL_ARRAY}BeamContentId");
	private static readonly UnrealType UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_STATSTYPE = new($"{UNREAL_OPTIONAL_ARRAY}BeamStatsType");

	public static readonly List<UnrealType> UNREAL_ALL_SEMTYPES;
	public static readonly List<string> UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES;
	// End of Semantic Types

	/// <summary>
	/// Exists so we don't keep reallocating while building the field names.
	/// </summary>
	private static readonly StringBuilder kSchemaGenerationBuilder = new(2048);

	static UnrealSourceGenerator()
	{
		NAMESPACED_TYPES_OVERRIDES = new() { { "Player", new("PlayerId") }, { "DeleteRole", new("DeleteRoleRequestBody") } };
		NAMESPACED_ENDPOINT_OVERRIDES = new() { { "PostToken", "Authenticate" } };
		POLYMORPHIC_WRAPPER_TYPE_OVERRIDES = new()
		{
			{ "UOneOf_UContentReference_UTextReference_UBinaryReference*", new("UBaseContentReference*") },
			{ "UOneOf_UCronTrigger_UExactTrigger*", new("UBeamJobTrigger*") },
			{ "UOneOf_UHttpCall_UPublishMessage_UServiceCall*", new("UBeamJobType*") }
		};

		SERVICE_NAME_OVERRIDES = new() { { "PlayerParty", "Party" }, { "Social", "Friends" }, };

		UNREAL_ALL_SEMTYPES = new List<UnrealType>
		{
			UNREAL_U_SEMTYPE_CID,
			UNREAL_U_SEMTYPE_PID,
			UNREAL_U_SEMTYPE_ACCOUNTID,
			UNREAL_U_SEMTYPE_GAMERTAG,
			UNREAL_U_SEMTYPE_CONTENTMANIFESTID,
			UNREAL_U_SEMTYPE_CONTENTID,
			UNREAL_U_SEMTYPE_STATSTYPE,
		};

		UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES = new()
		{
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_CID),
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_PID),
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_ACCOUNTID),
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_GAMERTAG),
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_CONTENTMANIFESTID),
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_CONTENTID),
			GetNamespacedTypeNameFromUnrealType(UNREAL_U_SEMTYPE_STATSTYPE),
		};
	}

	/// <summary>
	/// Set this before calling <see cref="Generate"/> so that you can define what the export macro will be used for generating the core types.
	/// </summary>
	public static string exportMacro = "BEAMABLECORE_API";

	/// <summary>
	/// Set this before calling <see cref="Generate"/> so that you can define what the export macro will be used for generating the core types.
	/// </summary>
	public static string blueprintExportMacro = "BEAMABLECOREBLUEPRINTNODES_API";

	/// <summary>
	/// Set this before calling <see cref="Generate"/> when you want to describe a prefix to add before the path to the autogen folder in include statements.
	/// Typically, this is the same as <see cref="headerFileOutputPath"/>. However, for microservice clients we remove the "Source/" part of the path.
	/// </summary>
	public static string includeStatementPrefix = "BeamableCore/Public/";

	/// <summary>
	/// Set this before calling <see cref="Generate"/> when you want to describe a prefix to add before the path to the autogen folder in include statements.
	/// Typically, this is the same as <see cref="blueprintHeaderFileOutputPath"/>. However, for microservice clients we remove the "Source/" part of the path.
	/// </summary>
	public static string blueprintIncludeStatementPrefix = "BeamableCoreBlueprintNodes/Public/BeamFlow/ApiRequest/";

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

	private sealed class UEGenerationContext
	{
		/// <summary>
		/// The list of all schemas found in the OAPI documents ingested.
		/// </summary>
		public IReadOnlyList<NamedOpenApiSchema> Schemas;

		/// <summary>
		/// The dictionary of <see cref="ReplacementTypeInfo"/> we take in. The key here must match the <see cref="OpenApiSchema.Reference"/>'s <see cref="OpenApiReference.Id"/>.
		/// </summary>
		public IReadOnlyDictionary<OpenApiReferenceId, ReplacementTypeInfo> ReplacementTypes;

		// Computed by parsing the OApi Documents

		/// <summary>
		/// Dictionary that maps <see cref="OpenApiReferenceId"/> to the list of schemas that share that ID. We use this to properly namespace schemas whose Ids are the same.
		/// </summary>
		public Dictionary<OpenApiReferenceId, List<NamedOpenApiSchema>> SchemaNameCollisions;

		/// <summary>
		/// Dictionary that maps whether or not a particular endpoint had its name colliding in the global namespace (across every other service).
		/// </summary>
		public Dictionary<string, bool> GlobalEndpointNameCollisions;

		/// <summary>
		/// Dictionary that maps, inside a single OAPI document, if there are endpoints whose names collide; if a name collides globally but not locally, we shorten the name
		/// of the functions in the Subsystem class for this API while resolving the global conflict. 
		/// </summary>
		public Dictionary<string, Dictionary<string, bool>> PerSubsystemEndpointNameCollisions;

		/// <summary>
		/// For each <see cref="FieldDeclarationHandle"/>, we track if that field is required or not. We use this to find out whether or not that field is <see cref="UNREAL_OPTIONAL"/>.
		/// </summary>
		public Dictionary<FieldDeclarationHandle, bool> FieldRequiredMap;

		/// <summary>
		/// For each <see cref="FieldDeclarationHandle"/>, we track the underlying semantic type of the field. This is only for use with <see cref="UNREAL_ALL_SEMTYPES"/>.
		/// Basically, we need to know what underlying type the backend is expecting this to be in the JSON so we can output the properly serialization code. IE: Serialize this gamertag field as long vs string.
		/// </summary>
		public Dictionary<FieldDeclarationHandle, UnrealType> FieldSemanticTypesUnderlyingTypeMap;

		/// <summary>
		/// As we build out the endpoint classes, we figure out which schemas are used as response types.
		/// These are kept here so that we can output an interface implementation for these types when we iterate over the schemas at a later step.
		/// </summary>
		public HashSet<TypeRequestBody> UnrealTypesUsedAsResponses;

		/// <summary>
		/// This gets filled set during the parsing of unreal types (<see cref="UnrealSourceGenerator.GetUnrealTypeForField"/>).
		/// The general idea is that, when we find a polymorphic wrapper type there we also find the 'string' value that we can expect its "type" field to have so that we know which wrapped type to deserialize. 
		/// </summary>
		public ConcurrentDictionary<string, string> PolymorphicWrappedSchemaExpectedTypeValues;

		// Computed from the ReplacementTypeInfo

		/// <summary>
		/// <see cref="IGenerationContext.ReplacementTypes"/> are types that are mapped to <see cref="OpenApiReferenceId"/>s.
		/// These end up not being generated and instead are expected to be declared manually by the engine code. These are here for manual replacement for UX reasons.
		/// For example, when the default type generation is a UObject but instead you'd rather it be a simple UStruct and other similar things. 
		/// </summary>
		public List<UnrealType> AllReplacementTypes;

		/// <summary>
		/// These are parallel to <see cref="AllReplacementTypes"/> and contain those types Optional versions. 
		/// </summary>
		public List<UnrealType> AllOptionalReplacementTypes;

		/// <summary>
		/// These are parallel to <see cref="AllReplacementTypes"/> and contain those types namespaced names.
		/// </summary>
		public List<NamespacedType> AllNamespacedReplacementTypes;

		/// <summary>
		/// These map the types in <see cref="AllReplacementTypes"/> to their Include strings. 
		/// </summary>
		public Dictionary<UnrealType, string> ReplacementTypesIncludes;
	}

	private sealed class UEGenerationOutput
	{
		/// <summary>
		/// List of all "generic" types that are JSON serializable that should be created.
		/// </summary>
		public List<UnrealJsonSerializableTypeDeclaration> JsonSerializableTypes;

		/// <summary>
		/// List of all Enums that must be created.
		/// </summary>
		public List<UnrealEnumDeclaration> EnumTypes;

		/// <summary>
		/// List of all Optional types that must be created.
		/// </summary>
		public List<UnrealOptionalDeclaration> OptionalTypes;

		/// <summary>
		/// List of all Wrapper Array types that must be created. Wrapper array types are FBeamArray implementations that are required for TArray{TArray{FSomeType}} as this would not be BP-compatible. 
		/// </summary>
		public List<UnrealWrapperContainerDeclaration> ArrayWrapperTypes;

		/// <summary>
		/// List of all Wrapper Map types that must be created. Wrapper Map types are FBeamMap implementations that are required for TArray{TMap{FString, FSomeType}} as this would not be BP-compatible. 
		/// </summary>
		public List<UnrealWrapperContainerDeclaration> MapWrapperTypes;

		/// <summary>
		/// List of all polymorphic wrapper declarations.
		/// </summary>
		public List<UnrealJsonSerializableTypeDeclaration> PolymorphicWrappersDeclarations;

		/// <summary>
		/// List of all UBeam____Api Subsystems declarations.
		/// </summary>
		public List<UnrealApiSubsystemDeclaration> SubsystemDeclarations;

		/// <summary>
		/// Whenever an endpoint returns a primitive value, we need to create a wrapper type to hold that primitive value. This is because UBeamBackend expects a UObject to be the Response in all cases.
		/// </summary>
		public List<UnrealJsonSerializableTypeDeclaration> ResponseWrapperTypes;
	}

	private static string GetLog(string header, string message)
	{
		return $"[UE-CODE-GEN] Type=[{genType}] => [{header}] {message}";
	}

	public List<GeneratedFileDescriptor> Generate(IGenerationContext context)
	{
		var outputFiles = new List<GeneratedFileDescriptor>(16);

		var ueGenContext = new UEGenerationContext()
		{
			Schemas = context.OrderedSchemas,
			ReplacementTypes = context.ReplacementTypes,

			// Will be computed by the pre-processing of the OAPI docs
			SchemaNameCollisions = new(),
			GlobalEndpointNameCollisions = new(),
			PerSubsystemEndpointNameCollisions = new(),
			FieldRequiredMap = new(),
			FieldSemanticTypesUnderlyingTypeMap = new(),
			UnrealTypesUsedAsResponses = new(),
			PolymorphicWrappedSchemaExpectedTypeValues = new(1, context.OrderedSchemas.Count),

			// Computed from the replacement types
			ReplacementTypesIncludes = context.ReplacementTypes.ToDictionary(g => new UnrealType(g.Value.EngineReplacementType), g => g.Value.EngineImport),
			AllReplacementTypes = context.ReplacementTypes.Select(kvp => new UnrealType(kvp.Value.EngineReplacementType)).ToList(),
			AllOptionalReplacementTypes = context.ReplacementTypes.Select(kvp => new UnrealType(kvp.Value.EngineOptionalReplacementType)).ToList(),
			AllNamespacedReplacementTypes = context.ReplacementTypes.Select(kvp => new UnrealType(kvp.Value.EngineReplacementType)).Select(GetNamespacedTypeNameFromUnrealType).ToList(),
		};

		var ueGenOutput = new UEGenerationOutput()
		{
			JsonSerializableTypes = new(),
			EnumTypes = new(),
			OptionalTypes = new(),
			ArrayWrapperTypes = new(),
			MapWrapperTypes = new(),
			PolymorphicWrappersDeclarations = new(),
			SubsystemDeclarations = new(),
			ResponseWrapperTypes = new(),
		};

		// Prepare Logs
		Log.Debug(GetLog("Generation Prep", $"Preparing all replacement types. ReplacementTypes=[{string.Join(",", ueGenContext.ReplacementTypes.Select(kvp => $"({kvp.Key}, {kvp.Value.EngineImport})"))}]"));

		// Build a list of dictionaries of schemas whose names appear in the list more than once.
		BuildSchemaIdCollisionMap(ueGenContext);

		// Build a list of dictionaries of endpoint names whose that are declared in more than one service.
		BuildEndpointNameCollidedMaps(context.Documents, ueGenContext);

		// Go through all properties of all schemas and see if they are required or not
		BuildRequiredFieldMaps(context.Documents, ueGenContext);
		BuildSemanticTypesUnderlyingTypeMaps(context.Documents, ueGenContext);

		// Build the data required to generate all subsystems and their respective endpoints
		BuildSubsystemDeclarations(context.Documents, ueGenContext, ueGenOutput);

		// Build the data required to generate all serializable types, enums, optionals, array and map wrapper types.
		// Array and Map Wrapper types are required due to UE's TMap and TArray not supporting nested data structures. As in, TArray<TArray<int>> doesn't work --- but TArray<FArrayOfInt> does.
		BuildSerializableTypeDeclarations(ueGenContext, ueGenOutput);

		// Generate the actual files we'll need from the data we've built.
		var processDictionary = new Dictionary<string, string>(16);

		// A dictionary that'll be filled with the UnrealType name of ONLY THE TYPES THAT'LL BE CONTAINED IN GENERATED FILES; the value is the path to this type.
		// INFO: UnrealTypes defined in "previousGenerationPassesData" will not be added here.
		var newGeneratedUnrealTypes = new Dictionary<UnrealType, string>();

		// Generate all Optional Type Files (except the ones that were already generated in a previous run).
		var optionalDeclarations = ueGenOutput.OptionalTypes
			.Except(ueGenOutput.OptionalTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
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
				new GeneratedFileDescriptor { FileName = headerFileName, Content = s.headerDeclaration },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
				new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}Library.h", Content = s.bpLibraryHeader },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Optionals/{s.decl.NamespacedTypeName}Library.cpp", Content = s.bpLibraryCpp },
			};
		}));

		// Generate Array Wrapper Type Files
		var arrayWrapperDeclarations = ueGenOutput.ArrayWrapperTypes
			.Except(ueGenOutput.ArrayWrapperTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
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
			return new[]
			{
				new GeneratedFileDescriptor { FileName = headerFileName, Content = s.headerDeclaration },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Arrays/{s.decl.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
			};
		}));

		// Generate Map Wrapper Type Files
		var mapWrapperDeclarations = ueGenOutput.MapWrapperTypes
			.Except(ueGenOutput.MapWrapperTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
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
			return new[]
			{
				new GeneratedFileDescriptor { FileName = headerFileName, Content = s.headerDeclaration },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/Maps/{s.decl.NamespacedTypeName}.cpp", Content = s.cppDeclaration },
			};
		}));

		// Generate all enum type declarations
		var enumTypesCode = ueGenOutput.EnumTypes
			.Except(ueGenOutput.EnumTypes.Where(t => previousGenerationPassesData.InEngineTypeToIncludePaths.ContainsKey(t.UnrealTypeName)))
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
		var allSerializableTypes = ueGenOutput.JsonSerializableTypes.Union(ueGenOutput.PolymorphicWrappersDeclarations).Union(ueGenOutput.ResponseWrapperTypes).ToList();
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
				new GeneratedFileDescriptor { FileName = headerFileName, Content = s.serializableHeader, },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}.cpp", Content = s.serializableCpp, },
				new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}Library.h", Content = s.serializableTypeLibraryHeader, },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/{s.decl.NamespacedTypeName}Library.cpp", Content = s.serializableTypeLibraryCpp, },
			};
		}));

		// Subsystem Declarations
		var subsystemsCode = ueGenOutput.SubsystemDeclarations.Select(decl =>
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
			return new[]
			{
				new GeneratedFileDescriptor { FileName = $"{headerFileOutputPath}AutoGen/SubSystems/Beam{s.decl.SubsystemName}Api.h", Content = s.subsystemHeader },
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/SubSystems/Beam{s.decl.SubsystemName}Api.cpp", Content = s.subsystemCpp },
			};
		}));

		var subsystemEndpoints = ueGenOutput.SubsystemDeclarations.SelectMany(sd => sd.GetAllEndpoints()).ToList();
		var subsystemEndpointsCode = subsystemEndpoints.Select(decl =>
		{
			decl.IntoProcessMap(processDictionary, ueGenOutput.JsonSerializableTypes);
			var endpointHeader = UnrealEndpointDeclaration.U_ENDPOINT_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMap(processDictionary, ueGenOutput.JsonSerializableTypes);
			var endpointCpp = UnrealEndpointDeclaration.U_ENDPOINT_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMap(processDictionary, ueGenOutput.JsonSerializableTypes);
			var beamFlowNodeHeader = UnrealEndpointDeclaration.BEAM_FLOW_BP_NODE_HEADER.ProcessReplacement(processDictionary);
			processDictionary.Clear();

			decl.IntoProcessMap(processDictionary, ueGenOutput.JsonSerializableTypes);
			var beamFlowNodeCpp = UnrealEndpointDeclaration.BEAM_FLOW_BP_NODE_CPP.ProcessReplacement(processDictionary);
			processDictionary.Clear();


			return (decl, endpointHeader, endpointCpp, beamFlowNodeHeader, beamFlowNodeCpp);
		});
		outputFiles.AddRange(subsystemEndpointsCode.SelectMany((sc, i) =>
		{
			return new[]
			{
				new GeneratedFileDescriptor
				{
					FileName = $"{headerFileOutputPath}AutoGen/SubSystems/{sc.decl.NamespacedOwnerServiceName}/{sc.decl.GlobalNamespacedEndpointName}Request.h", Content = sc.endpointHeader
				},
				new GeneratedFileDescriptor { FileName = $"{cppFileOutputPath}AutoGen/SubSystems/{sc.decl.NamespacedOwnerServiceName}/{sc.decl.GlobalNamespacedEndpointName}Request.cpp", Content = sc.endpointCpp },
				new GeneratedFileDescriptor
				{
					FileName = $"{blueprintHeaderFileOutputPath}AutoGen/{sc.decl.NamespacedOwnerServiceName}/K2BeamNode_ApiRequest_{sc.decl.GlobalNamespacedEndpointName}.h", Content = sc.beamFlowNodeHeader
				},
				new GeneratedFileDescriptor
				{
					FileName = $"{blueprintCppFileOutputPath}AutoGen/{sc.decl.NamespacedOwnerServiceName}/K2BeamNode_ApiRequest_{sc.decl.GlobalNamespacedEndpointName}.cpp", Content = sc.beamFlowNodeCpp
				},
			};
		}));

		// Prints out all the identified semtype declarations
		foreach ((string key, string value) in newGeneratedUnrealTypes)
		{
			previousGenerationPassesData.InEngineTypeToIncludePaths.Add(key, value);
			Log.Verbose(GetLog("File Generation", $"Mapped reference id to include path. ReferenceId=[{key}], Path=[{value}]"));
		}

		outputFiles.Add(new GeneratedFileDescriptor() { FileName = $"{currentGenerationPassDataFilePath}.json", Content = JsonConvert.SerializeObject(previousGenerationPassesData), });
		return outputFiles;
	}

	private static void BuildSerializableTypeDeclarations(UEGenerationContext context, UEGenerationOutput output)
	{
		var logHeader = nameof(BuildSerializableTypeDeclarations).SpaceOutOnUpperCase();
		Log.Debug(GetLog(logHeader, $"Began building serializable type declarations."));

		output.JsonSerializableTypes.EnsureCapacity(context.Schemas.Count);
		output.EnumTypes.EnsureCapacity(context.Schemas.Count);
		output.OptionalTypes.EnsureCapacity(context.Schemas.Count);
		output.ArrayWrapperTypes.EnsureCapacity(context.Schemas.Count);
		output.MapWrapperTypes.EnsureCapacity(context.Schemas.Count);
		output.PolymorphicWrappersDeclarations.EnsureCapacity(context.Schemas.Count);

		// Allocate a list to keep track of all Schema types that we have already declared.
		var listOfAlreadyDeclaredTypes = new List<NamespacedType>(context.Schemas.Count);

		// Add replacement types so that we don't generate them when we see them
		listOfAlreadyDeclaredTypes.AddRange(context.AllNamespacedReplacementTypes);
		if (genType == GenerationType.Microservice)
		{
			listOfAlreadyDeclaredTypes.AddRange(previousGenerationPassesData.InEngineTypeToIncludePaths.Keys.Select(s => GetNamespacedTypeNameFromUnrealType(new UnrealType() { TypeString = s })));
			Log.Debug(GetLog(logHeader, $"Registering Beamable Auto-Generated types as known types."));
		}

		// Pre-declare things that we always want
		if (genType != GenerationType.Microservice)
		{
			Log.Debug(GetLog(logHeader, $"Declaring always generated types for Beamable's Auto-Generation types."));
			// Declare primitive optional and arrays
			{
				// Strings
				{
					var array = MakeWrapperDeclaration(context, "Shared", new("FArrayOfString"));
					output.ArrayWrapperTypes.Add(array);

					var map = MakeWrapperDeclaration(context, "Shared", new("FMapOfString"));
					output.MapWrapperTypes.Add(map);

					var optional = MakeOptionalDeclaration(context, "Shared", UNREAL_OPTIONAL_STRING, UNREAL_STRING);
					output.OptionalTypes.Add(optional);
				}

				// Int
				{
					var array = MakeWrapperDeclaration(context, "Shared", new("FArrayOfInt32"));
					output.ArrayWrapperTypes.Add(array);

					var map = MakeWrapperDeclaration(context, "Shared", new("FMapOfInt32"));
					output.MapWrapperTypes.Add(map);

					var optional = MakeOptionalDeclaration(context, "Shared", UNREAL_OPTIONAL_INT, UNREAL_INT);
					output.OptionalTypes.Add(optional);
				}
			}

			// Declare replacement type optionals
			{
				for (int i = 0; i < context.AllReplacementTypes.Count; i++)
				{
					var replacementUnrealType = context.AllReplacementTypes[i];
					var replacementOptionalType = context.AllOptionalReplacementTypes[i];

					var optionalDeclaration = MakeOptionalDeclaration(context, "Shared", replacementOptionalType, replacementUnrealType);
					output.OptionalTypes.Add(optionalDeclaration);

					Log.Debug(GetLog(logHeader, $"Declared replacement type optional. OptionalType=[{optionalDeclaration.UnrealTypeName.AsStr}]"));
				}
			}
		}

		// Convert the schema into the generation format
		foreach (var namedOpenApiSchema in context.Schemas)
		{
			// We need to decide on whether we'll name the type simply or if we'll use their service title to augment the name.
			var schema = namedOpenApiSchema.Schema;

			// When generating code for microservices, we don't want anything that is not marked to be generated. 
			if (genType == GenerationType.Microservice)
			{
				if (schema.Extensions.TryGetValue(METHOD_SKIP_CLIENT_GENERATION_KEY, out var value) && value is OpenApiBoolean b && b.Value)
				{
					Log.Debug(GetLog(logHeader, $"Skipping generation of microservice type. SchemaName=[{namedOpenApiSchema.UniqueName}]"));
					continue;
				}
			}

			Log.Debug(GetLog(logHeader, $"Beginning generation of schema. SchemaName=[{namedOpenApiSchema.UniqueName}]"));
			UnrealType schemaUnrealType = GetNonOptionalUnrealTypeForField(context, namedOpenApiSchema.Document, schema);
			NamespacedType schemaNamespacedType = GetNamespacedTypeNameFromUnrealType(schemaUnrealType);

			GetNamespacedServiceNameFromApiDoc(namedOpenApiSchema.Document.Info, out var serviceTitle, out var serviceName);

			// Make sure we don't declare two types with the same name
			if (listOfAlreadyDeclaredTypes.Contains(schemaNamespacedType))
			{
				Log.Debug(GetLog(logHeader, $"Type is already declared so we are skipping it. SchemaNamespacedType=[{schemaNamespacedType}]"));
				continue;
			}

			listOfAlreadyDeclaredTypes.Add(schemaNamespacedType);

			var isResponseBodyType = context.UnrealTypesUsedAsResponses.FirstOrDefault(c => c.Equals(schemaUnrealType));

			// Find Enum declarations even within arrays and maps 
			// TODO: Declare this instead of serialized type
			if (schemaUnrealType.IsUnrealEnum())
			{
				var enumValuesNames = schema.Enum.OfType<OpenApiString>().Select(v => v.Value).ToList();
				var enumDecl = MakeEnumDeclaration(schemaUnrealType, enumValuesNames, serviceName);
				output.EnumTypes.Add(enumDecl);
				Log.Debug(GetLog(logHeader, $"Generated Enum type. SchemaNamespacedType=[{schemaNamespacedType}], Enum=[{enumDecl.UnrealTypeName}], Values=[{string.Join(", ", enumDecl.EnumValues)}]"));
			}
			else if (schemaUnrealType.IsUnrealJson())
			{
				// We skip the generation for this schema if we bump into a schema that maps to UNREAL_JSON.
				Log.Debug(GetLog(logHeader, $"Skipping Unreal JSON type will be represented as {UNREAL_JSON}."));
			}
			else if (schemaUnrealType.IsRawPrimitive())
			{
				// We skip the generation for this schema if we bump into a schema that maps to RAW PRIMITIVE TYPE.
				Log.Debug(GetLog(logHeader, $"Skipping raw primitive type will be represented as a known system type (such as FDateTime, Guid or so on...)."));
			}
			else
			{
				// Prepare the data for injection in the template string.
				var serializableTypeDeclaration = new UnrealJsonSerializableTypeDeclaration
				{
					UnrealTypeName = schemaUnrealType,
					NamespacedTypeName = schemaNamespacedType,
					ServiceName = serviceName,
					PropertyIncludes = new List<string>(8),
					UPropertyDeclarations = new List<UnrealPropertyDeclaration>(8),
					JsonUtilsInclude = "",
					IsResponseBodyType = isResponseBodyType.Type,
					IsSelfReferential = IsSelfReferentialSchema(namedOpenApiSchema.Document, namedOpenApiSchema.Schema),
				};

				Log.Debug(GetLog(logHeader, $"Began generating fields of serializable type. OwnerSchema=[{schemaNamespacedType}], UnrealType=[{serializableTypeDeclaration.UnrealTypeName.AsStr}]"));
				foreach ((string fieldName, OpenApiSchema fieldSchema) in schema.Properties)
				{
					var handle = new FieldDeclarationHandle(schemaNamespacedType, fieldName);
					// see schema type and format
					var unrealType = GetUnrealTypeForField(out var nonOverridenUnrealType, context, namedOpenApiSchema.Document, fieldSchema, handle);
					if (string.IsNullOrEmpty(unrealType))
					{
						using var sw = new StringWriter();
						var writer = new OpenApiJsonWriter(sw);
						fieldSchema.SerializeAsV3WithoutReference(writer);
						Log.Debug(GetLog(logHeader, $"Skipping unsupported field type in serializable type. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldHandle=[{handle.AsStr}]," +
						                            $" Schema=[{sw}]"));

						continue;
					}

					// Check if this field is an poly wrapper field, or polymorphic array/map. If it is, we need to build up a new serializable type for it.
					if (nonOverridenUnrealType.ContainsPolymorphicType())
					{
						var polyWrapperDecl = MakePolymorphicWrapperDeclaration(context, serviceName, unrealType, nonOverridenUnrealType);
						output.PolymorphicWrappersDeclarations.Add(polyWrapperDecl);
						Log.Debug(GetLog(logHeader, $"Generated Polymorphic Wrapper type. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldName=[{fieldName}]," +
						                            $" TypeName=[{polyWrapperDecl.UnrealTypeName}]"));
					}

					// Make the new property declaration for this field.
					var propertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealType, fieldName, kSchemaGenerationBuilder);
					var nonOptionalUnrealType = GetNonOptionalUnrealTypeForField(context, namedOpenApiSchema.Document, fieldSchema);
					var propertyDisplayName = propertyName;
					var uPropertyDeclarationData = new UnrealPropertyDeclaration
					{
						PropertyUnrealType = unrealType,
						PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealType),
						PropertyName = UnrealPropertyDeclaration.GetSanitizedPropertyName(propertyName),
						AsParameterName = UnrealPropertyDeclaration.GetSanitizedParameterName(propertyName),
						PropertyDisplayName = UnrealPropertyDeclaration.GetSanitizedPropertyDisplayName(propertyDisplayName.SpaceOutOnUpperCase()),
						RawFieldName = fieldName,
						NonOptionalTypeName = nonOptionalUnrealType,
					};


					// We get whatever the underlying type for this field's semantic type is...
					if (context.FieldSemanticTypesUnderlyingTypeMap.TryGetValue(handle, out uPropertyDeclarationData.SemTypeSerializationType))
					{
						// If this is an Array of Semantic Type, the underlying type must be the type of data in the array.
						if (uPropertyDeclarationData.SemTypeSerializationType.IsUnrealArray())
							uPropertyDeclarationData.SemTypeSerializationType = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(uPropertyDeclarationData.SemTypeSerializationType);

						// If this is an Map of Semantic Type, the underlying type must be the type of data in the map.
						if (uPropertyDeclarationData.SemTypeSerializationType.IsUnrealMap())
							uPropertyDeclarationData.SemTypeSerializationType = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(uPropertyDeclarationData.SemTypeSerializationType);

						Log.Debug(GetLog(logHeader, $"Found semantic type. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldName=[{fieldName}]," +
						                            $" SemTypeSerializationType=[{uPropertyDeclarationData.SemTypeSerializationType}]"));
					}

					// Check if this is an optional type, if it is --- declare it. (We don't support optional arrays of poly wrappers)
					if (unrealType.IsOptional())
					{
						var optionalDeclaration = MakeOptionalDeclaration(context, serviceName, unrealType, nonOptionalUnrealType);

						// Only add it if its not there already 
						if (output.OptionalTypes.All(d => !d.UnrealTypeName.Equals(unrealType))) output.OptionalTypes.Add(optionalDeclaration);


						Log.Debug(GetLog(logHeader, $"Generating Optional Type for Field. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldHandle=[{handle.AsStr}]," +
						                            $" OptionalType=[{optionalDeclaration.UnrealTypeName}]," +
						                            $" ValueType=[{optionalDeclaration.ValueUnrealTypeName}]," +
						                            $" WasReplacementType=[{context.AllReplacementTypes.Contains(optionalDeclaration.ValueUnrealTypeName)}]"));
					}

					// For Unreal arrays and maps, we store the Relevant Template parameter.
					if (nonOptionalUnrealType.IsUnrealMap())
					{
						uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOptionalUnrealType);

						Log.Debug(GetLog(logHeader, $"Parsing Template Param For Unreal Map. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldHandle=[{handle.AsStr}]," +
						                            $" MapType=[{uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam}]" +
						                            $" WasReplacementType=[{context.AllReplacementTypes.Contains(uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam)}]"));
					}

					if (nonOptionalUnrealType.IsUnrealArray())
					{
						uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOptionalUnrealType);
						Log.Debug(GetLog(logHeader, $"Parsing Template Param For Unreal Array. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldHandle=[{handle.AsStr}]," +
						                            $" MapType=[{uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam}]" +
						                            $" WasReplacementType=[{context.AllReplacementTypes.Contains(uPropertyDeclarationData.NonOptionalTypeNameRelevantTemplateParam)}]"));
					}


					// Wrapper types can only appear inside Non-Optional declarations of TMap/TArray ---
					// as such, we can find all of them by checking them against the NonOptionalUnrealType.
					if (nonOptionalUnrealType.ContainsWrapperContainer())
					{
						var wrapper = MakeWrapperDeclaration(context, serviceName, nonOptionalUnrealType);

						if (nonOptionalUnrealType.ContainsWrapperArray()) output.ArrayWrapperTypes.Add(wrapper);
						if (nonOptionalUnrealType.ContainsWrapperMap()) output.MapWrapperTypes.Add(wrapper);

						Log.Debug(GetLog(logHeader, $"Generating Wrapper Type for Field. OwnerSchema=[{schemaNamespacedType}]," +
						                            $" FieldHandle=[{handle.AsStr}]," +
						                            $" OptionalType=[{wrapper.UnrealTypeName}]," +
						                            $" ValueType=[{wrapper.ValueUnrealTypeName}]," +
						                            $" WasReplacementType=[{context.AllReplacementTypes.Contains(wrapper.ValueUnrealTypeName)}]"));
					}

					AddJsonAndDefaultValueHelperIncludesIfNecessary(unrealType, ref serializableTypeDeclaration);

					serializableTypeDeclaration.PropertyIncludes.Add(GetIncludeStatementForUnrealType(context, unrealType));
					serializableTypeDeclaration.UPropertyDeclarations.Add(uPropertyDeclarationData);

					// INFO: We are just checking for some interesting properties here...
					{
						// If this field's type is a self referential type, we log it out.
						if (IsSelfReferentialSchema(namedOpenApiSchema.Document, fieldSchema))
						{
							Log.Debug(GetLog(logHeader, $"Found self-referential schema in Field. OwnerSchema=[{schemaNamespacedType}]," +
							                            $" FieldHandle=[{handle.AsStr}]"));
						}

						// If this field's type is a replacement type, we log it out.
						if (context.AllReplacementTypes.Contains(unrealType))
						{
							Log.Debug(GetLog(logHeader, $"Field type was replacement type. OwnerSchema=[{schemaNamespacedType}]," +
							                            $" FieldHandle=[{handle.AsStr}]"));
						}
					}

					kSchemaGenerationBuilder.Clear();
				}

				Log.Debug(GetLog(logHeader, $"Finished generating fields of serializable type. OwnerSchema=[{schemaNamespacedType}], UnrealType=[{serializableTypeDeclaration.UnrealTypeName.AsStr}]"));

				// Remove any includes to yourself to guarantee no cyclical dependencies
				serializableTypeDeclaration.PropertyIncludes.Remove(GetIncludeStatementForUnrealType(context, schemaUnrealType));

				output.JsonSerializableTypes.Add(serializableTypeDeclaration);
				Log.Debug(GetLog(logHeader, $"Generated JsonSerializable type. SchemaNamespacedType=[{schemaNamespacedType}]," +
				                            $" TypeName=[{serializableTypeDeclaration.UnrealTypeName}]," +
				                            $" TypeIncludes=[{string.Join(", ", serializableTypeDeclaration.PropertyIncludes)}]"));
			}
		}
	}

	/*
	 * GENERATION HELPER FUNCTIONS ---- THESE ARE SIMPLY HERE TO MAKE IT EASIER TO GO THROUGH THE GENERATION ALGORITHM AND TO DOCUMENT IMPORTANT CONCEPTS OF THE ALGORITHM.
	 * These could all be inlined and it's unlikely they would ever be used outside of the main generation algorithm's flow.
	 */

	/// <summary>
	/// Fills the given <see cref="UEGenerationContext.SchemaNameCollisions"/> dictionary with lists containing the named schema collisions across all services we are generating for.
	/// We use this dictionary to make sure we have the correct names for each named schema and also their property declarations. 
	/// </summary>
	private static void BuildSchemaIdCollisionMap(UEGenerationContext context)
	{
		var logHeader = nameof(BuildSchemaIdCollisionMap).SpaceOutOnUpperCase();
		Log.Debug(GetLog(logHeader, $"Began building schema id collision map."));

		context.SchemaNameCollisions.EnsureCapacity(context.Schemas.Count);
		foreach (var namedOpenApiSchema in context.Schemas)
		{
			var name = namedOpenApiSchema.ReferenceId;
			if (namedOpenApiSchema.Schema.Extensions.TryGetValue(MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME, out var nameOverride))
			{
				name = new OpenApiReferenceId((nameOverride as OpenApiString).Value);
			}

			if (!context.SchemaNameCollisions.TryGetValue(name, out var list))
			{
				list = new List<NamedOpenApiSchema>(8);
				context.SchemaNameCollisions.Add(name, list);
			}

			list.Add(namedOpenApiSchema);

			Log.Verbose(GetLog(logHeader, $"Iterating over schema for collision. SchemaName=[{name.AsStr}], SchemaUniqueName=[{namedOpenApiSchema.UniqueName}]"));
		}

		var log = context.SchemaNameCollisions.Where(kvp => kvp.Value.Count > 1).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		foreach (var kvp in log)
		{
			var referenceId = kvp.Key.AsStr;
			var collisionWith = kvp.Value.Select(ns => ns.UniqueName);
			Log.Debug(GetLog(logHeader, $"Found schema name collision. ReferenceId=[{referenceId}], CollidedWith={string.Join(",", collisionWith)}"));
		}

		Log.Debug(GetLog(logHeader, $"Finished building schema id collision map."));
	}

	/// <summary>
	/// Generates two separate name collision maps related to each endpoint.
	/// <paramref name="globalEndpointNameCollisions"/> answers the question: "Does this endpoint name collides with any other endpoint from any other service?"
	/// <paramref name="perSubsystemCollisions"/> answers the question: "Does this endpoint name collide with any other endpoint from within the subsystem it'll exist inside of?"
	///
	/// The reason we have the first one is that UE does not support namespaces yet. 
	/// </summary>
	private static void BuildEndpointNameCollidedMaps(IReadOnlyList<OpenApiDocument> openApiDocuments, UEGenerationContext context)
	{
		context.GlobalEndpointNameCollisions.EnsureCapacity(context.Schemas.Count);

		var perNameDocuments = openApiDocuments.GroupBy(d =>
		{
			GetNamespacedServiceNameFromApiDoc(d.Info, out _, out var serviceName);
			return serviceName;
		}).ToDictionary(g => g.Key, g => g.ToList());

		var logHeader = nameof(BuildEndpointNameCollidedMaps).SpaceOutOnUpperCase();
		Log.Debug(GetLog(logHeader, $"Began building endpoint name collision map. Services=[{string.Join(", ", perNameDocuments.Keys)}]"));

		// This keeps track of the first time we see each endpoint so that following collisions can log what they collided with.  
		var helperLogDictGlobal = new Dictionary<NamespacedType, (string serviceName, OperationType operationType, string endpointPath)>(perNameDocuments.Count);
		var helperLogDictLocal = new Dictionary<NamespacedType, (OperationType operationType, string endpointPath)>(perNameDocuments.Count);

		context.PerSubsystemEndpointNameCollisions.EnsureCapacity(perNameDocuments.Count);
		foreach ((string serviceName, List<OpenApiDocument> documents) in perNameDocuments)
		{
			// Reset this helper so that it remains per-service.
			helperLogDictLocal.Clear();

			context.PerSubsystemEndpointNameCollisions.Add(serviceName, new Dictionary<string, bool>(16));
			foreach (OpenApiDocument openApiDocument in documents)
			{
				GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out _);

				var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
				Log.Debug(GetLog(logHeader, $"Began parsing service for building endpoint name collision map. ServiceName=[{serviceName}], ServiceTitle=[{serviceTitle}], ServiceType=[{serviceType}]"));

				foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
				{
					foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
					{
						var endpointName = GetSubsystemNamespacedEndpointName(serviceName, serviceType, operationType, endpointPath);

						// If it collides with another endpoint globally...
						if (!context.GlobalEndpointNameCollisions.TryAdd(endpointName, false))
						{
							context.GlobalEndpointNameCollisions[endpointName] = true;
							var firstData = helperLogDictGlobal[endpointName];
							Log.Debug(GetLog(logHeader, $"Found global endpoint name collision." +
							                            $" FirstService=[{firstData.serviceName}], FirstOperation=[{firstData.operationType}], FirstEndpointPath=[{firstData.endpointPath}]," +
							                            $" Service=[{serviceName}], Operation=[{operationType}], EndpointPath=[{endpointPath}]," +
							                            $" NamespacedName=[{endpointName.AsStr}]"));
						}
						else
						{
							helperLogDictGlobal[endpointName] = (serviceName, operationType, endpointPath);
						}

						// If it collides with another endpoint in this service...
						if (!context.PerSubsystemEndpointNameCollisions[serviceName].TryAdd(endpointName, false))
						{
							context.PerSubsystemEndpointNameCollisions[serviceName][endpointName] = true;

							var firstServiceData = helperLogDictLocal[endpointName];
							Log.Debug(GetLog(logHeader, $"Found endpoint name collision within service. Service=[{serviceName}], " +
							                            $" FirstOperation=[{firstServiceData.operationType}], FirstEndpointPath=[{firstServiceData.endpointPath}]," +
							                            $" Operation=[{operationType}], EndpointPath=[{endpointPath}]," +
							                            $" NamespacedName=[{endpointName.AsStr}]"));
						}
						else
						{
							helperLogDictLocal[endpointName] = (operationType, endpointPath);
						}
					}
				}

				Log.Debug(GetLog(logHeader, $"Finished parsing service for building endpoint name collision map. ServiceName=[{serviceName}], ServiceTitle=[{serviceTitle}], ServiceType=[{serviceType}]"));
			}
		}

		Log.Debug(GetLog(logHeader, $"Finished building endpoint name collision map. Services=[{string.Join(", ", perNameDocuments.Keys)}]"));
	}


	/// <summary>
	/// Goes through the <paramref name="namedOpenApiSchemata"/> and <paramref name="openApiDocuments"/> and, for each field of all relevant types in them, saves a flag that answers:
	/// "Is this type's field required?"
	///
	/// This map uses <see cref="GetNamespacedTypeNameFromSchema"/> and <see cref="GetEndpointFieldHandle"/> in order to keep track which fields of which types are tied to each flag.
	/// </summary>
	private static void BuildRequiredFieldMaps(IReadOnlyList<OpenApiDocument> openApiDocuments, UEGenerationContext context)
	{
		var logHeader = nameof(BuildRequiredFieldMaps).SpaceOutOnUpperCase();
		Log.Debug(GetLog(logHeader, $"Began building required field map."));

		context.FieldRequiredMap.EnsureCapacity(context.Schemas.Count);
		foreach (var ns in context.Schemas)
		{
			Log.Debug(GetLog(logHeader, $"Began parsing schema for building required field map. Schema=[{ns.UniqueName}], Required=[{string.Join(",", ns.Schema.Required)}]"));

			var properties = ns.Schema.Properties;
			foreach ((string fieldName, OpenApiSchema _) in properties)
			{
				var handle = GetSchemaFieldHandle(context, ns.Document, ns.ReferenceId, fieldName);
				var isOptional = !ns.Schema.Required.Contains(fieldName);
				context.FieldRequiredMap.TryAdd(handle, !isOptional);
				if (isOptional) Log.Debug(GetLog(logHeader, $"Found optional usage for type in Schema. OwnerSchema=[{ns.UniqueName}], FieldHandle={handle.AsStr}"));
			}

			Log.Debug(GetLog(logHeader, $"Finished parsing schema for building required field map. Schema=[{ns.UniqueName}], Required=[{string.Join(",", ns.Schema.Required)}]"));
		}

		foreach (var openApiDocument in openApiDocuments)
		{
			GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
			foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
			{
				foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
				{
					var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
					Log.Debug(GetLog(logHeader, $"Began parsing endpoint for building required field map. ServiceName=[{serviceName}]," +
					                            $" ServiceType=[{serviceType}], EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]"));
					foreach (var param in value.Parameters)
					{
						var handle = GetEndpointFieldHandle(context, serviceName, serviceType, operationType, endpointPath, param.Name);
						context.FieldRequiredMap.TryAdd(handle, param.Required);
						if (!param.Required) Log.Debug(GetLog(logHeader, $"Found optional usage for type in endpoint. ServiceName=[{serviceName}]," +
						                                                 $" ServiceType=[{serviceType}], EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]," +
						                                                 $" FieldHandle=[{handle}]"));
					}
					
					Log.Debug(GetLog(logHeader, $"Finished parsing endpoint for building required field map. ServiceName=[{serviceName}]," +
					                            $" ServiceType=[{serviceType}], EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]"));
				}
			}
		}
	}

	/// <summary>
	/// Goes through the <paramref name="namedOpenApiSchemata"/> and <paramref name="openApiDocuments"/> and, for each field of all relevant types in them, saves a flag that answers:
	/// "Is this type's field required?"
	///
	/// The generated map uses <see cref="GetSchemaFieldHandle"/> and <see cref="GetEndpointFieldHandle"/> in order to keep track which fields of which types are tied to each flag.
	/// </summary>
	private static void BuildSemanticTypesUnderlyingTypeMaps(IReadOnlyList<OpenApiDocument> openApiDocuments, UEGenerationContext context)
	{
		var logHeader = nameof(BuildSemanticTypesUnderlyingTypeMaps).SpaceOutOnUpperCase();

		context.FieldSemanticTypesUnderlyingTypeMap.EnsureCapacity(context.Schemas.Count);

		Log.Debug(GetLog(logHeader, $"Began parsing all schemas for semantic type's underlying types per usage."));
		foreach (var ns in context.Schemas)
		{
			Log.Debug(GetLog(logHeader, $"Began parsing schema for semantic type's underlying types per usage. Schema=[{ns.UniqueName}]"));
			// When generating code for microservices, we don't want anything that is not marked to be generated. 
			if (genType == GenerationType.Microservice)
			{
				if (ns.Schema.Extensions.TryGetValue(METHOD_SKIP_CLIENT_GENERATION_KEY, out var value) && value is OpenApiBoolean { Value: true })
				{
					Log.Debug(GetLog(logHeader, $"Skipping parsing schema for semantic types since we don't want to generate client code for this schema. Schema=[{ns.UniqueName}]"));
					continue;
				}
			}

			var properties = ns.Schema.Properties;
			foreach ((string fieldName, OpenApiSchema fieldSchema) in properties)
			{
				var handle = GetSchemaFieldHandle(context, ns.Document, ns.ReferenceId, fieldName);

				// Array fields
				if (fieldSchema.Type == "array")
				{
					var isReference = fieldSchema.Items.Reference != null;
					var arrayTypeSchema = isReference ? fieldSchema.Items.GetEffective(ns.Document) : fieldSchema.Items;
					if (arrayTypeSchema.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
					{
						var arraySerializationUnrealType = GetNonOptionalUnrealTypeForField(context, ns.Document, arrayTypeSchema, UnrealTypeGetFlags.ReturnUnderlyingSemanticType);
						context.FieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, arraySerializationUnrealType);
						Log.Debug(GetLog(logHeader, $"Found Semantic Type in Schema. Schema=[{ns.UniqueName}], Handle=[{handle}], UnderlyingType=[{arraySerializationUnrealType.AsNamespacedType().AsStr}]"));
						continue;
					}
				}

				// Map case
				if (fieldSchema.Type == "object" && fieldSchema.Reference == null && fieldSchema.AdditionalPropertiesAllowed)
				{
					if (fieldSchema.AdditionalProperties.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
					{
						var mapSerializationUnrealType =
							GetNonOptionalUnrealTypeForField(context, ns.Document, fieldSchema.AdditionalProperties, UnrealTypeGetFlags.ReturnUnderlyingSemanticType);
						context.FieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, mapSerializationUnrealType);

						Log.Debug(GetLog(logHeader, $"Found Semantic Type in Dictionary Schema. Schema=[{ns.UniqueName}], Handle=[{handle}], UnderlyingType=[{mapSerializationUnrealType.AsNamespacedType().AsStr}]"));
						continue;
					}
				}

				// Raw Semantic Type case
				if (fieldSchema.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var extension) && extension is OpenApiString)
				{
					var serializationUnrealType = GetNonOptionalUnrealTypeForField(context, ns.Document, fieldSchema, UnrealTypeGetFlags.ReturnUnderlyingSemanticType);
					context.FieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, serializationUnrealType);
					Log.Debug(GetLog(logHeader, $"Found Semantic Type in Schema. Schema=[{ns.UniqueName}], Handle=[{handle}], UnderlyingType=[{serializationUnrealType.AsNamespacedType().AsStr}]"));
				}
			}

			Log.Debug(GetLog(logHeader, $"Finished parsing schema for semantic type's underlying types per usage. Schema=[{ns.UniqueName}]"));
		}

		Log.Debug(GetLog(logHeader, $"Finished parsing all schemas for semantic type's underlying types per usage."));

		Log.Debug(GetLog(logHeader, $"Began parsing all endpoints for semantic type's underlying types per usage."));
		foreach (var openApiDocument in openApiDocuments)
		{
			GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
			foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
			{
				foreach ((OperationType operationType, OpenApiOperation value) in endpoint.Operations)
				{
					var serviceType = GetServiceTypeFromDocTitle(serviceTitle);
					Log.Debug(GetLog(logHeader, $"Began parsing route for semantic type's underlying types per usage. ServiceName=[{serviceName}], ServiceType=[{serviceType}]," +
					                            $" EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]"));
					foreach (var param in value.Parameters)
					{
						var handle = GetEndpointFieldHandle(context, serviceName, serviceType, operationType, endpointPath, param.Name);
						var fieldSchema = param.Schema;

						// Array fields
						if (fieldSchema is { Type: "array" })
						{
							var isReference = fieldSchema.Items.Reference != null;
							var arrayTypeSchema = isReference ? fieldSchema.Items.GetEffective(openApiDocument) : fieldSchema.Items;
							if (arrayTypeSchema.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
							{
								var arraySerializationUnrealType =
									GetNonOptionalUnrealTypeForField(context, openApiDocument, arrayTypeSchema, UnrealTypeGetFlags.ReturnUnderlyingSemanticType);
								context.FieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, arraySerializationUnrealType);
								Log.Debug(GetLog(logHeader, $"Found semantic type's underlying types in endpoint. ServiceName=[{serviceName}], ServiceType=[{serviceType}]," +
								                            $" EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]," +
								                            $" Handle=[{handle}], UnderlyingType=[{arraySerializationUnrealType.AsNamespacedType().AsStr}]"));
								continue;
							}
						}

						// Map case
						if (fieldSchema is { Type: "object", Reference: null, AdditionalPropertiesAllowed: true })
						{
							if (fieldSchema.AdditionalProperties.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var e) && e is OpenApiString)
							{
								var mapSerializationUnrealType =
									GetNonOptionalUnrealTypeForField(context, openApiDocument, fieldSchema.AdditionalProperties, UnrealTypeGetFlags.ReturnUnderlyingSemanticType);
								context.FieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, mapSerializationUnrealType);
								Log.Debug(GetLog(logHeader, $"Found semantic type's underlying types in endpoint. ServiceName=[{serviceName}], ServiceType=[{serviceType}]," +
								                            $" EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]," +
								                            $" Handle=[{handle}], UnderlyingType=[{mapSerializationUnrealType.AsNamespacedType().AsStr}]"));
								continue;
							}
						}

						// Raw Semantic Type case
						if (fieldSchema != null && fieldSchema.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var extension) && extension is OpenApiString)
						{
							var serializationUnrealType = GetNonOptionalUnrealTypeForField(context, openApiDocument, fieldSchema, UnrealTypeGetFlags.ReturnUnderlyingSemanticType);
							context.FieldSemanticTypesUnderlyingTypeMap.TryAdd(handle, serializationUnrealType);
							Log.Debug(GetLog(logHeader, $"Found semantic type's underlying types in endpoint. ServiceName=[{serviceName}], ServiceType=[{serviceType}]," +
							                            $" EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]," +
							                            $" Handle=[{handle}], UnderlyingType=[{serializationUnrealType.AsNamespacedType().AsStr}]"));
						}
					}

					Log.Debug(GetLog(logHeader, $"Finished parsing route for semantic type's underlying types per usage. ServiceName=[{serviceName}], ServiceType=[{serviceType}]," +
					                            $" EndpointOperation=[{operationType}], EndpointPath=[{endpointPath}]"));
				}
			}
		}

		Log.Debug(GetLog(logHeader, $"Finished parsing all endpoints for semantic type's underlying types per usage."));
	}

	/// <summary>
	/// Builds the necessary data we'll need to generate the code for each subsystem, their Request Type declarations and the relevant helper implementations to improve Blueprint UX.
	/// </summary>
	private static void BuildSubsystemDeclarations(IReadOnlyList<OpenApiDocument> openApiDocuments, UEGenerationContext context, UEGenerationOutput output)
	{
		var logHeader = nameof(BuildSubsystemDeclarations).SpaceOutOnUpperCase();
		context.UnrealTypesUsedAsResponses.EnsureCapacity(context.Schemas.Count);

		var isMsGen = genType == GenerationType.Microservice;
		output.SubsystemDeclarations.EnsureCapacity(openApiDocuments.Count);
		output.ResponseWrapperTypes.EnsureCapacity(openApiDocuments.Count);

		Log.Debug(GetLog(logHeader, $"Began building all subsystem declarations."));
		foreach (var openApiDocument in openApiDocuments)
		{
			GetNamespacedServiceNameFromApiDoc(openApiDocument.Info, out var serviceTitle, out var serviceName);
			var serviceType = GetServiceTypeFromDocTitle(serviceTitle);

			Log.Debug(GetLog(logHeader, $"Began building all subsystem declarations from document. ServiceName=[{serviceName}], ServiceType=[{serviceType}]"));

			var capitalizedServiceName = serviceName.Capitalize();
			var unrealServiceDecl = new UnrealApiSubsystemDeclaration { ServiceName = serviceName, SubsystemName = capitalizedServiceName, };

			// check and see if we already declared this subsystem (but an object/basic version of it)
			var alreadyDeclared = output.SubsystemDeclarations.Any(d => d.SubsystemName == unrealServiceDecl.SubsystemName);
			if (alreadyDeclared)
			{
				unrealServiceDecl = output.SubsystemDeclarations.First(d => d.SubsystemName == unrealServiceDecl.SubsystemName);
				Log.Debug(GetLog(logHeader, $"We had already found a different type of this service. Will append the endpoints of onto the same subsystem." +
				                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]"));
			}

			// Get the number of endpoints so we can pre-allocate the correct list sizes.
			var endpointCount = openApiDocument.Paths.SelectMany(endpoint => endpoint.Value.Operations).Count();

			unrealServiceDecl.IncludeStatements = new List<string>(endpointCount);

			unrealServiceDecl.EndpointRawFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);
			unrealServiceDecl.AuthenticatedEndpointRawFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);

			unrealServiceDecl.EndpointLambdaBindableFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);
			unrealServiceDecl.AuthenticatedEndpointLambdaBindableFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);

			unrealServiceDecl.EndpointUFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);
			unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations ??= new List<UnrealEndpointDeclaration>(endpointCount);

			Log.Debug(GetLog(logHeader, $"Began generating the endpoints for this service." +
			                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]"));

			foreach ((string endpointPath, OpenApiPathItem endpoint) in openApiDocument.Paths)
			{
				// For microservices, skip non-callable endpoints and endpoints flagged with skip-client-gen.
				if (genType == GenerationType.Microservice)
				{
					// If the method is hidden we should not Generate Client Code for it
					if (endpoint.Extensions.TryGetValue(METHOD_SKIP_CLIENT_GENERATION_KEY, out var shouldSkipClientCodeGen) &&
					    shouldSkipClientCodeGen is OpenApiBoolean { Value: true })
					{
						Log.Debug(GetLog(logHeader, $"Skipping generating the endpoint for this microservice client." +
						                            $" ServiceName=[{serviceName}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
						                            $" EndpointPath=[{endpointPath}]"));
						continue;
					}
				}

				foreach ((OperationType operationType, OpenApiOperation endpointData) in endpoint.Operations)
				{
					var unrealEndpoint = new UnrealEndpointDeclaration();

					// Find the service type from the document
					if (isMsGen)
					{
						serviceType = ServiceType.Basic; // We are never an object/api service if we are generating Microservice client code.
						unrealEndpoint.GlobalNamespacedEndpointName =
							GetMicroserviceSubsystemGlobalNamespacedEndpointName(unrealServiceDecl.SubsystemName, endpointPath, context.GlobalEndpointNameCollisions);
						unrealEndpoint.SubsystemNamespacedEndpointName =
							GetMicroserviceSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, endpointPath, context.PerSubsystemEndpointNameCollisions[serviceName]);
					}
					else
					{
						unrealEndpoint.GlobalNamespacedEndpointName =
							GetSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, serviceType, operationType, endpointPath, context.GlobalEndpointNameCollisions);
						unrealEndpoint.SubsystemNamespacedEndpointName =
							GetSubsystemNamespacedEndpointName(unrealServiceDecl.SubsystemName, serviceType, operationType, endpointPath, context.PerSubsystemEndpointNameCollisions[serviceName]);
					}

					unrealEndpoint.SelfUnrealType = $"U{unrealEndpoint.GlobalNamespacedEndpointName}Request*";
					unrealEndpoint.ServiceName = serviceName;
					unrealEndpoint.NamespacedOwnerServiceName = unrealServiceDecl.SubsystemName;
					// TODO: For now, we make all non-basic endpoints require auth. This is due to certain endpoints' OpenAPI spec not being correctly generated. We also need to correctly generate the server-only services in UE at a future date.
					unrealEndpoint.IsAuth = serviceType != ServiceType.Basic ||
					                        serviceTitle.Contains("inventory", StringComparison.InvariantCultureIgnoreCase) ||
					                        endpointData.Security[0].Any(kvp => kvp.Key.Reference.Id == "auth");
					unrealEndpoint.EndpointName = endpointPath;
					unrealEndpoint.EndpointRoute = isMsGen ? $"micro_{openApiDocument.Info.Title}{endpointPath}" : endpointPath;
					unrealEndpoint.EndpointVerb = operationType switch
					{
						OperationType.Get => "Get",
						OperationType.Put => "Put",
						OperationType.Post => "Post",
						OperationType.Delete => "Delete",
						OperationType.Patch => "Patch",
						OperationType.Trace => "Trace",
						OperationType.Options => "Options",
						OperationType.Head => "Head",
						_ => throw new ArgumentOutOfRangeException()
					};

					// Declare Query and Path parameters (not expected to ever show up during C#MS client codegen
					unrealEndpoint.RequestQueryParameters = new List<UnrealPropertyDeclaration>(4);
					unrealEndpoint.RequestPathParameters = new List<UnrealPropertyDeclaration>(4);
					foreach (var param in endpointData.Parameters)
					{
						var paramSchema = param.Schema.Reference != null ? param.Schema.GetEffective(openApiDocument) : param.Schema;
						var paramFieldHandle = GetEndpointFieldHandle(context, serviceName, serviceType, operationType, endpointPath, param.Name);

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
						unrealProperty.PropertyUnrealType = GetUnrealTypeForField(out _, context, openApiDocument, paramSchema, paramFieldHandle);
						unrealProperty.PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealProperty.PropertyUnrealType);
						unrealProperty.PropertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealProperty.PropertyUnrealType, param.Name, kSchemaGenerationBuilder);
						unrealProperty.AsParameterName = UnrealPropertyDeclaration.GetSanitizedParameterName(param.Name);
						unrealProperty.RawFieldName = param.Name;
						unrealProperty.PropertyDisplayName = UnrealPropertyDeclaration.GetSanitizedPropertyDisplayName(unrealProperty.PropertyName.SpaceOutOnUpperCase());
						unrealProperty.NonOptionalTypeName = GetNonOptionalUnrealTypeForField(context, openApiDocument, paramSchema);
						unrealProperty.BriefCommentString = $"{param.Description}";

						// Semantic type serialization for Query and Path Parameters is always FString
						if (context.FieldSemanticTypesUnderlyingTypeMap.TryGetValue(paramFieldHandle, out unrealProperty.SemTypeSerializationType))
						{
							Log.Debug(GetLog(logHeader, $"Found semantic type. Forcing it to be a FString since query/path parameters are always strings." +
							                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
							                            $" FieldHandle=[{paramFieldHandle}], FieldType=[{unrealProperty.PropertyUnrealType}]"));
							unrealProperty.SemTypeSerializationType = UNREAL_STRING;
						}

						// If this field's type is a replacement type, we log it out.
						if (context.AllReplacementTypes.Contains(unrealProperty.PropertyUnrealType))
						{
							Log.Debug(GetLog(logHeader, $"Found replacement type." +
							                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
							                            $" FieldHandle=[{paramFieldHandle}], FieldType=[{unrealProperty.PropertyUnrealType}]"));
						}

						// If this field's type is a self-referential type, we log it out.
						if (IsSelfReferentialSchema(openApiDocument, paramSchema))
						{
							Log.Debug(GetLog(logHeader, $"Found self-referential type." +
							                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
							                            $" FieldHandle=[{paramFieldHandle}], FieldType=[{unrealProperty.PropertyNamespacedType}]"));
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
								Log.Debug(GetLog(logHeader, $"Skipping endpoint parameter." +
								                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
								                            $" FieldHandle=[{paramFieldHandle}], FieldType=[{unrealProperty.PropertyNamespacedType}]," +
								                            $" Param=[{param.Name}], Param.In=[{param.In}]"));
								break;
						}
					}

					// Find and declare all request body properties. Request bodies must always point to a schema reference and can never be individual primitive types.
					unrealEndpoint.RequestBodyParameters = new List<UnrealPropertyDeclaration>(1);
					if (endpointData.RequestBody?.Content?.TryGetValue("application/json", out var requestMediaType) ?? false)
					{
						var bodySchema = requestMediaType.Schema.GetEffective(openApiDocument);

						var unrealProperty = new UnrealPropertyDeclaration();
						unrealProperty.PropertyUnrealType = GetNonOptionalUnrealTypeForField(context, openApiDocument, bodySchema);
						unrealProperty.PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealProperty.PropertyUnrealType);
						unrealProperty.PropertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealProperty.PropertyUnrealType, "Body", kSchemaGenerationBuilder);
						unrealProperty.AsParameterName = UnrealPropertyDeclaration.GetSanitizedParameterName("Body");
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
							var bodySchema = jsonResponse.Schema.GetEffective(openApiDocument);
							// Make the new property declaration for this field.
							var unrealType = GetNonOptionalUnrealTypeForField(context, openApiDocument, bodySchema);

							// Check if it don't exist in the schema and if it is NOT a raw primitive type 
							if (jsonResponse.Schema.Reference != null && !unrealType.IsRawPrimitive())
							{
								unrealEndpoint.ResponseBodyUnrealType = unrealType;
								unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealType);
								unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(unrealType);

								// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
								context.UnrealTypesUsedAsResponses.Add(new TypeRequestBody { UnrealType = unrealType, Type = ResponseBodyType.Json, });

								using var sw = new StringWriter();
								var writer = new OpenApiJsonWriter(sw);
								bodySchema.SerializeAsV3WithoutReference(writer);
								Log.Debug(GetLog(logHeader, $"Defined JSON endpoint response." +
								                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
								                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}" +
								                            $" \n\tGenEndpoint=[{unrealEndpoint.GlobalNamespacedEndpointName}]," +
								                            $" \n\tGenQueryParams=[{string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
								                            $" \n\tGenPathParams=[{string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
								                            $" \n\tGenBodyParams=[{string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
								                            $" \n\tGenBodyUnrealType=[{unrealEndpoint.ResponseBodyUnrealType}]," +
								                            $" \n\tBodySchema=[{sw}]"));
							}
							else
							{
								// Prepare the wrapper around the primitive this endpoint returns as a response payload.
								var wrapperBody = new UnrealJsonSerializableTypeDeclaration
								{
									UnrealTypeName = new($"U{unrealEndpoint.GlobalNamespacedEndpointName}Response*"),
									NamespacedTypeName = new($"{unrealEndpoint.GlobalNamespacedEndpointName}Response"),
									ServiceName = serviceName,
									PropertyIncludes = new List<string>(8),
									UPropertyDeclarations = new List<UnrealPropertyDeclaration>(8),
									JsonUtilsInclude = "",
									DefaultValueHelpersInclude = "",
									IsResponseBodyType = ResponseBodyType.PrimitiveWrapper,
								};

								Log.Debug(GetLog(logHeader, $"Found endpoint that returns a primitive. Creating a wrapper UObject for it." +
								                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
								                            $" Endpoint=[{unrealEndpoint.EndpointRoute}]," +
								                            $" WrapperName=[{wrapperBody.UnrealTypeName}]"));

								var fieldName = "Value";
								var propertyName = UnrealPropertyDeclaration.GetPrimitiveUPropertyFieldName(unrealType, fieldName, kSchemaGenerationBuilder);
								var propertyDisplayName = propertyName;
								var wrappedPrimitiveProperty = new UnrealPropertyDeclaration
								{
									PropertyUnrealType = unrealType,
									PropertyNamespacedType = GetNamespacedTypeNameFromUnrealType(unrealType),
									PropertyName = UnrealPropertyDeclaration.GetSanitizedPropertyName(propertyName),
									PropertyDisplayName = UnrealPropertyDeclaration.GetSanitizedPropertyDisplayName(propertyDisplayName.SpaceOutOnUpperCase()),
									AsParameterName = UnrealPropertyDeclaration.GetSanitizedParameterName(propertyName),
									RawFieldName = fieldName,
									NonOptionalTypeName = unrealType,
								};
								wrapperBody.PropertyIncludes.Add(GetIncludeStatementForUnrealType(context, unrealType));
								wrapperBody.UPropertyDeclarations.Add(wrappedPrimitiveProperty);
								AddJsonAndDefaultValueHelperIncludesIfNecessary(unrealType, ref wrapperBody, true);

								// Wrapper types can be directly returned from endpoints. In case this happens, we need to declare the wrapper types we found here.
								if (unrealType.ContainsWrapperContainer())
								{
									var wrapper = MakeWrapperDeclaration(context, serviceName, unrealType);

									if (unrealType.ContainsWrapperArray()) output.ArrayWrapperTypes.Add(wrapper);
									if (unrealType.ContainsWrapperMap()) output.MapWrapperTypes.Add(wrapper);

									Log.Debug(GetLog(logHeader, $"Found endpoint that returns a nested container." +
									                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
									                            $" Endpoint=[{unrealEndpoint.EndpointRoute}]," +
									                            $" UObjectWrapperName=[{wrapperBody.UnrealTypeName}], ContainerWrapper={wrapper.UnrealTypeName}"));
								}

								output.ResponseWrapperTypes.Add(wrapperBody);

								// Configure the endpoint
								var ueType = unrealEndpoint.ResponseBodyUnrealType = MakeUnrealUObjectTypeFromNamespacedType(wrapperBody.NamespacedTypeName);
								unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
								unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

								// Add the response type to a list of serializable types that we'll need to declare with an additional specific interface.
								context.UnrealTypesUsedAsResponses.Add(new TypeRequestBody { UnrealType = ueType, Type = ResponseBodyType.PrimitiveWrapper, });

								using var sw = new StringWriter();
								var writer = new OpenApiJsonWriter(sw);
								bodySchema.SerializeAsV3WithoutReference(writer);

								Log.Debug(GetLog(logHeader, $"Defined PrimitiveWrapper  endpoint response." +
								                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
								                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}" +
								                            $" \n\tGenEndpoint=[{unrealEndpoint.GlobalNamespacedEndpointName}]," +
								                            $" \n\tGenQueryParams=[{string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
								                            $" \n\tGenPathParams=[{string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
								                            $" \n\tGenBodyParams=[{string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
								                            $" \n\tGenBodyUnrealType=[{unrealEndpoint.ResponseBodyUnrealType}]," +
								                            $" \n\tBodySchema=[{sw}]"));
							}
						}
						else if (response.Content.TryGetValue("text/plain", out jsonResponse) || response.Content.Count == 0)
						{
							var ueType = unrealEndpoint.ResponseBodyUnrealType = UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE;
							unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
							unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

							// We don't add this type to the list of response types as this type is NOT autogenerated.
							Log.Debug(GetLog(logHeader, $"Skipped 'text/plain' endpoint as these are not auto-generated." +
							                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
							                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}" +
							                            $" \n\tGenEndpoint=[{unrealEndpoint.GlobalNamespacedEndpointName}]," +
							                            $" \n\tGenQueryParams=[{string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenPathParams=[{string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenBodyParams=[{string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenBodyUnrealType=[{unrealEndpoint.ResponseBodyUnrealType}]"));
						}
						else if (response.Content.TryGetValue("text/html", out jsonResponse) || response.Content.Count == 0)
						{
							// We currently don't treat it in the code gen, in the future we can replace it with the correct type
							// It's just a copy from the text/plain
							// The response is a string even thought is mark as a HTML
							var ueType = unrealEndpoint.ResponseBodyUnrealType = UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE;
							unrealEndpoint.ResponseBodyNamespacedType = GetNamespacedTypeNameFromUnrealType(ueType);
							unrealEndpoint.ResponseBodyNonPtrUnrealType = RemovePtrFromUnrealTypeIfAny(ueType);

							// We don't add this type to the list of response types as this type is NOT autogenerated.
							Log.Debug(GetLog(logHeader, $"Skipped 'text/html' endpoint as these are not auto-generated." +
							                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
							                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}" +
							                            $" \n\tGenEndpoint=[{unrealEndpoint.GlobalNamespacedEndpointName}]," +
							                            $" \n\tGenQueryParams=[{string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenPathParams=[{string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenBodyParams=[{string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenBodyUnrealType=[{unrealEndpoint.ResponseBodyUnrealType}]"));
						}
						else if (response.Content.TryGetValue("text/csv", out _))
						{
							Log.Debug(GetLog(logHeader, $"Skipped 'text/csv' endpoint as these are not auto-generated." +
							                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
							                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}" +
							                            $" \n\tGenEndpoint=[{unrealEndpoint.GlobalNamespacedEndpointName}]," +
							                            $" \n\tGenQueryParams=[{string.Join("\n", unrealEndpoint.RequestQueryParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenPathParams=[{string.Join("\n", unrealEndpoint.RequestPathParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenBodyParams=[{string.Join("\n", unrealEndpoint.RequestBodyParameters.Select(qd => $"{qd.PropertyUnrealType} {qd.PropertyName}"))}]," +
							                            $" \n\tGenBodyUnrealType=[{unrealEndpoint.ResponseBodyUnrealType}]"));
							continue;
						}
					}

					unrealEndpoint.RequestTypeIncludeStatements = string.Join("\n", unrealEndpoint.GetAllUnrealTypes().Select(ut => GetIncludeStatementForUnrealType(context, ut)));
					unrealEndpoint.ResponseTypeIncludeStatement = GetIncludeStatementForUnrealType(context, unrealEndpoint.ResponseBodyUnrealType);

					Log.Debug(GetLog(logHeader, $"Generated endpoint file includes." +
					                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
					                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}" +
					                            $" \n\tGenEndpointIncludes=[{string.Join("\n", new[] { unrealEndpoint.RequestTypeIncludeStatements, unrealEndpoint.ResponseTypeIncludeStatement })}]"));

					if (unrealEndpoint.IsAuth)
					{
						unrealServiceDecl.AuthenticatedEndpointRawFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.AuthenticatedEndpointLambdaBindableFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations.Add(unrealEndpoint);

						Log.Debug(GetLog(logHeader, $"Adding endpoint to service's auth endpoints." +
						                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
						                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}"));
					}
					else
					{
						unrealServiceDecl.EndpointRawFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.EndpointLambdaBindableFunctionDeclarations.Add(unrealEndpoint);
						unrealServiceDecl.EndpointUFunctionDeclarations.Add(unrealEndpoint);


						Log.Debug(GetLog(logHeader, $"Adding endpoint to service's non-auth endpoints." +
						                            $" \n\tServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
						                            $" \n\tEndpointOperation=[{operationType}], EndpointPath={endpointPath}"));
					}
				}
			}

			Log.Debug(GetLog(logHeader, $"Finished generating the endpoints for this service." +
			                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]"));


			var allRequestIncludes = unrealServiceDecl.GetAllEndpoints()
				.Select(e => $"#include \"{includeStatementPrefix}AutoGen/SubSystems/{e.NamespacedOwnerServiceName}/{e.GlobalNamespacedEndpointName}Request.h\"")
				.ToArray();
			unrealServiceDecl.IncludeStatements.AddRange(allRequestIncludes);

			Log.Debug(GetLog(logHeader, $"Defined include statements for subsystem." +
			                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]," +
			                            $" Includes=[{string.Join(", ", unrealServiceDecl.IncludeStatements)}]"));

			// If we had declared it already, replace that old declaration with the new one.
			if (alreadyDeclared)
			{
				output.SubsystemDeclarations.RemoveAll(d => d.SubsystemName == unrealServiceDecl.SubsystemName);
				Log.Debug(GetLog(logHeader, $"We had already found a different type of this service. Updating the declaration for this subsystem." +
				                            $" ServiceName=[{serviceName}], ServiceType=[{serviceType}], SubsystemName=[{unrealServiceDecl.SubsystemName}]"));
			}

			unrealServiceDecl.EndpointUFunctionWithRetryDeclarations = unrealServiceDecl.EndpointUFunctionDeclarations;
			unrealServiceDecl.AuthenticatedEndpointUFunctionWithRetryDeclarations = unrealServiceDecl.AuthenticatedEndpointUFunctionDeclarations;
			output.SubsystemDeclarations.Add(unrealServiceDecl);
		}
	}


	/// <summary>
	/// This takes in a normal unreal type/non-overriden unreal type pair of either:
	/// - UOneOf_
	/// - TArray{UOneOf_}
	/// - TMap{FString, UOneOf_}
	///
	/// It'll then handle converting that signature and generating the declaration data, including applying name overrides if they exist.
	/// </summary>
	private static UnrealJsonSerializableTypeDeclaration MakePolymorphicWrapperDeclaration(UEGenerationContext context, string serviceName, UnrealType unrealType, UnrealType nonOverridenUnrealType)
	{
		UnrealType nonOverridenPolyWrapperType, overridenWrapperType;
		if (nonOverridenUnrealType.IsPolymorphicType())
		{
			nonOverridenPolyWrapperType = new(nonOverridenUnrealType);
			overridenWrapperType = new(unrealType);
		}
		else if (nonOverridenUnrealType.ContainsPolymorphicType() && nonOverridenUnrealType.IsUnrealArray())
		{
			nonOverridenPolyWrapperType = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOverridenUnrealType);
			overridenWrapperType = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(unrealType);
		}
		else if (nonOverridenUnrealType.ContainsPolymorphicType() && nonOverridenUnrealType.IsUnrealMap())
		{
			nonOverridenPolyWrapperType = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOverridenUnrealType);
			overridenWrapperType = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(unrealType);
		}
		else
		{
			throw new Exception(
				"Should never see this. If you do, this means someone is using a polymorphic return value in an unsupported way. Figure out which way and add support for it here.");
		}

		// We are extracting the various UnrealTypes from the UOneOf_UTypeA_UTypeB_UTypeC and converting them into a list containing [UTypeA*, UTypeB*, UTypeC*]
		var ptrWrappedTypes = nonOverridenPolyWrapperType.AsStr.Substring(nonOverridenPolyWrapperType.AsStr.IndexOf('_') + 1).Split("_")
			.Select(nonPtrWrappedTypes => nonPtrWrappedTypes.EndsWith("*") ? nonPtrWrappedTypes : $"{nonPtrWrappedTypes}*")
			.Select(s => new UnrealType(s))
			.ToArray();


		var polyWrapperDecl = new UnrealJsonSerializableTypeDeclaration
		{
			UnrealTypeName = overridenWrapperType,
			NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(overridenWrapperType),
			ServiceName = serviceName,
			PolymorphicWrappedTypes = ptrWrappedTypes.Select(s => new PolymorphicWrappedData { UnrealType = s, ExpectedTypeValue = context.PolymorphicWrappedSchemaExpectedTypeValues[s] }).ToList(),
			UPropertyDeclarations = ptrWrappedTypes.Select(wrappedUnrealType => new UnrealPropertyDeclaration
			{
				PropertyUnrealType = new(wrappedUnrealType),
				PropertyName = UnrealPropertyDeclaration.GetSanitizedPropertyName(context.PolymorphicWrappedSchemaExpectedTypeValues[wrappedUnrealType].Capitalize()),
				PropertyDisplayName = UnrealPropertyDeclaration.GetSanitizedPropertyDisplayName(context.PolymorphicWrappedSchemaExpectedTypeValues[wrappedUnrealType].Capitalize()),
				AsParameterName = UnrealPropertyDeclaration.GetSanitizedParameterName(context.PolymorphicWrappedSchemaExpectedTypeValues[wrappedUnrealType].Capitalize()),
				RawFieldName = context.PolymorphicWrappedSchemaExpectedTypeValues[wrappedUnrealType],
				NonOptionalTypeName = MakeUnrealUObjectTypeFromNamespacedType(GetNamespacedTypeNameFromUnrealType(wrappedUnrealType))
			}).ToList(),
			PropertyIncludes = ptrWrappedTypes.Select(t => GetIncludeStatementForUnrealType(context, t)).ToList(),
			// We only need this include if we have any array, wrapper or optional types --- since this is a template it's worth not including it to keep compile times as small as we can have them.
			JsonUtilsInclude = "#include \"Serialization/BeamJsonUtils.h\""
		};

		Log.Debug(GetLog(nameof(MakePolymorphicWrapperDeclaration).SpaceOutOnUpperCase(), $"UnrealType=[{polyWrapperDecl.UnrealTypeName.AsStr}]," +
		                                                                                  $" WrappedPolymorphicTypes=[{string.Join(", ", ptrWrappedTypes)}]"));
		return polyWrapperDecl;
	}

	/// <summary>
	/// The given type can be in one of three formats:
	/// - TMap{FString, FArrayOf|FMapOf}
	/// - TArray{FArrayOf|FMapOf}
	/// - FArrayOf|FMapOf
	///
	/// This function fills out a declaration that will ensure the file containing the FArrayOf|FMapOf type exists.
	/// </summary>
	private static UnrealWrapperContainerDeclaration MakeWrapperDeclaration(UEGenerationContext context, string serviceName, UnrealType nonOptionalUnrealType)
	{
		var wrapper = new UnrealWrapperContainerDeclaration();
		// If it's a TMap we want the second parameter, if it's an array we want the first template parameter. If its just the type, we use it.
		if (nonOptionalUnrealType.IsUnrealMap()) wrapper.UnrealTypeName = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(nonOptionalUnrealType);
		else if (nonOptionalUnrealType.IsUnrealArray()) wrapper.UnrealTypeName = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(nonOptionalUnrealType);
		else if (nonOptionalUnrealType.IsWrapperContainer()) wrapper.UnrealTypeName = nonOptionalUnrealType;
		else throw new CliException($"The does not contain a wrapper type in its signature; TYPE={nonOptionalUnrealType}. If you're a customer please report a bug.");

		wrapper.ValueUnrealTypeName = GetWrappedUnrealTypeFromUnrealWrapperType(wrapper.UnrealTypeName);

		wrapper.NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(wrapper.UnrealTypeName);
		wrapper.ServiceName = serviceName;
		wrapper.UnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(context, wrapper.UnrealTypeName);

		wrapper.ValueNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(wrapper.ValueUnrealTypeName);
		wrapper.ValueUnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(context, wrapper.ValueUnrealTypeName);
		return wrapper;
	}

	/// <summary>
	/// The Unreal Type is the optional type in the form of: <see cref="UNREAL_OPTIONAL"/>.
	/// The Non-Optional Unreal type is whatever type the Optional is wrapping (in unreal-type form).
	///
	/// This function will fill out the optional declaration as needed.
	/// </summary>
	private static UnrealOptionalDeclaration MakeOptionalDeclaration(UEGenerationContext context, string serviceName, UnrealType unrealType, UnrealType nonOptionalUnrealType)
	{
		return new UnrealOptionalDeclaration
		{
			UnrealTypeName = unrealType,
			ServiceName = serviceName,
			NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(unrealType),
			UnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(context, unrealType),
			ValueUnrealTypeName = nonOptionalUnrealType,
			ValueNamespacedTypeName = GetNamespacedTypeNameFromUnrealType(nonOptionalUnrealType),
			ValueUnrealTypeIncludeStatement = GetIncludeStatementForUnrealType(context, nonOptionalUnrealType)
		};
	}

	/// <summary>
	/// This function takes in an Enum unreal type in the form of:
	/// - E____
	///
	/// With that unreal type and the list of possible values for the enum, it creates the declaration for it. 
	/// </summary>
	private static UnrealEnumDeclaration MakeEnumDeclaration(UnrealType unrealType, List<string> enumValuesNames, string serviceName) =>
		new() { UnrealTypeName = unrealType, NamespacedTypeName = GetNamespacedTypeNameFromUnrealType(unrealType), EnumValues = enumValuesNames, ServiceName = serviceName };


	/// <summary>
	/// This decides whether or not we'll need the cpp to include the BeamJsonUtils.h; we only need to do that if we have complex types (Containers or Wrappers).
	/// This also decides whether or not we need DefaultValueHelper.h; we need to do this for deserializing some of the primitive UE types (<see cref="UnrealType.IsNumericPrimitive"/>). 
	/// </summary>
	private static void AddJsonAndDefaultValueHelperIncludesIfNecessary(UnrealType unrealType, ref UnrealJsonSerializableTypeDeclaration serializableTypeData,
		bool forceJson = false, bool forceDefaultHelper = false)
	{
		// If this is a field that will require BeamJsonUtils for deserialization --- add it to the list of includes of this type.
		if (forceJson || unrealType.RequiresJsonUtils())
		{
			// We only need this include if we have any array, wrapper or optional types --- since this is a template it's worth not including it to keep compile times as small as we can have them.
			serializableTypeData.JsonUtilsInclude = string.IsNullOrEmpty(serializableTypeData.JsonUtilsInclude)
				? "#include \"Serialization/BeamJsonUtils.h\""
				: serializableTypeData.JsonUtilsInclude;
		}

		// Decide if we need to add the default value helper in order to parse primitive numeric types
		if (forceDefaultHelper || unrealType.IsNumericPrimitive())
		{
			serializableTypeData.DefaultValueHelpersInclude = string.IsNullOrEmpty(serializableTypeData.DefaultValueHelpersInclude)
				? "#include \"Misc/DefaultValueHelper.h\""
				: serializableTypeData.DefaultValueHelpersInclude;
		}
	}

	/*
	 * NAMESPACE CONFLICT RESOLUTION FUNCTIONS ---- THESE ARE MEANT TO RESOLVE NAME CONFLICTS AND PRODUCE A TYPE NAME (WITHOUT THE UNREAL PREFIXES) THAT IS UNIQUE ACROSS ALL THE GENERATION SPACE.
	 * We use these to control what the "final name" of any give type will look like. The other use relies on UNREAL_TYPES_OVERRIDES and NAMESPACED_ENDPOINT_OVERRIDES to manually enforce a change
	 * between a "auto-magically generated name" and a "manually defined one". This is important due to some of our types conflicting with each other as well as with Unreal's own types.
	 *
	 */


	/// <summary>
	/// This is just a string type-def that enforces a specific format. 
	/// This string represents a name free of conflict across all other names of that category. We generate these for schemas and endpoints before generating actual <see cref="UnrealType"/>s.
	/// </summary>
	public readonly struct NamespacedType : IEquatable<string>, IEquatable<NamespacedType>
	{
		public readonly string TypeString;
		public string AsStr => $"{TypeString}";
		public NamespacedType(string namespacedType) => TypeString = namespacedType;
		public static implicit operator string(NamespacedType w) => w.AsStr;
		public bool Equals(string other) => AsStr.Equals(other);
		public bool Equals(NamespacedType other) => AsStr.Equals(other.AsStr);
		public override bool Equals(object obj) => obj is NamespacedType && Equals((NamespacedType)obj);
		public override int GetHashCode() => (TypeString != null ? TypeString.GetHashCode() : 0);
		public override string ToString() => this;
	}

	/// <summary>
	/// This is just a string type-def that enforces a specific format.
	/// 
	/// Makes a declaration handle string. This string represents a unique declaration (member field) in the space of all declarations in the code. 
	/// We use this for building helper maps that we use during the generation:
	///  - Whether or not a field is required at that specific declaration point; 
	///  - Type for the serialization of a Semantic Type field.
	/// </summary>
	private readonly struct FieldDeclarationHandle : IEquatable<string>, IEquatable<FieldDeclarationHandle>
	{
		public readonly string Owner;
		public readonly string FieldOrParamName;

		public string AsStr => $"{Owner}.{FieldOrParamName}";

		public FieldDeclarationHandle(string owner, string fieldOrParamName)
		{
			Owner = owner;
			FieldOrParamName = fieldOrParamName;
		}

		public static implicit operator string(FieldDeclarationHandle w) => w.AsStr;
		public bool Equals(string other) => AsStr.Equals(other);
		public bool Equals(FieldDeclarationHandle other) => AsStr.Equals(other.AsStr);
		public override bool Equals(object obj) => obj is FieldDeclarationHandle && Equals((FieldDeclarationHandle)obj);
		public override string ToString() => this;

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Owner != null ? Owner.GetHashCode() : 0) * 397) ^ (FieldOrParamName != null ? FieldOrParamName.GetHashCode() : 0);
			}
		}
	}

	/// <summary>
	/// Basically generates the <see cref="FieldDeclarationHandle"/>'s for fields/parameters of an endpoint's resulting UE type.
	/// </summary>
	private static FieldDeclarationHandle GetEndpointFieldHandle(UEGenerationContext context, string serviceName, ServiceType serviceType, OperationType operationType, string endpointPath, string paramName)
	{
		return genType switch
		{
			GenerationType.BasicObject => new FieldDeclarationHandle($"{serviceName}_{GetSubsystemNamespacedEndpointName(serviceName, serviceType, operationType, endpointPath, context.GlobalEndpointNameCollisions)}",
				paramName),
			GenerationType.Microservice => new FieldDeclarationHandle($"{serviceName}_{GetMicroserviceSubsystemGlobalNamespacedEndpointName(serviceName, endpointPath, context.GlobalEndpointNameCollisions)}", paramName),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	/// <summary>
	/// Basically generates the <see cref="FieldDeclarationHandle"/>'s for fields of a schema's resulting UE type.
	/// </summary>
	private static FieldDeclarationHandle GetSchemaFieldHandle(UEGenerationContext context, OpenApiDocument parentDoc, OpenApiReferenceId referenceId, string fieldName) =>
		new(GetNamespacedTypeNameFromSchema(context, parentDoc, referenceId, false), fieldName);

	/// <summary>
	/// Generates the service title (basic/object) and the service name (auth, realms, etc...) from the OpenApiInfo struct.
	/// </summary>
	private static void GetNamespacedServiceNameFromApiDoc(OpenApiInfo parentDocInfo, out string serviceTitle, out string serviceName)
	{
		var serviceNames = parentDocInfo.Title.Split(" ");
		serviceTitle = serviceNames.Length == 1 ? "Basic" : serviceNames[1].Sanitize().Capitalize();

		string serviceNameCapitalize = serviceNames[0].Sanitize().Capitalize();

		if (!SERVICE_NAME_OVERRIDES.TryGetValue(serviceNameCapitalize, out serviceName))
		{
			serviceName = serviceNameCapitalize;
		}
	}

	private static ServiceType GetServiceTypeFromDocTitle(string serviceTitle)
	{
		if (string.IsNullOrEmpty(serviceTitle)) return ServiceType.Basic;
		if (serviceTitle.Contains("object", StringComparison.OrdinalIgnoreCase)) return ServiceType.Object;
		if (serviceTitle.Contains("actor", StringComparison.OrdinalIgnoreCase)) return ServiceType.Api;
		return ServiceType.Basic;
	}

	/// <summary>
	/// Returns the namespaced type for the given schema of the given document.
	/// This either returns <paramref name="referenceId"/> (in the case of only one schema across all documents having this name or if all the repeated schema declarations have the same properties) 
	/// OR 
	/// it returns the type name compounded with the it's owner service's name and type (derived from the <paramref name="parentDoc"/>'s <see cref="Info.Title"/>).
	///
	/// If we are asking for the Optional version of the type, we add optional to it's name.
	/// 
	/// This GetNamespacedTypeNameFromSchema gets the correct schema name for the purposes of the Unreal Code Gen. It solves the problem of NamedSchemas not being unique in global scope.
	/// As in, there can be two Account NamedSchemas but they will always be from different documents.
	/// </summary>
	private static NamespacedType GetNamespacedTypeNameFromSchema(UEGenerationContext context, OpenApiDocument parentDoc, OpenApiReferenceId referenceId, bool isOptional)
	{
		var isMsGen = genType == GenerationType.Microservice;
		if (isMsGen)
		{
			if (parentDoc.Components.Schemas[referenceId].Extensions.TryGetValue(MICROSERVICE_EXTENSION_BEAMABLE_TYPE_NAME, out var nameExtension))
				referenceId = (nameExtension as OpenApiString)!.Value;
		}

		var hasMappedOverlaps = context.SchemaNameCollisions.TryGetValue(referenceId, out var overlaps);
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

				referenceId = $"{serviceName.Capitalize()}{serviceTitle.Capitalize()}{referenceId}";
			}
		}


		// Override the schema name if any is configured.
		referenceId = NAMESPACED_TYPES_OVERRIDES.TryGetValue(referenceId, out var overridenName) ? overridenName.AsStr : referenceId;

		referenceId += referenceId.AsStr.EndsWith("Request") ? "Body" : "";

		// Adjust the schema name if it is an optional schema
		referenceId = isOptional ? $"Optional{referenceId}" : referenceId;

		// Return the namespaced name.
		return new(referenceId);
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="serviceType"></param>
	/// <param name="httpVerb"></param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	private static NamespacedType GetSubsystemNamespacedEndpointName(string serviceName, ServiceType serviceType, OperationType httpVerb, string endpointPath,
		IReadOnlyDictionary<string, bool> endpointNameOverlaps = null)
	{
		// If an object service, we need to skip 4 '/' to get what we want (/object/mail/{objectId}/whatWeWant)
		var nameRelevantPath = endpointPath.Substring(endpointPath.IndexOf('/', 1) + 1);
		nameRelevantPath = nameRelevantPath.Substring(nameRelevantPath.IndexOf('/') + 1);

		var methodName = SwaggerService.FormatPathNameAsMethodName(nameRelevantPath);

		var routeParams = new List<string>();
		var routeParamExtension = "";
		var isInParam = false;
		for (var i = 0; i < endpointPath.Length; i++)
		{
			if (endpointPath[i] == '{')
			{
				isInParam = true;
			}
			else if (endpointPath[i] == '}')
			{
				isInParam = false;
				routeParams.Add(routeParamExtension);
				routeParamExtension = "";
			}
			else if (isInParam)
			{
				routeParamExtension += endpointPath[i];
			}
		}

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
			OperationType.Patch => $"Patch{methodName}",
			OperationType.Trace => $"Trace{methodName}",
			OperationType.Options => $"Options{methodName}",
			OperationType.Head => $"Head{methodName}",
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

			// Lets append the route params
			if (routeParams.Count != 0)
				methodName += $"By{string.Join("And", routeParams.Select(r => r.Sanitize().Capitalize()))}";
		}

		// In case we want to manually override an endpoint's name...
		methodName = NAMESPACED_ENDPOINT_OVERRIDES.TryGetValue(methodName, out string overridenMethodName) ? overridenMethodName : methodName;
		return new(methodName);
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="isObjectService"></param>
	/// <param name="httpVerb"></param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	private static NamespacedType GetMicroserviceSubsystemGlobalNamespacedEndpointName(string serviceName, string endpointPath, Dictionary<string, bool> endpointNameOverlaps = null)
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
		return new(methodName);
	}

	/// <summary>
	/// Gets a uniquely identifiable name for an endpoint living inside a service (may or may not be an object service). 
	/// </summary>
	/// <param name="serviceName">When null, we assume it's not an object service. This impacts how we generate the namespaced name.</param>
	/// <param name="endpointPath"></param>
	/// <param name="endpointNameOverlaps">Not passing in this, will make you ignore name overlap resolution</param>
	private static NamespacedType GetMicroserviceSubsystemNamespacedEndpointName(string serviceName, string endpointPath, Dictionary<string, bool> endpointNameOverlaps = null)
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
		return new(methodName);
	}


	/*
	 * UNREAL TYPE FUNCTIONS ---- THESE ARE MEANT TO CONVERT SCHEMA DECLARATIONS INTO THEIR FINAL TYPE IN UNREAL. THESE USES THE NAMESPACED FUNCTIONS ABOVE IN ORDER TO ENSURE NO NAME CONFLICTS HAPPEN.
	 * From an Unreal Type, we can find the namespaced type, check if it's an array, optional array, wrapper array, map and so on and so forth --- this allows us to specialize the code generation at
	 * each point using UE's own prefixes and code patterns. This is a good thing since we need to enforce this standard anyway to remain 100% BP compatible.
	 */

	/// <summary>
	/// This is just a string type-def that enforces a specific format. 
	/// This string represents a name free of conflict across all other names of that category. We generate these for schemas and endpoints before generating actual <see cref="UnrealType"/>s.
	/// </summary>
	public struct UnrealType : IEquatable<string>, IEquatable<UnrealType>
	{
		public string TypeString;
		public string AsStr => $"{TypeString}";
		public UnrealType(string namespacedType) => TypeString = namespacedType;
		public static implicit operator string(UnrealType w) => w.AsStr;
		public bool Equals(string other) => AsStr.Equals(other);
		public bool Equals(UnrealType other) => AsStr.Equals(other.AsStr);
		public override bool Equals(object obj) => obj is UnrealType && Equals((UnrealType)obj);
		public override int GetHashCode() => (TypeString != null ? TypeString.GetHashCode() : 0);
		public override string ToString() => this;

		public NamespacedType AsNamespacedType() => GetNamespacedTypeNameFromUnrealType(this);

		public bool RequiresJsonUtils() => IsUnrealArray() || IsUnrealMap() || IsOptional() || IsUnrealUObject() || IsAnySemanticType() || IsUnrealJson();

		#region Primitives

		public bool IsUnrealByte() => AsStr == UNREAL_BYTE;
		public bool IsUnrealShort() => AsStr == UNREAL_SHORT;
		public bool IsUnrealInt() => AsStr == UNREAL_INT;
		public bool IsUnrealLong() => AsStr == UNREAL_LONG;
		public bool IsUnrealBool() => AsStr == UNREAL_BOOL;
		public bool IsUnrealFloat() => AsStr == UNREAL_FLOAT;
		public bool IsUnrealDouble() => AsStr == UNREAL_DOUBLE;
		public bool IsUnrealGuid() => AsStr == UNREAL_GUID;
		public bool IsUnrealDateTime() => AsStr == UNREAL_DATE_TIME;
		public bool IsUnrealString() => AsStr == UNREAL_STRING;

		public bool IsNumericPrimitive() => IsUnrealByte() || IsUnrealShort() || IsUnrealInt() || IsUnrealLong() || IsUnrealFloat() || IsUnrealDouble();

		public bool IsRawPrimitive() => IsNumericPrimitive() || IsUnrealGuid() || IsUnrealDateTime() || IsUnrealString() || IsUnrealBool();

		public bool IsUnrealArray() => AsStr.StartsWith(UNREAL_ARRAY);
		public bool IsUnrealMap() => AsStr.StartsWith(UNREAL_MAP);
		public bool IsUnrealEnum() => AsStr.StartsWith(UNREAL_U_ENUM_PREFIX);
		public bool IsUnrealStruct() => AsStr.StartsWith(UNREAL_U_STRUCT_PREFIX);
		public bool IsUnrealUObject() => AsStr.StartsWith(UNREAL_U_OBJECT_PREFIX);

		#endregion

		#region Generated Types

		public bool IsOptional() => AsStr.StartsWith(UNREAL_OPTIONAL);
		public bool IsOptionalMap() => AsStr.StartsWith(UNREAL_OPTIONAL_MAP);
		public bool IsOptionalArray() => AsStr.StartsWith(UNREAL_OPTIONAL_ARRAY);
		public bool IsOptionalDateTime() => AsStr.StartsWith(UNREAL_OPTIONAL_DATE_TIME);
		public bool IsOptionalBool() => AsStr.StartsWith(UNREAL_OPTIONAL_BOOL);

		/// <summary>
		/// Will only work with non-Overriden Unreal Types.
		/// </summary>
		public bool IsPolymorphicType() => AsStr.StartsWith(UNREAL_U_POLY_WRAPPER_PREFIX);

		/// <summary>
		/// Checks if the given non-overriden Unreal Type contains UOneOf_ in its name. 
		/// </summary>
		public bool ContainsPolymorphicType() => AsStr.Contains(UNREAL_U_POLY_WRAPPER_PREFIX);

		public bool ContainsWrapperContainer() => ContainsWrapperArray() || ContainsWrapperMap();
		public bool ContainsWrapperMap() => AsStr.Contains(UNREAL_WRAPPER_MAP);
		public bool ContainsWrapperArray() => AsStr.Contains(UNREAL_WRAPPER_ARRAY);

		public bool IsWrapperContainer() => IsWrapperMap() || IsWrapperArray();
		public bool IsWrapperMap() => AsStr.StartsWith(UNREAL_WRAPPER_MAP);
		public bool IsWrapperArray() => AsStr.StartsWith(UNREAL_WRAPPER_ARRAY);
		public bool IsBeamNode() => AsStr.StartsWith(UNREAL_U_BEAM_NODE_PREFIX);

		#endregion

		#region Known Types

		public bool IsPlainTextResponse() => AsStr.StartsWith(UNREAL_U_BEAM_PLAIN_TEXT_RESPONSE_TYPE);

		public bool IsBeamCid() => AsStr.StartsWith(UNREAL_U_SEMTYPE_CID);
		public bool IsBeamPid() => AsStr.StartsWith(UNREAL_U_SEMTYPE_PID);
		public bool IsAccountId() => AsStr.StartsWith(UNREAL_U_SEMTYPE_ACCOUNTID);
		public bool IsGamerTag() => AsStr.StartsWith(UNREAL_U_SEMTYPE_GAMERTAG);
		public bool IsContentId() => AsStr.StartsWith(UNREAL_U_SEMTYPE_CONTENTID);
		public bool IsContentManifestId() => AsStr.StartsWith(UNREAL_U_SEMTYPE_CONTENTMANIFESTID);
		public bool IsStatsType() => AsStr.StartsWith(UNREAL_U_SEMTYPE_STATSTYPE);

		public bool IsAnySemanticType() => UNREAL_ALL_SEMTYPES.Contains(this);
		public bool ContainsAnySemanticType() => UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(AsStr.Contains);

		public bool IsUnrealJson() => AsStr.StartsWith(UNREAL_JSON);

		#endregion
	}

	/// <summary>
	/// Passed into <see cref="UnrealSourceGenerator.GetUnrealTypeForField"/> and other similar functions to define which version of a particular type we want.  
	/// </summary>
	[Flags]
	private enum UnrealTypeGetFlags
	{
		/// <summary>
		/// Will provide the version of the type with respect to the given declaration for it (ie.: if requesting the tp
		/// </summary>
		None,

		/// <summary>
		/// This will ignore whether or not the field is required by the OAPI and will return the regular type instead of the FOptional{type} version of the field's type.
		/// </summary>
		ReturnUnderlyingOptionalType,

		/// <summary>
		/// When passing this, if the field's type is a semantic type, it will return the underlying semantic type instead of the actual semantic type. 
		/// </summary>
		ReturnUnderlyingSemanticType
	}

	/// <summary>
	/// Gets the Unreal type name for a given field declared in a <see cref="OpenApiSchema.Properties"/> dictionary.
	/// </summary>
	/// <param name="nonOverridenType">This is the default name built by the code-gen without caring about <see cref="NAMESPACED_TYPES_OVERRIDES"/>.</param>
	/// <param name="context">The <see cref="UEGenerationContext"/> containing all the helper data structures for the generation algorithm.</param>
	/// <param name="parentDoc">The <see cref="OpenApiDocument"/> containing the schema that owns the given field.</param>
	/// <param name="schema">The field schema (value of <see cref="OpenApiSchema.Properties"/>).</param>
	/// <param name="fieldDeclarationHandle"> A <see cref="FieldDeclarationHandle"/> created by either <see cref="GetSchemaFieldHandle"/> or <see cref="GetEndpointFieldHandle"/>. Should null or empty, if generating the type name instead of a field/parameter's declaration. </param>
	/// <param name="flags"><see cref="UnrealTypeGetFlags"/> describe some options that will affect the resulting type.</param>
	/// <returns>The correct Unreal-land type as a string.</returns>
	private static UnrealType GetUnrealTypeForField(out UnrealType nonOverridenType, UEGenerationContext context, OpenApiDocument parentDoc, OpenApiSchema schema, FieldDeclarationHandle fieldDeclarationHandle,
		UnrealTypeGetFlags flags = UnrealTypeGetFlags.None)
	{
		// The field is considered an optional type ONLY if it is in the dictionary AND it's value in the dictionary is false.
		// This dictionary must be built from all NamedSchemas's properties (fields) and contain true/false for whether or not that field of that type is required.
		var isOptional = !flags.HasFlag(UnrealTypeGetFlags.ReturnUnderlyingOptionalType) && context.FieldRequiredMap.TryGetValue(fieldDeclarationHandle, out var isRequired) && !isRequired;
		var isEnum = schema.GetEffective(parentDoc).Enum.Count > 0;
		var isArbitraryJsonObject = IsArbitraryJsonBlob(parentDoc, schema);

		// We warn of self-referential types as these are interesting schemas that are more likely to create problems.
		var isSelfReferential = IsSelfReferentialSchema(parentDoc, schema);
		if (isSelfReferential) BeamableLogger.LogWarning("Found Self-ReferentialType {FieldDeclarationHandle}, {SchemaName}", fieldDeclarationHandle, schema.Reference?.Id);

		var semType = "";
		if (!flags.HasFlag(UnrealTypeGetFlags.ReturnUnderlyingSemanticType) && IsSemanticTypeSchema(schema) && schema.Extensions[Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE] is OpenApiString s)
			semType = s.Value;

		// Happens in the case where 
		var isPolymorphicWrapper = IsPolymorphicWrapperSchema(schema);
		var isDictionary = schema.Reference == null && schema.AdditionalPropertiesAllowed;

		switch (schema.Type, schema.Format, schema.Reference?.Id, semType)
		{
			// Handle semantic types
			case ("array", _, _, _) when string.Equals(semType, "Cid", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_CID : UNREAL_U_SEMTYPE_ARRAY_CID);
			case ("array", _, _, _) when string.Equals(semType, "Pid", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_PID : UNREAL_U_SEMTYPE_ARRAY_PID);
			case ("array", _, _, _) when string.Equals(semType, "AccountId", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_ACCOUNTID : UNREAL_U_SEMTYPE_ARRAY_ACCOUNTID);
			case ("array", _, _, _) when string.Equals(semType, "GamerTag", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_GAMERTAG : UNREAL_U_SEMTYPE_ARRAY_GAMERTAG);
			case ("array", _, _, _) when string.Equals(semType, "ContentManifestId", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_CONTENTMANIFESTID : UNREAL_U_SEMTYPE_ARRAY_CONTENTMANIFESTID);
			case ("array", _, _, _) when string.Equals(semType, "ContentId", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_CONTENTID : UNREAL_U_SEMTYPE_ARRAY_CONTENTID);
			case ("array", _, _, _) when string.Equals(semType, "StatsType", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_ARRAY_U_SEMTYPE_STATSTYPE : UNREAL_U_SEMTYPE_ARRAY_STATSTYPE);


			case (_, _, _, _) when string.Equals(semType, "Cid", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CID : UNREAL_U_SEMTYPE_CID);
			case (_, _, _, _) when string.Equals(semType, "Pid", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_PID : UNREAL_U_SEMTYPE_PID);
			case (_, _, _, _) when string.Equals(semType, "AccountId", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_ACCOUNTID : UNREAL_U_SEMTYPE_ACCOUNTID);
			case (_, _, _, _) when string.Equals(semType, "GamerTag", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_GAMERTAG : UNREAL_U_SEMTYPE_GAMERTAG);
			case (_, _, _, _) when string.Equals(semType, "ContentManifestId", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CONTENTMANIFESTID : UNREAL_U_SEMTYPE_CONTENTMANIFESTID);
			case (_, _, _, _) when string.Equals(semType, "ContentId", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_CONTENTID : UNREAL_U_SEMTYPE_CONTENTID);
			case (_, _, _, _) when string.Equals(semType, "StatsType", StringComparison.InvariantCultureIgnoreCase):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_U_SEMTYPE_STATSTYPE : UNREAL_U_SEMTYPE_STATSTYPE);

			case (_, _, _, _) when isArbitraryJsonObject:
				return new UnrealType(nonOverridenType = UNREAL_JSON);

			// Handle replacement types (types that we replace by hand-crafted types inside the SDK)
			case var (_, _, referenceId, _) when !string.IsNullOrEmpty(referenceId) && context.ReplacementTypes.TryGetValue(referenceId, out var replacementTypeInfo):
			{
				nonOverridenType = new(isOptional ? replacementTypeInfo.EngineOptionalReplacementType : replacementTypeInfo.EngineReplacementType);
				Log.Debug(GetLog(nameof(GetUnrealTypeForField).SpaceOutOnUpperCase(),
					$" FieldHandle=[{fieldDeclarationHandle}]," +
					$" ReferenceId=[{referenceId}]," +
					$" UnrealType=[{nonOverridenType}]"));
				return new(nonOverridenType);
			}

			// Handles any field of any existing Schema Types
			case var (_, _, _, _) when isPolymorphicWrapper:
			{
				var str = "";
				foreach (OpenApiSchema openApiSchema in schema.OneOf)
				{
					var polyWrappedSchema = openApiSchema.GetEffective(parentDoc);
					var wrappedUnrealType = GetNonOptionalUnrealTypeForField(context, parentDoc, polyWrappedSchema);
					str += $"_{RemovePtrFromUnrealTypeIfAny(wrappedUnrealType)}";

					if (polyWrappedSchema.Properties.TryGetValue("type", out var defaults))
					{
						var val = defaults.Default as OpenApiString;
						if (context.PolymorphicWrappedSchemaExpectedTypeValues.TryGetValue(wrappedUnrealType, out var existing) &&
						    (existing != val?.Value && existing != openApiSchema.Reference.Id.Sanitize()))
							throw new Exception(
								"Found a wrapped type that is currently used in two different ways. We don't support that cause it doesn't make a lot of sense. You should never see this.");

						context.PolymorphicWrappedSchemaExpectedTypeValues.TryAdd(wrappedUnrealType, val?.Value ?? openApiSchema.Reference.Id.Sanitize());
					}
				}

				nonOverridenType = MakeUnrealUObjectTypeFromNamespacedType(new NamespacedType($"OneOf{str}"));
				var appliedOverride = POLYMORPHIC_WRAPPER_TYPE_OVERRIDES.TryGetValue(nonOverridenType, out var overriden) ? overriden : nonOverridenType;
				return new UnrealType(POLYMORPHIC_WRAPPER_TYPE_OVERRIDES.ContainsKey(nonOverridenType)
					? appliedOverride
					: throw new Exception($"Should never see this!!! If you do, add an override to the UNREAL_TYPES_OVERRIDE with this as the key={nonOverridenType}"));
			}
			case ("object", _, "System.DateTime", _):
				return new(nonOverridenType = isOptional ? UNREAL_OPTIONAL_DATE_TIME : UNREAL_DATE_TIME);
			case ("string", _, "System.Guid", _):
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_GUID : UNREAL_GUID;
			case var (_, _, referenceId, _) when !string.IsNullOrEmpty(referenceId):
			{
				var namespacedType = GetNamespacedTypeNameFromSchema(context, parentDoc, referenceId, isOptional);

				UnrealType unrealType;
				if (isOptional) unrealType = MakeUnrealUStructTypeFromNamespacedType(namespacedType);
				else if (isEnum) unrealType = MakeUnrealUEnumTypeFromNamespacedType(namespacedType);
				else unrealType = MakeUnrealUObjectTypeFromNamespacedType(namespacedType);

				return nonOverridenType = unrealType;
			}
			// Handles any dictionary/map fields
			case ("object", _, _, _) when isDictionary:
			{
				if (schema.AdditionalProperties == null)
					return nonOverridenType = new UnrealType(UNREAL_MAP + $"<{UNREAL_STRING}, {UNREAL_STRING}>");

				// Get the data type but force it to not be an optional by passing in a blank field name!
				// We do this as it makes no sense to have an map of optionals --- the semantics for optional and maps are that the entire map is optional, instead.
				var dataType = GetNonOptionalUnrealTypeForContainerField(out var nonOverridenDataUnrealType, context, parentDoc, schema.AdditionalProperties, fieldDeclarationHandle).AsStr;
				var nonOverridenDataType = nonOverridenDataUnrealType.AsStr;

				// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
				// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
				if (new UnrealType(dataType).IsUnrealMap())
				{
					dataType = dataType.Remove(0, dataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
					dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F' from the type.
					dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
					dataType = UNREAL_WRAPPER_MAP + dataType;
				}
				else if (new UnrealType(dataType).IsUnrealArray())
				{
					dataType = dataType.Remove(0, dataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
					dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
					dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
					dataType = UNREAL_WRAPPER_ARRAY + dataType;
				}

				// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
				// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
				if (new UnrealType(nonOverridenDataType).IsUnrealMap())
				{
					nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
					nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F' from the type.
					nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType));
					nonOverridenDataType = UNREAL_WRAPPER_MAP + nonOverridenDataType;
				}
				else if (new UnrealType(nonOverridenDataType).IsUnrealArray())
				{
					nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
					nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
					nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType));
					nonOverridenDataType = UNREAL_WRAPPER_ARRAY + nonOverridenDataType;
				}


				nonOverridenType = new(isOptional
					? UNREAL_OPTIONAL_MAP + $"{GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType))}" // Remove the "F" from the Unreal type when declaring an optional map
					: UNREAL_MAP + $"<{UNREAL_STRING}, {nonOverridenDataType}>");

				// Depending on whether or not this is an optional map or not, we generate a different Unreal Type for this field's declaration.
				return new(isOptional
					? UNREAL_OPTIONAL_MAP + $"{GetNamespacedTypeNameFromUnrealType(new(dataType))}" // Remove the "F" from the Unreal type when declaring an optional map
					: UNREAL_MAP + $"<{UNREAL_STRING}, {dataType}>");
			}
			case ("object", _, _, _) when schema.Reference == null && !schema.AdditionalPropertiesAllowed:
				if (parentDoc.Components.Schemas.TryGetValue(schema.Title, out var innerSchema))
				{
					return GetUnrealTypeForField(out nonOverridenType, context, parentDoc, innerSchema, fieldDeclarationHandle, flags);
				}
				throw new Exception(
					"Object fields must either reference some other schema or must be a map/dictionary!");
			case ("array", _, _, _):
			{
				var isReference = schema.Items.Reference != null;
				var arrayTypeSchema = isReference ? schema.Items.GetEffective(parentDoc) : schema.Items;

				if (isOptional)
				{
					// Get the data type but force it to not be an optional by passing in a blank field map!
					// We do this as it makes no sense to have an array of optionals --- the semantics for optional and arrays are that the entire array is optional, instead.
					var dataType = GetNonOptionalUnrealTypeForContainerField(out var nonOverridenDataUnrealType, context, parentDoc, arrayTypeSchema, fieldDeclarationHandle).AsStr;
					dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
					var nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataUnrealType)).AsStr;

					// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
					// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
					if (new UnrealType(dataType).IsUnrealMap())
					{
						dataType = dataType.Remove(0, dataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
						dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F' from the type.
						dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
						dataType = UNREAL_WRAPPER_MAP + dataType;
					}
					else if (new UnrealType(dataType).IsUnrealArray())
					{
						dataType = dataType.Remove(0, dataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
						dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
						dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
						dataType = UNREAL_WRAPPER_ARRAY + dataType;
					}

					// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
					// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
					if (new UnrealType(nonOverridenDataType).IsUnrealMap())
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType));
						nonOverridenDataType = UNREAL_WRAPPER_MAP + nonOverridenDataType;
					}
					else if (new UnrealType(nonOverridenDataType).IsUnrealArray())
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType));
						nonOverridenDataType = UNREAL_WRAPPER_ARRAY + nonOverridenDataType;
					}

					// Remove the "F" from the Unreal type when declaring an optional array
					nonOverridenType = new(UNREAL_OPTIONAL_ARRAY + $"{GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType))}");
					return new(UNREAL_OPTIONAL_ARRAY + $"{GetNamespacedTypeNameFromUnrealType(new(dataType))}");
				}
				else
				{
					var dataType = GetUnrealTypeForField(out var nonOverridenDataUnrealType, context, parentDoc, arrayTypeSchema, fieldDeclarationHandle).AsStr;
					var nonOverridenDataType = nonOverridenDataUnrealType.AsStr;

					// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
					// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
					if (new UnrealType(dataType).IsUnrealMap())
					{
						dataType = dataType.Remove(0, dataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
						dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F' from the type.
						dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
						dataType = UNREAL_WRAPPER_MAP + dataType;
					}
					else if (new UnrealType(dataType).IsUnrealArray())
					{
						dataType = dataType.Remove(0, dataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
						dataType = dataType.Substring(0, dataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
						dataType = GetNamespacedTypeNameFromUnrealType(new(dataType));
						dataType = UNREAL_WRAPPER_ARRAY + dataType;
					}

					// Since Unreal doesn't support nested containers in Blueprints, we generate wrapper types for arrays like these.
					// WE ONLY SUPPORT SINGLE NESTING OF CONTAINERS!!!
					if (new UnrealType(nonOverridenDataType).IsUnrealMap())
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf(',') + 1).Trim(); // Find the ',' in TMap<FString, WHAT_WE_WANT> 
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType));
						nonOverridenDataType = UNREAL_WRAPPER_MAP + nonOverridenDataType;
					}
					else if (new UnrealType(nonOverridenDataType).IsUnrealArray())
					{
						nonOverridenDataType = nonOverridenDataType.Remove(0, nonOverridenDataType.IndexOf('<') + 1); // Find the '<' in TArray<WHAT_WE_WANT>
						nonOverridenDataType = nonOverridenDataType.Substring(0, nonOverridenDataType.Length - 1); // Remove '>' and 'F'/'U' from the type.
						nonOverridenDataType = GetNamespacedTypeNameFromUnrealType(new(nonOverridenDataType));
						nonOverridenDataType = UNREAL_WRAPPER_ARRAY + nonOverridenDataType;
					}

					nonOverridenType = new UnrealType(UNREAL_ARRAY + $"<{nonOverridenDataType}>");
					return new UnrealType(UNREAL_ARRAY + $"<{dataType}>");
				}
			}
			// Handle Primitive Types 
			case ("number", "float", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_FLOAT : UNREAL_FLOAT;
			}
			case ("number", "double", _, _):
			case ("number", _, _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_DOUBLE : UNREAL_DOUBLE;
			}
			case ("boolean", _, _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_BOOL : UNREAL_BOOL;
			}
			case ("string", "uuid", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_GUID : UNREAL_GUID;
			}
			case ("string", "date-time", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_DATE_TIME : UNREAL_DATE_TIME;
			}
			case ("string", "byte", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_BYTE : UNREAL_BYTE;
			}
			case ("string", _, _, _) when (schema?.Extensions.TryGetValue("x-beamable-object-id", out _) ?? false):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_STRING : UNREAL_STRING;
			}
			case ("System.String", _, _, _):
			case ("string", _, _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_STRING : UNREAL_STRING;
			}
			case ("integer", "int16", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_SHORT : UNREAL_SHORT;
			}
			case ("integer", "int32", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_INT : UNREAL_INT;
			}
			case ("integer", "int64", _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_LONG : UNREAL_LONG;
			}
			case ("integer", _, _, _):
			{
				return nonOverridenType = isOptional ? UNREAL_OPTIONAL_INT : UNREAL_INT;
			}
			default:
				return nonOverridenType = new("");
		}
	}

	/// <summary>
	/// Gets a guaranteed non-optional type for the given field schema, even if the field schema is in-fact Optional.
	/// We use this in order to get the correct type to pass into the serialization/deserialization templated functions that work with FBeamOptionals in Unreal-land.
	/// </summary>
	private static UnrealType GetNonOptionalUnrealTypeForField(UEGenerationContext context, [NotNull] OpenApiDocument parentDoc, [NotNull] OpenApiSchema fieldSchema,
		UnrealTypeGetFlags flags = UnrealTypeGetFlags.ReturnUnderlyingOptionalType)
	{
		// This passes in a blank field map which means it's not possible for it to found in the fieldRequiredMaps. This means we will get the Required Version of it. 
		return GetUnrealTypeForField(out _, context, parentDoc, fieldSchema, new FieldDeclarationHandle("", ""), UnrealTypeGetFlags.ReturnUnderlyingOptionalType | flags);
	}

	/// <summary>
	/// Variation of <see cref="GetNonOptionalUnrealTypeForField"/> for recursive use inside <see cref="GetUnrealTypeForField"/>.
	/// It passes the field handle of the container, prefixed by C, so that we can print the field when replacing a type inside a container field. 
	/// </summary>
	private static UnrealType GetNonOptionalUnrealTypeForContainerField(out UnrealType nonOverridenType, UEGenerationContext context, [NotNull] OpenApiDocument parentDoc, [NotNull] OpenApiSchema fieldSchema,
		FieldDeclarationHandle containerFieldHandle, UnrealTypeGetFlags flags = UnrealTypeGetFlags.ReturnUnderlyingOptionalType)
	{
		// This passes in a blank field map which means it's not possible for it to found in the fieldRequiredMaps. This means we will get the Required Version of it. 
		return GetUnrealTypeForField(out nonOverridenType, context, parentDoc, fieldSchema, new FieldDeclarationHandle($"C_{containerFieldHandle.Owner}", containerFieldHandle.FieldOrParamName),
			UnrealTypeGetFlags.ReturnUnderlyingOptionalType | flags);
	}

	/// <summary>
	/// Makes a UnrealType from a NamespacedType that the caller knows should become a UObject*.
	/// </summary>
	private static UnrealType MakeUnrealUObjectTypeFromNamespacedType(NamespacedType namespacedType) => new($"U{namespacedType.AsStr.Capitalize()}*");

	/// <summary>
	/// Makes a UnrealType from a NamespacedType that the caller knows should become a F_____.
	/// </summary>
	private static UnrealType MakeUnrealUStructTypeFromNamespacedType(NamespacedType referenceId) => new($"F{referenceId.AsStr.Capitalize()}");

	/// <summary>
	/// Makes a UnrealType from a NamespacedType that the caller knows should become a F_____.
	/// </summary>
	private static UnrealType MakeUnrealUEnumTypeFromNamespacedType(NamespacedType referenceId)
	{
		var prefix = genType != GenerationType.Microservice ? "EBeam" : "E";
		string enumName = referenceId.AsStr.Replace(".", "").Capitalize();
		return new UnrealType($"{prefix}{enumName}");
	}

	/// <summary>
	/// Checks if the given schema should be interpreted as a FJsonObject type in UE.
	/// These types are not Blueprint compatible.
	/// </summary>
	private static bool IsArbitraryJsonBlob(OpenApiDocument parentDoc, OpenApiSchema schema) => schema.GetEffective(parentDoc).Extensions.ContainsKey("x-beamable-json-object");

	/// <summary>
	/// Checks if the given schema has a known semantic type.
	/// </summary>
	private static bool IsSemanticTypeSchema(OpenApiSchema schema) => schema.Extensions.TryGetValue(Constants.EXTENSION_BEAMABLE_SEMANTIC_TYPE, out var ext) && ext is OpenApiString;

	/// <summary>
	/// Checks if the given schema is a Polymorphic schema. This does not recursively descend in case of containers.
	/// </summary>
	private static bool IsPolymorphicWrapperSchema(OpenApiSchema schema) => schema.OneOf.Count > 0;

	/// <summary>
	/// Checks if the given schema references itself (as in, its recursive).
	/// </summary>
	private static bool IsSelfReferentialSchema(OpenApiDocument parentDoc, OpenApiSchema schema) => schema.GetEffective(parentDoc).Extensions.ContainsKey(Constants.EXTENSION_BEAMABLE_SELF_REFERENTIAL_TYPE);

	/// <summary>
	/// Given an unreal string created with <see cref="GetUnrealTypeForField"/> turns it into a valid Namespaced Type Name (<see cref="GetNamespacedTypeNameFromSchema"/>).
	/// </summary>
	private static NamespacedType GetNamespacedTypeNameFromUnrealType(UnrealType unrealTypeName)
	{
		if (unrealTypeName.IsUnrealByte())
			return new("Int8");

		if (unrealTypeName.IsUnrealShort())
			return new("Int16");
		if (unrealTypeName.IsUnrealInt())
			return new("Int32");

		if (unrealTypeName.IsUnrealLong())
			return new("Int64");

		if (unrealTypeName.IsUnrealBool())
			return new("Bool");

		if (unrealTypeName.IsUnrealFloat())
			return new("Float");

		if (unrealTypeName.IsUnrealDouble())
			return new("Double");


		// F"AnyTypes"/E"AnyEnums" we just remove the F's/E's
		if (char.IsUpper(unrealTypeName.AsStr[1]) && (unrealTypeName.IsUnrealStruct() || unrealTypeName.IsUnrealEnum()))
			return new(unrealTypeName.AsStr.AsSpan(1).ToString());

		// U"AnyTypes"* we just remove the U's and *'s
		if (char.IsUpper(unrealTypeName.AsStr[1]) && unrealTypeName.IsUnrealUObject())
			return new(unrealTypeName.AsStr.AsSpan(1, unrealTypeName.AsStr.Length - 2).ToString());

		return new(unrealTypeName);
	}

	/// <summary>
	/// Given an unreal string created with <see cref="GetUnrealTypeForField"/> turns it into a valid Namespaced Type Name (<see cref="GetNamespacedTypeNameFromSchema"/>).
	/// </summary>
	private static UnrealType GetWrappedUnrealTypeFromUnrealWrapperType(string unrealWrapperType)
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
		if (namespacedWrappedType == "DateTime")
			return UNREAL_DATE_TIME;
		if (namespacedWrappedType == "JsonObject")
			return UNREAL_JSON;

		// If (SomethingArrayey | SomethingMappy | SomethingOptional) aren't any of the raw cases above, we just prepend an 'U' and '*' to it.
		return new($"U{namespacedWrappedType}*");
	}

	/// <summary>
	/// If the given <paramref name="ueType"/> ends with a '*' (as in, is a pointer declaration), we remove it. 
	/// </summary>
	public static string RemovePtrFromUnrealTypeIfAny(string ueType) => ueType.EndsWith("*") ? ueType.Substring(0, ueType.Length - 1) : ueType;

	/// <summary>
	/// Gets the header file name for any given UnrealType.
	/// </summary>
	private static string GetIncludeStatementForUnrealType(UEGenerationContext context, UnrealType unrealType)
	{
		if (string.IsNullOrEmpty(unrealType))
			throw new Exception("Investigate this... It should never happen!");

		// First go over all non-generated first-class types
		{
			if (unrealType.IsPlainTextResponse())
				return @"#include ""Serialization/BeamPlainTextResponseBody.h""";

			// TODO: Add sem type includes here... 
			if (unrealType.IsBeamCid())
				return @"#include ""BeamBackend/SemanticTypes/BeamCid.h""";

			if (unrealType.IsBeamPid())
				return @"#include ""BeamBackend/SemanticTypes/BeamPid.h""";

			if (unrealType.IsAccountId())
				return @"#include ""BeamBackend/SemanticTypes/BeamAccountId.h""";

			if (unrealType.IsGamerTag())
				return @"#include ""BeamBackend/SemanticTypes/BeamGamerTag.h""";

			if (unrealType.IsContentId())
				return @"#include ""BeamBackend/SemanticTypes/BeamContentId.h""";

			if (unrealType.IsContentManifestId())
				return @"#include ""BeamBackend/SemanticTypes/BeamContentManifestId.h""";

			if (unrealType.IsStatsType())
				return @"#include ""BeamBackend/SemanticTypes/BeamStatsType.h""";

			if (unrealType.IsUnrealJson())
				return @"#include ""Dom/JsonObject.h""";

			if (unrealType.IsRawPrimitive())
				return @"#include ""Serialization/BeamJsonUtils.h""";

			if (context.ReplacementTypesIncludes.TryGetValue(unrealType, out var replacementTypeInclude))
				return replacementTypeInclude;
		}

		// Then, go over all generated types
		{
			if (unrealType.IsUnrealEnum())
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";

				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{includeStatementPrefix}AutoGen/Enums/{header}\"";
			}

			if (unrealType.IsOptional())
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{includeStatementPrefix}AutoGen/Optionals/{header}\"";
			}

			if (unrealType.IsWrapperArray())
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{includeStatementPrefix}AutoGen/Arrays/{header}\"";
			}

			if (unrealType.IsWrapperMap())
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				return $"#include \"{includeStatementPrefix}AutoGen/Maps/{header}\"";
			}

			if (unrealType.IsUnrealArray())
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var firstTemplate = UnrealPropertyDeclaration.ExtractFirstTemplateParamFromType(unrealType);
				if (MustInclude(firstTemplate))
				{
					return GetIncludeStatementForUnrealType(context, firstTemplate);
				}
			}

			if (unrealType.IsUnrealMap())
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";
				var secondTemplate = UnrealPropertyDeclaration.ExtractSecondTemplateParamFromType(unrealType);
				if (MustInclude(secondTemplate))
				{
					return GetIncludeStatementForUnrealType(context, secondTemplate);
				}
			}

			if (MustInclude(unrealType))
			{
				if (previousGenerationPassesData.InEngineTypeToIncludePaths.TryGetValue(unrealType, out var includeStatement))
					return $"#include \"{includeStatement}\"";

				var header = $"{GetNamespacedTypeNameFromUnrealType(unrealType)}.h";
				if (unrealType.IsBeamNode())
					return $"#include \"{blueprintIncludeStatementPrefix}AutoGen/{header}\"";

				return $"#include \"{includeStatementPrefix}AutoGen/{header}\"";
			}
		}

		return "";

		bool MustInclude(UnrealType s) => s.IsUnrealUObject() || s.IsUnrealEnum() || s.IsUnrealStruct() && !s.IsUnrealGuid() && !s.IsUnrealString() && !s.IsUnrealDateTime();
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
