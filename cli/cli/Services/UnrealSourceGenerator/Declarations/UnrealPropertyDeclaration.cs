using System.Text;

namespace cli.Unreal;

public struct UnrealPropertyDeclaration
{
	public string RawFieldName;
	public UnrealSourceGenerator.UnrealType PropertyUnrealType;
	public UnrealSourceGenerator.NamespacedType PropertyNamespacedType;
	public string PropertyName;
	public string PropertyDisplayName;
	public string AsParameterName;

	/// <summary>
	/// This is the type the optional wraps around. 
	/// </summary>
	public UnrealSourceGenerator.UnrealType NonOptionalTypeName;

	/// <summary>
	/// For optional arrays and maps, the (de)serialization code output needs to know what the type of the array/map is. This holds that variable.
	/// If said type is a Semantic Type, <see cref="SemTypeSerializationType"/> will contain the (de)serialization type for each element of the array. 
	/// </summary>
	public UnrealSourceGenerator.UnrealType NonOptionalTypeNameRelevantTemplateParam;

	/// <summary>
	/// If this property represents a Semantic Type (<see cref="UnrealSourceGenerator.UNREAL_ALL_SEMTYPES"/>), this contains the underlying primitive type that we expect to receive.
	/// For example, <see cref="UnrealSourceGenerator.UNREAL_U_SEMTYPE_CID"/> can be either a '<see cref="UnrealSourceGenerator.UNREAL_STRING"/>' or a '<see cref="UnrealSourceGenerator.UNREAL_LONG"/>'
	/// for (de)serialization purposes. In each declaration, this variable would hold either of those values so that we can appropriately call the serialize functions.
	///
	/// There's one exception to this --- for semantic types that are not defined in the spec (ie: <see cref="UnrealSourceGenerator.UNREAL_OPTIONAL_U_REPTYPE_CLIENTPERMISSION"/>),
	/// this is always an FString and the semantic type is expected to inherit from FBeamJsonSerializableUStruct/IBeamJsonSerializableUObject first (FBeamSemanticType as a second inheritance). 
	/// </summary>
	public UnrealSourceGenerator.UnrealType SemTypeSerializationType;


	public string BriefCommentString;

	/// <summary>
	/// Helper function to help deal with the fact that our schemas sometimes get a '$' on their fields because scala is a funny language.
	/// Funny does not mean funny here.
	/// </summary>
	public static string GetSanitizedPropertyName(string n)
	{
		var escaped = n.StartsWith('$') ? n[1..] : n;
		return escaped;
	}

	/// <summary>
	/// This escapes any names that match a UE global variable declaration so we don't inadvertently use them as parameter names.
	/// </summary>
	public static string GetSanitizedParameterName(string n)
	{
		// UNREAL GLOBAL VARIABLES
		string[] KnownUnrealGlobalVariables = new[]
		{
			"LogPath", "LogController", "LogPhysics", "LogBlueprint", "LogBlueprintUserMessages", "LogAnimation", "LogRootMotion", "LogLevel", "LogSkeletalMesh", "LogStaticMesh", "LogNet", "LogNetLifecycle",
			"LogNetSubObject", "LogRep", "LogNetPlayerMovement", "LogNetTraffic", "LogRepTraffic", "LogNetDormancy", "LogSkeletalControl", "LogSubtitle", "LogTexture", "LogTextureUpload", "LogPlayerManagement",
			"LogSecurity", "LogEngineSessionManager", "LogViewport"
		};

		var escaped = n.StartsWith('$') ? n[1..] : n;
		escaped = KnownUnrealGlobalVariables.Contains(escaped) ? $"_{escaped}" : escaped;
		return escaped;
	}
	

	/// <summary>
	/// Helper function to help deal with the fact that our schemas sometimes get a '$' on their fields because scala is a funny language.
	/// Funny does not mean funny here.
	/// </summary>
	public static string GetSanitizedPropertyDisplayName(string n) => n.StartsWith('$') ? n[1..] : n;

	public void IntoProcessMap(Dictionary<string, string> helperDict)
	{
		helperDict.Add(nameof(PropertyUnrealType), PropertyUnrealType);
		helperDict.Add(nameof(PropertyNamespacedType), PropertyNamespacedType);
		helperDict.Add(nameof(PropertyName), PropertyName);
		helperDict.Add(nameof(PropertyDisplayName), PropertyDisplayName);
		helperDict.Add(nameof(RawFieldName), RawFieldName);
		helperDict.Add(nameof(NonOptionalTypeName), NonOptionalTypeName);
		helperDict.Add(nameof(NonOptionalTypeNameRelevantTemplateParam), NonOptionalTypeNameRelevantTemplateParam);
		helperDict.Add(nameof(BriefCommentString), BriefCommentString);

		if (string.IsNullOrEmpty(SemTypeSerializationType))
			SemTypeSerializationType = UnrealSourceGenerator.UNREAL_STRING;
		helperDict.Add(nameof(SemTypeSerializationType), SemTypeSerializationType);
	}

