using System.Text;
using static cli.Unreal.UnrealSourceGenerator;

namespace cli.Unreal;

public enum ResponseBodyType
{
	None,
	Json,
	Csv,
	PrimitiveWrapper
}

public struct TypeRequestBody : IEquatable<string>, IComparable<string>
{
	public UnrealType UnrealType;
	public ResponseBodyType Type;

	public bool Equals(TypeRequestBody other)
	{
		return UnrealType == other.UnrealType;
	}

	public bool Equals(string other)
	{
		return UnrealType == other;
	}

	public int CompareTo(string other)
	{
		return string.Compare(UnrealType, other, StringComparison.Ordinal);
	}

	public override int GetHashCode()
	{
		return (UnrealType.AsStr != null ? UnrealType.GetHashCode() : 0);
	}
}

public struct UnrealCsvRowTypeDeclaration
{
	public UnrealType RowUnrealType;
	public NamespacedType RowNamespacedType;

	public int KeyDeclarationIdx;

	public List<UnrealPropertyDeclaration> PropertyDeclarations;
	public string IncludesForProperties;

	private string _keyFieldPropertyName;
	private string _headerFieldsContent;


	public void IntoProcessMap(Dictionary<string, string> processDictionary)
	{
		var @this = this;
		var propertyDeclarations = string.Join("\n\t", PropertyDeclarations.Select(ud =>
		{
			ud.IntoProcessMap(processDictionary);
			var decl = ud.GetDeclarationTemplate().ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return decl;
		}));

		_keyFieldPropertyName = PropertyDeclarations[KeyDeclarationIdx].PropertyName;

		_headerFieldsContent = string.Join(",\n\t", PropertyDeclarations.Select((ud, i) =>
		{
			if (i == @this.KeyDeclarationIdx)
				return "KeyField";

			return $"GET_MEMBER_NAME_STRING_CHECKED({@this.RowUnrealType}, {ud.PropertyName})";
		}));

		processDictionary.Add(nameof(exportMacro), exportMacro);
		processDictionary.Add(nameof(RowNamespacedType), RowNamespacedType);
		processDictionary.Add(nameof(RowUnrealType), RowUnrealType);
		processDictionary.Add(nameof(PropertyDeclarations), propertyDeclarations);
		processDictionary.Add(nameof(_keyFieldPropertyName), _keyFieldPropertyName);
		processDictionary.Add(nameof(_headerFieldsContent), _headerFieldsContent);
		processDictionary.Add(nameof(IncludesForProperties), IncludesForProperties);
	}


	public const string CSV_ROW_TYPE_HEADER =
		$@"#pragma once

#include ""CoreMinimal.h""
#include ""Engine/DataTable.h""
₢{nameof(IncludesForProperties)}₢

#include ""₢{nameof(RowNamespacedType)}₢.generated.h""

USTRUCT(BlueprintType)
struct ₢{nameof(exportMacro)}₢ ₢{nameof(RowUnrealType)}₢ : public FTableRowBase
{{
	GENERATED_BODY()

	const static FString KeyField;
	const static TArray<FString> HeaderFields;

	₢{nameof(PropertyDeclarations)}₢		
}};";

	public const string CSV_ROW_TYPE_CPP =
		@$"
#include ""AutoGen/Rows/₢{nameof(RowNamespacedType)}₢.h""

const FString ₢{nameof(RowUnrealType)}₢::KeyField = GET_MEMBER_NAME_STRING_CHECKED(₢{nameof(RowUnrealType)}₢, ₢{nameof(_keyFieldPropertyName)}₢);
const TArray<FString> ₢{nameof(RowUnrealType)}₢::HeaderFields = {{
	₢{nameof(_headerFieldsContent)}₢	
}};

";
}

public struct UnrealCsvSerializableTypeDeclaration
{
	public UnrealType UnrealTypeName;
	public NamespacedType NamespacedTypeName;

	public UnrealType RowUnrealType;
	public NamespacedType RowNamespacedTypeName;
	public bool NeedsKeyGeneration;
	public bool NeedsHeaderRow;

	private string _defineResponseBodyInterface;


