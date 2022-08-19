using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Serialization;
using Microsoft.OpenApi.Models;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Text;

namespace cli;

public class UnitySourceGenerator : SwaggerService.ISourceGenerator
{
	private const string BeamableOptionalNamespace = "Beamable.Common.Content";

	public List<GeneratedFileDescriptors> Generate(IGenerationContext context)
	{
		var res = new List<GeneratedFileDescriptors>();

		foreach (var document in context.Documents)
		{
			res.Add(UnityHelper.GenerateService(document));
		}

		var modelFile = GenerateModel(context);
		res.Add(modelFile);
		return res;
	}


	private GeneratedFileDescriptors GenerateModel(IGenerationContext context)
	{
		var unit = new CodeCompileUnit();
		var root = new CodeNamespace("Beamable.Api.Open.Models");
		root.Imports.Add(new CodeNamespaceImport(BeamableOptionalNamespace));
		unit.Namespaces.Add(root);


		// add all of the model types...
		var nameToTypes = new Dictionary<string, SchemaTypeDeclarations>();
		var nameToOptionalRefCount = new Dictionary<string, int>();

		foreach (var schema in context.OrderedSchemas)
		{
			// build all the types associated with this model...
			var types = UnityHelper.GenerateDeclarations(schema.Name, schema.Schema);
			nameToTypes.Add(schema.Name, types);

			// and always add the root model type...
			root.Types.Add(types.Model);

			// make some counts about how many fields are referencing variant types
			foreach (var member in types.Model.Members)
			{
				if (member is CodeMemberField field)
				{
					if (!nameToOptionalRefCount.TryGetValue(field.Type.BaseType, out var existing))
					{
						existing = 0;
					}
					nameToOptionalRefCount[field.Type.BaseType] = existing + 1;
				}
			}
		}

		void AddTypeIfRequired(CodeTypeDeclaration countCheck, CodeTypeDeclaration decl=null)
		{
			decl ??= countCheck;
			if (countCheck != null && decl != null && nameToOptionalRefCount.TryGetValue(countCheck.Name, out _))
			{
				root.Types.Add(decl);
			}
		}

		foreach (var kvp in nameToTypes)
		{
			AddTypeIfRequired(kvp.Value.Optional);
			AddTypeIfRequired(kvp.Value.OptionalArray);
			AddTypeIfRequired(kvp.Value.OptionalMap);
			AddTypeIfRequired(kvp.Value.OptionalMapArray);
			// AddTypeIfRequired(kvp.Value.OptionalMapArray, kvp.Value.MapArray);
		}

		return new GeneratedFileDescriptors { FileName = "Models.cs", Content = UnityHelper.GenerateCsharp(unit) };
	}

}

public class SchemaTypeDeclarations
{
	public CodeTypeDeclaration Model;
	public CodeTypeDeclaration Optional;
	public CodeTypeDeclaration OptionalArray;
	public CodeTypeDeclaration OptionalMap;
	public CodeTypeDeclaration OptionalMapArray;
	public CodeTypeDeclaration MapArray;
}

public static class UnityHelper
{
	const string PARAM_SERIALIZER = "s";


