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
		// return new List<GeneratedFileDescriptors>
		// {
		// files.Add(new GeneratedFileDescriptors { FileName = "Models.cs", Content = modelString });
		// };
		foreach (var document in context.Documents)
		{
			GetTypeNames(document, out var typeName, out var title, out var className);

			files.Add(new GeneratedFileDescriptors
			{
				FileName = $"{className}.cs", Content = GenerateService(document)
			});
		}

		return files;
		//
		// return "";
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


	private CodeTypeReference GetTypeName(string name)
	{
		var fieldType = new CodeTypeReference("object");
		switch (name)
		{
			case "string":
				fieldType = new CodeTypeReference(typeof(string));
				break;
			case "boolean":
				fieldType = new CodeTypeReference(typeof(bool));
				break;
			case "integer":
				// TODO: we need to know WHAT type of number to make this. We'll need to pull this from a config setting, because
				// we can't just guess that'll it be int or long or double or whatever :/
				fieldType = new CodeTypeReference(typeof(int));
				break;
			default:
				fieldType = new CodeTypeReference(name);
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
				fieldType = GetTypeName(schema.Items.Type);
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
				fieldType = GetTypeName(schema.Type);
				break;
		}

		return fieldType;
	}

	public string GenerateModels(IGenerationContext context)
	{
		var unit = new CodeCompileUnit();

		var root = new CodeNamespace("Beamable.Api.Models");
		unit.Namespaces.Add(root);

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
				var fieldType = GetSchemaTypeName(prop.Value);
				// var fieldType = new CodeTypeReference("object");
				// switch (prop.Value.Type)
				// {
				// 	case "array":
				// 		if (prop.Value == null || prop.Value.Items == null || prop.Value.Items.Reference == null)
				// 		{
				//
				// 		}
				// 		fieldType = new CodeTypeReference($"List<{prop.Value.Items.Reference.Id}>");
				// 		break;
				//
				// 	case "object" when prop.Value.Reference == null && prop.Value.AdditionalPropertiesAllowed:
				// 		fieldType = new CodeTypeReference(typeof(Dictionary<string, object>));
				// 		break;
				// 	case "object" when prop.Value.Reference != null:
				// 		fieldType = new CodeTypeReference(prop.Value.Reference.Id);
				// 		break;
				// 	case "string":
				// 		fieldType = new CodeTypeReference(typeof(string));
				// 		break;
				// 	case "boolean":
				// 		fieldType = new CodeTypeReference(typeof(bool));
				// 		break;
				// 	case "integer":
				// 		// TODO: we need to know WHAT type of number to make this. We'll need to pull this from a config setting, because
				// 		// we can't just guess that'll it be int or long or double or whatever :/
				// 		fieldType = new CodeTypeReference(typeof(int));
				// 		break;
				// 	default:
				// 		fieldType = new CodeTypeReference(prop.Value.Type);
				// 		break;
				// }


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
		var unit = new CodeCompileUnit();

		GetTypeNames(document, out var typeName, out var title, out var className);

		var root = new CodeNamespace($"Beamable.Api.{title}");
		root.Imports.Add(new CodeNamespaceImport("Beamable.Api.Models"));
		unit.Namespaces.Add(root);


		var type = new CodeTypeDeclaration($"{className}");
		root.Types.Add(type);

		var requesterField = new CodeMemberField(typeof(IBeamableRequester), "_requester")
		{
			Attributes = MemberAttributes.Private
		};
		type.Members.Add(requesterField);

		var cons = new CodeConstructor { Attributes = MemberAttributes.Public };
		cons.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IBeamableRequester), "requester"));
		cons.Statements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "_requester"), new CodeArgumentReferenceExpression("requester")));
		type.Members.Add(cons);

		foreach (var path in document.Paths)
		{
			var methodName = path.Key.Substring(path.Key.LastIndexOf('/') + 1);
			methodName = char.ToUpper(methodName[0]) + methodName.Substring(1);
			foreach (var operation in path.Value.Operations)
			{
				if (!operation.Value.Responses.TryGetValue("200", out var response))
				{
					continue; // TODO: support application/csv for content
				}

				if (!response.Content.TryGetValue("application/json", out var jsonResponse))
				{
					continue;
				}
				var operationName = operation.Key + methodName;
				var returnType = new CodeTypeReference(typeof(Promise));
				returnType.TypeArguments.Add(jsonResponse.Schema.Reference.Id);
				var method = new CodeMemberMethod
				{
					Name = operationName,
					Attributes = MemberAttributes.Public,
					ReturnType =returnType
				};
				type.Members.Add(method);
			}
		}

		return GenerateCsharp(unit);
	}
}
