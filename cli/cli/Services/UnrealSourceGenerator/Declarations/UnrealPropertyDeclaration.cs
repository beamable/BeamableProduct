using System.Text;

namespace cli.Unreal;

public struct UnrealPropertyDeclaration
{
	public string RawFieldName;
	public string PropertyUnrealType;
	public string PropertyNamespacedType;
	public string PropertyName;
	public string PropertyDisplayName;

	/// <summary>
	/// This is the type the optional wraps around. 
	/// </summary>
	public string NonOptionalTypeName;

	/// <summary>
	/// For optional arrays and maps, the (de)serialization code output needs to know what the type of the array/map is. This holds that variable.
	/// If said type is a Semantic Type, <see cref="SemTypeSerializationType"/> will contain the (de)serialization type for each element of the array. 
	/// </summary>
	public string NonOptionalTypeNameRelevantTemplateParam;

	/// <summary>
	/// If this property represents a Semantic Type (<see cref="UnrealSourceGenerator.UNREAL_ALL_SEMTYPES"/>), this contains the underlying primitive type that we expect to receive.
	/// For example, <see cref="UnrealSourceGenerator.UNREAL_U_SEMTYPE_CID"/> can be either a '<see cref="UnrealSourceGenerator.UNREAL_STRING"/>' or a '<see cref="UnrealSourceGenerator.UNREAL_LONG"/>'
	/// for (de)serialization purposes. In each declaration, this variable would hold either of those values so that we can appropriately call the serialize functions.
	///
	/// There's one exception to this --- for semantic types that are not defined in the spec (ie: <see cref="UnrealSourceGenerator.UNREAL_OPTIONAL_U_REPTYPE_CLIENTPERMISSION"/>),
	/// this is always an FString and the semantic type is expected to inherit from FBeamJsonSerializableUStruct/IBeamJsonSerializableUObject first (FBeamSemanticType as a second inheritance). 
	/// </summary>
	public string SemTypeSerializationType;


	public string BriefCommentString;

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

	public const string U_PROPERTY_DECLARATION =
		$@"UPROPERTY(EditAnywhere, BlueprintReadWrite, DisplayName=""₢{nameof(PropertyDisplayName)}₢"", Category=""Beam"")
	₢{nameof(PropertyUnrealType)}₢ ₢{nameof(PropertyName)}₢ = {{}};";


	public const string PRIMITIVE_U_PROPERTY_SERIALIZE = @$"Serializer->WriteValue(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢);";
	public const string GUID_U_PROPERTY_SERIALIZE = @$"Serializer->WriteValue(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢.ToString());";

