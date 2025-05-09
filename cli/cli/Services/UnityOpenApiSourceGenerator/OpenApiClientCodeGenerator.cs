using Beamable.Common;
using Beamable.Common.Dependencies;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Text;

namespace Beamable.Server.Generator
{
	public class OpenApiClientCodeGenerator
	{
		private const string MICROSERVICE_CLIENTS_TYPE_NAME = "MicroserviceClients";
		private const string MICROSERVICE_CLIENT_TYPE_NAME = "MicroserviceClient";
		
		private const string COMPONENT_INTERFACE_KEY = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_INTERFACE_KEY;
		private const string COMPONENT_FEDERATION_CLASS_NAME_KEY = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_FEDERATION_CLASS_NAME_KEY;
		private const string COMPONENT_IS_HIDDEN_METHOD_KEY = Constants.Features.Services.MICROSERVICE_IS_HIDDEN_METHOD_KEY;

		private readonly string _serviceNamespaceClassName;
		private readonly string _serviceName;
		private readonly CodeCompileUnit _targetUnit;

		private string TargetClassName => $"{_serviceName}Client";
		private string TargetParameterClassName => GetTargetParameterClassName(_serviceName);
		private string TargetExtensionClassName => $"ExtensionsFor{_serviceName}Client";

		private const string PARAMETER_STRING = "Parameter";
		private const string CLIENT_NAMESPACE = "Beamable.Server.Clients";
		private const string LIST_BASE_PREFIX = "System.Collections.Generic.List";

		private string ExtensionClassToFind => $"public class {TargetExtensionClassName}";
		private string ExtensionClassToReplace => $"public static class {TargetExtensionClassName}";

		private static string GetTargetParameterClassName(string serviceName) =>
			$"MicroserviceParameters{serviceName}Client";

		/// <summary>
		/// Define the class.
		/// </summary>
		/// <param name="serviceObject"></param>
		public OpenApiClientCodeGenerator(OpenApiDocument document)
		{
			_targetUnit = new CodeCompileUnit();

			_serviceNamespaceClassName =
				document.Extensions.TryGetValue(Constants.Features.Services.MICROSERVICE_CLASS_TYPE_KEY,
					out var classTypeName) && classTypeName is OpenApiString classTypeString
					? classTypeString.Value
					: document.Info.Title;

			_serviceName = document.Info.Title;
			
			CodeNamespace newNamespace = GenerateNamespace();
			
			var targetClass = new CodeTypeDeclaration(TargetClassName)
			{
				IsClass = true, TypeAttributes = TypeAttributes.Public | TypeAttributes.Sealed
			};
			targetClass.BaseTypes.Add(new CodeTypeReference(MICROSERVICE_CLIENT_TYPE_NAME));
			
			targetClass.Members.Add(new CodeConstructor()
			{
				Attributes = MemberAttributes.Public,
				Parameters =
				{
					new CodeParameterDeclarationExpression(new CodeTypeReference("BeamContext"),
						"context = null")
				},
				BaseConstructorArgs = { new CodeArgumentReferenceExpression("context") }
			});

			var parameterClass = new CodeTypeDeclaration(TargetParameterClassName);
			parameterClass.IsClass = true;
			parameterClass.CustomAttributes.Add(
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
			parameterClass.TypeAttributes =
				TypeAttributes.NotPublic | TypeAttributes.Sealed;

			targetClass.Comments.Add(
				new CodeCommentStatement($"<summary> A generated client for <see cref=\"{_serviceNamespaceClassName}\"/> </summary",
					true));

			var extensionClass = new CodeTypeDeclaration(TargetExtensionClassName);
			extensionClass.IsClass = true;
			extensionClass.TypeAttributes = TypeAttributes.Public;
			extensionClass.CustomAttributes = new CodeAttributeDeclarationCollection
			{
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(BeamContextSystemAttribute)))
			};
			
