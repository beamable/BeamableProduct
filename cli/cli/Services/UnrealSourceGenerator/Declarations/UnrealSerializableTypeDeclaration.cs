using System.Text;

namespace cli.Unreal;

public struct UnrealSerializableTypeDeclaration
{
	public string NamespacedTypeName;
	public List<string> PropertyIncludes;
	public List<UnrealPropertyDeclaration> UPropertyDeclarations;
	public string JsonUtilsInclude;
	public string DefaultValueHelpersInclude;
	public bool IsSomeRequestsResponseBody;

	private string _includeResponseBodyInterface;
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

	public void IntoProcessMap(Dictionary<string, string> processDictionary)
	{
		UPropertyDeclarations.Sort((d1, d2) =>
		{
			var isD1Array = d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY) || d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_WRAPPER_ARRAY) ? 1 : 0;
			var isD2Array = d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY) || d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_WRAPPER_ARRAY) ? 1 : 0;

			var isD1Map = d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_MAP) || d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_WRAPPER_MAP) ? 1 : 0;
			var isD2Map = d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_MAP) || d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_WRAPPER_MAP) ? 1 : 0;

			var isD1Optional = d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL) ? 1 : 0;
			var isD2Optional = d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL) ? 1 : 0;

			var isD1OptionalArray = d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_ARRAY) ? 1 : 0;
			var isD2OptionalArray = d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_ARRAY) ? 1 : 0;

			var isD1OptionalMap = d1.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_MAP) ? 1 : 0;
			var isD2OptionalMap = d2.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_MAP) ? 1 : 0;

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
			var decl = UnrealPropertyDeclaration.U_PROPERTY_DECLARATION.ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return decl;
		}));

		var propertySerialization = string.Join("\n\t", _uPropertySerialize.Select(ud =>
		{
			ud.IntoProcessMap(processDictionary);

			var decl = UnrealPropertyDeclaration.GetSerializeTemplateForUnrealType(ud.PropertyUnrealType).ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return decl;
		}));

		var propertyDeserialization = string.Join("\n\t", _uPropertyDeserialize.Select(ud =>
		{
			ud.IntoProcessMap(processDictionary);

			var decl = UnrealPropertyDeclaration.GetDeserializeTemplateForUnrealType(ud.PropertyUnrealType).ProcessReplacement(processDictionary);
			processDictionary.Clear();
			return decl;
		}));


		var makeSb = new StringBuilder(1024);
		var makeOptionalParamNamesSb = new StringBuilder(1024);
		var makeAssignmentSb = new StringBuilder(1024);
		var breakSb = new StringBuilder(1024);
		var breakAssignmentSb = new StringBuilder(1024);
		foreach (var unrealPropertyDeclaration in UPropertyDeclarations)
		{
			var paramDeclaration = $"{unrealPropertyDeclaration.PropertyUnrealType} {unrealPropertyDeclaration.PropertyName}";

			makeSb.Append($"{paramDeclaration}, ");
			if (unrealPropertyDeclaration.PropertyUnrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL))
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

		_includeResponseBodyInterface = IsSomeRequestsResponseBody ? @"#include ""BeamBackend/BeamBaseResponseBodyInterface.h""" : "";
		_inheritResponseBodyInterface = IsSomeRequestsResponseBody ? ", public IBeamBaseResponseBodyInterface" : "";
		_declareResponseBodyInterface = IsSomeRequestsResponseBody ? "virtual void DeserializeRequestResponse(UObject* RequestData, FString ResponseContent) override;" : "";
		_defineResponseBodyInterface = IsSomeRequestsResponseBody
			? @$"
void U{NamespacedTypeName}::DeserializeRequestResponse(UObject* RequestData, FString ResponseContent)
{{
	OuterOwner = RequestData;
	BeamDeserialize(ResponseContent);	
}}"
			: "";

		processDictionary.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		processDictionary.Add(nameof(UPropertyDeclarations), propertyDeclarations);
		processDictionary.Add(nameof(PropertyIncludes), string.Join("\n", PropertyIncludes.Where(s => !string.IsNullOrEmpty(s)).Distinct()));
		processDictionary.Add(nameof(JsonUtilsInclude), JsonUtilsInclude);
		processDictionary.Add(nameof(DefaultValueHelpersInclude), DefaultValueHelpersInclude);


		processDictionary.Add(nameof(_includeResponseBodyInterface), _includeResponseBodyInterface);
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

		processDictionary.Add(nameof(BREAK_UTILITY_DECLARATION), UPropertyDeclarations.Count != 0 ? BREAK_UTILITY_DECLARATION.ProcessReplacement(processDictionary) : "");
		processDictionary.Add(nameof(BREAK_UTILITY_DEFINITION), UPropertyDeclarations.Count != 0 ? BREAK_UTILITY_DEFINITION.ProcessReplacement(processDictionary) : "");
	}

	public const string SERIALIZABLE_TYPE_HEADER =
		$@"