	public void IntoProcessMap(Dictionary<string, string> processDictionary)
	{
		// Adds the implementation
		_defineResponseBodyInterface = @$"
void U{NamespacedTypeName}::DeserializeRequestResponse(UObject* RequestData, FString ResponseContent)
{{
	CsvData = NewObject<UDataTable>(RequestData);

	{(NeedsKeyGeneration ? $"// Generate this only if the CSV has no unique value\n\tUBeamCsvUtils::AddAutoGenKeyField(ResponseContent);" : @"")}

	{(NeedsHeaderRow ? $"// Generate this only if the CSV will not contain the header row\n\tUBeamCsvUtils::AddHeaderRow(ResponseContent, {RowUnrealType}::HeaderFields);" : @"")}
	
	// Generate this always.
	UBeamCsvUtils::ParseIntoDataTable(CsvData,
	                                  {RowUnrealType}::StaticStruct(),
	                                  {RowUnrealType}::KeyField,
	                                  ResponseContent);

	UBeamCsvUtils::StoreNameAsColumn<{RowUnrealType}>(CsvData, {RowUnrealType}::KeyField);
}}";
		processDictionary.Add(nameof(exportMacro), exportMacro);
		processDictionary.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		processDictionary.Add(nameof(RowUnrealType), RowUnrealType);
		processDictionary.Add(nameof(RowNamespacedTypeName), RowNamespacedTypeName);
		processDictionary.Add(nameof(_defineResponseBodyInterface), _defineResponseBodyInterface);
	}

	public const string CSV_SERIALIZABLE_TYPE_HEADER =
		$@"#pragma once

#include ""CoreMinimal.h""
#include ""BeamBackend/BeamBaseResponseBodyInterface.h""
#include ""Serialization/BeamCsvUtils.h""

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

UCLASS(BlueprintType, Category=""Beam"")
class ₢{nameof(exportMacro)}₢ U₢{nameof(NamespacedTypeName)}₢ : public UObject, public IBeamBaseResponseBodyInterface
{{
	GENERATED_BODY()

public:
	UPROPERTY(BlueprintReadOnly)
	UDataTable* CsvData;

	virtual void DeserializeRequestResponse(UObject* RequestData, FString ResponseContent) override;
}};";

	public const string CSV_SERIALIZABLE_TYPE_CPP =
		@$"
#include ""AutoGen/₢{nameof(NamespacedTypeName)}₢.h""
#include ""AutoGen/Rows/₢{nameof(RowNamespacedTypeName)}₢.h""

₢{nameof(_defineResponseBodyInterface)}₢

";
}

public struct PolymorphicWrappedData
{
	public string UnrealType;
	public string ExpectedTypeValue;
}

public struct UnrealJsonSerializableTypeDeclaration
{
	public UnrealType UnrealTypeName;
	public NamespacedType NamespacedTypeName;

	public List<string> PropertyIncludes;
	public List<UnrealPropertyDeclaration> UPropertyDeclarations;

	public string JsonUtilsInclude;
	public string DefaultValueHelpersInclude;
	public ResponseBodyType IsResponseBodyType;

	public bool IsSelfReferential;

	public List<PolymorphicWrappedData> PolymorphicWrappedTypes;
	public bool IsPolymorphicWrapper => PolymorphicWrappedTypes?.Count > 0;


	private string _responseBodyIncludes;
	private string _inheritResponseBodyInterface;
	private string _declareResponseBodyInterface;
	private string _defineResponseBodyInterface;

	private List<UnrealPropertyDeclaration> _uPropertySerialize;
	private List<UnrealPropertyDeclaration> _uPropertyDeserialize;

	private string _makeParams;
	private string _makeOptionalParamNames;
	private string _makeAssignments;
	private string _breakParams;
	private string _breakAssignments;

	private string _declarePolyWrapperGetType;
	private string _definePolyWrapperGetType;


