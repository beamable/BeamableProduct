using cli.Services.Web.CodeGen;
using cli.Unreal;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace cli.Services.Web.Helpers;

public static class WebSchema
{
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
		var tsClass = new TsClass(schemaRefId).AddModifier(TsModifier.Export);
		var itemsSchemaModules = new List<string>();
		var mapPropertyToModule = new Dictionary<string, List<string>>();
		var props = schema.Properties
			.OrderBy(p => p.Key, StringComparer.Ordinal)
			.ToList();

		var (tsRequiredProps, tsOptionalProps) = CreateProperties(schema, props, mapPropertyToModule);
		var tsAllProps = tsRequiredProps.Concat(tsOptionalProps).ToList();

		var (tsTypeRequired, tsTypeOptional, tsTypeAll) =
			CreateTypeAliases(schemaRefId, tsRequiredProps, tsOptionalProps);

		var tsConstructor = BuildConstructor(tsRequiredProps, tsOptionalProps, tsTypeAll);

		BuildClassAndImports(tsClass, tsImports, tsAllProps, tsConstructor, schema, itemsSchema, itemsSchemaModules,
			enums,
			mapPropertyToModule);

		PrepareFile(tsFile, tsImports, tsRequiredProps, tsOptionalProps, tsTypeRequired, tsTypeOptional, tsTypeAll,
			tsClass);

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

	private static (TsTypeAlias tsTypeRequired, TsTypeAlias tsTypeOptional, TsTypeAlias tsTypeAll)
		CreateTypeAliases(string schemaRefId, List<TsProperty> tsRequiredProps, List<TsProperty> tsOptionalProps)
	{
		// type alias for required properties
		var tsTypeRequired = new TsTypeAlias($"{schemaRefId}Required")
			.SetType(TsType.Object(
				tsRequiredProps.Select(p => (p.Name, p.Type, TsType.PropType.Required)).ToArray()));
		// type alias for optional properties
		var tsTypeOptional = new TsTypeAlias($"{schemaRefId}Optional")
			.SetType(TsType.Object(
				tsOptionalProps.Select(p => (p.Name, p.Type, TsType.PropType.Optional)).ToArray()));
		// type alias for all properties
		var tsTypeAll = new TsTypeAlias($"{schemaRefId}Props").AddModifier(TsModifier.Export);

		switch (tsRequiredProps.Count)
		{
			case > 0 when tsOptionalProps.Count > 0:
				tsTypeAll.SetType(
					TsType.Intersection(TsType.Of(tsTypeRequired.Name), TsType.Of(tsTypeOptional.Name)));
				break;
			case > 0:
				tsTypeAll.SetType(TsType.Of(tsTypeRequired.Name));
				break;
			default:
			{
				tsTypeAll.SetType(tsOptionalProps.Count > 0
					? TsType.Of(tsTypeOptional.Name)
					: TsType.Object());
				break;
			}
		}

		return (tsTypeRequired, tsTypeOptional, tsTypeAll);
	}

	private static TsConstructor BuildConstructor(List<TsProperty> tsRequiredProps, List<TsProperty> tsOptionalProps,
		TsTypeAlias tsTypeAll)
	{
		var init = new TsIdentifier("init");
		var optionals = new TsIdentifier("optionals");
		// create a constructor parameter for the init object
		var tsConstructorParam = new TsConstructorParameter(init.Identifier, TsType.Of(tsTypeAll.Name));
		// destructure the init parameter to get the required properties
		var objDestructure = new TsObjectDestructureStatement(
			tsRequiredProps.Select(p => p.Name).ToArray(), init);

		if (tsOptionalProps.Count > 0)
			objDestructure.WithRest(optionals.Identifier);

		// create assignments for the required properties
		var initRequiredProps = tsRequiredProps
			.Select(p =>
				new TsAssignmentStatement(
					new TsMemberAccessExpression(new TsIdentifier("this"), p.Name),
					new TsIdentifier(p.Name)))
			.Select(TsNode (x) => x)
			.ToArray();
		// create assignments for the optional properties
		var initOptionalProps = new TsExpressionStatement(new TsInvokeExpression(
			new TsIdentifier("Object.assign"),
			new TsIdentifier("this"),
			optionals));
		var tsConstructor = new TsConstructor()
			.AddParameter(tsConstructorParam)
			.AddBody(objDestructure);

		if (tsRequiredProps.Count > 0)
			tsConstructor.AddBody(new TsBlankLine()).AddBody(initRequiredProps);

		if (tsOptionalProps.Count > 0)
			tsConstructor.AddBody(new TsBlankLine()).AddBody(initOptionalProps);

		return tsConstructor;
	}

	private static void BuildClassAndImports(TsClass tsClass, List<TsImport> tsImports,
		List<TsProperty> tsAllProps, TsConstructor tsConstructor, OpenApiSchema schema,
		OpenApiSchema itemsSchema, List<string> itemsSchemaModules,
		List<TsEnum> enums, Dictionary<string, List<string>> mapPropertyToModule)
	{
		if (itemsSchema != null)
		{
			var refId = schema.Items.Reference?.Id;
			if (refId != null)
			{
				tsClass.SetExtends(new TsIdentifier($"Array<{refId}>"));
				itemsSchemaModules.Add(refId);
			}
			else
			{
				var type = OpenApiTsTypeMapper.Map(itemsSchema, ref itemsSchemaModules).Render();
				tsClass.SetExtends(new TsIdentifier($"Array<{type}>"));
			}
		}

		foreach (var tsProp in tsAllProps)
		{
			tsClass.AddProperty(tsProp).SetConstructor(tsConstructor);

			if (!mapPropertyToModule.TryGetValue(tsProp.Name, out var modules))
				continue;

			// add imports for each distinct module
			modules.ForEach(module =>
			{
				// if module is the same name as the class, skip it
				if (module == tsClass.Name)
					return;

				var isEnum = enums.Any(e => e.Name == module);
				var importPath = isEnum ? $"./enums/{module}" : $"./{module}";
				var tsImport = new TsImport(importPath).AddNamedImport(module);

				// Only add the import if one with the same module path has not been added already
				if (tsImports.All(existing => existing.Module != tsImport.Module))
					tsImports.Add(tsImport);
			});
		}

		// add imports for each distinct item type module
		itemsSchemaModules.Distinct().ToList().ForEach(module =>
		{
			var tsImport = new TsImport($"./{module}").AddNamedImport(module);
			tsImports.Add(tsImport);
		});
	}

	private static void PrepareFile(TsFile tsFile, List<TsImport> tsImports,
		List<TsProperty> tsRequiredProps, List<TsProperty> tsOptionalProps,
		TsTypeAlias tsTypeRequired, TsTypeAlias tsTypeOptional, TsTypeAlias tsTypeAll,
		TsClass tsClass)
	{
		if (tsRequiredProps.Count > 0)
			tsFile.AddDeclaration(tsTypeRequired);

		if (tsOptionalProps.Count > 0)
			tsFile.AddDeclaration(tsTypeOptional);

		// add the type alias declaration for all properties and class definition to the file
		tsFile.AddDeclaration(tsTypeAll).AddDeclaration(tsClass);

		// ensure imports are in a consistent alphabetical order
		foreach (var tsImport in tsImports.OrderBy(i => i.Module, StringComparer.Ordinal))
			tsFile.AddImport(tsImport);
	}
}
