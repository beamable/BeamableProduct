using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using cli.Unreal;

namespace cli.Services.Analytics;

/// <summary>
/// Per-property data for a generated analytics-event header. Pre-renders the validator-call list
/// and deserializer RHS so the per-field templates remain pure ProcessReplacement substitutions.
/// </summary>
public struct AnalyticsValidatorFieldDeclaration
{
	public string JsonName;
	public string CppName;
	public string CppType;
	public string DefaultInit;

	/// <summary>Lines like "\t\tBeamValidators::ValidateMinimum(Foo, 0, FooResult);", each terminated with "\n". Empty when the property has no constraints.</summary>
	public string ValidatorCalls;

	/// <summary>Right-hand-side of the per-field deserialize assignment, e.g. "Bag->GetIntegerField(TEXT(\"Foo\"))" or "(float)Bag->GetNumberField(TEXT(\"Foo\"))".</summary>
	public string DeserializeRhs;

	/// <summary>True when this field is a nested custom type (JSON Schema "object"). Picks the custom-type variant of the per-field templates.</summary>
	public bool IsCustomType;

	/// <summary>Pre-rendered C++ expression for the field's SchemaPath. Either a literal like `TEXT("properties.foo")` (top-level) or `SchemaPathPrefix + TEXT(".properties.foo")` (inside a nested struct's Validate).</summary>
	public string SchemaPathExpr;

	/// <summary>Pre-rendered Doxygen comment block (one or more "\t/// ...\n" lines) sourced from JSON Schema's "$comment". Empty when the field has no comment. UHT promotes "///" blocks to UPROPERTY ToolTip metadata, so this surfaces in editor tooltips and runtime reflection too.</summary>
	public string CommentLine;

	public void IntoProcessDict(Dictionary<string, string> dict)
	{
		dict.Clear();
		dict.Add(nameof(JsonName), JsonName);
		dict.Add(nameof(CppName), CppName);
		dict.Add(nameof(CppType), CppType);
		dict.Add(nameof(DefaultInit), DefaultInit);
		dict.Add(nameof(ValidatorCalls), ValidatorCalls);
		dict.Add(nameof(DeserializeRhs), DeserializeRhs);
		dict.Add(nameof(SchemaPathExpr), SchemaPathExpr);
		dict.Add(nameof(CommentLine), CommentLine);
	}

	public string RenderProperty(Dictionary<string, string> dict)
	{
		IntoProcessDict(dict);
		return PROPERTY_DECL.ProcessReplacement(dict);
	}

	public string RenderValidate(Dictionary<string, string> dict)
	{
		IntoProcessDict(dict);
		return (IsCustomType ? VALIDATE_BLOCK_CUSTOM : VALIDATE_BLOCK).ProcessReplacement(dict);
	}

	public string RenderSerialize(Dictionary<string, string> dict)
	{
		IntoProcessDict(dict);
		return (IsCustomType ? SERIALIZE_LINE_CUSTOM : SERIALIZE_LINE).ProcessReplacement(dict);
	}

	public string RenderDeserialize(Dictionary<string, string> dict)
	{
		IntoProcessDict(dict);
		return (IsCustomType ? DESERIALIZE_BLOCK_CUSTOM : DESERIALIZE_BLOCK).ProcessReplacement(dict);
	}

	public const string PROPERTY_DECL = $@"₢{nameof(CommentLine)}₢	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category=""Analytics"")
	₢{nameof(CppType)}₢ ₢{nameof(CppName)}₢₢{nameof(DefaultInit)}₢;";

	public const string VALIDATE_BLOCK = $@"		FBeamValidationResult ₢{nameof(CppName)}₢Result(TEXT(""₢{nameof(CppName)}₢""));
		₢{nameof(CppName)}₢Result.SchemaPath = ₢{nameof(SchemaPathExpr)}₢;
₢{nameof(ValidatorCalls)}₢		OutContext.RecordResult(₢{nameof(CppName)}₢Result);";

	public const string VALIDATE_BLOCK_CUSTOM = $@"		₢{nameof(CppName)}₢.Validate(OutContext, ₢{nameof(SchemaPathExpr)}₢);";

