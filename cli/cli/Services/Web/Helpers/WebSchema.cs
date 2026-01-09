using cli.Services.Web.CodeGen;
using cli.Unreal;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace cli.Services.Web.Helpers;

public static class WebSchema
{
	public static readonly TsFile SCHEMA_BARREL_FILE = new("index");

	public static void BuildSchemaBarrel(GeneratedFileDescriptor fileDescriptor, bool isEnum)
	{
		var fileName = Path.GetFileNameWithoutExtension(fileDescriptor.FileName);
		var tsExport = new TsExport(isEnum ? $"./enums/{fileName}" : $"./{fileName}");
		SCHEMA_BARREL_FILE.AddExport(tsExport);
	}

	public static GeneratedFileDescriptor GenerateSchemaEnum(NamedOpenApiSchema namedSchema, List<TsEnum> enums)
	{
		var schema = namedSchema.Schema;
		// skip if schema is not an enum
		if (schema.Enum == null || schema.Enum.Count == 0)
			return null;

		var schemaRefId = namedSchema.ReferenceId.ToString();
		var tsFile = new TsFile(schemaRefId);
		var tsEnum = new TsEnum(schemaRefId).AddModifier(TsModifier.Export);
		enums.Add(tsEnum);

		foreach (var item in schema.Enum)
		{
			// only string fields are allowed
			if (item is not OpenApiString enumMember)
				continue;

			var enumMemberName = enumMember.Value;
			var safeEnumMemberName = StringHelper.ToSafeIdentifier(enumMemberName);
			var tsEnumMember = new TsEnumMember(safeEnumMemberName.Capitalize(), safeEnumMemberName);
			tsEnum.AddMember(tsEnumMember);
		}

		tsFile.AddDeclaration(tsEnum);

		return new GeneratedFileDescriptor
		{
			FileName = $"schemas/enums/{tsFile.FileName}.ts", Content = tsFile.Render()
		};
	}

	public static GeneratedFileDescriptor GenerateSchema(NamedOpenApiSchema namedSchema, List<TsEnum> enums)
	{
		var schema = namedSchema.Schema;
		// skip if schema is an enum
		if (schema.Enum?.Count > 0)
			return null;

		var schemaRefId = namedSchema.ReferenceId.ToString();
		var itemsSchema = schema.Items;
		var tsFile = new TsFile(schemaRefId);
		var tsImports = new List<TsImport>();
		var itemsSchemaModules = new List<string>();
		var mapPropertyToModule = new Dictionary<string, List<string>>();
		var props = schema.Properties
			.OrderBy(p => p.Key, StringComparer.Ordinal)
			.ToList();

		var (tsRequiredProps, tsOptionalProps) = CreateProperties(schema, props, mapPropertyToModule);
		var tsAllProps = tsRequiredProps.Concat(tsOptionalProps).ToList();

		var tsTypeAlias = CreateTypeAlias(schemaRefId, tsRequiredProps, tsOptionalProps, schema, itemsSchema,
			itemsSchemaModules);

		BuildImports(tsTypeAlias, tsImports, tsAllProps, itemsSchemaModules, enums, mapPropertyToModule);

		PrepareFile(tsFile, tsImports, tsTypeAlias);

		return new GeneratedFileDescriptor { FileName = $"schemas/{tsFile.FileName}.ts", Content = tsFile.Render() };
	}

	private static (List<TsProperty> tsRequiredProps, List<TsProperty> tsOptionalProps) CreateProperties(
		OpenApiSchema schema, List<KeyValuePair<string, OpenApiSchema>> props,
		Dictionary<string, List<string>> mapPropertyToModule)
	{
		var tsRequiredProps = new List<TsProperty>();
		var tsOptionalProps = new List<TsProperty>();

		foreach ((string pKey, OpenApiSchema pValue) in props)
		{
			var modules = new List<string>();
			var isRequired = schema.Required.Contains(pKey);
			var tsType = OpenApiTsTypeMapper.Map(pValue, ref modules);
			var tsProp = new TsProperty(pKey, tsType);

			if (isRequired)
				tsRequiredProps.Add(tsProp);
			else
			{
				tsProp.AsOptional();
				tsOptionalProps.Add(tsProp);
			}

			if (modules.Count > 0)
				mapPropertyToModule.TryAdd(pKey, modules);
		}

		return (tsRequiredProps, tsOptionalProps);
	}

	private static TsTypeAlias CreateTypeAlias(string schemaRefId, List<TsProperty> tsRequiredProps,
		List<TsProperty> tsOptionalProps, OpenApiSchema schema, OpenApiSchema itemsSchema,
		List<string> itemsSchemaModules)
	{
		// type alias for all properties
		TsTypeAlias tsTypeAlias;
		var required = tsRequiredProps.Select(p => (p.Name, p.Type, TsType.PropType.Required));
		var optional = tsOptionalProps.Select(p => (p.Name, p.Type, TsType.PropType.Optional));
		var all = required.Concat(optional);

		if (itemsSchema != null)
		{
			var refId = schema.Items.Reference?.Id;
			if (refId != null)
			{
				tsTypeAlias = new TsTypeAlias(schemaRefId).AddModifier(TsModifier.Export)
					.SetType(TsType.Intersection(TsType.ArrayOf(TsType.Of(refId)), TsType.Object(all.ToArray())));
				itemsSchemaModules.Add(refId);
			}
			else
			{
				var type = OpenApiTsTypeMapper.Map(itemsSchema, ref itemsSchemaModules).Render();
				tsTypeAlias = new TsTypeAlias(schemaRefId).AddModifier(TsModifier.Export)
					.SetType(TsType.Intersection(TsType.ArrayOf(TsType.Of(type)), TsType.Object(all.ToArray())));
			}
		}
		else
		{
			tsTypeAlias = new TsTypeAlias(schemaRefId)
				.AddModifier(TsModifier.Export)
				.SetType(TsType.Object(all.ToArray()));
		}

		return tsTypeAlias;
	}

	private static void BuildImports(TsTypeAlias tsTypeAlias, List<TsImport> tsImports, List<TsProperty> tsAllProps,
		List<string> itemsSchemaModules, List<TsEnum> enums, Dictionary<string, List<string>> mapPropertyToModule)
	{
		foreach (var tsProp in tsAllProps)
		{
			if (!mapPropertyToModule.TryGetValue(tsProp.Name, out var modules))
				continue;

			// add imports for each distinct module
			modules.ForEach(module =>
			{
				// if module is the same name as the type alias, skip it
				if (module == tsTypeAlias.Name)
					return;

				var isEnum = enums.Any(e => e.Name == module);
				var importPath = isEnum ? $"./enums/{module}" : $"./{module}";
				var tsImport = new TsImport(importPath, typeImportOnly: true).AddNamedImport(module);

				// Only add the import if one with the same module path has not been added already
				if (tsImports.All(existing => existing.Module != tsImport.Module))
					tsImports.Add(tsImport);
			});
		}

		// add imports for each distinct item type module
		itemsSchemaModules.Distinct().ToList().ForEach(module =>
		{
			var tsImport = new TsImport($"./{module}", typeImportOnly: true).AddNamedImport(module);
			tsImports.Add(tsImport);
		});
	}

	private static void PrepareFile(TsFile tsFile, List<TsImport> tsImports, TsTypeAlias tsTypeAll)
	{
		// add the type alias declaration for the schema
		tsFile.AddDeclaration(tsTypeAll);

		// ensure imports are in a consistent alphabetical order
		foreach (var tsImport in tsImports.OrderBy(i => i.Module, StringComparer.Ordinal))
			tsFile.AddImport(tsImport);
	}
}
