using cli.Unreal;
using System.Collections;
using System.CommandLine;
using System.CommandLine.Help;
using Beamable.Server;
using System.Reflection;

namespace cli.Services;

public struct UnrealCliCommandDeclaration
{
	/// <summary>
	/// BeamCli<Commands>
	/// </summary>
	public string CommandName;

	public string CommandKeywords;
	public string HelpString;

	public List<UnrealCliStreamDeclaration> Streams;

	private string _streamFieldDeclarations;
	private string _parseStreamDataImpl;
	private string _streamDataTypeIncludes;

	public void IntoProcessDict(Dictionary<string, string> dict)
	{
		// We declare these in opposite order
		foreach (var dec in Streams) dec.RootDataTypes.Reverse();

		_streamFieldDeclarations = string.Join("\n\n\t", Streams.Select(s =>
		{
			dict.Clear();
			s.IntoProcessDict(dict);
			return UnrealCliStreamDeclaration.STREAM_COMMAND_FIELDS.ProcessReplacement(dict);
		}));

		var streams = string.Join("\n", Streams.SelectMany(s => s.RootDataTypes)
			.Select(dt =>
			{
				dict.Clear();
				dt.IntoProcessDict(dict);
				return UnrealCliStreamDataDeclaration.STREAM_OBJECTS_DECL.ProcessReplacement(dict);
			}));

		_parseStreamDataImpl = string.Join("\n", Streams.Select(s =>
		{
			dict.Clear();
			s.IntoProcessDict(dict);
			return UnrealCliStreamDeclaration.STREAM_PARSE_IMPL.ProcessReplacement(dict);
		}));

		// Gets the name of the classes and forward declare them
		_streamDataTypeIncludes = string.Join("\n", Streams.SelectMany(s =>
		{
			return s.RootDataTypes.SelectMany(dt => dt.DataTypeIncludes);
		}));

		dict.Clear();
		dict.Add(nameof(CommandName), CommandName);
		dict.Add(nameof(CommandKeywords), CommandKeywords);
		dict.Add(nameof(HelpString), HelpString);
		dict.Add(nameof(Streams), streams);
		dict.Add(nameof(_streamFieldDeclarations), _streamFieldDeclarations);
		dict.Add(nameof(_parseStreamDataImpl), _parseStreamDataImpl);
		dict.Add(nameof(_streamDataTypeIncludes), _streamDataTypeIncludes);
	}

	public const string HEADER_COMMAND_TEMPLATE = $@"#pragma once

#include ""Subsystems/CLI/BeamCliCommand.h""
#include ""Serialization/BeamJsonUtils.h""
₢{nameof(_streamDataTypeIncludes)}₢
#include ""₢{nameof(CommandName)}₢Command.generated.h""

₢{nameof(Streams)}₢

/**
 ₢{nameof(HelpString)}₢
 */
UCLASS()
class U₢{nameof(CommandName)}₢Command : public UBeamCliCommand
{{
	GENERATED_BODY()

public:
	₢{nameof(_streamFieldDeclarations)}₢	

	TFunction<void (const int& ResCode, const FBeamOperationHandle& Op)> OnCompleted;
	virtual bool HandleStreamReceived(FBeamOperationHandle Op, FString ReceivedStreamType, int64 Timestamp, TSharedRef<FJsonObject> DataJson, bool isServer) override;
	virtual void HandleStreamCompleted(FBeamOperationHandle Op, int ResultCode, bool isServer) override;
	virtual FString GetCommand() override;
}};
";

	public const string CPP_COMMAND_TEMPLATE = $@"#include ""₢{nameof(CommandName)}₢Command.h""

#include ""BeamLogging.h""
#include ""Serialization/JsonSerializerMacros.h""

FString U₢{nameof(CommandName)}₢Command::GetCommand()
{{
	return FString(TEXT(""₢{nameof(CommandKeywords)}₢""));
}}
		
bool U₢{nameof(CommandName)}₢Command::HandleStreamReceived(FBeamOperationHandle Op, FString ReceivedStreamType, int64 Timestamp, TSharedRef<FJsonObject> DataJson, bool isServer)
{{
	₢{nameof(_parseStreamDataImpl)}₢
	
	return false;
}}

void U₢{nameof(CommandName)}₢Command::HandleStreamCompleted(FBeamOperationHandle Op, int ResultCode, bool isServer)
{{
	if (OnCompleted)
	{{
		AsyncTask(ENamedThreads::GameThread, [this, ResultCode, Op]
		{{
			OnCompleted(ResultCode, Op);
		}});
	}}
}}
";
}