	public const string SERIALIZE_LINE = $@"		Serializer->WriteValue(TEXT(""₢{nameof(CppName)}₢""), ₢{nameof(CppName)}₢);";

	public const string SERIALIZE_LINE_CUSTOM = $@"		Serializer->WriteIdentifierPrefix(TEXT(""₢{nameof(CppName)}₢""));
		₢{nameof(CppName)}₢.BeamSerialize(Serializer);";

	public const string DESERIALIZE_BLOCK = $@"		if (Bag->HasField(TEXT(""₢{nameof(CppName)}₢"")))
			₢{nameof(CppName)}₢ = ₢{nameof(DeserializeRhs)}₢;";

	public const string DESERIALIZE_BLOCK_CUSTOM = $@"		if (Bag->HasField(TEXT(""₢{nameof(CppName)}₢"")))
			₢{nameof(CppName)}₢.BeamDeserializeProperties(Bag->GetObjectField(TEXT(""₢{nameof(CppName)}₢"")));";
}

/// <summary>
/// One generated USTRUCT for a nested custom type encountered inside an event schema. Emitted as a
/// sibling USTRUCT in the same .h file as the event that uses it (declared before the event so the
/// event can name it as a UPROPERTY type). Inherits FBeamJsonSerializableUStruct so it integrates
/// with the standard Beam serialize/deserialize pipeline; defines a Validate(OutContext, prefix)
/// helper so the parent can attribute schema paths to the right ancestor.
/// </summary>
public struct AnalyticsValidatorCustomTypeDeclaration
{
	public string StructName;
	public List<AnalyticsValidatorFieldDeclaration> Fields;

	public void IntoProcessDict(Dictionary<string, string> helperDict)
	{
		var propertyDeclarations = string.Join("\n\n", Fields.Select(f => f.RenderProperty(helperDict)));
		var validateBlocks = string.Join("\n\n", Fields.Select(f => f.RenderValidate(helperDict)));
		var serializeLines = string.Join("\n", Fields.Select(f => f.RenderSerialize(helperDict)));
		var deserializeBlocks = string.Join("\n", Fields.Select(f => f.RenderDeserialize(helperDict)));

		helperDict.Clear();
		helperDict.Add(nameof(StructName), StructName);
		helperDict.Add(nameof(propertyDeclarations), propertyDeclarations);
		helperDict.Add(nameof(validateBlocks), validateBlocks);
		helperDict.Add(nameof(serializeLines), serializeLines);
		helperDict.Add(nameof(deserializeBlocks), deserializeBlocks);
	}

	public const string CUSTOM_TYPE_TEMPLATE = $@"USTRUCT(BlueprintType)
struct ₢{nameof(StructName)}₢ : public FBeamJsonSerializableUStruct
{{
	GENERATED_BODY()

₢propertyDeclarations₢

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override
	{{
₢serializeLines₢
	}}

	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override
	{{
₢serializeLines₢
	}}

	virtual void BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag) override
	{{
₢deserializeBlocks₢
	}}

	void Validate(FBeamValidationContext& OutContext, const FString& SchemaPathPrefix) const
	{{
₢validateBlocks₢
	}}
}};";
}

/// <summary>
/// One generated header file: an analytics-event USTRUCT deriving from FBeamAnalyticsEvent with
/// overrides for the wire-format identifiers, validate(), and the BeamSerialize/BeamDeserialize trio.
/// </summary>
public struct AnalyticsValidatorEventDeclaration
{
	public string EventName;
	public string StructName;
	/// <summary>Struct name without the leading 'F' — used for the .generated.h include and the output file name.</summary>
	public string StructFileBase;
	public string SchemaVersion;
	public string OpCode;
	public string Category;
	/// <summary>Either empty, or "// Description: ...\n" — the trailing newline keeps the blank line between description and #pragma once consistent whether the description is present or not.</summary>
	public string DescriptionComment;

	public List<AnalyticsValidatorFieldDeclaration> Fields;