	public static GeneratedFileDescriptors GenerateService(OpenApiDocument document)
	{
		const string varUrl = "gsUrl"; // gs stands for generated-source
		const string varQuery = "gsQuery";
		const string varReq = "gsReq";
		const string paramIncludeAuth = "includeAuthHeader";

		var unit = new CodeCompileUnit();

		GetTypeNames(document, out var typeName, out var title, out var className);

		var root = new CodeNamespace($"Beamable.Api.Open.{title}");
		root.Imports.Add(new CodeNamespaceImport("Beamable.Api.Open.Models"));
		root.Imports.Add(new CodeNamespaceImport("Beamable.Common"));
		root.Imports.Add(new CodeNamespaceImport("IBeamableRequester = Beamable.Common.Api.IBeamableRequester"));
		root.Imports.Add(new CodeNamespaceImport("Method = Beamable.Common.Api.Method"));

		// root..Add(new CodeSnippetExpression(using IBeamableRequester = Beamable.Common.Api.IBeamableRequester));
		// using IBeamableRequester = Beamable.Common.Api.IBeamableRequester;
		// using Method = Beamable.Common.Api.Method;
		unit.Namespaces.Add(root);


		var type = new CodeTypeDeclaration($"{className}");
		root.Types.Add(type);

		var requesterField = new CodeMemberField(nameof(IBeamableRequester), "_requester")
		{
			Attributes = MemberAttributes.Private
		};
		type.Members.Add(requesterField);

		var cons = new CodeConstructor { Attributes = MemberAttributes.Public };
		cons.Parameters.Add(new CodeParameterDeclarationExpression(nameof(IBeamableRequester), "requester"));
		cons.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_requester"), new CodeArgumentReferenceExpression("requester")));
		type.Members.Add(cons);

		foreach (var path in document.Paths)
		{
			var skipsLeft = document.Info.Title.Contains("object") ? 4 : 3;
			var index = 0;
			for (var i = 0; i < path.Key.Length; i++)
			{
				if (path.Key[i] == '/')
				{
					skipsLeft--;
				}

				if (skipsLeft == 0)
				{
					index = i + 1;
					break;
				}
			}
			var methodName = path.Key.Substring(index);
			if (methodName.Length > 1)
			{
				methodName = char.ToUpper(methodName[0]) + methodName.Substring(1);
			}

			for (var i = methodName.Length - 2; i >= 0; i--)
			{
				if (methodName[i] == '-' || methodName[i] == '/')
				{
					if (i + 2 >= methodName.Length)
					{
						methodName = methodName[..i] + char.ToUpper(methodName[i + 1]);
					}
					else
					{
						methodName = methodName[..i] + char.ToUpper(methodName[i+1]) + methodName[(i+2)..];
					}
				}
			}

			foreach (var operation in path.Value.Operations)
			{
				var httpMethod = Method.GET;

				switch (operation.Key)
				{
					case OperationType.Get:
						httpMethod = Method.GET;
						break;
					case OperationType.Delete:
						httpMethod = Method.DELETE;
						break;
					case OperationType.Post:
						httpMethod = Method.POST;
						break;
					case OperationType.Put:
						httpMethod = Method.PUT;
						break;
				}

				if (!operation.Value.Responses.TryGetValue("200", out var response))
				{
					continue; // TODO: support application/csv for content
				}

				if (!response.Content.TryGetValue("application/json", out var jsonResponse))
				{
					continue;
				}
				var operationName = operation.Key + methodName;
				var returnType = new CodeTypeReference(nameof(Promise));
				var responseType = new CodeTypeReference(jsonResponse.Schema.Reference.Id);
				returnType.TypeArguments.Add(responseType);

				var method = new CodeMemberMethod
				{
					Name = operationName,
					Attributes = MemberAttributes.Public,
					ReturnType = returnType
				};

				var uri = path.Key;
				bool hasReqBody = false;
				method.Parameters.Add(new CodeParameterDeclarationExpression
				{
					Name = paramIncludeAuth, Type = new CodeTypeReference(typeof(bool))
				});

				foreach (var param in operation.Value.Parameters)
				{

					method.Parameters.Add(new CodeParameterDeclarationExpression
					{
						Name = param.Name, Type = new GenSchema(param.Schema).GetTypeReference()
					});
					// uri = uri.Replace($"{{{param.Name}}}", @"""+");
				}
				if (operation.Value.RequestBody?.Content?.TryGetValue("application/json", out var requestMediaType) ?? false)
				{
					hasReqBody = true;
					method.Parameters.Add(new CodeParameterDeclarationExpression
					{
						Name = varReq, Type = new CodeTypeReference(requestMediaType.Schema.Reference.Id)
					});
				}



				method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), varUrl,
					new CodePrimitiveExpression(uri)));
				var queryArgs = new List<string>();
				foreach (var param in operation.Value.Parameters)
				{
					switch (param.In)
					{
						case ParameterLocation.Path:
							var replace = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression(varUrl),
								nameof(string.Replace), new CodePrimitiveExpression($"{{{param.Name}}}"),
								new CodeVariableReferenceExpression(param.Name));

							method.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(varUrl),
								replace));
							break;
						case ParameterLocation.Query:
							queryArgs.Add(param.Name); // these are variables in the method
							break;
					}
				}

				if (queryArgs.Count > 0)
				{
					method.Statements.Add(new CodeCommentStatement("the method takes its inputs as query strings"));
					var query = "?" + string.Join("&", queryArgs.Select(q => $"{q}={{{q}}}"));
					method.Statements.Add(new CodeVariableDeclarationStatement(typeof(string), varQuery,
						new CodeSnippetExpression($"$\"{query}\"")));

					// encode the url
					// method.Statements.Add(
					// 	new CodeCommentStatement("the query may need to be encoded if it contains any special characters"));
					// method.Statements.Add(
					// 	new CodeAssignStatement(new CodeVariableReferenceExpression(varQuery),
					// 		new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_requester"),
					// 			nameof(IBeamableRequester.EscapeURL), new CodeVariableReferenceExpression(varQuery))));

					method.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(varUrl), new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)), nameof(string.Concat), new CodeVariableReferenceExpression(varUrl), new CodeVariableReferenceExpression(varQuery))));
				}


				// make the request itself.
				var requestCommand = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_requester"),
					nameof(IBeamableRequester.Request));
				requestCommand.Method.TypeArguments.Add(responseType);


				requestCommand.Parameters.Add(new CodeTypeReferenceExpression("Method." + httpMethod));
				requestCommand.Parameters.Add(new CodeVariableReferenceExpression(varUrl));

				if (hasReqBody)
				{
					requestCommand.Parameters.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(JsonSerializable)),
						nameof(JsonSerializable.ToJson), new CodeVariableReferenceExpression(varReq)));
				}
				else
				{
					requestCommand.Parameters.Add(new CodeDefaultValueExpression(new CodeTypeReference(typeof(object))));
				}

				requestCommand.Parameters.Add(new CodeArgumentReferenceExpression(paramIncludeAuth)); // TODO: maybe not every method should have this open exposed?

				// if (hasReqBody)
				// {
					requestCommand.Parameters.Add(new CodeMethodReferenceExpression(
						new CodeTypeReferenceExpression(typeof(JsonSerializable)),
						nameof(JsonSerializable.FromJson))
					{
						TypeArguments = { responseType }
					});
				// }

				// TODO: Some parameters come in correctly as Query


				var returnCommand = new CodeMethodReturnStatement(requestCommand);
				method.Statements.Add(new CodeCommentStatement("make the request and return the result"));
				method.Statements.Add(returnCommand);


				type.Members.Add(method);
			}
		}

		return new GeneratedFileDescriptors
		{
			FileName = $"{className}.gs.cs",
			Content = GenerateCsharp(unit)
		};
	}

	static void GetTypeNames(OpenApiDocument document, out string typeName, out string title, out string className)
	{
		var words = document.Info.Title.Split(' ');
		typeName = "";
		title = "";
		className = "";
		for (var j = 0; j < words.Length; j++)
		{
			var casedWord = "";
			var word = words[j];
			for (var i = 0; i < word.Length; i++)
			{
				var letter = i == 0
					? char.ToUpper(word[i])
					: char.ToLower(word[i]);
				casedWord += letter;
			}

			if (j == 0)
			{
				title = casedWord;
			}
			else if (j == 1)
			{
				typeName = casedWord;
			}

			className += casedWord + "Api";
		}
	}


	public static SchemaTypeDeclarations GenerateDeclarations(string name, OpenApiSchema schema)
	{
		return new SchemaTypeDeclarations
		{
			Model = GenerateModelDecl(name, schema),
			Optional = GenerateOptionalDecl(name, schema),
			OptionalArray = GenerateOptionalArrayDecl(name, schema),
			OptionalMap = GenerateOptionalMapDecl(name, schema),
			OptionalMapArray = GenerateOptionalMapArrayDecl(name, schema),
			// MapArray = GenerateMapArrayDecl(name, schema)
		};
	}

	public static CodeTypeDeclaration GenerateOptionalMapDecl(string name, OpenApiSchema schema)
	{
		switch (schema.Type, schema.Format)
		{
			case ("array", _):
			case ("object", _):
				var className = SanitizeClassName(name);
				var optionalClassName = $"OptionalMapOf{className}";

				var type = new CodeTypeDeclaration();
				type.CustomAttributes.Add(
					new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));

				type.Name = optionalClassName;
				var baseType = new CodeTypeReference(typeof(OptionalSerializableDictionaryStringToSomething<>));
				var genericType = new CodeTypeReference(className);
				baseType.TypeArguments.Add(genericType);
				type.BaseTypes.Add(baseType);
				return type;
		}

		return null;
	}

	public static CodeTypeDeclaration GenerateMapArrayDecl(string name, OpenApiSchema schema)
	{
		switch (schema.Type, schema.Format)
		{
			case ("array", _):
			case ("object", _):
				var className = SanitizeClassName(name);
				var optionalClassName = $"MapOf{className}Array";
				var type = new CodeTypeDeclaration();
				type.CustomAttributes.Add(
					new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));

				type.Name = optionalClassName;
				var baseType = new CodeTypeReference(typeof(SerializableDictionaryStringToSomething<>));
				var genericType = new CodeTypeReference(className);
				genericType.ArrayRank = 1;
				baseType.TypeArguments.Add(genericType);
				type.BaseTypes.Add(baseType);
				return type;
		}
		return null;
	}


	public static CodeTypeDeclaration GenerateOptionalMapArrayDecl(string name, OpenApiSchema schema)
	{
		switch (schema.Type, schema.Format)
		{
			case ("array", _):
			case ("object", _):
				var className = SanitizeClassName(name);
				var optionalClassName = $"OptionalMapOf{className}Array";
				var type = new CodeTypeDeclaration();
				type.CustomAttributes.Add(
					new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));

				type.Name = optionalClassName;
				var baseType = new CodeTypeReference(typeof(OptionalSerializableDictionaryStringToSomething<>));
				var genericType = new CodeTypeReference(className);
				genericType.ArrayRank = 1;
				baseType.TypeArguments.Add(genericType);
				type.BaseTypes.Add(baseType);
				return type;
		}
		return null;
	}

	public static CodeTypeDeclaration GenerateOptionalArrayDecl(string name, OpenApiSchema schema)
	{
		switch (schema.Type, schema.Format)
		{
			case ("array", _):
			case ("object", _):
				var className = SanitizeClassName(name);
				var optionalClassName = $"Optional{className}Array";
				var type = new CodeTypeDeclaration();
				type.CustomAttributes.Add(
					new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));

				type.Name = optionalClassName;
				var baseType = new CodeTypeReference(typeof(OptionalArray<>));
				var genericType = new CodeTypeReference(className);
				baseType.TypeArguments.Add(genericType);
				type.BaseTypes.Add(baseType);

				// add a default constructor...
				var defaultCons = new CodeConstructor();
				defaultCons.Attributes = MemberAttributes.Public;
				type.Members.Add(defaultCons);

				// add a constructor that accepts a value...
				var typedCons = new CodeConstructor();
				var argType = new CodeTypeReference(className);
				argType.ArrayRank = 1;
				typedCons.Parameters.Add(new CodeParameterDeclarationExpression(argType, "value"));
				typedCons.Attributes = MemberAttributes.Public;
				typedCons.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(nameof(Optional.HasValue)),
					new CodePrimitiveExpression(true)));
				typedCons.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(nameof(Optional<int>.Value)),
					new CodeVariableReferenceExpression("value")));
				type.Members.Add(typedCons);

				return type;
		}
		return null;
	}

	public static CodeTypeDeclaration GenerateOptionalDecl(string name, OpenApiSchema schema)
	{
		switch (schema.Type, schema.Format)
		{
			case ("array", _):
			case ("object", _):
				var type = new CodeTypeDeclaration($"Optional{SanitizeClassName(name)}");

				// make sure the model is serializable
				type.CustomAttributes.Add(
					new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));

				var baseType = new CodeTypeReference(typeof(Optional<>));
				type.BaseTypes.Add(baseType);
				var genericType = new CodeTypeReference(SanitizeClassName(name));
				baseType.TypeArguments.Add(genericType);

				// add a default constructor...
				var defaultCons = new CodeConstructor();
				defaultCons.Attributes = MemberAttributes.Public;
				type.Members.Add(defaultCons);

				// add a constructor that accepts a value...
				var typedCons = new CodeConstructor();
				typedCons.Parameters.Add(new CodeParameterDeclarationExpression(genericType, "value"));
				typedCons.Attributes = MemberAttributes.Public;
				typedCons.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(nameof(Optional.HasValue)),
					new CodePrimitiveExpression(true)));
				typedCons.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(nameof(Optional<int>.Value)),
					new CodeVariableReferenceExpression("value")));
				type.Members.Add(typedCons);

				return type;
		}
		// we don't need anything, because the primitive types are already included in the code base.
		return null;

	}

	public static CodeTypeDeclaration GenerateModelDecl(string name, OpenApiSchema schema)
	{
		var type = new CodeTypeDeclaration(SanitizeClassName(name));

		// make sure the model is serializable
		type.CustomAttributes.Add(
			new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));

		// add the serialization interface
		type.BaseTypes.Add(new CodeTypeReference(typeof(JsonSerializable.ISerializable)));

		// add the implementation of the serialization interface as a method...
		var serializeMethod = new CodeMemberMethod();
		serializeMethod.Name = nameof(JsonSerializable.IStreamSerializer.Serialize);
		serializeMethod.Parameters.Add(
			new CodeParameterDeclarationExpression(typeof(JsonSerializable.IStreamSerializer), PARAM_SERIALIZER));
		serializeMethod.Attributes = MemberAttributes.Public;
		type.Members.Add(serializeMethod);

		foreach (var property in schema.Properties)
		{
			// construct some primitive information about this field.
			var fieldApiName = property.Key;
			var fieldSchema = property.Value;
			var isRequired = schema.Required.Contains(fieldApiName);

			// add the field to the model type.
			var field = GenerateField(fieldApiName, fieldSchema, isRequired);
			type.Members.Add(field);

			// add the field's serialization to the serializeMethod.
			var serializationStatement = GenerateSerializationStatement(fieldSchema, field, fieldApiName, isRequired);
			if (serializationStatement != null)
			{
				serializeMethod.Statements.Add(serializationStatement);
			}
		}

		return type;
	}

	public static CodeMemberField GenerateField(string fieldName, OpenApiSchema schema, bool isRequired)
	{
		var fieldSchema = new GenSchema(schema);
		var field = new CodeMemberField();

		field.Name = SanitizeFieldName(fieldName);
		field.Type = isRequired ? fieldSchema.GetTypeReference() : fieldSchema.GetOptionalTypeReference();
		field.Attributes = MemberAttributes.Public;

		// the init expression is only required when its an object of some sort...
		if (!isRequired || schema.Type == "object")
		{
			field.InitExpression = new CodeObjectCreateExpression(field.Type);
		}

		return field;
	}

	public static string SanitizeClassName(string schemaName)
	{
		var className = SanitizeFieldName(schemaName.Replace(" ", ""));
		return className; // TODO: add upercasing and all that..
	}

	private static string SanitizeFieldName(string propKey)
	{
		string AppendKey(string str) => str + "Key";
		var protectedKeys = new HashSet<string>
		{
			// TODO: add all other C# keywords...
			"do", "as", "if", "for", "int", "long", "params", "string", "var", "protected", "void", "while", "public", "private", "class", "interface", "const"
		};

		return protectedKeys.Contains(propKey)
			? AppendKey(propKey)
			: propKey;
	}

	public static CodeStatement GenerateSerializationStatement(OpenApiSchema schema, CodeMemberField field, string apiFieldName, bool isRequired)
	{
		var invokeSerializationMethod = GetSerializationMethodReference(schema);
		if (invokeSerializationMethod == null) return null;
		var varRef = new CodeSnippetExpression($"ref {field.Name}");
		var referenceField = new CodeVariableReferenceExpression(field.Name);
		var serializeName = new CodePrimitiveExpression(apiFieldName);

		if (!isRequired)
		{
			var hasValue = new CodeFieldReferenceExpression(referenceField, nameof(Optional.HasValue));
			var hasKey =
				new CodeMethodInvokeExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER), nameof(JsonSerializable.IStreamSerializer.HasKey), serializeName);
			var valueIsNotNull = new CodeBinaryOperatorExpression(referenceField,
				CodeBinaryOperatorType.IdentityInequality,
				new CodeDefaultValueExpression(field.Type));

			var expr = new CodeBinaryOperatorExpression(hasKey, CodeBinaryOperatorType.BooleanOr,
				new CodeBinaryOperatorExpression(valueIsNotNull, CodeBinaryOperatorType.BooleanAnd, hasValue));

			var serializationStatement =
				new CodeMethodInvokeExpression(invokeSerializationMethod, serializeName,
					new CodeFieldReferenceExpression(varRef, nameof(Optional<int>.Value)));
			var conditionStatement =
				new CodeConditionStatement(expr,
					// call the serialize
					new CodeExpressionStatement(serializationStatement),

					// and set the HasValue to true
					new CodeAssignStatement(new CodeFieldReferenceExpression(referenceField, nameof(Optional.HasValue)), new CodePrimitiveExpression(true)));
			return conditionStatement;

		}
		else
		{
			return new CodeExpressionStatement(new CodeMethodInvokeExpression(invokeSerializationMethod, serializeName,
				varRef));
		}
	}

	private static CodeMethodReferenceExpression GetSerializationMethodReference(OpenApiSchema schema)
	{
		switch (schema.Type)
		{

			case "object" when schema.AdditionalPropertiesAllowed:
				var method = new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
					nameof(JsonSerializable.IStreamSerializer.SerializeDictionary));

				var elemType = new GenSchema(schema.AdditionalProperties).GetTypeReference();

				if (schema.AdditionalProperties?.Type == "array")
				{
					var keyType = new CodeTypeReference(typeof(SerializableDictionaryStringToSomething<>));
					keyType.TypeArguments.Add(elemType);
					method.TypeArguments.Add(keyType);
				}
				else
				{
					var keyType = new GenSchema(schema).GetTypeReference();
					method.TypeArguments.Add(keyType);
				}

				method.TypeArguments.Add(elemType);

				return method;
			case "array":
				return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
					nameof(JsonSerializable.IStreamSerializer.SerializeArray));
			default:


				if (string.IsNullOrEmpty(schema.Type) || schema.Type == "object") return null;

				return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
					nameof(JsonSerializable.IStreamSerializer.Serialize));
		}
	}

	public static string GenerateCsharp(CodeCompileUnit unit)
	{
		const int COUNT_OF_AUTO_GENERATED_MESSAGE_TEXT = 357;
		CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
		CodeGeneratorOptions options = new CodeGeneratorOptions { BracingStyle = "C", BlankLinesBetweenMembers = false};
		var sb = new StringBuilder();
		using var sourceWriter = new StringWriter(sb);

		provider.GenerateCodeFromCompileUnit(
			unit, sourceWriter, options);
		sourceWriter.Flush();
		var source = sb.ToString();
		return source.Substring(COUNT_OF_AUTO_GENERATED_MESSAGE_TEXT); // magic nu
	}
}