	public void IntoProcessMap(Dictionary<string, string> processDictionary)
	{
		UPropertyDeclarations.Sort((d1, d2) =>
		{
			var isD1Array = d1.PropertyUnrealType.IsUnrealArray() || d1.PropertyUnrealType.IsWrapperArray() ? 1 : 0;
			var isD2Array = d2.PropertyUnrealType.IsUnrealArray() || d2.PropertyUnrealType.IsWrapperArray() ? 1 : 0;

			var isD1Map = d1.PropertyUnrealType.IsUnrealMap() || d1.PropertyUnrealType.IsWrapperMap() ? 1 : 0;
			var isD2Map = d2.PropertyUnrealType.IsUnrealMap() || d2.PropertyUnrealType.IsWrapperMap() ? 1 : 0;

			var isD1Optional = d1.PropertyUnrealType.IsOptional() ? 1 : 0;
			var isD2Optional = d2.PropertyUnrealType.IsOptional() ? 1 : 0;

			var isD1OptionalArray = d1.PropertyUnrealType.IsOptionalArray() ? 1 : 0;
			var isD2OptionalArray = d2.PropertyUnrealType.IsOptionalArray() ? 1 : 0;

			var isD1OptionalMap = d1.PropertyUnrealType.IsOptionalMap() ? 1 : 0;
			var isD2OptionalMap = d2.PropertyUnrealType.IsOptionalMap() ? 1 : 0;

			var propNameD1 = d1.PropertyName.StartsWith("b") ? d1.PropertyName[1..] : d1.PropertyName;
			var propNameD2 = d2.PropertyName.StartsWith("b") ? d2.PropertyName[1..] : d1.PropertyName;

			var baseValueCompD1 = String.Compare(propNameD1, propNameD2, StringComparison.Ordinal);
			var baseValueCompD2 = String.Compare(propNameD2, propNameD1, StringComparison.Ordinal);

			var d1ArrayModifier = isD1Array * 1_000;
			var d1MapModifier = isD1Map * 100_000;
			var d1OptionalModifier = isD1Optional * 100_000_000 + (isD1OptionalArray * 1_000_000) + (isD1OptionalMap * 10_000_000);

			var d2ArrayModifier = isD2Array * 1_000;
			var d2MapModifier = isD2Map * 100_000;
			var d2OptionalModifier = isD2Optional * 100_000_000 + (isD2OptionalArray * 1_000_000) + (isD2OptionalMap * 10_000_000);

			var compValD1 = baseValueCompD1 + d1ArrayModifier + d1MapModifier + d1OptionalModifier;
			var compValD2 = baseValueCompD2 + d2ArrayModifier + d2MapModifier + d2OptionalModifier;

			return compValD1.CompareTo(compValD2);
		});
		_uPropertySerialize = _uPropertyDeserialize = UPropertyDeclarations;

		var propertyDeclarations = string.Join("\n\t", UPropertyDeclarations.Select(ud =>
		{
			ud.IntoProcessMap(processDictionary);
			var decl = ud.GetDeclarationTemplate().ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return decl;
		}));

		string propertySerialization, propertyDeserialization;
		if (!IsPolymorphicWrapper)
		{
			propertySerialization = string.Join("\n\t", _uPropertySerialize.Select(ud =>
			{
				ud.IntoProcessMap(processDictionary);

				var decl = UnrealPropertyDeclaration.GetSerializeTemplateForUnrealType(ud.PropertyUnrealType).ProcessReplacement(processDictionary);
				processDictionary.Clear();
				return decl;
			}));
			propertyDeserialization = string.Join("\n\t", _uPropertyDeserialize.Select(ud =>
			{
				ud.IntoProcessMap(processDictionary);

				var decl = UnrealPropertyDeclaration.GetDeserializeTemplateForUnrealType(ud.PropertyUnrealType).ProcessReplacement(processDictionary);
				processDictionary.Clear();
				return decl;
			}));
			_declarePolyWrapperGetType = "";
			_definePolyWrapperGetType = "";
		}
		else
		{
			propertySerialization = "const auto Type = GetCurrentType();\n\t";
			propertyDeserialization = "const auto Type = Bag->GetStringField(\"type\");\n\t";

			_declarePolyWrapperGetType = "FString GetCurrentType() const;";

			/*
			 * Used to generate this:
			 checkf((ContentReference && !TextReference && !BinaryReference) ||
				    (!ContentReference && TextReference && !BinaryReference) ||
				    (!ContentReference && !TextReference && BinaryReference), TEXT(""You should always only have one of these set. Set the others as nullptr.""))
			 */
			var check = "checkf(";
			var checkAppend = ") || \n\t\t";
			var checkEnd = @"), TEXT(""You should always only have one of these set. Set the others as nullptr.""))";

			/*
			 * Used to generate this:
			    if (ContentReference) return TEXT(""content"");
			    if (TextReference) return TEXT(""text"");
			    if (BinaryReference) return TEXT(""binary"");
			 */
			var body = "\t";

			for (var i = 0; i < PolymorphicWrappedTypes.Count; i++)
			{
				var propData = UPropertyDeclarations[i];
				var polyData = PolymorphicWrappedTypes[i];

				propertySerialization += $"if (Type.Equals(TEXT(\"{polyData.ExpectedTypeValue}\")))\n\t\t";
				propertySerialization += $"UBeamJsonUtils::SerializeUObject({propData.PropertyName}, Serializer);\n\t";

				propertyDeserialization += $"if (Type.Equals(TEXT(\"{polyData.ExpectedTypeValue}\")))\n\t\t";
				propertyDeserialization += $"UBeamJsonUtils::DeserializeUObject(TEXT(\"\"), Bag, {propData.PropertyName}, OuterOwner);\n\t";

				var checkPart = $"({propData.PropertyName}";
				for (var i2 = 0; i2 < PolymorphicWrappedTypes.Count; i2++)
				{
					if (i == i2) continue;
					var propData2 = UPropertyDeclarations[i2];
					checkPart += $" && !{propData2.PropertyName}";
				}

				check += checkPart + (i == PolymorphicWrappedTypes.Count - 1 ? checkEnd : checkAppend);
				body += $"if ({propData.PropertyName}) return TEXT(\"{polyData.ExpectedTypeValue}\");\n\t";
			}

			_definePolyWrapperGetType = $"FString U{NamespacedTypeName}::GetCurrentType() const\n";
			_definePolyWrapperGetType += "{\n\t";
			_definePolyWrapperGetType += check + "\n\n";
			_definePolyWrapperGetType += body + "\n";
			_definePolyWrapperGetType += "\treturn TEXT(\"\");";
			_definePolyWrapperGetType += "\n}";
		}

		var makeSb = new StringBuilder(1024);
		var makeOptionalParamNamesSb = new StringBuilder(1024);
		var makeAssignmentSb = new StringBuilder(1024);
		var breakSb = new StringBuilder(1024);
		var breakAssignmentSb = new StringBuilder(1024);
		foreach (var unrealPropertyDeclaration in UPropertyDeclarations)
		{
			// We skip properties that are not blueprint compatible when generating the Make/Break helper functions.
			if (!unrealPropertyDeclaration.IsBlueprintCompatible()) continue;

			var paramDeclaration = $"{unrealPropertyDeclaration.PropertyUnrealType} {unrealPropertyDeclaration.PropertyName}";

			makeSb.Append($"{paramDeclaration}, ");
			if (unrealPropertyDeclaration.PropertyUnrealType.IsOptional())
			{
				makeOptionalParamNamesSb.Append($"{unrealPropertyDeclaration.PropertyName}, ");
			}

			makeAssignmentSb.Append($"Serializable->{unrealPropertyDeclaration.PropertyName} = {unrealPropertyDeclaration.PropertyName};\n\t");

			breakSb.Append($", {unrealPropertyDeclaration.PropertyUnrealType}& {unrealPropertyDeclaration.PropertyName}");
			breakAssignmentSb.Append($"{unrealPropertyDeclaration.PropertyName} = Serializable->{unrealPropertyDeclaration.PropertyName};\n\t");
		}

		_makeParams = makeSb.ToString();
		_makeOptionalParamNames = makeOptionalParamNamesSb.ToString();
		_makeAssignments = makeAssignmentSb.ToString();

		_breakParams = breakSb.ToString();
		_breakAssignments = breakAssignmentSb.ToString();

		_responseBodyIncludes = IsResponseBodyType != ResponseBodyType.None ? @"#include ""BeamBackend/BeamBaseResponseBodyInterface.h""" : "";
		_inheritResponseBodyInterface = IsResponseBodyType != ResponseBodyType.None ? ", public IBeamBaseResponseBodyInterface" : "";
		_declareResponseBodyInterface = IsResponseBodyType != ResponseBodyType.None ? "virtual void DeserializeRequestResponse(UObject* RequestData, FString ResponseContent) override;" : "";
		if (IsResponseBodyType == ResponseBodyType.Json)
		{
			_defineResponseBodyInterface = @$"
void U{NamespacedTypeName}::DeserializeRequestResponse(UObject* RequestData, FString ResponseContent)
{{
	OuterOwner = RequestData;
	BeamDeserialize(ResponseContent);	
}}";
		}
		else if (IsResponseBodyType == ResponseBodyType.Csv)
		{
			throw new Exception("You should never be seeing this! Instead, schemas that represent the result of the 'text/csv' endpoints have a special code-gen path.");
		}
		else if (IsResponseBodyType == ResponseBodyType.PrimitiveWrapper)
		{
			var wrappedPrimitiveType = UPropertyDeclarations[0].PropertyUnrealType;
			var propertyName = UPropertyDeclarations[0].PropertyName;
			_defineResponseBodyInterface = @$"
void U{NamespacedTypeName}::DeserializeRequestResponse(UObject* RequestData, FString ResponseContent)
{{
	OuterOwner = RequestData;
	UBeamJsonUtils::DeserializeRawPrimitive<{wrappedPrimitiveType}>(ResponseContent, {propertyName}, OuterOwner);
}}";
		}
		else
		{
			_defineResponseBodyInterface = "";
		}


		processDictionary.Add(nameof(exportMacro), exportMacro);
		processDictionary.Add(nameof(includeStatementPrefix), includeStatementPrefix);
		processDictionary.Add(nameof(blueprintIncludeStatementPrefix), blueprintIncludeStatementPrefix);

		processDictionary.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		processDictionary.Add(nameof(UPropertyDeclarations), propertyDeclarations);
		processDictionary.Add(nameof(PropertyIncludes), string.Join("\n", PropertyIncludes.Where(s => !string.IsNullOrEmpty(s)).Distinct()));
		processDictionary.Add(nameof(JsonUtilsInclude), JsonUtilsInclude);
		processDictionary.Add(nameof(DefaultValueHelpersInclude), DefaultValueHelpersInclude);


		processDictionary.Add(nameof(_responseBodyIncludes), _responseBodyIncludes);
		processDictionary.Add(nameof(_inheritResponseBodyInterface), _inheritResponseBodyInterface);
		processDictionary.Add(nameof(_declareResponseBodyInterface), _declareResponseBodyInterface);
		processDictionary.Add(nameof(_defineResponseBodyInterface), _defineResponseBodyInterface);


		processDictionary.Add(nameof(_uPropertySerialize), propertySerialization);
		processDictionary.Add(nameof(_uPropertyDeserialize), propertyDeserialization);

		processDictionary.Add(nameof(_makeParams), _makeParams);
		processDictionary.Add(nameof(_makeOptionalParamNames), _makeOptionalParamNames);
		processDictionary.Add(nameof(_makeAssignments), _makeAssignments);
		processDictionary.Add(nameof(_breakParams), _breakParams);
		processDictionary.Add(nameof(_breakAssignments), _breakAssignments);
		processDictionary.Add(nameof(_declarePolyWrapperGetType), _declarePolyWrapperGetType);
		processDictionary.Add(nameof(_definePolyWrapperGetType), _definePolyWrapperGetType);

		processDictionary.Add(nameof(BREAK_UTILITY_DECLARATION), UPropertyDeclarations.Count != 0 ? BREAK_UTILITY_DECLARATION.ProcessReplacement(processDictionary) : "");
		processDictionary.Add(nameof(BREAK_UTILITY_DEFINITION), UPropertyDeclarations.Count != 0 ? BREAK_UTILITY_DEFINITION.ProcessReplacement(processDictionary) : "");
	}

