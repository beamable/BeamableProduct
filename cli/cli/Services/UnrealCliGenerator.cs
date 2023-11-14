using cli.Unreal;
using Newtonsoft.Json;
using System.CommandLine;
using System.CommandLine.Help;
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

	public void IntoProcessDict(Dictionary<string, string> dict)
	{
		_streamFieldDeclarations = string.Join("\n\n\t", Streams.Select(s =>
		{
			dict.Clear();
			s.IntoProcessDict(dict);
			return UnrealCliStreamDeclaration.STREAM_COMMAND_FIELDS.ProcessReplacement(dict);
		}));

		var streams = string.Join("\n", Streams.Select(s =>
		{
			dict.Clear();
			s.IntoProcessDict(dict);
			return UnrealCliStreamDeclaration.STREAM_STRUCTS_DECL.ProcessReplacement(dict);
		}));

		_parseStreamDataImpl = string.Join("\n", Streams.Select(s =>
		{
			dict.Clear();
			s.IntoProcessDict(dict);
			return UnrealCliStreamDeclaration.STREAM_PARSE_IMPL.ProcessReplacement(dict);
		}));

		dict.Clear();
		dict.Add(nameof(CommandName), CommandName);
		dict.Add(nameof(CommandKeywords), CommandKeywords);
		dict.Add(nameof(HelpString), HelpString);
		dict.Add(nameof(Streams), streams);
		dict.Add(nameof(_streamFieldDeclarations), _streamFieldDeclarations);
		dict.Add(nameof(_parseStreamDataImpl), _parseStreamDataImpl);
	}

	public const string HEADER_COMMAND_TEMPLATE = $@"#pragma once

#include ""Subsystems/CLI/BeamCliCommand.h""
#include ""₢{nameof(CommandName)}₢Command.generated.h""

class FMonitoredProcess;

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
	virtual TSharedPtr<FMonitoredProcess> RunImpl(const TArray<FString>& CommandParams, const FBeamOperationHandle& Op = {{}}) override;
}};
";

	public const string CPP_COMMAND_TEMPLATE = $@"#include ""₢{nameof(CommandName)}₢Command.h""

#include ""BeamLogging.h""
#include ""Misc/MonitoredProcess.h""
#include ""JsonObjectConverter.h""
#include ""Serialization/JsonSerializerMacros.h""
		
TSharedPtr<FMonitoredProcess> U₢{nameof(CommandName)}₢Command::RunImpl(const TArray<FString>& CommandParams, const FBeamOperationHandle& Op)
{{
	FString Params = (""₢{nameof(CommandKeywords)}₢ --reporter-use-fatal"");
	for (const auto& CommandParam : CommandParams)
		Params.Appendf(TEXT("" %s""), *CommandParam);
	Params = PrepareParams(Params);
	UE_LOG(LogBeamCli, Verbose, TEXT(""₢{nameof(CommandName)}₢ Command - Invocation: %s %s""), *PathToCli, *Params)

	const auto CliProcess = MakeShared<FMonitoredProcess>(PathToCli, Params, FPaths::ProjectDir(), true, true);
	CliProcess->OnOutput().BindLambda([this, Op](const FString& Out)
	{{
		UE_LOG(LogBeamCli, Verbose, TEXT(""₢{nameof(CommandName)}₢ Command - Std Out: %s""), *Out);
		FString OutCopy = Out;
		FString MessageJson;
		while (ConsumeMessageFromOutput(OutCopy, MessageJson))
		{{
			auto Bag = FJsonDataBag();
			Bag.FromJson(MessageJson);
			const auto StreamType = Bag.GetString(""type"");
			const auto Timestamp = static_cast<int64>(Bag.GetField(""ts"")->AsNumber());
			const auto DataJson = Bag.JsonObject->GetObjectField(""data"").ToSharedRef();

			₢{nameof(_parseStreamDataImpl)}₢
		}}
	}});
	CliProcess->OnCompleted().BindLambda([this, Op](int ResultCode)
	{{
		if (OnCompleted)
		{{
			AsyncTask(ENamedThreads::GameThread, [this, ResultCode, Op]
			{{
				OnCompleted(ResultCode, Op);
			}});
		}}
	}});
	return CliProcess;
}}
";
}

public struct UnrealCliStreamDeclaration
{
	public string CommandName;
	public string RawStreamName;
	public string StreamName;

	public string StreamDataName;
	public List<UnrealPropertyDeclaration> StreamDataProperties;