public class GenCodeTypeReference : CodeTypeReference
{
	public string DisplayName { get; set; }

	public string UpperDisplayName => char.ToUpper(DisplayName[0]) + DisplayName.Substring(1);

	public GenCodeTypeReference()
	{

	}

	public GenCodeTypeReference(Type runtimeType) : base(runtimeType)
	{
		DisplayName = GetDisplayName(runtimeType);
	}

	public GenCodeTypeReference(string typeName) : base(typeName)
	{
		DisplayName = typeName;
	}

	public GenCodeTypeReference(GenCodeTypeReference type, int arrayRank) : base(type, arrayRank)
	{
		DisplayName = type.DisplayName + "Array";
	}


	private static Dictionary<Type, string> _typeToDisplayName = new Dictionary<Type, string>
	{
		[typeof(int)] = "int",
		[typeof(long)] = "long",
		[typeof(short)] = "short",
		[typeof(byte)] = "byte",
		[typeof(double)] = "double",
		[typeof(float)] = "float",
		[typeof(bool)] = "bool",
		[typeof(string)] = "string",
		[typeof(Guid)] = "Guid",
	};
	public static string GetDisplayName(Type runtimeType)
	{
		if (_typeToDisplayName.TryGetValue(runtimeType, out var name))
		{
			return name;
		}

		return runtimeType.Name;
	}
}

