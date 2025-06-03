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
		var tsClass = new TsClass(schemaRefId).AddModifier(TsModifier.Export);
		var itemsSchemaModules = new List<string>();
		var mapPropertyToModule = new Dictionary<string, List<string>>();
		var props = schema.Properties
			.OrderBy(p => p.Key, StringComparer.Ordinal)
			.ToList();

		var (tsRequiredProps, tsOptionalProps) = CreateProperties(schema, props, mapPropertyToModule);
		var tsAllProps = tsRequiredProps.Concat(tsOptionalProps).ToList();

		var tsTypeAll = CreateTypeAlias(schemaRefId, tsRequiredProps, tsOptionalProps);

		var tsConstructor = BuildConstructor(tsRequiredProps, tsOptionalProps, tsTypeAll, itemsSchema, tsClass);
		var tsToJsonMethod = BuildToJsonMethod(tsAllProps, tsTypeAll, tsClass);
		var tsFromJsonMethod = BuildFromJsonMethod(tsAllProps, tsTypeAll, tsClass);

		BuildClassAndImports(tsClass, tsImports, tsAllProps, tsConstructor, tsToJsonMethod, tsFromJsonMethod,
			schema, itemsSchema, itemsSchemaModules, enums, mapPropertyToModule);

		PrepareFile(tsFile, tsImports, tsRequiredProps, tsOptionalProps, tsTypeAll, tsClass);

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
		List<TsProperty> tsOptionalProps)
	{
		// type alias for all properties
		var required = tsRequiredProps.Select(p => (p.Name, p.Type, TsType.PropType.Required));
		var optional = tsOptionalProps.Select(p => (p.Name, p.Type, TsType.PropType.Optional));
		var all = required.Concat(optional);
		var tsTypeAll = new TsTypeAlias($"{schemaRefId}Props")
			.AddModifier(TsModifier.Export)
			.SetType(TsType.Object(all.ToArray()));

		return tsTypeAll;
	}

	private static TsConstructor BuildConstructor(List<TsProperty> tsRequiredProps, List<TsProperty> tsOptionalProps,
		TsTypeAlias tsTypeAll, OpenApiSchema itemsSchema, TsClass tsClass)
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
			.AddComment(new TsComment(
				$"Creates an instance of `{tsClass.Name}`.\n" +
				$"@param {{{tsTypeAll.Name}}} {init.Identifier} The initialization properties for the `{tsClass.Name}` instance.",
				TsCommentStyle.Doc));

		if (itemsSchema != null)
			tsConstructor.AddBody(new TsExpressionStatement(new TsInvokeExpression(new TsIdentifier("super"))));

		tsConstructor.AddBody(objDestructure);

		if (tsRequiredProps.Count > 0)
			tsConstructor.AddBody(new TsBlankLine()).AddBody(initRequiredProps);

		if (tsOptionalProps.Count > 0)
			tsConstructor.AddBody(new TsBlankLine()).AddBody(initOptionalProps);

		return tsConstructor;
	}

	private static TsMethod BuildToJsonMethod(List<TsProperty> tsAllProps, TsTypeAlias tsTypeAll, TsClass tsClass)
	{
		var tsToJsonMethod = new TsMethod("toJSON").SetReturnType(TsType.Of(tsTypeAll.Name));
		var returnObject = new TsObjectLiteralExpression();

		foreach (var tsProp in tsAllProps)
			returnObject.AddMember(tsProp.Identifier,
				new TsMemberAccessExpression(new TsIdentifier("this"), tsProp.Name));

		var returnCall = new TsReturnStatement(returnObject);
		tsToJsonMethod.AddBody(returnCall);
		tsToJsonMethod.AddComment(new TsComment(
			$"Plain object of the `{tsClass.Name}` instance.\n" +
			$"@returns {{{tsTypeAll.Name}}} The plain object of type `{tsTypeAll.Name}`.",
			TsCommentStyle.Doc));
		return tsToJsonMethod;
	}

	private static TsMethod BuildFromJsonMethod(List<TsProperty> tsAllProps, TsTypeAlias tsTypeAll, TsClass tsClass)
	{
		var tsFromJsonMethod = new TsMethod("fromJSON")
			.AddModifier(TsModifier.Static)
			.AddParameter(new TsFunctionParameter("obj", TsType.Unknown))
			.SetReturnType(TsType.Of(tsClass.Name));
		var constructorInvocation =
			new TsInvokeExpression(new TsIdentifier($"new {tsClass.Name}"),
				new TsIdentifier($"obj as {tsTypeAll.Name}"));

		var returnCall = new TsReturnStatement(constructorInvocation);
		tsFromJsonMethod.AddBody(returnCall);
		tsFromJsonMethod.AddComment(new TsComment(
			$"Creates an instance of `{tsClass.Name}` from a plain object.\n" +
			$"@param {{{TsType.Unknown.Render()}}} obj The plain object to convert.\n" +
			$"@returns {{{tsClass.Name}}} A new `{tsClass.Name}` instance.",
			TsCommentStyle.Doc));
		return tsFromJsonMethod;
	}

	private static void BuildClassAndImports(TsClass tsClass, List<TsImport> tsImports, List<TsProperty> tsAllProps,
		TsConstructor tsConstructor, TsMethod tsToJsonMethod, TsMethod tsFromJsonMethod, OpenApiSchema schema,
		OpenApiSchema itemsSchema, List<string> itemsSchemaModules, List<TsEnum> enums,
		Dictionary<string, List<string>> mapPropertyToModule)
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
			tsClass.AddProperty(tsProp);

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

		tsClass.SetConstructor(tsConstructor)
			.AddMethod(tsToJsonMethod)
			.AddMethod(tsFromJsonMethod);

		// add imports for each distinct item type module
		itemsSchemaModules.Distinct().ToList().ForEach(module =>
		{
			var tsImport = new TsImport($"./{module}").AddNamedImport(module);
			tsImports.Add(tsImport);
		});
	}

	private static void PrepareFile(TsFile tsFile, List<TsImport> tsImports, List<TsProperty> tsRequiredProps,
		List<TsProperty> tsOptionalProps, TsTypeAlias tsTypeAll, TsClass tsClass)
	{
		// add the type alias declaration for all properties and class definition to the file
		tsFile.AddDeclaration(tsTypeAll).AddDeclaration(tsClass);

		// ensure imports are in a consistent alphabetical order
		foreach (var tsImport in tsImports.OrderBy(i => i.Module, StringComparer.Ordinal))
			tsFile.AddImport(tsImport);
	}
}