public struct UnrealCliStreamDataDeclaration
{
	public UnrealSourceGenerator.UnrealType StreamDataName;
	public UnrealSourceGenerator.NamespacedType NamespacedStreamDataName;
	public List<UnrealPropertyDeclaration> StreamDataProperties;
	public List<string> DataTypeIncludes;

	private string _propertySerialization;
	private string _propertyDeserialization;

	public void IntoProcessDict(Dictionary<string, string> helperDict)
	{
		NamespacedStreamDataName = StreamDataName.AsNamespacedType();
		_propertySerialization = string.Join("\n\t\t", StreamDataProperties.Select(ud =>
		{
			ud.IntoProcessMap(helperDict);

			var decl = UnrealPropertyDeclaration.GetSerializeTemplateForUnrealType(ud.PropertyUnrealType).ProcessReplacement(helperDict);
			helperDict.Clear();
			return decl;
		}));
		_propertyDeserialization = string.Join("\n\t\t", StreamDataProperties.Select(ud =>
		{
			ud.IntoProcessMap(helperDict);

			var decl = UnrealPropertyDeclaration.GetDeserializeTemplateForUnrealType(ud.PropertyUnrealType).ProcessReplacement(helperDict);
			helperDict.Clear();
			return decl;
		}));

		var properties = string.Join("\n\t", StreamDataProperties.Select(p => $"UPROPERTY(EditAnywhere, BlueprintReadWrite)\n\t{p.PropertyUnrealType} {p.PropertyName} = {{}};"));

		helperDict.Clear();
		helperDict.Add(nameof(NamespacedStreamDataName), NamespacedStreamDataName);
		helperDict.Add(nameof(StreamDataName), StreamDataName);
		helperDict.Add(nameof(StreamDataProperties), properties);
		helperDict.Add(nameof(DataTypeIncludes), string.Join("\n", DataTypeIncludes));
		helperDict.Add(nameof(_propertySerialization), _propertySerialization);
		helperDict.Add(nameof(_propertyDeserialization), _propertyDeserialization);
	}

	public const string STREAM_OBJECTS_DECL = $@"
UCLASS(BlueprintType)
class U₢{nameof(NamespacedStreamDataName)}₢ : public UObject, public IBeamJsonSerializableUObject
{{
	GENERATED_BODY()

public:	
	
	₢{nameof(StreamDataProperties)}₢

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override
	{{
		₢{nameof(_propertySerialization)}₢	
	}}

	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override
	{{
		₢{nameof(_propertySerialization)}₢	
	}}

	virtual void BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag) override
	{{
		₢{nameof(_propertyDeserialization)}₢	
	}}
}};
";

	public const string HEADER_DATA_TEMPLATE = $@"
#pragma once

₢{nameof(DataTypeIncludes)}₢
#include ""Serialization/BeamJsonUtils.h""
#include ""₢{nameof(NamespacedStreamDataName)}₢.generated.h""

{STREAM_OBJECTS_DECL}

";
}

public struct UnrealCliStreamDeclaration
{
	public string CommandName;
	public string RawStreamName;
	public string StreamName;

	public List<UnrealCliStreamDataDeclaration> RootDataTypes;

	private UnrealSourceGenerator.UnrealType _rootDataType;
	private UnrealSourceGenerator.NamespacedType _rootDataNamespacedType;


	public void IntoProcessDict(Dictionary<string, string> dictionary)
	{
		_rootDataType = RootDataTypes[0].StreamDataName;
		_rootDataNamespacedType = _rootDataType.AsNamespacedType();

		dictionary.Clear();
		dictionary.Add(nameof(CommandName), CommandName);
		dictionary.Add(nameof(RawStreamName), RawStreamName);
		dictionary.Add(nameof(StreamName), StreamName);
		dictionary.Add(nameof(_rootDataType), _rootDataType);
		dictionary.Add(nameof(_rootDataNamespacedType), _rootDataNamespacedType);
	}

	public const string STREAM_COMMAND_FIELDS = $@"inline static FString StreamType₢{nameof(StreamName)}₢ = FString(TEXT(""₢{nameof(RawStreamName)}₢""));
	UPROPERTY() TArray<₢{nameof(_rootDataType)}₢> ₢{nameof(StreamName)}₢Stream;
	UPROPERTY() TArray<int64> ₢{nameof(StreamName)}₢Timestamps;
	TFunction<void (TArray<₢{nameof(_rootDataType)}₢>& StreamData, TArray<int64>& Timestamps, const FBeamOperationHandle& Op)> On₢{nameof(StreamName)}₢StreamOutput;";

