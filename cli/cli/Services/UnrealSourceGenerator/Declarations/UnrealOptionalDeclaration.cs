namespace cli.Unreal;

public struct UnrealOptionalDeclaration
{
	public UnrealSourceGenerator.UnrealType UnrealTypeName;
	public UnrealSourceGenerator.NamespacedType NamespacedTypeName;
	public string UnrealTypeIncludeStatement;

	public UnrealSourceGenerator.UnrealType ValueUnrealTypeName;
	public UnrealSourceGenerator.NamespacedType ValueNamespacedTypeName;
	public string ValueUnrealTypeIncludeStatement;

	private string _valueInitializerStatement;

	public void BakeIntoProcessMap(Dictionary<string, string> helperDict)
	{
		_valueInitializerStatement = ValueUnrealTypeName.IsUnrealUObject() ? "nullptr" : $"{ValueUnrealTypeName}()";

		helperDict.Add(nameof(UnrealSourceGenerator.exportMacro), UnrealSourceGenerator.exportMacro);
		helperDict.Add(nameof(UnrealTypeName), UnrealTypeName);
		helperDict.Add(nameof(NamespacedTypeName), NamespacedTypeName);
		helperDict.Add(nameof(UnrealTypeIncludeStatement), UnrealTypeIncludeStatement);
		helperDict.Add(nameof(ValueUnrealTypeName), ValueUnrealTypeName);
		helperDict.Add(nameof(_valueInitializerStatement), _valueInitializerStatement);
		helperDict.Add(nameof(ValueNamespacedTypeName), ValueNamespacedTypeName);
		helperDict.Add(nameof(ValueUnrealTypeIncludeStatement), ValueUnrealTypeIncludeStatement);
	}

	public const string OPTIONAL_HEADER_DECL = $@"#pragma once

#include ""CoreMinimal.h""
#include ""Serialization/BeamOptional.h""
₢{nameof(ValueUnrealTypeIncludeStatement)}₢

#include ""₢{nameof(NamespacedTypeName)}₢.generated.h""

// Has Native Make/Break require static blueprint pure functions to present as nodes that
// don't require an execution pin connection. This is super relevant for Blueprint UX. 
USTRUCT(BlueprintType, meta=(HasNativeMake=""/Script/BeamableCore.₢{nameof(NamespacedTypeName)}₢Library.MakeOptional"", BeamOptionalType=""₢{nameof(ValueUnrealTypeName)}₢""))
struct ₢{nameof(UnrealSourceGenerator.exportMacro)}₢ ₢{nameof(UnrealTypeName)}₢ : public FBeamOptional
{{
	GENERATED_BODY()

	UPROPERTY(BlueprintReadOnly, EditAnywhere)
	₢{nameof(ValueUnrealTypeName)}₢ Val;

	₢{nameof(UnrealTypeName)}₢();

	explicit ₢{nameof(UnrealTypeName)}₢(₢{nameof(ValueUnrealTypeName)}₢ Val);

	virtual const void* GetAddr() const override;

	virtual void Set(const void* Data) override;
}};";

	public const string OPTIONAL_CPP_DECL = $@"
₢{nameof(UnrealTypeIncludeStatement)}₢

₢{nameof(UnrealTypeName)}₢::₢{nameof(UnrealTypeName)}₢()
{{
	Val = ₢{nameof(_valueInitializerStatement)}₢;
	IsSet = false;
}}

₢{nameof(UnrealTypeName)}₢::₢{nameof(UnrealTypeName)}₢(₢{nameof(ValueUnrealTypeName)}₢ Val): Val(Val)
{{
	IsSet = true;
}}

const void* ₢{nameof(UnrealTypeName)}₢::GetAddr() const {{ return &Val; }}

void ₢{nameof(UnrealTypeName)}₢::Set(const void* Data)
{{
	Val = *((₢{nameof(ValueUnrealTypeName)}₢*)Data);
	IsSet = true;
}}";

