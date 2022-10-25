using System.Text.RegularExpressions;

namespace cli.Unreal;

public struct UnrealEnumDeclaration
{
	public string UnrealTypeName;
	public string NamespacedTypeName;

	public List<string> EnumValues;

	public void BakeIntoProcessMap(Dictionary<string, string> helperDict)
	{
		var enumValues = string.Join(",\n\t", EnumValues.Select(v =>
		{
			var serializationName = v;
			var enumValue = $@"{v.Capitalize()} UMETA(DisplayName=""{v.SpaceOutOnUpperCase()}"", SerializationName=""{serializationName}"")";
			
			return enumValue;
		}));

		helperDict.Add(nameof(UnrealTypeName), UnrealTypeName);
		helperDict.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		helperDict.Add(nameof(EnumValues), enumValues);
	}

	public const string U_ENUM_HEADER = $@"
#pragma once

#include ""CoreMinimal.h""

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

UENUM(BlueprintType, Category=""Beam|Enums"")
enum class ₢{nameof(UnrealTypeName)}₢ : uint8
{{
	₢{nameof(EnumValues)}₢		
}};

UCLASS(BlueprintType, Category=""Beam|Enums"")
class BEAMABLECORE_API U₢{nameof(NamespacedTypeName)}₢Library : public UBlueprintFunctionLibrary
{{
	GENERATED_BODY()
public:		
	
	UFUNCTION(BlueprintPure, meta = (DisplayName = ""Beam - ₢{nameof(NamespacedTypeName)}₢ To Serialization Name"", CompactNodeTitle = ""->""), Category=""Beam|Enums"")
	static FString ₢{nameof(NamespacedTypeName)}₢ToSerializationName(₢{nameof(UnrealTypeName)}₢ Value)
	{{
		const UEnum* Enum = StaticEnum<₢{nameof(UnrealTypeName)}₢>();
		const int32 NameIndex = Enum->GetIndexByValue(static_cast<int64>(Value));
		const FString SerializationName = Enum->GetMetaData(TEXT(""SerializationName""), NameIndex);		
		return SerializationName;
		
	}}

	UFUNCTION(BlueprintPure, meta = (DisplayName = ""Beam - Serialization Name To ₢{nameof(NamespacedTypeName)}₢"", CompactNodeTitle = ""->""), Category=""Beam|Enums"")
	static ₢{nameof(UnrealTypeName)}₢ SerializationNameTo₢{nameof(NamespacedTypeName)}₢(FString Value)
	{{
		const UEnum* Enum = StaticEnum<₢{nameof(UnrealTypeName)}₢>();
		for (int32 NameIndex = 0; NameIndex < Enum->NumEnums() - 1; ++NameIndex)
		{{
			const FString& SerializationName = Enum->GetMetaData(TEXT(""SerializationName""), NameIndex);
			if(Value == SerializationName)
				return static_cast<₢{nameof(UnrealTypeName)}₢>(Enum->GetValueByIndex(NameIndex));
		}}
		
		ensureAlways(false); //  This should be impossible!
		return ₢{nameof(UnrealTypeName)}₢();
	}}	
}};

";
}

public struct UnrealWrapperContainerDeclaration
{
	public string UnrealTypeName;
	public string NamespacedTypeName;
	public string UnrealTypeIncludeStatement;

	public string ValueUnrealTypeName;
	public string ValueNamespacedTypeName;
	public string ValueUnrealTypeIncludeStatement;

	public void BakeIntoProcessMap(Dictionary<string, string> helperDict)
	{
		helperDict.Add(nameof(UnrealTypeName), UnrealTypeName);
		helperDict.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		helperDict.Add(nameof(UnrealTypeIncludeStatement), UnrealTypeIncludeStatement);
		helperDict.Add(nameof(ValueUnrealTypeName), ValueUnrealTypeName);
		helperDict.Add(nameof(ValueNamespacedTypeName), ValueNamespacedTypeName);
		helperDict.Add(nameof(ValueUnrealTypeIncludeStatement), ValueUnrealTypeIncludeStatement);
	}