	public const string STREAM_PARSE_IMPL = $@"
	if(ReceivedStreamType.Equals(StreamType₢{nameof(StreamName)}₢) && On₢{nameof(StreamName)}₢StreamOutput)
	{{
		AsyncTask(ENamedThreads::GameThread, [this, DataJson, Timestamp, Op]
		{{
			₢{nameof(_rootDataType)}₢ Data = NewObject<U₢{nameof(_rootDataNamespacedType)}₢>(this);
			Data->OuterOwner = this;
			Data->BeamDeserializeProperties(DataJson);

			₢{nameof(StreamName)}₢Stream.Add(Data);
			₢{nameof(StreamName)}₢Timestamps.Add(Timestamp);
		
		
			On₢{nameof(StreamName)}₢StreamOutput(₢{nameof(StreamName)}₢Stream, ₢{nameof(StreamName)}₢Timestamps, Op);
		}});
		
		return true;				
	}}";
}

public class UnrealCliGenerator : ICliGenerator
{
	public List<GeneratedFileDescriptor> Generate(CliGeneratorContext context)
	{
		// the following commands are more complicated and either use nullables or enums
		var invalidCommands = new string[] { "beam", "beam services register", "beam services modify", "beam services enable", "beam oapi generate", "beam deployment", "beam deployment" };
		var invalidCommandPallets = new string[] { "beam deployment" };

		var files = new List<GeneratedFileDescriptor>();
		var allDataTypes = new List<UnrealCliStreamDataDeclaration>();
		foreach (var command in context.Commands)
		{
			if (!command.hasValidOutput && command.executionPath != "beam") continue;
			if (invalidCommands.Contains(command.executionPath)) continue;
			if (invalidCommandPallets.Any(p => command.executionPath.StartsWith(p)))
				continue;

			var nonBeamCommandNames = command.executionPath.Substring(command.executionPath.IndexOf(" ", StringComparison.Ordinal) + 1);
			var commandName = $"BeamCli{string.Join("", nonBeamCommandNames.Split(" ").Select(c => c.Sanitize().Capitalize()))}";
			var cliCommandDeclaration = new UnrealCliCommandDeclaration()
			{
				CommandName = commandName,
				// execution path => "beam project ps"; what we want is only the "project ps"
				CommandKeywords = nonBeamCommandNames,
				HelpString = GenerateHelpString(command),
				Streams = command.resultStreams.Select(rs =>
				{
					// For the default stream, we don't add it to the type name
					var streamChannel = rs.channel == "stream" ? "" : rs.channel.Sanitize().Capitalize();

					// Recursively descend into the type stream's output type declaration collecting the various data structs required to compose the object.
					// This will redeclare structs per stream
					List<UnrealCliStreamDataDeclaration> dataTypes = new();
					FindDataDeclarations(rs.runtimeType, dataTypes, streamChannel, new UnrealSourceGenerator.UnrealType($"U{commandName}{streamChannel}StreamData*"));
					dataTypes = dataTypes.DistinctBy(dt => dt.StreamDataName.AsStr).ToList();

					// Just leave this at the stream declaration file... 
					// All the other data declarations should be in their own file (and distinctBy their names)... this does mean that we'll need to get the include path for these data types.
					var streamRootDataDeclaration = dataTypes[0];
					var nonRootDataTypes = dataTypes.Skip(1).ToList();
					allDataTypes.AddRange(nonRootDataTypes);

					// Set the include paths for this declaration.
					var dataIncludePaths = nonRootDataTypes.Select(GetNonRootStreamDataIncludePath).ToList();
					streamRootDataDeclaration.DataTypeIncludes = dataIncludePaths;

					return new UnrealCliStreamDeclaration()
					{
						CommandName = commandName, RawStreamName = rs.channel, StreamName = streamChannel, RootDataTypes = new() { streamRootDataDeclaration },
					};
				}).ToList()
			};

			var dict = new Dictionary<string, string>();

			cliCommandDeclaration.IntoProcessDict(dict);
			files.Add(new GeneratedFileDescriptor()
			{
				FileName = $"BeamableCoreRuntimeEditor/Public/Subsystems/CLI/Autogen/{commandName}Command.h", Content = UnrealCliCommandDeclaration.HEADER_COMMAND_TEMPLATE.ProcessReplacement(dict)
			});
			files.Add(new GeneratedFileDescriptor()
			{
				FileName = $"BeamableCoreRuntimeEditor/Public/Subsystems/CLI/Autogen/{commandName}Command.cpp", Content = UnrealCliCommandDeclaration.CPP_COMMAND_TEMPLATE.ProcessReplacement(dict)
			});
		}

		var dataTypesDict = new Dictionary<string, string>();
		allDataTypes = allDataTypes.DistinctBy(dt => dt.StreamDataName.AsStr).ToList();
		foreach (UnrealCliStreamDataDeclaration unrealCliStreamDataDeclaration in allDataTypes)
		{
			unrealCliStreamDataDeclaration.IntoProcessDict(dataTypesDict);
			var dataName = unrealCliStreamDataDeclaration.NamespacedStreamDataName;
			files.Add(new GeneratedFileDescriptor()
			{
				FileName = $"BeamableCoreRuntimeEditor/Public/Subsystems/CLI/Autogen/StreamData/{dataName}.h", Content = UnrealCliStreamDataDeclaration.HEADER_DATA_TEMPLATE.ProcessReplacement(dataTypesDict)
			});
			dataTypesDict.Clear();
		}

		return files;
	}