	public const string JSON_SERIALIZABLE_TYPE_HEADER =
		$@"#pragma once

#include ""CoreMinimal.h""
₢{nameof(_responseBodyIncludes)}₢
#include ""Serialization/BeamJsonSerializable.h""
₢{nameof(PropertyIncludes)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

UCLASS(BlueprintType, Category=""Beam"")
class ₢{nameof(exportMacro)}₢ U₢{nameof(NamespacedTypeName)}₢ : public UObject, public IBeamJsonSerializableUObject₢{nameof(_inheritResponseBodyInterface)}₢
{{
	GENERATED_BODY()

public:
	₢{nameof(UPropertyDeclarations)}₢

	₢{nameof(_declareResponseBodyInterface)}₢

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override;
	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override;
	virtual void BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag) override;
	₢{nameof(_declarePolyWrapperGetType)}₢
}};";

	public const string JSON_SERIALIZABLE_TYPE_CPP =
		@$"
#include ""₢{nameof(includeStatementPrefix)}₢AutoGen/₢{nameof(NamespacedTypeName)}₢.h""
₢{nameof(JsonUtilsInclude)}₢
₢{nameof(DefaultValueHelpersInclude)}₢

₢{nameof(_defineResponseBodyInterface)}₢

void U₢{nameof(NamespacedTypeName)}₢::BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const
{{
	₢{nameof(_uPropertySerialize)}₢
}}

void U₢{nameof(NamespacedTypeName)}₢::BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const
{{
	₢{nameof(_uPropertySerialize)}₢		
}}

void U₢{nameof(NamespacedTypeName)}₢::BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag)
{{
	₢{nameof(_uPropertyDeserialize)}₢
}}