	public const string ARRAY_WRAPPER_HEADER_DECL = $@"
#pragma once

#include ""CoreMinimal.h""
#include ""Serialization/BeamArray.h""
₢{nameof(ValueUnrealTypeIncludeStatement)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""
		
USTRUCT(BlueprintType, Category=""Beam|Wrappers|Arrays"")
struct BEAMABLECORE_API ₢{nameof(UnrealTypeName)}₢ : public FBeamArray
{{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=""Beam"")
	TArray<₢{nameof(ValueUnrealTypeName)}₢> Values;

	₢{nameof(UnrealTypeName)}₢();

	explicit ₢{nameof(UnrealTypeName)}₢(const TArray<₢{nameof(ValueUnrealTypeName)}₢>& Values);

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override;

	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override;

	virtual void BeamDeserializeElements(const TArray<TSharedPtr<FJsonValue>>& Elements) override;
}};
";

	public const string ARRAY_WRAPPER_CPP_DECL = $@"
₢{nameof(UnrealTypeIncludeStatement)}₢
#include ""Serialization/BeamJsonUtils.h""

₢{nameof(UnrealTypeName)}₢::₢{nameof(UnrealTypeName)}₢() = default;

₢{nameof(UnrealTypeName)}₢::₢{nameof(UnrealTypeName)}₢(const TArray<₢{nameof(ValueUnrealTypeName)}₢>& Values): Values(Values)
{{
}}

void ₢{nameof(UnrealTypeName)}₢::BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const
{{
	UBeamJsonUtils::SerializeArray<₢{nameof(ValueUnrealTypeName)}₢>(Values, Serializer);
}}

void ₢{nameof(UnrealTypeName)}₢::BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const
{{
	UBeamJsonUtils::SerializeArray<₢{nameof(ValueUnrealTypeName)}₢>(Values, Serializer);
}}

void ₢{nameof(UnrealTypeName)}₢::BeamDeserializeElements(const TArray<TSharedPtr<FJsonValue>>& Elements)
{{
	UBeamJsonUtils::DeserializeArray<₢{nameof(ValueUnrealTypeName)}₢>(Elements, Values);
}}
";

	public const string MAP_WRAPPER_HEADER_DECL = $@"
#pragma once

#include ""CoreMinimal.h""
#include ""Serialization/BeamMap.h""
₢{nameof(ValueUnrealTypeIncludeStatement)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

USTRUCT(BlueprintType, Category=""Beam|Wrappers|Maps"")
struct ₢{nameof(UnrealTypeName)}₢ : public FBeamMap
{{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=""Beam"")
	TMap<FString, ₢{nameof(ValueUnrealTypeName)}₢> Values;

	₢{nameof(UnrealTypeName)}₢();

	₢{nameof(UnrealTypeName)}₢(const TMap<FString, ₢{nameof(ValueUnrealTypeName)}₢>& Val);

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override;

	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override;

	virtual void BeamDeserializeElements(const TSharedPtr<FJsonObject>& Elements) override;
}};";

	public const string MAP_WRAPPER_CPP_DECL = $@"
₢{nameof(UnrealTypeIncludeStatement)}₢
#include ""Serialization/BeamJsonUtils.h""

₢{nameof(UnrealTypeName)}₢::₢{nameof(UnrealTypeName)}₢() = default;

₢{nameof(UnrealTypeName)}₢::₢{nameof(UnrealTypeName)}₢(const TMap<FString, ₢{nameof(ValueUnrealTypeName)}₢>& Val): Values(Val)
{{}}

void FMapOfString::BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const
{{
	UBeamJsonUtils::SerializeMap<₢{nameof(ValueUnrealTypeName)}₢>(Values, Serializer);
}}

void FMapOfString::BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const
{{
	UBeamJsonUtils::SerializeMap<₢{nameof(ValueUnrealTypeName)}₢>(Values, Serializer);
}}

void FMapOfString::BeamDeserializeElements(const TSharedPtr<FJsonObject>& Elements)
{{
	UBeamJsonUtils::DeserializeMap<₢{nameof(ValueUnrealTypeName)}₢>(Elements, Values);
}}";
}