	/// <summary>Nested custom types referenced (transitively) by this event. Ordered leaf-first so each USTRUCT is declared before any USTRUCT that names it as a UPROPERTY type.</summary>
	public List<AnalyticsValidatorCustomTypeDeclaration> CustomTypes;

	public void IntoProcessDict(Dictionary<string, string> helperDict)
	{
		var propertyDeclarations = string.Join("\n\n", Fields.Select(f => f.RenderProperty(helperDict)));
		var validateBlocks = string.Join("\n\n", Fields.Select(f => f.RenderValidate(helperDict)));
		var serializeLines = string.Join("\n", Fields.Select(f => f.RenderSerialize(helperDict)));
		var deserializeBlocks = string.Join("\n", Fields.Select(f => f.RenderDeserialize(helperDict)));

		var customTypeDeclarations = CustomTypes == null || CustomTypes.Count == 0
			? string.Empty
			: string.Join("\n\n", CustomTypes.Select(ct =>
			{
				ct.IntoProcessDict(helperDict);
				return AnalyticsValidatorCustomTypeDeclaration.CUSTOM_TYPE_TEMPLATE.ProcessReplacement(helperDict);
			})) + "\n\n";

		helperDict.Clear();
		helperDict.Add(nameof(EventName), EventName);
		helperDict.Add(nameof(StructName), StructName);
		helperDict.Add(nameof(StructFileBase), StructFileBase);
		helperDict.Add(nameof(SchemaVersion), SchemaVersion);
		helperDict.Add(nameof(OpCode), OpCode);
		helperDict.Add(nameof(Category), Category);
		helperDict.Add(nameof(DescriptionComment), DescriptionComment);
		helperDict.Add(nameof(propertyDeclarations), propertyDeclarations);
		helperDict.Add(nameof(validateBlocks), validateBlocks);
		helperDict.Add(nameof(serializeLines), serializeLines);
		helperDict.Add(nameof(deserializeBlocks), deserializeBlocks);
		helperDict.Add(nameof(customTypeDeclarations), customTypeDeclarations);
	}

	public const string EVENT_HEADER_TEMPLATE = $@"// Auto-generated by BeamableCLI - DO NOT EDIT
// Source event: ₢{nameof(EventName)}₢
// Schema version: ₢{nameof(SchemaVersion)}₢
₢{nameof(DescriptionComment)}₢
#pragma once

#include ""CoreMinimal.h""
#include ""Analytics/BeamAnalyticsEvent.h""
#include ""Serialization/BeamJsonSerializable.h""
#include ""Runtime/BeamValidators.h""

#include ""₢{nameof(StructFileBase)}₢.generated.h""

₢customTypeDeclarations₢USTRUCT(BlueprintType, meta=(BeamAnalyticsEventBase))
struct ₢{nameof(StructName)}₢ : public FBeamAnalyticsEvent
{{
	GENERATED_BODY()

₢propertyDeclarations₢

	₢{nameof(StructName)}₢() {{ Version = TEXT(""₢{nameof(SchemaVersion)}₢""); }}

	virtual FString GetOpCode()    const override {{ return TEXT(""₢{nameof(OpCode)}₢""); }}
	virtual FString GetCategory()  const override {{ return TEXT(""₢{nameof(Category)}₢""); }}
	virtual FString GetEventName() const override {{ return TEXT(""₢{nameof(EventName)}₢""); }}

	virtual void Validate(FBeamValidationContext& OutContext) const override
	{{
₢validateBlocks₢
	}}

	virtual void BeamSerializeProperties(TUnrealJsonSerializer& Serializer) const override
	{{
		FBeamAnalyticsEvent::BeamSerializeProperties(Serializer);
₢serializeLines₢
	}}

	virtual void BeamSerializeProperties(TUnrealPrettyJsonSerializer& Serializer) const override
	{{
		FBeamAnalyticsEvent::BeamSerializeProperties(Serializer);
₢serializeLines₢
	}}

	virtual void BeamDeserializeProperties(const TSharedPtr<FJsonObject>& Bag) override
	{{
		FBeamAnalyticsEvent::BeamDeserializeProperties(Bag);
₢deserializeBlocks₢
	}}
}};
";
}