₢{nameof(_definePolyWrapperGetType)}₢

";

	public const string JSON_SERIALIZABLE_TYPES_LIBRARY_HEADER = $@"#pragma once

#include ""CoreMinimal.h""
#include ""₢{nameof(includeStatementPrefix)}₢AutoGen/₢{nameof(NamespacedTypeName)}₢.h""

#include ""₢{nameof(NamespacedTypeName)}₢Library.generated.h""


UCLASS(BlueprintType, Category=""Beam"")
class ₢{nameof(exportMacro)}₢ U₢{nameof(NamespacedTypeName)}₢Library : public UBlueprintFunctionLibrary
{{
	GENERATED_BODY()

public:

	UFUNCTION(BlueprintPure, Category=""Beam|Json"", DisplayName=""Beam - ₢{nameof(NamespacedTypeName)}₢ To JSON String"")
	static FString ₢{nameof(NamespacedTypeName)}₢ToJsonString(const U₢{nameof(NamespacedTypeName)}₢* Serializable, const bool Pretty);

	UFUNCTION(BlueprintPure, Category=""Beam|Backend"", DisplayName=""Beam - Make ₢{nameof(NamespacedTypeName)}₢"", meta=(DefaultToSelf=""Outer"", AdvancedDisplay=""₢{nameof(_makeOptionalParamNames)}₢Outer"", NativeMakeFunc))
	static U₢{nameof(NamespacedTypeName)}₢* Make(₢{nameof(_makeParams)}₢UObject* Outer);

	₢{nameof(BREAK_UTILITY_DECLARATION)}₢
}};";

	public const string JSON_SERIALIZABLE_TYPES_LIBRARY_CPP = $@"
