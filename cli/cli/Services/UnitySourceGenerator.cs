using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Serialization;
using Microsoft.OpenApi.Models;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace cli;


public class UnitySourceGenerator : SwaggerService.ISourceGenerator
{
	// TODO: use DI to get config settings into the service.

	const string PARAM_SERIALIZER = "s";


	public List<GeneratedFileDescriptors> Generate(IGenerationContext context)
	{
		var files = new List<GeneratedFileDescriptors>();

		// foreach (var document in context.Documents)
		// {
		// 	GetTypeNames(document, out var typeName, out var title, out var className);
		//
		// 	files.Add(new GeneratedFileDescriptors
		// 	{
		// 		FileName = $"{className}.cs", Content = GenerateService(document)
		// 	});
		// }
		var modelString = GenerateModels(context);
		files.Add(new GeneratedFileDescriptors { FileName = "Models.cs", Content = modelString });


		return files;
	}

	public string GenerateCsharp(CodeCompileUnit unit)
	{
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
	public const int COUNT_OF_AUTO_GENERATED_MESSAGE_TEXT = 357;


	private static CodeSchemaTypeReference GetTypeName(string name, string format)
	{
		var fieldType = new CodeSchemaTypeReference("object");
		var type = SwaggerType.Object;
		switch (name, format)
		{
			case ("string", "uuid"):
				fieldType = new CodeSchemaTypeReference(typeof(Guid));
				type = SwaggerType.UUID;
				break;
			case ("string", "byte"):
				fieldType = new CodeSchemaTypeReference(typeof(byte));
				type = SwaggerType.Byte;
				break;
			case ("System.String", _):
			case ("string", _):
				fieldType = new CodeSchemaTypeReference(typeof(string));
				type = SwaggerType.String;
				break;
			case ("boolean", _):
				fieldType = new CodeSchemaTypeReference(typeof(bool));
				type = SwaggerType.Boolean;
				break;
			case ("integer", "int16"):
				fieldType = new CodeSchemaTypeReference(typeof(short));
				type = SwaggerType.Short;
				break;
			case ("integer", "int64"):
				fieldType = new CodeSchemaTypeReference(typeof(long));
				type = SwaggerType.Long;
				break;
			case ("integer", "int32"):
			case ("integer", _):
				fieldType = new CodeSchemaTypeReference(typeof(int));
				type = SwaggerType.Int;
				break;
			case ("number", "float"):
				fieldType = new CodeSchemaTypeReference(typeof(float));
				type = SwaggerType.Float;
				break;
			case ("number", "double"):
			case ("number", _):
				fieldType = new CodeSchemaTypeReference(typeof(double));
				type = SwaggerType.Double;
				break;
			case ("object", _):
				fieldType = new CodeSchemaTypeReference(typeof(object));
				type= SwaggerType.Object;
				break;
			default:
				fieldType = new CodeSchemaTypeReference(name);
				type = SwaggerType.Object;
				break;
		}

		fieldType.SwaggerType = type;
		return fieldType;
	}

	private CodeSchemaTypeReference GetSchemaTypeName(OpenApiSchema schema)
	{
		var fieldType = new CodeSchemaTypeReference("object");
		var type = SwaggerType.Object;
		switch (schema.Type)
		{
			case "array" when schema.Items.Reference == null:
				// (fieldType.ArrayElementType, fieldType.SwaggerType) = GetTypeName(schema.Items.Type, schema.Items.Format);
				var elemType = GetTypeName(schema.Items.Type, schema.Items.Format);
				fieldType.ArrayRank = 1;
				fieldType.ArrayElementType = elemType;
				fieldType.SwaggerType = SwaggerType.Array;
				break;
			case "array" when schema.Items.Reference != null:
				// fieldType = new CodeSchemaTypeReference($"List<{schema.Items.Reference.Id}>");
				// type = SwaggerType.List;
				var listType = GetTypeName(schema.Items.Reference.Id, "");
				fieldType.ArrayRank = 1;
				fieldType.ArrayElementType = listType;
				fieldType.SwaggerType = SwaggerType.Array;
				break;
			case "object" when schema.Reference == null && schema.AdditionalPropertiesAllowed:

				var subType = GetSchemaTypeName(schema.AdditionalProperties);
				fieldType = GetMapClassType(subType);
				// fieldType.TypeArguments.Add(GetTypeName("string", ""));

				// var subProps = GetSchemaTypeName(schema.AdditionalProperties);
				// var valueType = GetTypeName(subProps.GetSwaggerTypeName(), schema.Format);
				// fieldType.TypeArguments.Add(subType);

				fieldType.SwaggerType = SwaggerType.Map;
				break;
			case "object" when schema.Reference != null:
				fieldType = new CodeSchemaTypeReference(schema.Reference.Id);
				break;
			default:
				fieldType = GetTypeName(schema.Type, schema.Format);
				break;
		}

		return fieldType;
	}

	public enum SwaggerType
	{
		Object,
		Array,
		List,
		Map,
		String,
		Long,
		Boolean,
		Float,
		Double,
		Int,
		Short,
		Byte,
		UUID
	}

	public string GenerateModels(IGenerationContext context)
	{
		var unit = new CodeCompileUnit();

		var root = new CodeNamespace("Beamable.Api.Models");
		unit.Namespaces.Add(root);
		root.Imports.Add(new CodeNamespaceImport("Beamable.Common.Content"));
		root.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

		// generate models
		foreach (var schema in context.OrderedSchemas)
		{
			var className = GetClassName(schema.Name);
			var type = new CodeTypeDeclaration(className);
			type.TypeAttributes = TypeAttributes.Serializable | TypeAttributes.Public | TypeAttributes.Class;
			type.CustomAttributes.Add(
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));
			type.BaseTypes.Add(new CodeTypeReference(typeof(JsonSerializable.ISerializable)));
			root.Types.Add(type);

			// generate the serialization method for this model...
			var serializeMethod = new CodeMemberMethod();
			serializeMethod.Name = nameof(JsonSerializable.IStreamSerializer.Serialize);
			serializeMethod.Parameters.Add(
				new CodeParameterDeclarationExpression(typeof(JsonSerializable.IStreamSerializer), PARAM_SERIALIZER));
			serializeMethod.Attributes = MemberAttributes.Public;
			type.Members.Add(serializeMethod);


			foreach (var prop in schema.Schema.Properties)
			{
				if (string.IsNullOrEmpty(prop.Value.Type)) continue;

				var fieldType = GetSchemaTypeName(prop.Value);
				var fieldName = GetSafeFieldName(prop.Key);

				var isRequired = schema.Schema.Required.Contains(fieldName);
				if (!isRequired)
				{
					fieldType = GetOptionalClassType(fieldType);
				}

				var field = new CodeMemberField(fieldType, fieldName);
				field.Attributes = MemberAttributes.Public;
				switch (fieldType.SwaggerType)
				{
					case SwaggerType.String when isRequired:
						break;
					case SwaggerType.Array:
						break;
					default:
						field.InitExpression = new CodeObjectCreateExpression(fieldType);
						break;
				}

				// var isList = fieldType.
				var invokeSerializationMethod = GetSerializationMethodReference(fieldType);
				var varRef = new CodeSnippetExpression($"ref {fieldName}");
				var referenceField = new CodeVariableReferenceExpression(fieldName);
				var serializeName = new CodePrimitiveExpression(prop.Key);

				if (!isRequired)
				{

					var hasValue = new CodeFieldReferenceExpression(referenceField, nameof(Optional.HasValue));
					var hasKey =
						new CodeMethodInvokeExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER), nameof(JsonSerializable.IStreamSerializer.HasKey), serializeName);
					var valueIsNotNull = new CodeBinaryOperatorExpression(referenceField,
						CodeBinaryOperatorType.IdentityInequality,
						new CodeDefaultValueExpression(fieldType));

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
					serializeMethod.Statements.Add(conditionStatement);

				}
				else
				{
					serializeMethod.Statements.Add(new CodeMethodInvokeExpression(invokeSerializationMethod, serializeName, varRef));

				}


				type.Members.Add(field);
			}

		}

		// generate optional types...
		foreach (var schema in context.OrderedSchemas)
		{
			var optionalType = CreateOptionalType(schema);
			var optionalArrayType = CreateOptionalArrayType(schema);
			root.Types.Add(optionalType);
			root.Types.Add(optionalArrayType);
		}

		return GenerateCsharp(unit);
	}

	private static string GetSafeFieldName(string propKey)
	{
		// need to dodge keywords of the C# lang
		switch (propKey)
		{
			case "params":
				return "gsParams";
			default:
				return propKey;
		}
	}

	public static string GetClassName(string schemaName)
	{
		var className = schemaName.Replace(" ", "");
		return className; // TODO: add upercasing and all that..
	}

	public static string GetOptionalClassName(string schemaName)
	{
		var className = GetClassName(schemaName);
		return $"Optional{className}";
	}

	public static string GetOptionalArrayClassName(string schemaName)
	{
		var className = GetClassName(schemaName);
		return $"Optional{className}Array";
	}

	public static CodeSchemaTypeReference GetMapClassType(CodeSchemaTypeReference codeType)
	{
		var clone = new CodeSchemaTypeReference();
		clone.SwaggerType = codeType.SwaggerType;

		switch (clone.SwaggerType)
		{
			case SwaggerType.Long:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToLong));
				break;
			case SwaggerType.Boolean:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToBool));
				break;
			case SwaggerType.Byte:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToByte));
				break;
			case SwaggerType.Double:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToDouble));
				break;
			case SwaggerType.Float:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToFloat));
				break;
			case SwaggerType.Int:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToInt));
				break;
			case SwaggerType.Short:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToShort));
				break;
			case SwaggerType.UUID:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToGuid));
				break;
			case SwaggerType.Array:
				clone = new CodeSchemaTypeReference("SerializableDictionaryStringTo");
				break;
			case SwaggerType.Object:
				clone = new CodeSchemaTypeReference($"SerializableDictionaryStringTo{GetClassName(codeType.BaseType)}");
				break;
			default:
				clone = new CodeSchemaTypeReference(typeof(SerializableDictionaryStringToSomething<>));
				clone.TypeArguments.Add(codeType);
				break;
		}

		return clone;
	}

	public static CodeSchemaTypeReference GetOptionalClassType(CodeSchemaTypeReference codeType)
	{
		// need to clone the type...
		var clone = new CodeSchemaTypeReference();
		clone.SwaggerType = codeType.SwaggerType;

		switch (codeType.SwaggerType)
		{
			case SwaggerType.Array when codeType.ArraySchemaType != null:
				var subType = GetTypeName(codeType.BaseType, "");
				// var optionalSubType = GetOptionalClassType(codeType.ArraySchemaType);
				clone.BaseType = $"Optional{subType.GetSwaggerTypeName()}Array";
				clone.ArrayRank = 0;
				break;
			case SwaggerType.Map when codeType.KeySchemaType != null && codeType.ValueSchemaType != null:
				clone.BaseType = "OptionalDictionaryStringToObject";
				break;
			case SwaggerType.Double:
				clone.BaseType = nameof(OptionalDouble);
				break;
			case SwaggerType.Float:
				clone.BaseType = nameof(OptionalFloat);
				break;
			case SwaggerType.Byte:
				clone.BaseType = nameof(OptionalByte);
				break;
			case SwaggerType.Boolean:
				clone.BaseType = nameof(OptionalBoolean);
				break;
			case SwaggerType.Int:
				clone.BaseType = nameof(OptionalInt);
				break;
			case SwaggerType.Short:
				clone.BaseType = nameof(OptionalShort);
				break;
			case SwaggerType.UUID:
				clone.BaseType = nameof(OptionalGuid);
				break;
			case SwaggerType.String:
				clone.BaseType = nameof(OptionalString);
				break;
			case SwaggerType.Long:
				clone.BaseType = nameof(OptionalLong);
				break;
			default:
				clone.BaseType = GetOptionalClassName(codeType.BaseType);
				break;
		}
		return clone;
	}


	public static CodeTypeDeclaration CreateOptionalType(NamedOpenApiSchema schema)
	{
		var className = GetClassName(schema.Name);
		var optionalClassName = GetOptionalClassName(schema.Name);
		var type = new CodeTypeDeclaration();
		type.CustomAttributes.Add(
			new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));

		type.Name = optionalClassName;
		var baseType = new CodeTypeReference(typeof(Optional<>));
		var genericType = new CodeTypeReference(className);
		baseType.TypeArguments.Add(genericType);
		type.BaseTypes.Add(baseType);

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

	public static CodeTypeDeclaration CreateOptionalArrayType(NamedOpenApiSchema schema)
	{
		var className = GetClassName(schema.Name);
		var optionalClassName = GetOptionalArrayClassName(schema.Name);
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


	private static CodeMethodReferenceExpression GetSerializationMethodReference(CodeSchemaTypeReference schemaTypeReference)
	{
		if (schemaTypeReference.SwaggerType == SwaggerType.Array)
		{
			return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
				nameof(JsonSerializable.IStreamSerializer.SerializeArray));
		}
		else if (schemaTypeReference.SwaggerType == SwaggerType.Map)
		{
			return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
				nameof(JsonSerializable.IStreamSerializer.SerializeDictionary));
		}
		else
		{
			return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
				nameof(JsonSerializable.IStreamSerializer.Serialize));
		}


		// switch (schemaTypeReference.SwaggerType)
		// {
		// 	case SwaggerType.List:
		// 		return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
		// 			nameof(JsonSerializable.IStreamSerializer.SerializeList));
		// 	case SwaggerType.Array:
		// 		return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
		// 			nameof(JsonSerializable.IStreamSerializer.SerializeArray));
		// 	case SwaggerType.Map:
		// 		return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
		// 			nameof(JsonSerializable.IStreamSerializer.SerializeDictionary));
		// 	default:
		// 		return new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression(PARAM_SERIALIZER),
		// 			nameof(JsonSerializable.IStreamSerializer.Serialize));
		// }
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


	public string GenerateService(OpenApiDocument document)
	{
		const string varUrl = "gsUrl"; // gs stands for generated-source
		const string varQuery = "gsQuery";
		const string varReq = "gsReq";
		const string paramIncludeAuth = "includeAuthHeader";

		var unit = new CodeCompileUnit();

		GetTypeNames(document, out var typeName, out var title, out var className);

		var root = new CodeNamespace($"Beamable.Api.{title}");
		root.Imports.Add(new CodeNamespaceImport("Beamable.Api.Models"));
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
						Name = param.Name, Type = GetSchemaTypeName(param.Schema)
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

		return GenerateCsharp(unit);
	}

	public class CodeSchemaTypeReference : CodeTypeReference
	{
		public bool IsArray => ArrayRank > 0;
		public SwaggerType SwaggerType = SwaggerType.Object;

		public CodeSchemaTypeReference ArraySchemaType => ArrayElementType as CodeSchemaTypeReference;


		public CodeSchemaTypeReference KeySchemaType =>
			TypeArguments.Count == 2 ? TypeArguments[0] as CodeSchemaTypeReference : null;

		public CodeSchemaTypeReference ValueSchemaType =>
			TypeArguments.Count == 2 ? TypeArguments[0] as CodeSchemaTypeReference : null;

		public CodeSchemaTypeReference(){}

		public CodeSchemaTypeReference(string name) : base(name)
		{

		}

		public CodeSchemaTypeReference(Type type) : base(type)
		{
			// RuntimeType = type;
		}

		public string GetSwaggerTypeName()
		{
			switch (SwaggerType)
			{
				case SwaggerType.String:
					return typeof(String).Name;
					break;
				default:
				case SwaggerType.Object:
					return BaseType;
			}
		}
	}
}