	private static string GetNonRootStreamDataIncludePath(UnrealCliStreamDataDeclaration dataDeclaration)
	{
		return $@"#include ""Subsystems/CLI/Autogen/StreamData/{dataDeclaration.NamespacedStreamDataName.AsStr}.h""";
	}

	private static string GenerateHelpString(BeamCommandDescriptor descriptor)
	{
		var textWriter = new StringWriter();
		var helpContext = new HelpContext(new HelpBuilder(LocalizationResources.Instance), descriptor.command, textWriter);
		helpContext.HelpBuilder.Write(helpContext);
		return textWriter.GetStringBuilder().ToString();
	}

	private static void FindDataDeclarations(Type t, List<UnrealCliStreamDataDeclaration> outDeclarations, string streamChannel, UnrealSourceGenerator.UnrealType typeNameOverride)
	{
		typeNameOverride = string.IsNullOrEmpty(typeNameOverride.AsStr) ? GetStreamDataUnrealType(t, streamChannel) : typeNameOverride;

		var streamDataProperties = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.Select(fieldInfo =>
			{
				var fieldType = fieldInfo.FieldType;
				var underlyingFieldType = fieldInfo.FieldType;
				CheckIsContainer(fieldType, out var arr, out var list, out var dict);
				if (arr || list || dict)
				{
					if (list)
						underlyingFieldType = underlyingFieldType.GenericTypeArguments[0];
					else if (dict)
						underlyingFieldType = underlyingFieldType.GenericTypeArguments[1];
					else
						underlyingFieldType = underlyingFieldType.GetElementType()!;
				}

				var typeName = GetStreamDataUnrealType(fieldInfo.FieldType, streamChannel);
				return new UnrealPropertyDeclaration()
				{
					PropertyName = UnrealPropertyDeclaration.GetSanitizedPropertyName(fieldInfo.Name.Capitalize()),
					PropertyDisplayName = UnrealPropertyDeclaration.GetSanitizedPropertyDisplayName(fieldInfo.Name.Capitalize()),
					AsParameterName = UnrealPropertyDeclaration.GetSanitizedParameterName(fieldInfo.Name.Capitalize()),
					PropertyUnrealType = typeName,
					PropertyNamespacedType = typeName.AsNamespacedType(),
					NonOptionalTypeName = typeName,
					NonOptionalTypeNameRelevantTemplateParam = GetStreamDataUnrealType(underlyingFieldType, streamChannel),
					RawFieldName = fieldInfo.Name,
					SemTypeSerializationType = GetStreamDataUnrealType(typeof(string), streamChannel),
				};
			})
			.Where(p => !string.IsNullOrEmpty(p.PropertyUnrealType.AsStr))
			.ToList();

		// We should never get a "t" that is a container --- the property iterating loop below should filter them out into their element types. 
		CheckIsContainer(t, out var isArray, out var isList, out var isDictionary);
		if (isArray || isList || isDictionary || t.IsEnum)
			throw new Exception("We don't support generating nested containers or enums here. Wrap your internal container in a class/struct OR pass your enum as an integer value or string.");

		if (typeof(ValueType).IsAssignableFrom(t) || typeof(object).IsAssignableFrom(t))
		{
			var declaration = new UnrealCliStreamDataDeclaration() { StreamDataName = typeNameOverride, NamespacedStreamDataName = typeNameOverride.AsNamespacedType(), StreamDataProperties = streamDataProperties, };

			var types = t.GetFields().Select(fieldInfo => fieldInfo.FieldType);
			var newDataTypes = new List<UnrealCliStreamDataDeclaration>();
			foreach (Type type in types)
			{
				// Check if its an array, if it is we need to verify its internal type
				// Check if its a dictionary, if it is we need to verify its second parameter
				var subType = type;
				CheckIsContainer(subType, out isArray, out isList, out isDictionary);
				if (isList || isArray || isDictionary)
				{
					if (isList)
						subType = subType.GenericTypeArguments[0];
					else if (isDictionary)
						subType = subType.GenericTypeArguments[1];
					else
						subType = subType.GetElementType()!;
				}

				if ((subType.IsClass || subType.IsValueType) && !subType.IsPrimitive && subType != typeof(string) && subType != typeof(DateTime))
				{
					FindDataDeclarations(subType, newDataTypes, streamChannel, new UnrealSourceGenerator.UnrealType());
				}
			}

			declaration.DataTypeIncludes = newDataTypes.DistinctBy(dt => dt.StreamDataName).Select(GetNonRootStreamDataIncludePath).ToList();
			outDeclarations.Add(declaration);
			outDeclarations.AddRange(newDataTypes);
		}
	}