	/// <summary>
	/// This function returns the correct template for declaring this field.
	/// This allows us to correctly generate fields that are not UPROPERTIES.
	///
	/// Primarily we want to minimize the amount of fields that are unusable in blueprints.
	/// </summary>
	public string GetDeclarationTemplate() => IsBlueprintCompatible() ? U_PROPERTY_DECLARATION : U_FIELD_DECLARATION;

	/// <summary>
	/// This checks to see if this property is blueprint compatible or not.
	/// </summary>
	public bool IsBlueprintCompatible() => !PropertyUnrealType.IsUnrealJson();

	public const string U_PROPERTY_DECLARATION =
		$@"UPROPERTY(EditAnywhere, BlueprintReadWrite, DisplayName=""₢{nameof(PropertyDisplayName)}₢"", Category=""Beam"")
	₢{nameof(PropertyUnrealType)}₢ ₢{nameof(PropertyName)}₢ = {{}};";

	public const string U_FIELD_DECLARATION =
		$@"₢{nameof(PropertyUnrealType)}₢ ₢{nameof(PropertyName)}₢ = {{}};";

	// Fallback for non-supported types
	public const string STRING_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢""));";

	public const string PRIMITIVE_U_PROPERTY_SERIALIZE = @$"UBeamJsonUtils::SerializeRawPrimitive(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string UNREAL_RAW_PRIMITIVE_DESERIALIZE = @$"UBeamJsonUtils::DeserializeRawPrimitive(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢);";


	public const string UNREAL_JSON_FIELD_SERIALIZE =
		$@"UBeamJsonUtils::SerializeJsonObject(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string UNREAL_JSON_FIELD_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeJsonObject(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string U_ENUM_U_PROPERTY_SERIALIZE =
		$@"Serializer->WriteValue(TEXT(""₢{nameof(RawFieldName)}₢""), UBeamJsonUtils::EnumToSerializationName(₢{nameof(PropertyName)}₢));";

	public const string U_ENUM_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeRawPrimitive(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢);";

	public const string U_STRUCT_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeUStruct<₢{nameof(PropertyUnrealType)}₢>(""₢{nameof(RawFieldName)}₢"", ₢{nameof(PropertyName)}₢, Serializer);";

	public const string U_STRUCT_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeUStruct<₢{nameof(PropertyUnrealType)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string U_OBJECT_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeUObject<₢{nameof(PropertyUnrealType)}₢>(""₢{nameof(RawFieldName)}₢"", ₢{nameof(PropertyName)}₢, Serializer);";

	public const string U_OBJECT_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeUObject<₢{nameof(PropertyUnrealType)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string ARRAY_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string ARRAY_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string ARRAY_SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string ARRAY_SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string MAP_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string MAP_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string MAP_SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string MAP_SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeSemanticType<₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeSemanticType<₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string OPTIONAL_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeOptional<₢{nameof(NonOptionalTypeName)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string OPTIONAL_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeOptional<₢{nameof(NonOptionalTypeName)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string OPTIONAL_SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string OPTIONAL_SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(SemTypeSerializationType)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string OPTIONAL_WRAPPER_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string OPTIONAL_WRAPPER_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string OPTIONAL_WRAPPER_SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string OPTIONAL_WRAPPER_SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public static UnrealSourceGenerator.UnrealType ExtractFirstTemplateParamFromType(string unrealType)
	{
		var startIdx = unrealType.IndexOf('<');
		if (startIdx == -1)
			return new("");

		startIdx += 1;

		var endIdx = unrealType.IndexOf(',');
		if (endIdx < 0) endIdx = unrealType.IndexOf('>');
		return new(unrealType.AsSpan(startIdx, endIdx - startIdx).ToString());
	}

	public static UnrealSourceGenerator.UnrealType ExtractSecondTemplateParamFromType(string unrealType)
	{
		var startIdx = unrealType.IndexOf(',');
		if (startIdx == -1)
			return new("");

		startIdx += 1;

		// Get the next ',' idx and if it wasn't found, get the '>' instead.
		var endIdx = unrealType.IndexOf(',', startIdx);
		if (endIdx < 0) endIdx = unrealType.IndexOf('>');

		return new(unrealType.AsSpan(startIdx, endIdx - startIdx).ToString().Trim());
	}

	public static string GetSerializeTemplateForUnrealType(UnrealSourceGenerator.UnrealType unrealType)
	{
		if (unrealType.IsOptional())
		{
			var isSemType = unrealType.ContainsAnySemanticType();
			if (unrealType.IsOptionalMap() || unrealType.IsOptionalArray())
				return isSemType ? OPTIONAL_WRAPPER_SEMTYPE_U_PROPERTY_SERIALIZE : OPTIONAL_WRAPPER_U_PROPERTY_SERIALIZE;

			return isSemType ? OPTIONAL_SEMTYPE_U_PROPERTY_SERIALIZE : OPTIONAL_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.IsUnrealEnum())
			return U_ENUM_U_PROPERTY_SERIALIZE;

		if (unrealType.IsUnrealMap())
		{
			var isSemType = unrealType.ContainsAnySemanticType();
			return isSemType ? MAP_SEMTYPE_U_PROPERTY_SERIALIZE : MAP_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.IsUnrealArray())
		{
			var isSemType = unrealType.ContainsAnySemanticType();
			return isSemType ? ARRAY_SEMTYPE_U_PROPERTY_SERIALIZE : ARRAY_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.IsUnrealJson())
			return UNREAL_JSON_FIELD_SERIALIZE;

		if (unrealType.IsRawPrimitive())
		{
			return PRIMITIVE_U_PROPERTY_SERIALIZE;
		}

		// Semantic types serialization
		if (unrealType.IsAnySemanticType())
			return SEMTYPE_U_PROPERTY_SERIALIZE;

		if (unrealType.IsUnrealUObject())
			return U_OBJECT_U_PROPERTY_SERIALIZE;

		if (unrealType.IsUnrealStruct())
			return U_STRUCT_U_PROPERTY_SERIALIZE;

		return PRIMITIVE_U_PROPERTY_SERIALIZE;
	}

	public static string GetDeserializeTemplateForUnrealType(UnrealSourceGenerator.UnrealType unrealType)
	{
		if (unrealType.IsOptional())
		{
			var isSemType = unrealType.ContainsAnySemanticType();
			if (unrealType.IsOptionalMap() || unrealType.IsOptionalArray())
			{
				return isSemType ? OPTIONAL_WRAPPER_SEMTYPE_U_PROPERTY_DESERIALIZE : OPTIONAL_WRAPPER_U_PROPERTY_DESERIALIZE;
			}

			return isSemType ? OPTIONAL_SEMTYPE_U_PROPERTY_DESERIALIZE : OPTIONAL_U_PROPERTY_DESERIALIZE;
		}

		if (unrealType.IsUnrealEnum())
			return U_ENUM_U_PROPERTY_DESERIALIZE;


		if (unrealType.IsUnrealMap())
		{
			var isSemType = unrealType.ContainsAnySemanticType();
			return isSemType ? MAP_SEMTYPE_U_PROPERTY_DESERIALIZE : MAP_U_PROPERTY_DESERIALIZE;
		}

		if (unrealType.IsUnrealArray())
		{
			var isSemType = unrealType.ContainsAnySemanticType();
			return isSemType ? ARRAY_SEMTYPE_U_PROPERTY_DESERIALIZE : ARRAY_U_PROPERTY_DESERIALIZE;
		}

		if (unrealType.IsUnrealJson())
			return UNREAL_JSON_FIELD_DESERIALIZE;

		if (unrealType.IsRawPrimitive())
			return UNREAL_RAW_PRIMITIVE_DESERIALIZE;

		// Semantic types serialization
		if (unrealType.IsAnySemanticType())
			return SEMTYPE_U_PROPERTY_DESERIALIZE;

		if (unrealType.IsUnrealUObject())
			return U_OBJECT_U_PROPERTY_DESERIALIZE;

		if (unrealType.IsUnrealStruct())
			return U_STRUCT_U_PROPERTY_DESERIALIZE;


		return STRING_U_PROPERTY_DESERIALIZE;
	}

	public static string GetPrimitiveUPropertyFieldName(UnrealSourceGenerator.UnrealType unrealType, string fieldName, StringBuilder stringBuilder)
	{
		stringBuilder.Clear();
		var wordStartIdx = 0;
		int idx;
		do
		{
			idx = fieldName.IndexOf("_", wordStartIdx, StringComparison.Ordinal);

			// If we start with "_", we change the field name after the underscore
			if (idx == 0)
			{
				wordStartIdx = 1;
				idx = -1;
			}

			// // Otherwise, we keep the full name if 
			var length = idx == -1 ? fieldName.Length - wordStartIdx : idx - wordStartIdx;

			var word = fieldName.AsSpan(wordStartIdx, length);
			var name = string.Concat(char.ToUpper(word[0]).ToString(), word.Slice(1));
			stringBuilder.Append(name);

			wordStartIdx = idx + 1;
		} while (wordStartIdx < fieldName.Length && idx != -1);

		if (unrealType.IsUnrealBool() || unrealType.IsOptionalBool())
		{
			stringBuilder.Insert(0, "b");
		}

		return GetSanitizedPropertyName(stringBuilder.ToString());
	}
}