public class GenSchema
{
	public OpenApiSchema Schema;

	public GenSchema(OpenApiSchema schema)
	{
		Schema = schema;
	}

	public GenCodeTypeReference GetOptionalTypeReference()
	{
		var innerType = GetTypeReference();
		var clazzName = $"Optional{innerType.UpperDisplayName}";
		return new GenCodeTypeReference(clazzName);
	}

	public GenCodeTypeReference GetTypeReference()
	{
		switch (Schema?.Type, Schema?.Format, Schema?.Reference?.Id)
		{
			case ("array", _, _) when Schema.Items.Reference == null:
				var genElem = new GenSchema(Schema.Items);
				var elemType = genElem.GetTypeReference();
				return new GenCodeTypeReference(elemType, 1);
			case ("array", _, _) when Schema.Items.Reference != null:
				var referenceType = new GenCodeTypeReference(Schema.Items.Reference.Id);
				return new GenCodeTypeReference(referenceType, 1);
			case var (_, _, referenceId) when !string.IsNullOrEmpty(referenceId):
				return new GenCodeTypeReference(referenceId);
			case ("object", _, _) when Schema.Reference == null && Schema.AdditionalPropertiesAllowed:
				var genValues = new GenSchema(Schema.AdditionalProperties);
				var genType = genValues.GetTypeReference();
				var mapTypeName = $"MapOf{genType.UpperDisplayName}";
				var type = new GenCodeTypeReference(mapTypeName);
				return type;
			case ("object", _, _) when Schema.Reference == null && !Schema.AdditionalPropertiesAllowed:
				throw new Exception("Cannot build a reference to a schema that is just an object...");
			case ("number", "float", _):
				return new GenCodeTypeReference(typeof(float));
			case ("number", "double", _):
			case ("number", _, _):
				return new GenCodeTypeReference(typeof(double));
			case ("boolean", _, _):
				return new GenCodeTypeReference(typeof(bool));
			case ("string", "uuid", _):
				return new GenCodeTypeReference(typeof(Guid));
			case ("string", "byte", _):
				return new GenCodeTypeReference(typeof(byte));
			case ("System.String", _, _):
			case ("string", _, _):
				return new GenCodeTypeReference(typeof(string));
			case ("integer", "int16", _):
				return new GenCodeTypeReference(typeof(short));
			case ("integer", "int32", _):
				return new GenCodeTypeReference(typeof(int));
			case ("integer", "int64", _):
				return new GenCodeTypeReference(typeof(long));
			case ("integer", _, _):
				return new GenCodeTypeReference(typeof(int));
			default:
				return new GenCodeTypeReference(typeof(object));
				// throw new Exception("Unknown code reference");
		}
	}
}