	public void IntoProcessDict(Dictionary<string, string> dictionary)
	{
		var properties = string.Join("\n\t", StreamDataProperties.Select(p => $"UPROPERTY()\n\t{p.PropertyUnrealType} {p.PropertyName} = {{}};"));

		dictionary.Clear();
		dictionary.Add(nameof(CommandName), CommandName);
		dictionary.Add(nameof(RawStreamName), RawStreamName);
		dictionary.Add(nameof(StreamName), StreamName);
		dictionary.Add(nameof(StreamDataName), StreamDataName);
		dictionary.Add(nameof(StreamDataProperties), properties);
	}

	public const string STREAM_COMMAND_FIELDS = $@"TArray<F₢{nameof(StreamDataName)}₢> ₢{nameof(StreamName)}₢Stream;
	TArray<int64> ₢{nameof(StreamName)}₢Timestamps;
	TFunction<void (const TArray<F₢{nameof(StreamDataName)}₢>& StreamData, const TArray<int64>& Timestamps, const FBeamOperationHandle& Op)> On₢{nameof(StreamName)}₢StreamOutput;";

	public const string STREAM_STRUCTS_DECL = $@"
USTRUCT()
struct F₢{nameof(StreamDataName)}₢
{{
	GENERATED_BODY()

	inline static FString StreamTypeName = FString(TEXT(""₢{nameof(RawStreamName)}₢""));

	₢{nameof(StreamDataProperties)}₢	
}};
";

	public const string STREAM_PARSE_IMPL = $@"
			if(StreamType.Equals(F₢{nameof(StreamDataName)}₢::StreamTypeName))
			{{
				F₢{nameof(StreamDataName)}₢ Data;
				FJsonObjectConverter::JsonObjectToUStruct(DataJson, F₢{nameof(StreamDataName)}₢::StaticStruct(), &Data);

				₢{nameof(StreamName)}₢Stream.Add(Data);
				₢{nameof(StreamName)}₢Timestamps.Add(Timestamp);

				UE_LOG(LogBeamCli, Verbose, TEXT(""₢{nameof(CommandName)}₢ Command - Message Received: %s""), *MessageJson);
				AsyncTask(ENamedThreads::GameThread, [this, Op]
				{{
					On₢{nameof(StreamName)}₢StreamOutput(₢{nameof(StreamName)}₢Stream, ₢{nameof(StreamName)}₢Timestamps, Op);
				}});				
			}}
";
}

public class UnrealCliGenerator : ICliGenerator
{
	public List<GeneratedFileDescriptor> Generate(CliGeneratorContext context)
	{
		// the following commands are more complicated and either use nullables or enums
		var invalidCommands = new string[] { "beam", "beam services register", "beam services modify", "beam services enable", "beam oapi generate", };

		var files = new List<GeneratedFileDescriptor>();
		foreach (var command in context.Commands)
		{
			if (!command.hasValidOutput && command.executionPath != "beam") continue;
			if (invalidCommands.Contains(command.executionPath)) continue;

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

					var streamDataProperties = rs.runtimeType.GetFields()
						.Select(fieldInfo => new UnrealPropertyDeclaration() { PropertyName = fieldInfo.Name, PropertyUnrealType = UnrealSourceGenerator.GetUnrealTypeFromReflectionType(fieldInfo.FieldType) })
						.Where(p => p.PropertyUnrealType != null)
						.ToList();

					return new UnrealCliStreamDeclaration()
					{
						CommandName = commandName,
						RawStreamName = rs.channel,
						StreamName = streamChannel,
						StreamDataName = $"{commandName}{streamChannel}StreamData",
						StreamDataProperties = streamDataProperties
					};
				}).ToList()
			};

			var dict = new Dictionary<string, string>();

			cliCommandDeclaration.IntoProcessDict(dict);
			files.Add(new GeneratedFileDescriptor() { FileName = $"BeamableCoreRuntimeEditor/Public/Subsystems/CLI/Autogen/{commandName}Command.h", Content = UnrealCliCommandDeclaration.HEADER_COMMAND_TEMPLATE.ProcessReplacement(dict) });
			files.Add(new GeneratedFileDescriptor() { FileName = $"BeamableCoreRuntimeEditor/Public/Subsystems/CLI/Autogen/{commandName}Command.cpp", Content = UnrealCliCommandDeclaration.CPP_COMMAND_TEMPLATE.ProcessReplacement(dict) });
		}


		return files;
	}

	public static string GenerateHelpString(BeamCommandDescriptor descriptor)
	{
		var textWriter = new StringWriter();
		var helpContext = new HelpContext(new HelpBuilder(LocalizationResources.Instance), descriptor.command, textWriter);
		helpContext.HelpBuilder.Write(helpContext);
		return textWriter.GetStringBuilder().ToString();
	}
}
