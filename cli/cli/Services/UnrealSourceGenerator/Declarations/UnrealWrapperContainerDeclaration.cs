using System.Text.RegularExpressions;

namespace cli.Unreal;

public struct UnrealWrapperContainerDeclaration
{
	public UnrealSourceGenerator.UnrealType UnrealTypeName;
	public UnrealSourceGenerator.NamespacedType NamespacedTypeName;
	public string UnrealTypeIncludeStatement;

	public UnrealSourceGenerator.UnrealType ValueUnrealTypeName;
	public UnrealSourceGenerator.NamespacedType ValueNamespacedTypeName;
	public string ValueUnrealTypeIncludeStatement;

	public void BakeIntoProcessMap(Dictionary<string, string> helperDict)
	{
		helperDict.Add(nameof(UnrealSourceGenerator.exportMacro), UnrealSourceGenerator.exportMacro);
		helperDict.Add(nameof(UnrealTypeName), UnrealTypeName);
		helperDict.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		helperDict.Add(nameof(UnrealTypeIncludeStatement), UnrealTypeIncludeStatement);
		helperDict.Add(nameof(ValueUnrealTypeName), ValueUnrealTypeName);
		helperDict.Add(nameof(ValueNamespacedTypeName), ValueNamespacedTypeName);
		helperDict.Add(nameof(ValueUnrealTypeIncludeStatement), ValueUnrealTypeIncludeStatement);
	}


	public const string ARRAY_WRAPPER_HEADER_DECL = $@"#pragma once

#include ""CoreMinimal.h""
#include ""Serialization/BeamArray.h""
₢{nameof(ValueUnrealTypeIncludeStatement)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""
		
USTRUCT(BlueprintType, Category=""Beam|Wrappers|Arrays"")
struct ₢{nameof(UnrealSourceGenerator.exportMacro)}₢ ₢{nameof(UnrealTypeName)}₢ : public FBeamArray
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

	public const string MAP_WRAPPER_HEADER_DECL = $@"#pragma once

#include ""CoreMinimal.h""
#include ""Serialization/BeamMap.h""
₢{nameof(ValueUnrealTypeIncludeStatement)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

USTRUCT(BlueprintType, Category=""Beam|Wrappers|Maps"")
struct ₢{nameof(UnrealSourceGenerator.exportMacro)}₢ ₢{nameof(UnrealTypeName)}₢ : public FBeamMap
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

void ₢{nameof(UnrealTypeName)}₢::BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const
{{
	UBeamJsonUtils::SerializeMap<₢{nameof(ValueUnrealTypeName)}₢>(Values, Serializer);
}}

void ₢{nameof(UnrealTypeName)}₢::BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const
{{
	UBeamJsonUtils::SerializeMap<₢{nameof(ValueUnrealTypeName)}₢>(Values, Serializer);
}}

void ₢{nameof(UnrealTypeName)}₢::BeamDeserializeElements(const TSharedPtr<FJsonObject>& Elements)
{{
	UBeamJsonUtils::DeserializeMap<₢{nameof(ValueUnrealTypeName)}₢>(Elements, Values);
}}";
}
