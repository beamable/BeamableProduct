using System.Text;

namespace cli.Unreal;

public struct UnrealPropertyDeclaration
{
	public string RawFieldName;
	public string PropertyUnrealType;
	public string PropertyNamespacedType;
	public string PropertyName;
	public string PropertyDisplayName;

	public string FirstTemplateParameter;

	/// <summary>
	/// Used for and optionals.
	/// </summary>
	public string NonOptionalTypeName;

	/// <summary>
	/// Used for and optionals.
	/// </summary>
	public string NonOptionalTypeNameRelevantTemplateParam;


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
	}

	public const string U_PROPERTY_DECLARATION =
		$@"UPROPERTY(EditAnywhere, BlueprintReadWrite, DisplayName=""₢{nameof(PropertyDisplayName)}₢"", Category=""Beam"")
	₢{nameof(PropertyUnrealType)}₢ ₢{nameof(PropertyName)}₢;";


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

	public const string U_ENUM_U_PROPERTY_SERIALIZE = $@"Serializer->WriteValue(TEXT(""₢{nameof(RawFieldName)}₢""), U₢{nameof(PropertyNamespacedType)}₢Library::₢{nameof(PropertyNamespacedType)}₢ToSerializationName(₢{nameof(PropertyName)}₢));";
	public const string U_ENUM_U_PROPERTY_DESERIALIZE = $@"₢{nameof(PropertyName)}₢ = U₢{nameof(PropertyNamespacedType)}₢Library::SerializationNameTo₢{nameof(PropertyNamespacedType)}₢(Bag->GetStringField(TEXT(""₢{nameof(RawFieldName)}₢"")));";

	public const string U_OBJECT_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeUObject<₢{nameof(PropertyUnrealType)}₢>(""₢{nameof(RawFieldName)}₢"", ₢{nameof(PropertyName)}₢, Serializer);";

	public const string U_OBJECT_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeUObject<₢{nameof(PropertyUnrealType)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string ARRAY_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string ARRAY_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeArray<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(Bag->GetArrayField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string MAP_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), ₢{nameof(PropertyName)}₢, Serializer);";

	public const string MAP_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeMap<₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(Bag->GetObjectField(TEXT(""₢{nameof(RawFieldName)}₢"")), ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string OPTIONAL_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeOptional<₢{nameof(NonOptionalTypeName)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string OPTIONAL_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeOptional<₢{nameof(NonOptionalTypeName)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

	public const string OPTIONAL_WRAPPER_U_PROPERTY_SERIALIZE =
		$@"UBeamJsonUtils::SerializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(TEXT(""₢{nameof(RawFieldName)}₢""), &₢{nameof(PropertyName)}₢, Serializer);";

	public const string OPTIONAL_WRAPPER_U_PROPERTY_DESERIALIZE =
		$@"UBeamJsonUtils::DeserializeOptional<₢{nameof(NonOptionalTypeName)}₢, ₢{nameof(NonOptionalTypeNameRelevantTemplateParam)}₢>(""₢{nameof(RawFieldName)}₢"", Bag, ₢{nameof(PropertyName)}₢, OuterOwner);";

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

	public static string GetSerializeTemplateForUnrealType(string unrealType)
	{
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL))
		{
			if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_MAP) || unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_ARRAY))
				return OPTIONAL_WRAPPER_U_PROPERTY_SERIALIZE;

			return OPTIONAL_U_PROPERTY_SERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
			return U_ENUM_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_MAP))
			return MAP_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY))
			return ARRAY_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_GUID))
			return GUID_U_PROPERTY_SERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
			return U_OBJECT_U_PROPERTY_SERIALIZE;

		return PRIMITIVE_U_PROPERTY_SERIALIZE;
	}

	public static string GetDeserializeTemplateForUnrealType(string unrealType)
	{
		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL))
		{
			if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_MAP) || unrealType.StartsWith(UnrealSourceGenerator.UNREAL_OPTIONAL_ARRAY))
			{
				return OPTIONAL_WRAPPER_U_PROPERTY_DESERIALIZE;
			}

			return OPTIONAL_U_PROPERTY_DESERIALIZE;
		}

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_ENUM_PREFIX))
			return U_ENUM_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_U_OBJECT_PREFIX))
			return U_OBJECT_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_MAP))
			return MAP_U_PROPERTY_DESERIALIZE;

		if (unrealType.StartsWith(UnrealSourceGenerator.UNREAL_ARRAY))
			return ARRAY_U_PROPERTY_DESERIALIZE;

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