/// <summary>
/// Emits one Unreal C++ header per enabled analytics event. Each header defines a USTRUCT
/// deriving from FBeamAnalyticsEvent with overrides for GetOpCode / GetCategory / GetEventName,
/// a Validate(FBeamValidationContext&amp;) override that calls into the BeamValidators namespace
/// library, and the BeamSerialize / BeamDeserialize property overrides that read/write the
/// payload's JSON shape. Consumed by the `beam analytics generate-validators` command.
/// </summary>
public class AnalyticsValidatorGenerator
{
	public List<GeneratedFileDescriptor> Generate(List<AnalyticsEventSchema> events, string apiPrefix = "")
	{
		var dict = new Dictionary<string, string>();
		var files = new List<GeneratedFileDescriptor>();
		foreach (var ev in events)
		{
			if (!ev.Enabled) continue;

			var declaration = BuildEventDeclaration(ev, apiPrefix);
			declaration.IntoProcessDict(dict);
			files.Add(new GeneratedFileDescriptor
			{
				FileName = $"{declaration.StructFileBase}.h",
				Content = AnalyticsValidatorEventDeclaration.EVENT_HEADER_TEMPLATE.ProcessReplacement(dict),
			});
			dict.Clear();
		}
		return files;
	}

	private static AnalyticsValidatorEventDeclaration BuildEventDeclaration(AnalyticsEventSchema ev, string apiPrefix)
	{
		var typePascal = ToPascalCase(ev.Name);
		var structName = $"F{apiPrefix}{typePascal}Event";
		// Nested custom types share the event's "F<prefix><EventPascal>" prefix and then append the
		// PascalCased path of the property they were derived from (e.g. FNewEventTestCustomCustom2).
		var nestedTypeNameRoot = $"F{apiPrefix}{typePascal}";
		var schema = ev.Schema;

		var schemaVersion = string.IsNullOrEmpty(schema.SchemaVersion) ? "1.0.0" : schema.SchemaVersion!;
		var opCode = string.IsNullOrEmpty(schema.BeamOpCode) ? "g.core" : schema.BeamOpCode!;
		var category = schema.BeamCategory ?? "";

		var descriptionComment = string.IsNullOrEmpty(ev.Description)
			? string.Empty
			: $"// Description: {EscapeForComment(ev.Description)}\n";

		var customTypes = new List<AnalyticsValidatorCustomTypeDeclaration>();
		var fields = schema.Properties
			.Select(kvp => MapField(kvp.Key, kvp.Value, schema.Required.Contains(kvp.Key),
				nestedTypeNameRoot, parentSchemaPathExpr: string.Empty, customTypes))
			.ToList();

		return new AnalyticsValidatorEventDeclaration
		{
			EventName = EscapeString(ev.Name),
			StructName = structName,
			StructFileBase = structName.Substring(1),
			SchemaVersion = EscapeString(schemaVersion),
			OpCode = EscapeString(opCode),
			Category = EscapeString(category),
			DescriptionComment = descriptionComment,
			Fields = fields,
			CustomTypes = customTypes,
		};
	}