			var registrationMethod = new CodeMemberMethod
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static
			};
			registrationMethod.CustomAttributes.Add(
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(RegisterBeamableDependenciesAttribute))));
			registrationMethod.Name = "RegisterService";
			registrationMethod.Parameters.Add(
				new CodeParameterDeclarationExpression(typeof(IDependencyBuilder), "builder"));
			registrationMethod.Statements.Add(new CodeMethodInvokeExpression
			{
				Method = new CodeMethodReferenceExpression(
					new CodeArgumentReferenceExpression("builder"),
					nameof(IDependencyBuilder.AddScoped), new CodeTypeReference(TargetClassName))
			});

			var extensionMethod = new CodeMemberMethod()
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static
			};
			extensionMethod.Name = _serviceName;
			extensionMethod.Parameters.Add(
				new CodeParameterDeclarationExpression($"this Beamable.Server.{MICROSERVICE_CLIENTS_TYPE_NAME}",
					"clients"));
			extensionMethod.Statements.Add(new CodeMethodReturnStatement(new CodeMethodInvokeExpression
			{
				Method = new CodeMethodReferenceExpression(
					new CodeArgumentReferenceExpression("clients"),
					"GetClient", new CodeTypeReference(TargetClassName))
			}));
			extensionMethod.ReturnType = new CodeTypeReference(TargetClassName);

			extensionClass.Members.Add(registrationMethod);
			extensionClass.Members.Add(extensionMethod);
			

			AddServiceNameInterface(targetClass, document);
			AddSpecificFederationsInterfaces(targetClass, document);
			AddMethods(targetClass, parameterClass, document);
			
			newNamespace.Types.Add(targetClass);
			newNamespace.Types.Add(parameterClass);
			newNamespace.Types.Add(extensionClass);
			_targetUnit.Namespaces.Add(newNamespace);
		}

		private static CodeNamespace GenerateNamespace()
		{
			CodeNamespace newNamespace = new CodeNamespace(CLIENT_NAMESPACE);
			newNamespace.Imports.Add(new CodeNamespaceImport("System"));
			newNamespace.Imports.Add(new CodeNamespaceImport("Beamable.Platform.SDK"));
			newNamespace.Imports.Add(new CodeNamespaceImport("Beamable.Server"));
			return newNamespace;
		}

		private void AddMethods(CodeTypeDeclaration targetClass, CodeTypeDeclaration parameterClass, OpenApiDocument document)
		{
			Dictionary<string, string> addedParameters = new();
			foreach ((string path, OpenApiPathItem item) in document.Paths)
			{

				if (item.Extensions.TryGetValue(COMPONENT_IS_HIDDEN_METHOD_KEY, out var isHidden) &&
				    isHidden is OpenApiBoolean { Value: true })
				{
					continue;
				}
				
				string methodName = path.Replace("/", string.Empty);
				foreach ((OperationType _, OpenApiOperation operation) in item.Operations)
				{
					if (operation.Tags.All(tag => tag.Name != Constants.Features.Services.MICROSERVICE_CALLABLE_METHOD_TAG))
					{
						continue;
					}

					if (!operation.Responses.TryGetValue("200", out var response) ||
					    !response.Content.TryGetValue("application/json", out var responseType))
					{
						continue;
					}

					if (!operation.RequestBody.Content.TryGetValue("application/json", out var requestType))
					{
						continue;
					}

					var responseSchema = responseType.Schema.GetEffective(document);
					var requestSchema = requestType.Schema.GetEffective(document);
					var parameters = requestSchema.Properties.ToDictionary(itemKey => itemKey.Key,
						itemValue => itemValue.Value.GetEffective(document));
					AddCallableMethod(targetClass, methodName, parameters, responseSchema, addedParameters);
				}
			}

			foreach ((string paramType, string paramName) in addedParameters)
			{
				AddParameterClass(parameterClass, paramType, paramName);
			}
		}

		void AddSpecificFederationsInterfaces(CodeTypeDeclaration targetClass, OpenApiDocument document)
		{
			const string federatedComponent = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_KEY;
			if (document.Extensions.TryGetValue(federatedComponent, out var config) && 
			    config is OpenApiArray federationsArray)
			{
				foreach (IOpenApiAny item in federationsArray)
				{
					var federationObject = (OpenApiObject)item;
					

					if (!federationObject.TryGetValue(COMPONENT_FEDERATION_CLASS_NAME_KEY, out var federationClass) ||
					    federationClass is not OpenApiString federationClassString ||
					    !federationObject.TryGetValue(COMPONENT_INTERFACE_KEY, out var fedInterface) ||
					    fedInterface is not OpenApiString fedInterfaceString)
					{
						continue;
					}

					bool isFederatedLogin = fedInterfaceString.Value.Equals(typeof(IFederatedLogin<>).FullName);
					bool isFederatedInventory = fedInterfaceString.Value.Equals(typeof(IFederatedInventory<>).FullName);
					
					if (isFederatedLogin || isFederatedInventory)
					{
						CodeTypeReference baseReference = isFederatedLogin
							? new CodeTypeReference(typeof(ISupportsFederatedLogin<>))
							: new CodeTypeReference(typeof(ISupportsFederatedInventory<>));
						var codeTypeReference = new CodeTypeReference(federationClassString.Value);
						baseReference.TypeArguments.Add(codeTypeReference);
						targetClass.BaseTypes.Add(baseReference);
					}
				}
			}
			
		}

		void AddServiceNameInterface(CodeTypeDeclaration targetClass, OpenApiDocument document)
		{
			targetClass.BaseTypes.Add(new CodeTypeReference(typeof(IHaveServiceName)));


			var nameProperty = new CodeMemberProperty();
			nameProperty.Type = new CodeTypeReference(typeof(string));
			nameProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			nameProperty.Name = nameof(IHaveServiceName.ServiceName);
			nameProperty.HasGet = true;
			nameProperty.HasSet = false;

			var returnStatement = new CodeMethodReturnStatement(new CodePrimitiveExpression(document.Info.Title));
			nameProperty.GetStatements.Add(returnStatement);
			targetClass.Members.Add(nameProperty);
		}

		void AddParameterClass(CodeTypeDeclaration parameterClass, string parameterType, string parameterName)
		{
			var wrapper = new CodeTypeDeclaration(parameterName);
			wrapper.IsClass = true;
			wrapper.CustomAttributes.Add(
				new CodeAttributeDeclaration(new CodeTypeReference(typeof(SerializableAttribute))));
			wrapper.TypeAttributes = TypeAttributes.NotPublic | TypeAttributes.Sealed;
			wrapper.BaseTypes.Add(
				new CodeTypeReference("MicroserviceClientDataWrapper", new CodeTypeReference(parameterType)));

			parameterClass.Members.Add(wrapper);
		}

		private void AddCallableMethod(CodeTypeDeclaration targetClass, string methodName,
			IDictionary<string, OpenApiSchema> parameters, OpenApiSchema returnMethodType,
			Dictionary<string, string> paramsTypeName)
		{
			// Declaring a ToString method
			CodeMemberMethod genMethod = new CodeMemberMethod();
			genMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			genMethod.Name = methodName;

			// the input arguments...
			var serializationFields = new Dictionary<string, object>();
			foreach ((string key, OpenApiSchema schema) in parameters)
			{
				var paramType = GetParsedType(schema);
				var paramName = key;
				paramsTypeName.TryAdd(paramType, GetParameterClassName(schema));
				genMethod.Parameters.Add(new CodeParameterDeclarationExpression(paramType, paramName));

				var rawFieldName = $"raw_{paramName}";
				var declare = new CodeParameterDeclarationExpression(typeof(object), rawFieldName);
				serializationFields.Add(paramName, rawFieldName);

				var assignment = new CodeAssignStatement(declare, new CodeArgumentReferenceExpression(paramName));
				genMethod.Statements.Add(assignment);
			}


			// add some docstrings to the method.
			genMethod.Comments.Add(new CodeCommentStatement("<summary>", true));
			genMethod.Comments.Add(new CodeCommentStatement(
				$"Call the {methodName} method on the {_serviceName} microservice", true));

			genMethod.Comments.Add(
				new CodeCommentStatement($"<see cref=\"{_serviceNamespaceClassName}.{methodName}\"/>", true));
			genMethod.Comments.Add(new CodeCommentStatement("</summary>", true));


			var genericPromiseType = $"Beamable.Common.Promise<{GetParsedType(returnMethodType)}>";
			genMethod.ReturnType = new CodeTypeReference(genericPromiseType);

			// Declaring a return statement for method ToString.
			var returnStatement = new CodeMethodReturnStatement();

			string servicePath = methodName;

			// servicePath = $"micro_{Descriptor.Name}/{servicePath}"; // micro is the feature name, so we don't accidentally stop out an existing service.

			const string serializedFieldVariableName = "serializedFields";

			// Create a dictionary and add key-value pairs
			var dictionaryType = new CodeTypeReference(typeof(Dictionary<string, object>));
			var dictionaryDeclaration = new CodeVariableDeclarationStatement(
				dictionaryType, serializedFieldVariableName,
				new CodeObjectCreateExpression(dictionaryType)
			);
			genMethod.Statements.Add(dictionaryDeclaration);

			foreach (KeyValuePair<string, object> kvp in serializationFields)
			{
				// Add key-value pairs to the dictionary
				genMethod.Statements.Add(
					new CodeMethodInvokeExpression(
						new CodeVariableReferenceExpression(serializedFieldVariableName),
						"Add",
						new CodePrimitiveExpression(kvp.Key),
						new CodeVariableReferenceExpression((string)kvp.Value)
					)
				);
			}

			var requestInvokeExpr = new CodeMethodInvokeExpression(
				new CodeMethodReferenceExpression(
					new CodeThisReferenceExpression(),
					"Request",
					new CodeTypeReference[] { new(GetParsedType(returnMethodType)) }),
				new CodeExpression[]
				{
					// first argument is the service name
					new CodePrimitiveExpression(_serviceName),

					// second argument is the path.
					new CodePrimitiveExpression(servicePath),

					// third argument is an array of pre-serialized json structures
					new CodeVariableReferenceExpression(serializedFieldVariableName),
				});

			returnStatement.Expression = requestInvokeExpr;


			//returnStatement.ex
			genMethod.Statements.Add(returnStatement);
			targetClass.Members.Add(genMethod);
		}

		private string GetParsedType(OpenApiSchema schema)
		{
			string schemaType = schema.Type;
			
			string nameBase = schema.Nullable && schemaType != "string" && schemaType != "object" && schemaType != "array" ? "System.Nullable<{0}>" : "{0}";
			string typeValue = (schemaType, schema.Format) switch
			{
				("integer", "int32") => "int",
				("integer", "int64") => "long",
				("integer", _) => "int",
				("number", "float") => "float",
				("number", "double") => "double",
				("number", "decimal") => "decimal",
				("number", _) => "double",
				("string", "date") => "DateTime",
				("string", "date-time") => "DateTime",
				("string", "uuid") => "Guid",
				("string", "byte") => "byte",
				("string", _) => "string",
				("boolean", _) => "bool",
				("array", _) => schema.Items.Format == "byte" ? $"{GetParsedType(schema.Items)}[]" : $"{LIST_BASE_PREFIX}<{GetParsedType(schema.Items)}>",
				_ => schema.Reference?.Id ?? "object"
			};
			return string.Format(nameBase,typeValue);
		}

		private string GetParameterClassName(OpenApiSchema schema, bool addParameterString = true)
		{
			string schemaType = schema.Type;
			string nameBase = schema.Nullable && schemaType != "string" && schemaType != "object" ? "System_Nullable_{0}" : "{0}";
			string parameterClassName = ((schemaType, schema.Format) switch
			{
				("integer", "int32") => typeof(int).GetTypeString(),
				("integer", "int64") => typeof(long).GetTypeString(),
				("integer", _) => typeof(int).GetTypeString(),
				("number", "float") => typeof(float).GetTypeString(),
				("number", "double") => typeof(double).GetTypeString(),
				("number", "decimal") => typeof(decimal).GetTypeString(),
				("number", _) => typeof(decimal).GetTypeString(),
				("string", "date") => typeof(DateTime).GetTypeString(),
				("string", "date-time") => typeof(DateTime).GetTypeString(),
				("string", "uuid") => typeof(Guid).GetTypeString(),
				("string", "byte") => typeof(byte).GetTypeString(),
				("string", _) => typeof(string).GetTypeString(),
				("boolean", _) => typeof(bool).GetTypeString(),
				("array", _) => schema.Items.Format == "byte" ? $"System_Array_{GetParameterClassName(schema.Items, false)}" : $"{LIST_BASE_PREFIX}_{GetParameterClassName(schema.Items, false)}",
				_ => schema.Reference?.Id ?? $"Object"
			}).Replace(".", "_");
			string prefix = addParameterString ? PARAMETER_STRING : "";
			return $"{prefix}{string.Format(nameBase, parameterClassName)}";
		}

		private string GetCSharpCodeString()
		{
			CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.BracingStyle = "C";
			var sb = new StringBuilder();
			using (var sourceWriter = new StringWriter(sb))
			{
				provider.GenerateCodeFromCompileUnit(
					_targetUnit, sourceWriter, options);
				sourceWriter.Flush();
				var source = sb.ToString();
				source = source.Replace(ExtensionClassToFind, ExtensionClassToReplace);
				return source;
			}
		}

		public void GenerateCSharpCode(string fileName)
		{
			File.WriteAllText(fileName, GetCSharpCodeString());
		}
	}
}