	private static void CheckIsContainer(Type t, out bool isArray, out bool isList, out bool isDictionary)
	{
		isArray = t.IsArray;
		isList = t.IsGenericType && typeof(IList).IsAssignableFrom(t);
		isDictionary = t.IsGenericType && typeof(IDictionary).IsAssignableFrom(t);
	}

	/// <summary>
	/// Gets an Unreal type from a <see cref="System.Type"/>. Used primarily for code generating the CLI interface for invocation from inside Unreal. 
	/// </summary>
	private static UnrealSourceGenerator.UnrealType GetStreamDataUnrealType(Type type, string streamChannel)
	{
		static string GetPrimitive(Type type)
		{
			if (type == typeof(byte))
				return UnrealSourceGenerator.UNREAL_BYTE;
			if (type == typeof(short))
				return UnrealSourceGenerator.UNREAL_SHORT;
			if (type == typeof(int))
				return UnrealSourceGenerator.UNREAL_INT;
			if (type == typeof(long))
				return UnrealSourceGenerator.UNREAL_LONG;
			if (type == typeof(bool))
				return UnrealSourceGenerator.UNREAL_BOOL;
			if (type == typeof(float))
				return UnrealSourceGenerator.UNREAL_FLOAT;
			if (type == typeof(double))
				return UnrealSourceGenerator.UNREAL_DOUBLE;
			if (type == typeof(string))
				return UnrealSourceGenerator.UNREAL_STRING;
			if (type == typeof(Guid))
				return UnrealSourceGenerator.UNREAL_GUID;
			if (type == typeof(DateTime))
				return UnrealSourceGenerator.UNREAL_DATE_TIME;

			return "";
		}

		var primitiveType = GetPrimitive(type);

		if (string.IsNullOrEmpty(primitiveType))
		{
			var isList = type.IsGenericType && typeof(IList).IsAssignableFrom(type);
			var isArray = type.IsArray;
			if (isList || isArray)
			{
				var subType = isArray ? type.GetElementType() : type.GenericTypeArguments[0];
				var subTypeUnreal = GetStreamDataUnrealType(subType, streamChannel);
				return new(UnrealSourceGenerator.UNREAL_ARRAY + $"<{subTypeUnreal}>");
			}

			var isDictionary = type.IsGenericType && typeof(IDictionary).IsAssignableFrom(type);
			if (isDictionary)
			{
				if (type.GenericTypeArguments[0] != typeof(string))
				{
					Log.Warning("Skipping unsupported field type {FieldTypeName} as we don't support arrays of non-string dictionaries here", type.FullName);
					return new(null);
				}

				var subType = type.GenericTypeArguments[1];
				var subTypeUnreal = GetStreamDataUnrealType(subType, streamChannel);
				return new(UnrealSourceGenerator.UNREAL_MAP + $"<{UnrealSourceGenerator.UNREAL_STRING}, {subTypeUnreal}>");
			}

			return new($"U{type.Name}{streamChannel}StreamData*");
		}

		return new(primitiveType);
	}
}