#include ""₢{nameof(includeStatementPrefix)}₢AutoGen/₢{nameof(NamespacedTypeName)}₢Library.h""

#include ""CoreMinimal.h""


FString U₢{nameof(NamespacedTypeName)}₢Library::₢{nameof(NamespacedTypeName)}₢ToJsonString(const U₢{nameof(NamespacedTypeName)}₢* Serializable, const bool Pretty)
{{
	FString Result = FString{{}};
	if(Pretty)
	{{
		TUnrealPrettyJsonSerializer JsonSerializer = TJsonStringWriter<TPrettyJsonPrintPolicy<TCHAR>>::Create(&Result);
		Serializable->BeamSerialize(JsonSerializer);
		JsonSerializer->Close();
	}}
	else
	{{
		TUnrealJsonSerializer JsonSerializer = TJsonStringWriter<TCondensedJsonPrintPolicy<TCHAR>>::Create(&Result);
		Serializable->BeamSerialize(JsonSerializer);
		JsonSerializer->Close();			
	}}
	return Result;
}}	

U₢{nameof(NamespacedTypeName)}₢* U₢{nameof(NamespacedTypeName)}₢Library::Make(₢{nameof(_makeParams)}₢UObject* Outer)
{{
	auto Serializable = NewObject<U₢{nameof(NamespacedTypeName)}₢>(Outer);
	₢{nameof(_makeAssignments)}₢
	return Serializable;
}}

₢{nameof(BREAK_UTILITY_DEFINITION)}₢

";

	public const string BREAK_UTILITY_DECLARATION = $@"UFUNCTION(BlueprintPure, Category=""Beam|Backend"", DisplayName=""Beam - Break ₢{nameof(NamespacedTypeName)}₢"", meta=(NativeBreakFunc))
	static void Break(const U₢{nameof(NamespacedTypeName)}₢* Serializable₢{nameof(_breakParams)}₢);";

	public const string BREAK_UTILITY_DEFINITION = $@"void U₢{nameof(NamespacedTypeName)}₢Library::Break(const U₢{nameof(NamespacedTypeName)}₢* Serializable₢{nameof(_breakParams)}₢)
{{
	₢{nameof(_breakAssignments)}₢	
}}";
}
