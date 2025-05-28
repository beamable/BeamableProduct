using static cli.Unreal.UnrealSourceGenerator;

namespace cli.Unreal;

public struct UnrealEnumDeclaration
{
	public UnrealType UnrealTypeName;
	public NamespacedType NamespacedTypeName;
	public string ServiceName;

	public List<string> EnumValues;

	public void BakeIntoProcessMap(Dictionary<string, string> helperDict)
	{
		var enumValues = string.Join(",\n\t", EnumValues.Select(v =>
		{
			var enumValue = $@"BEAM_{v} UMETA(DisplayName=""{v.SpaceOutOnUpperCase()}"")";

			return enumValue;
		}));

		helperDict.Add(nameof(exportMacro), exportMacro);
		helperDict.Add(nameof(UnrealTypeName), UnrealTypeName);
		helperDict.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		helperDict.Add(nameof(EnumValues), enumValues);
		helperDict.Add(nameof(ServiceName), ServiceName);
	}

	public const string U_ENUM_HEADER = $@"#pragma once

#include ""CoreMinimal.h""

#include ""Serialization/BeamJsonUtils.h""

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

UENUM(BlueprintType, Category=""Beam|₢{nameof(ServiceName)}₢|Utils|Enums"")
enum class ₢{nameof(UnrealTypeName)}₢ : uint8
{{
	₢{nameof(EnumValues)}₢		
}};
";
}