#pragma once

#include ""CoreMinimal.h""
₢{nameof(_includeResponseBodyInterface)}₢
#include ""Serialization/BeamJsonSerializable.h""
₢{nameof(PropertyIncludes)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

UCLASS(BlueprintType, Category=""Beam"")
class BEAMABLECORE_API U₢{nameof(NamespacedTypeName)}₢ : public UObject, public FBeamJsonSerializable₢{nameof(_inheritResponseBodyInterface)}₢
{{
	GENERATED_BODY()

public:
	₢{nameof(UPropertyDeclarations)}₢

	₢{nameof(_declareResponseBodyInterface)}₢

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override;
	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override;
	virtual void BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag) override;
}};";

	public const string SERIALIZABLE_TYPE_CPP =
		@$"
#include ""AutoGen/₢{nameof(NamespacedTypeName)}₢.h""
₢{nameof(JsonUtilsInclude)}₢
₢{nameof(DefaultValueHelpersInclude)}₢

₢{nameof(_defineResponseBodyInterface)}₢

void U₢{nameof(NamespacedTypeName)}₢ ::BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const
{{
	₢{nameof(_uPropertySerialize)}₢
}}

void U₢{nameof(NamespacedTypeName)}₢::BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const
{{
	₢{nameof(_uPropertySerialize)}₢		
}}

void U₢{nameof(NamespacedTypeName)}₢ ::BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag)
{{
	₢{nameof(_uPropertyDeserialize)}₢
}}";

	public const string SERIALIZABLE_TYPES_LIBRARY_HEADER = $@"
#pragma once

#include ""CoreMinimal.h""
#include ""AutoGen/₢{nameof(NamespacedTypeName)}₢.h""

#include ""₢{nameof(NamespacedTypeName)}₢Library.generated.h""


UCLASS(BlueprintType, Category=""Beam"")
class BEAMABLECORE_API U₢{nameof(NamespacedTypeName)}₢Library : public UBlueprintFunctionLibrary
{{
	GENERATED_BODY()

public:

	UFUNCTION(BlueprintPure, Category=""Beam|Json"", DisplayName=""Beam - ₢{nameof(NamespacedTypeName)}₢ To JSON String"")
	static FString ₢{nameof(NamespacedTypeName)}₢ToJsonString(const U₢{nameof(NamespacedTypeName)}₢* Serializable, const bool Pretty);

	UFUNCTION(BlueprintPure, Category=""Beam|Backend"", DisplayName=""Beam - Make ₢{nameof(NamespacedTypeName)}₢"", meta=(DefaultToSelf=""Outer"", AdvancedDisplay=""₢{nameof(_makeOptionalParamNames)}₢Outer"", NativeMakeFunc))
	static U₢{nameof(NamespacedTypeName)}₢* Make(₢{nameof(_makeParams)}₢UObject* Outer);

	₢{nameof(BREAK_UTILITY_DECLARATION)}₢
}};";

	public const string SERIALIZABLE_TYPES_LIBRARY_CPP = $@"
#include ""AutoGen/₢{nameof(NamespacedTypeName)}₢Library.h""

#include ""CoreMinimal.h""


FString U₢{nameof(NamespacedTypeName)}₢Library::₢{nameof(NamespacedTypeName)}₢ToJsonString(const U₢{nameof(NamespacedTypeName)}₢* Serializable, const bool Pretty)
{{
	FString Result = FString{{}};
	if(Pretty)
	{{
		TUnrealPrettyJsonSerializer JsonSerializer = TJsonStringWriter<TPrettyJsonPrintPolicy<wchar_t>>::Create(&Result);
		Serializable->BeamSerialize(JsonSerializer);
		JsonSerializer->Close();
	}}
	else
	{{
		TUnrealJsonSerializer JsonSerializer = TJsonStringWriter<TCondensedJsonPrintPolicy<wchar_t>>::Create(&Result);
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