	/// <summary>
	/// Maps one JSON-Schema property into a field declaration. For "object" properties this also
	/// (a) builds the nested USTRUCT's field list by recursing and (b) appends the nested struct to
	/// <paramref name="customTypes"/> in leaf-first order so the caller can emit them in dependency order.
	/// </summary>
	/// <param name="structNamePrefix">"F&lt;prefix&gt;&lt;EventPascal&gt;[&lt;ancestor path&gt;]" — used to name child custom types so their identifiers are unique within the event.</param>
	/// <param name="parentSchemaPathExpr">Empty at the event root (paths render as literals); otherwise the name of the enclosing Validate's <c>SchemaPathPrefix</c> parameter so leaf paths render as a concatenation.</param>
	private static AnalyticsValidatorFieldDeclaration MapField(string jsonName, JsonSchemaProperty prop, bool required,
		string structNamePrefix, string parentSchemaPathExpr, List<AnalyticsValidatorCustomTypeDeclaration> customTypes)
	{
		var pascal = ToPascalCase(jsonName);
		var schemaPathExpr = string.IsNullOrEmpty(parentSchemaPathExpr)
			? $"TEXT(\"properties.{EscapeString(jsonName)}\")"
			: $"{parentSchemaPathExpr} + TEXT(\".properties.{EscapeString(jsonName)}\")";
		var commentLine = BuildCommentLine(prop.Comment);

		if (prop.Type == "object")
		{
			var nestedStructName = $"{structNamePrefix}{pascal}";
			var nestedProps = prop.Properties ?? new Dictionary<string, JsonSchemaProperty>();
			var nestedRequired = prop.Required ?? new List<string>();

			var nestedFields = nestedProps
				.Select(kvp => MapField(kvp.Key, kvp.Value, nestedRequired.Contains(kvp.Key),
					nestedStructName, parentSchemaPathExpr: "SchemaPathPrefix", customTypes))
				.ToList();

			// Post-order: register the nested type after its children so the emitted list stays
			// dependency-ordered (leaves first → parents last).
			customTypes.Add(new AnalyticsValidatorCustomTypeDeclaration
			{
				StructName = nestedStructName,
				Fields = nestedFields,
			});

			return new AnalyticsValidatorFieldDeclaration
			{
				JsonName = EscapeString(jsonName),
				CppName = pascal,
				CppType = nestedStructName,
				DefaultInit = string.Empty,
				ValidatorCalls = string.Empty,
				DeserializeRhs = string.Empty,
				IsCustomType = true,
				SchemaPathExpr = schemaPathExpr,
				CommentLine = commentLine,
			};
		}

		string cppType, cppName, defaultInit, deserializeRhs;
		var keyLiteral = $"TEXT(\"{EscapeString(pascal)}\")";

		switch (prop.Type)
		{
			case "integer":
				cppType = "int32";
				cppName = pascal;
				defaultInit = " = 0";
				deserializeRhs = $"Bag->GetIntegerField({keyLiteral})";
				break;
			case "number":
				cppType = "float";
				cppName = pascal;
				defaultInit = " = 0.0f";
				deserializeRhs = $"(float)Bag->GetNumberField({keyLiteral})";
				break;
			case "boolean":
				cppType = "bool";
				cppName = "b" + pascal;
				defaultInit = " = false";
				keyLiteral = $"TEXT(\"{EscapeString("b" + pascal)}\")";
				deserializeRhs = $"Bag->GetBoolField({keyLiteral})";
				break;
			case "string":
			default:
				cppType = "FString";
				cppName = pascal;
				defaultInit = "";
				deserializeRhs = $"Bag->GetStringField({keyLiteral})";
				break;
		}

		return new AnalyticsValidatorFieldDeclaration
		{
			JsonName = EscapeString(jsonName),
			CppName = cppName,
			CppType = cppType,
			DefaultInit = defaultInit,
			ValidatorCalls = BuildValidatorCalls(cppName, prop, required),
			DeserializeRhs = deserializeRhs,
			SchemaPathExpr = schemaPathExpr,
			CommentLine = commentLine,
		};
	}

