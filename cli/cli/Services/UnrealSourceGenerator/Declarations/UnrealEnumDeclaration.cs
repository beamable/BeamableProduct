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
			var enumValue = $@"BEAM_{v} UMETA(DisplayName=""{v.SpaceOutOnUpperCase()}"")";

			return enumValue;
		}));

		helperDict.Add(nameof(UnrealSourceGenerator.exportMacro), UnrealSourceGenerator.exportMacro);
		helperDict.Add(nameof(UnrealTypeName), UnrealTypeName);
		helperDict.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		helperDict.Add(nameof(EnumValues), enumValues);
	}

	public const string U_ENUM_HEADER = $@"#pragma once

#include ""CoreMinimal.h""

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

UENUM(BlueprintType, Category=""Beam|Enums"")
enum class ₢{nameof(UnrealTypeName)}₢ : uint8
{{
	₢{nameof(EnumValues)}₢		
}};

UCLASS(BlueprintType, Category=""Beam|Enums"")
class ₢{nameof(UnrealSourceGenerator.exportMacro)}₢ U₢{nameof(NamespacedTypeName)}₢Library : public UBlueprintFunctionLibrary
{{
	GENERATED_BODY()
public:		
	
	UFUNCTION(BlueprintPure, meta = (DisplayName = ""Beam - ₢{nameof(NamespacedTypeName)}₢ To Serialization Name"", CompactNodeTitle = ""->""), Category=""Beam|Enums"")
	static FString ₢{nameof(NamespacedTypeName)}₢ToSerializationName(₢{nameof(UnrealTypeName)}₢ Value)
	{{
		const UEnum* Enum = StaticEnum<₢{nameof(UnrealTypeName)}₢>();
		const int32 NameIndex = Enum->GetIndexByValue(static_cast<int64>(Value));
		const FString SerializationName = Enum->GetNameStringByIndex(NameIndex);

		// We chop off the first five ""BEAM_"" characters. 		
		return SerializationName.RightChop(5);
		
	}}

	UFUNCTION(BlueprintPure, meta = (DisplayName = ""Beam - Serialization Name To ₢{nameof(NamespacedTypeName)}₢"", CompactNodeTitle = ""->""), Category=""Beam|Enums"")
	static ₢{nameof(UnrealTypeName)}₢ SerializationNameTo₢{nameof(NamespacedTypeName)}₢(FString Value)
	{{
		const UEnum* Enum = StaticEnum<₢{nameof(UnrealTypeName)}₢>();
		for (int32 NameIndex = 0; NameIndex < Enum->NumEnums() - 1; ++NameIndex)
		{{
			// We chop off the first five ""BEAM_"" characters.
			const FString& SerializationName = Enum->GetNameStringByIndex(NameIndex).RightChop(5);
			if(Value == SerializationName)
				return static_cast<₢{nameof(UnrealTypeName)}₢>(Enum->GetValueByIndex(NameIndex));
		}}
		
		ensureAlways(false); //  This should be impossible!
		return ₢{nameof(UnrealTypeName)}₢();
	}}	
}};

";
}