	public const string STRING_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢""));";
	public const string INT8_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = static_cast<int8>(Bag->GetIntegerField(""₢{nameof(RawFieldName)}₢""));";
	public const string INT16_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = static_cast<int16>(Bag->GetIntegerField(""₢{nameof(RawFieldName)}₢""));";
	public const string INT32_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = Bag->GetIntegerField(TEXT(""₢{nameof(RawFieldName)}₢""));";
	public const string INT64_U_PROPERTY_DESERIALIZE = @$"FDefaultValueHelper::ParseInt64(Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢);";
	public const string BOOL_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = Bag->GetBoolField(TEXT(""₢{nameof(RawFieldName)}₢""));";
	public const string FLOAT_U_PROPERTY_DESERIALIZE = @$"FDefaultValueHelper::ParseFloat(Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢);";
	public const string DOUBLE_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = Bag->GetNumberField(TEXT(""₢{nameof(RawFieldName)}₢""));";
	public const string GUID_U_PROPERTY_DESERIALIZE = $@"FGuid::Parse(Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢);";

	public const string U_ENUM_U_PROPERTY_SERIALIZE =
		$@"Serializer->WriteValue(TEXT(""₢{nameof(RawFieldName)}₢""), U₢{nameof(PropertyNamespacedType)}₢Library::₢{nameof(PropertyNamespacedType)}₢ToSerializationName(₢{nameof(PropertyName)}₢));";

	public const string U_ENUM_U_PROPERTY_DESERIALIZE =
		$@"₢{nameof(PropertyName)}₢ = U₢{nameof(PropertyNamespacedType)}₢Library::SerializationNameTo₢{nameof(PropertyNamespacedType)}₢(Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢"")));";

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
		$@"UBeamJsonUtils::DeserializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(Bag->GetArrayField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string ARRAY_SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string ARRAY_SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(Bag->GetArrayField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string MAP_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string MAP_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(Bag->GetObjectField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string MAP_SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string MAP_SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢, ₢{nameof(SemTypeSerializationType)}₢>(Bag->GetObjectField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string SEMTYPE_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeSemanticType<₢{nameof(SemTypeSerializationType)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string SEMTYPE_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeSemanticType<₢{nameof(SemTypeSerializationType)}₢>(Bag->TryGetField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

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

	public static string ExtractFirstTemplateParamFromType(string unrealType)
	{
		var startIdx = unrealType.IndexOf('<');
		if (startIdx == -1)
			return "";

		startIdx += 1;

		var endIdx = unrealType.IndexOf(',');
		if (endIdx < 0) endIdx = unrealType.IndexOf('>');
		return unrealType.AsSpan(startIdx, endIdx - startIdx).ToString();
	}

	public static string ExtractSecondTemplateParamFromType(string unrealType)
	{
		var startIdx = unrealType.IndexOf(',');
		if (startIdx == -1)
			return "";

		startIdx += 1;

		// Get the next ',' idx and if it wasn't found, get the '>' instead.
		var endIdx = unrealType.IndexOf(',', startIdx);
		if (endIdx < 0) endIdx = unrealType.IndexOf('>');

		return unrealType.AsSpan(startIdx, endIdx - startIdx).ToString().Trim();
	}

	public string GetSerializeTemplateForUnrealType(string unrealType)
	{
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL))
		{
			var isSemType = UnrealSourceGenerator.UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(unrealType.Contains);
			if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_MAP) || unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_ARRAY))
				return isSemType ? OPTIONAL_WRAPPER_SEMTYPE_U_PROPERTY_SERIALIZE : OPTIONAL_WRAPPER_U_PROPERTY_SERIALIZE;

			return isSemType ? OPTIONAL_SEMTYPE_U_PROPERTY_SERIALIZE : OPTIONAL_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
			return U_ENUM_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_MAP))
		{
			var isSemType = UnrealSourceGenerator.UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(unrealType.Contains);
			return isSemType ? MAP_SEMTYPE_U_PROPERTY_SERIALIZE : MAP_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY))
		{
			var isSemType = UnrealSourceGenerator.UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(unrealType.Contains);
			return isSemType ? ARRAY_SEMTYPE_U_PROPERTY_SERIALIZE : ARRAY_U_PROPERTY_SERIALIZE;
		}
		
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_GUID))
			return GUID_U_PROPERTY_SERIALIZE;
		
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_STRING) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BYTE) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_SHORT) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_INT) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_LONG) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BOOL) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_FLOAT) ||
		    unrealType.StartsWith(UnrealSourceGenerator.UNREAL_DOUBLE))
		{
			return PRIMITIVE_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_STRING) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BYTE) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_SHORT) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_INT) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_LONG) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BOOL) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_FLOAT) ||
			unrealType.StartsWith(UnrealSourceGenerator.UNREAL_DOUBLE))
		{
			return PRIMITIVE_U_PROPERTY_SERIALIZE;
		}

		// Semantic types serialization
		if (UnrealSourceGenerator.UNREAL_ALL_SEMTYPES.Contains(unrealType))
			return SEMTYPE_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
			return U_OBJECT_U_PROPERTY_SERIALIZE;
		
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_STRUCT_PREFIX))
			return U_STRUCT_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_STRUCT_PREFIX))
			return U_STRUCT_U_PROPERTY_SERIALIZE;

		return PRIMITIVE_U_PROPERTY_SERIALIZE;
	}

	public string GetDeserializeTemplateForUnrealType(string unrealType)
	{
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL))
		{
			var isSemType = UnrealSourceGenerator.UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(unrealType.EndsWith);
			if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_MAP) || unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_ARRAY))
			{
				return isSemType ? OPTIONAL_WRAPPER_SEMTYPE_U_PROPERTY_DESERIALIZE : OPTIONAL_WRAPPER_U_PROPERTY_DESERIALIZE;
			}

			return isSemType ? OPTIONAL_SEMTYPE_U_PROPERTY_DESERIALIZE : OPTIONAL_U_PROPERTY_DESERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
			return U_ENUM_U_PROPERTY_DESERIALIZE;


		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_MAP))
		{
			var isSemType = UnrealSourceGenerator.UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(unrealType.Contains);
			return isSemType ? MAP_SEMTYPE_U_PROPERTY_DESERIALIZE : MAP_U_PROPERTY_DESERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY))
		{
			var isSemType = UnrealSourceGenerator.UNREAL_ALL_SEMTYPES_NAMESPACED_NAMES.Any(unrealType.Contains);
			return isSemType ? ARRAY_SEMTYPE_U_PROPERTY_DESERIALIZE : ARRAY_U_PROPERTY_DESERIALIZE;
		}
		
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_STRING))
			return STRING_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BYTE))
			return INT8_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_SHORT))
			return INT16_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_INT))
			return INT32_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_INT))
			return INT32_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_LONG))
			return INT64_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BOOL))
			return BOOL_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_FLOAT))
			return FLOAT_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_DOUBLE))
			return DOUBLE_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_GUID))
			return GUID_U_PROPERTY_DESERIALIZE;

		// Semantic types serialization
		if (UnrealSourceGenerator.UNREAL_ALL_SEMTYPES.Contains(unrealType))
			return SEMTYPE_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
			return U_OBJECT_U_PROPERTY_DESERIALIZE;
		
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
			return U_OBJECT_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_STRUCT_PREFIX))
			return U_STRUCT_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_STRUCT_PREFIX))
			return U_STRUCT_U_PROPERTY_DESERIALIZE;

		return STRING_U_PROPERTY_DESERIALIZE;
	}

	public static string GetPrimitiveUPropertyFieldName(string unrealType, string fieldName, StringBuilder stringBuilder)
	{
		stringBuilder.Clear();
		var wordStartIdx = 0;
		int idx;
		do
		{
			idx = fieldName.IndexOf("_", wordStartIdx, StringComparison.Ordinal);
			var length = idx == -1 ? fieldName.Length - wordStartIdx : idx - wordStartIdx;

			var word = fieldName.AsSpan(wordStartIdx, length);
			var name = string.Concat(char.ToUpper(word[0]).ToString(), word.Slice(1));
			stringBuilder.Append(name);

			wordStartIdx = idx + 1;
		} while (wordStartIdx < fieldName.Length && idx != -1);

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_BOOL) || unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_BOOL))
		{
			stringBuilder.Insert(0, "b");
		}

		return stringBuilder.ToString();
	}
}