	/// <summary>
	/// Renders a JSON-Schema "$comment" string as a tab-indented Doxygen "///" comment block to sit
	/// immediately above the UPROPERTY line. UHT promotes "///" blocks into ToolTip metadata, so the
	/// same text is what shows in the editor tooltip and what reflection returns at runtime.
	/// Returns empty when the comment is null/empty so the property template renders unchanged.
	/// </summary>
	private static string BuildCommentLine(string? comment)
	{
		if (string.IsNullOrEmpty(comment)) return string.Empty;
		var sb = new StringBuilder();
		foreach (var line in comment.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'))
			sb.Append("\t/// ").Append(line).Append('\n');
		return sb.ToString();
	}

	private static string BuildValidatorCalls(string cppName, JsonSchemaProperty p, bool required)
	{
		var resultVar = $"{cppName}Result";
		var sb = new StringBuilder();
		void AddLine(string s) => sb.Append("\t\t").Append(s).Append("\n");

		switch (p.Type)
		{
			case "integer":
				if (p.Minimum.HasValue)
					AddLine($"BeamValidators::ValidateMinimum({cppName}, {IntLit(p.Minimum.Value)}, {resultVar});");
				if (p.Maximum.HasValue)
					AddLine($"BeamValidators::ValidateMaximum({cppName}, {IntLit(p.Maximum.Value)}, {resultVar});");
				if (p.ExclusiveMinimum.HasValue)
					AddLine($"BeamValidators::ValidateExclusiveMinimum({cppName}, {IntLit(p.ExclusiveMinimum.Value)}, {resultVar});");
				if (p.ExclusiveMaximum.HasValue)
					AddLine($"BeamValidators::ValidateExclusiveMaximum({cppName}, {IntLit(p.ExclusiveMaximum.Value)}, {resultVar});");
				if (p.MultipleOf.HasValue)
					AddLine($"BeamValidators::ValidateMultipleOf({cppName}, {IntLit(p.MultipleOf.Value)}, {resultVar});");
				break;

			case "number":
				if (p.Minimum.HasValue)
					AddLine($"BeamValidators::ValidateMinimum({cppName}, {FloatLit(p.Minimum.Value)}, {resultVar});");
				if (p.Maximum.HasValue)
					AddLine($"BeamValidators::ValidateMaximum({cppName}, {FloatLit(p.Maximum.Value)}, {resultVar});");
				if (p.ExclusiveMinimum.HasValue)
					AddLine($"BeamValidators::ValidateExclusiveMinimum({cppName}, {FloatLit(p.ExclusiveMinimum.Value)}, {resultVar});");
				if (p.ExclusiveMaximum.HasValue)
					AddLine($"BeamValidators::ValidateExclusiveMaximum({cppName}, {FloatLit(p.ExclusiveMaximum.Value)}, {resultVar});");
				if (p.MultipleOf.HasValue)
					AddLine($"BeamValidators::ValidateMultipleOf({cppName}, {FloatLit(p.MultipleOf.Value)}, {resultVar});");
				break;

			case "string":
				if (required)
					AddLine($"BeamValidators::ValidateRequired({cppName}, {resultVar});");
				if (p.MinLength.HasValue)
					AddLine($"BeamValidators::ValidateMinLength({cppName}, {p.MinLength.Value}, {resultVar});");
				if (p.MaxLength.HasValue)
					AddLine($"BeamValidators::ValidateMaxLength({cppName}, {p.MaxLength.Value}, {resultVar});");
				if (!string.IsNullOrEmpty(p.Pattern))
					AddLine($"BeamValidators::ValidatePattern({cppName}, TEXT(\"{EscapeString(p.Pattern!)}\"), {resultVar});");
				break;
		}

		return sb.ToString();
	}

	private static string IntLit(double value) =>
		((int)value).ToString(CultureInfo.InvariantCulture);

	private static string FloatLit(double value)
	{
		var s = value.ToString("R", CultureInfo.InvariantCulture);
		// C++ float literals need a decimal point or exponent before the trailing 'f'.
		if (!s.Contains('.') && !s.Contains('e') && !s.Contains('E')) s += ".0";
		return s + "f";
	}

	private static string EscapeString(string s) =>
		s.Replace("\\", "\\\\").Replace("\"", "\\\"");

	private static string EscapeForComment(string s) =>
		s.Replace("\r", " ").Replace("\n", " ");

	private static string ToPascalCase(string input)
	{
		if (string.IsNullOrEmpty(input)) return input;
		var words = Regex.Split(input, @"(?<=[a-z0-9])(?=[A-Z])|[\s\-_\.]+")
			.Where(w => !string.IsNullOrEmpty(w));
		var sb = new StringBuilder();
		foreach (var w in words)
		{
			sb.Append(char.ToUpperInvariant(w[0]));
			if (w.Length > 1) sb.Append(w.Substring(1));
		}
		return sb.ToString();
	}
}
