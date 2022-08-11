using Beamable.Common;
using Beamable.Common.Api;
using Microsoft.OpenApi.Models;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace cli;


public class UnitySourceGenerator : SwaggerService.ISourceGenerator
{
	// TODO: use DI to get config settings into the service.

	public List<GeneratedFileDescriptors> Generate(IGenerationContext context)
	{
		var files = new List<GeneratedFileDescriptors>();

		var modelString = GenerateModels(context);
		files.Add(new GeneratedFileDescriptors { FileName = "Models.cs", Content = modelString });
		foreach (var document in context.Documents)
		{
			GetTypeNames(document, out var typeName, out var title, out var className);

			files.Add(new GeneratedFileDescriptors
			{
				FileName = $"{className}.cs", Content = GenerateService(document)
			});
		}

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


	private CodeTypeReference GetTypeName(string name, string format)
	{
		var fieldType = new CodeTypeReference("object");
		switch (name, format)
		{
			case ("string", "uuid"):
				fieldType = new CodeTypeReference(typeof(Guid));
				break;
			case ("string", "byte"):
				fieldType = new CodeTypeReference(typeof(byte));
				break;
			case ("string", _):
				fieldType = new CodeTypeReference(typeof(string));
				break;
			case ("boolean", _):
				fieldType = new CodeTypeReference(typeof(bool));
				break;
			case ("integer", "int16"):
				fieldType = new CodeTypeReference(typeof(short));
				break;
			case ("integer", "int64"):
				fieldType = new CodeTypeReference(typeof(long));
				break;
			case ("integer", "int32"):
			case ("integer", _):
				fieldType = new CodeTypeReference(typeof(int));
				break;
			case ("number", "float"):
				fieldType = new CodeTypeReference(typeof(float));
				break;
			case ("number", "double"):
			case ("number", _):
				fieldType = new CodeTypeReference(typeof(double));
				break;
			default:
				fieldType = new CodeTypeReference(typeof(object));
				break;
		}

		return fieldType;
	}

	private CodeTypeReference GetSchemaTypeName(OpenApiSchema schema)
	{
		var fieldType = new CodeTypeReference("object");
		switch (schema.Type)
		{
			case "array" when schema.Items.Reference == null:
				fieldType.ArrayElementType = GetTypeName(schema.Items.Type, schema.Items.Format);
				fieldType.ArrayRank = 1;
				break;
			case "array" when schema.Items.Reference != null:
				fieldType = new CodeTypeReference($"List<{schema.Items.Reference.Id}>");
				break;
			case "object" when schema.Reference == null && schema.AdditionalPropertiesAllowed:
				fieldType = new CodeTypeReference(typeof(Dictionary<string, object>));
				break;
			case "object" when schema.Reference != null:
				fieldType = new CodeTypeReference(schema.Reference.Id);
				break;
			default:
				fieldType = GetTypeName(schema.Type, schema.Format);
				break;
		}

		return fieldType;
	}

	public string GenerateModels(IGenerationContext context)
	{
		var unit = new CodeCompileUnit();

		var root = new CodeNamespace("Beamable.Api.Models");
		unit.Namespaces.Add(root);
		root.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));

		foreach (var schema in context.OrderedSchemas)
		{
			var className = schema.Name;

			if (string.IsNullOrEmpty(className))
			{
				continue; // TODO this should be pulled out into the context generation
			}

			// className = schema.Document.Info.Title + className;
			className = className.Replace(" ", "");

			var type = new CodeTypeDeclaration(className);
			type.TypeAttributes = TypeAttributes.Serializable | TypeAttributes.Public | TypeAttributes.Class;
			type.CustomAttributes.Add(
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(System.SerializableAttribute))));
			root.Types.Add(type);

			foreach (var prop in schema.Schema.Properties)
			{
				if (string.IsNullOrEmpty(prop.Value.Type)) continue;
				var fieldType = GetSchemaTypeName(prop.Value);
				var field = new CodeMemberField(fieldType, prop.Key);
				field.Attributes = MemberAttributes.Public;
				type.Members.Add(field);
			}

		}

		return GenerateCsharp(unit);
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
					ReturnType =returnType
				};

				var uri = path.Key;
				bool hasReqBody = false;
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
					method.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(varUrl), new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeof(string)), nameof(string.Concat), new CodeVariableReferenceExpression(varUrl), new CodeVariableReferenceExpression(varQuery))));
				}

				// encode the url
				method.Statements.Add(
					new CodeCommentStatement("the url may need to be encoded if it contains any special characters"));
				method.Statements.Add(
					new CodeAssignStatement(new CodeVariableReferenceExpression(varUrl),
					new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_requester"),
					nameof(IBeamableRequester.EscapeURL), new CodeVariableReferenceExpression(varUrl))));


				// make the request itself.
				var requestCommand = new CodeMethodInvokeExpression(new CodeVariableReferenceExpression("_requester"),
					nameof(IBeamableRequester.Request));
				requestCommand.Method.TypeArguments.Add(responseType);


				requestCommand.Parameters.Add(new CodeTypeReferenceExpression("Method." + httpMethod));
				requestCommand.Parameters.Add(new CodeVariableReferenceExpression(varUrl));

				if (hasReqBody)
				{
					requestCommand.Parameters.Add(new CodeVariableReferenceExpression(varReq));
				}
				// TODO: Some parameters come in correctly as Query


				var returnCommand = new CodeMethodReturnStatement(requestCommand);
				method.Statements.Add(new CodeCommentStatement("make the request and return the result"));
				method.Statements.Add(returnCommand);


				type.Members.Add(method);
			}
		}

		return GenerateCsharp(unit);
	}
}