	public const string OPTIONAL_LIBRARY_HEADER_DECL = $@"#pragma once

#include ""CoreMinimal.h""
₢{nameof(UnrealTypeIncludeStatement)}₢

#include ""₢{nameof(NamespacedTypeName)}₢Library.generated.h""

UCLASS(BlueprintType)
class ₢{nameof(UnrealSourceGenerator.exportMacro)}₢ U₢{nameof(NamespacedTypeName)}₢Library : public UBlueprintFunctionLibrary
{{
	GENERATED_BODY()
public:	

	/**
	* @brief Constructs an ₢{nameof(UnrealTypeName)}₢ struct from the given value.	  
	*/
	UFUNCTION(BlueprintPure, Category=""Beam|Optionals"", meta=(DisplayName=""Beam - Make Optional ₢{nameof(ValueNamespacedTypeName)}₢"", NativeMakeFunc))
	static ₢{nameof(UnrealTypeName)}₢ MakeOptional(₢{nameof(ValueUnrealTypeName)}₢ Value);

	/**
	 * @brief Converts an ₢{nameof(ValueUnrealTypeName)}₢ into an ₢{nameof(UnrealTypeName)}₢ automatically.
	 * @param Value The ₢{nameof(ValueUnrealTypeName)}₢ to convert.
	 * @return An optional with the ₢{nameof(ValueNamespacedTypeName)}₢ set as it's value.
	 */
	UFUNCTION(BlueprintPure, Category=""Beam|Optionals"", meta = (DisplayName = ""Beam - ₢{nameof(ValueNamespacedTypeName)}₢ To Optional"", CompactNodeTitle = ""->"", BlueprintAutocast))
	static ₢{nameof(UnrealTypeName)}₢ Conv_OptionalFromValue(₢{nameof(ValueUnrealTypeName)}₢ Value);
	
	/**
	 * @brief Use this when the behavior changes based on whether or not a value is set on the optional.
	 * @param Optional The optional you wish to get data from.
	 * @param Value The value in the optional. 
	 * @return Whether or not the value was set. We provide no guarantees on what the value is if the optional is not set. 
	 */
	UFUNCTION(BlueprintCallable, Category=""Beam|Optionals"", meta=(DisplayName=""Beam - Optional Has Value"", ExpandBoolAsExecs=""ReturnValue""))
	static bool HasValue(const ₢{nameof(UnrealTypeName)}₢& Optional, ₢{nameof(ValueUnrealTypeName)}₢& Value);

	/**
	 * @brief Use this when the behaviour doesnt change based on whether or not the value is set, instead just provide a default value instead.
	 * @param Optional The optional you wish to get data from.
	 * @param DefaultValue The value that will be set if the Optional has no value in it.
	 * @param WasSet Whether or not the value was set. When false, the return value is the given DefaultValue.   
	 * @return The default value, if the Optional IS NOT set. The optional value, otherwise.
	 */
	UFUNCTION(BlueprintPure, Category=""Beam|Optionals"", meta=(DisplayName=""Beam - Get Optional's ₢{nameof(ValueNamespacedTypeName)}₢ Value""))
	static ₢{nameof(ValueUnrealTypeName)}₢ GetOptionalValue(const ₢{nameof(UnrealTypeName)}₢& Optional, ₢{nameof(ValueUnrealTypeName)}₢ DefaultValue, bool& WasSet);

	
}};
";

	public const string OPTIONAL_LIBRARY_CPP_DECL = $@"

#include ""AutoGen/Optionals/₢{nameof(NamespacedTypeName)}₢Library.h""

₢{nameof(UnrealTypeName)}₢ U₢{nameof(NamespacedTypeName)}₢Library::MakeOptional(₢{nameof(ValueUnrealTypeName)}₢ Value)
{{
	₢{nameof(UnrealTypeName)}₢ Optional;
	Optional.Val = Value;
	Optional.IsSet = true;
	return Optional;
}}

₢{nameof(UnrealTypeName)}₢ U₢{nameof(NamespacedTypeName)}₢Library::Conv_OptionalFromValue(₢{nameof(ValueUnrealTypeName)}₢ Value)
{{
	₢{nameof(UnrealTypeName)}₢ Optional;
	Optional.Val = Value;
	Optional.IsSet = true;
	return Optional;
}}

bool U₢{nameof(NamespacedTypeName)}₢Library::HasValue(const ₢{nameof(UnrealTypeName)}₢& Optional, ₢{nameof(ValueUnrealTypeName)}₢& Value)
{{
	Value = Optional.Val;
	return Optional.IsSet;
}}

₢{nameof(ValueUnrealTypeName)}₢ U₢{nameof(NamespacedTypeName)}₢Library::GetOptionalValue(const ₢{nameof(UnrealTypeName)}₢& Optional, ₢{nameof(ValueUnrealTypeName)}₢ DefaultValue, bool& WasSet)
{{
	WasSet = Optional.IsSet;
	return WasSet ? Optional.Val : DefaultValue;
}}

";
}
