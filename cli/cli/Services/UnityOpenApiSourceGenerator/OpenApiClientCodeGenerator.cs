using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Server.Common;
using cli;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text;
using TypeAttributes = System.Reflection.TypeAttributes;
using ServiceConstants = Beamable.Common.Constants.Features.Services;

namespace Beamable.Server.Generator
{
	public class OpenApiClientCodeGenerator
	{
		private const string MICROSERVICE_CLIENTS_TYPE_NAME = "MicroserviceClients";
		private const string MICROSERVICE_CLIENT_TYPE_NAME = "MicroserviceClient";

		private const string COMPONENT_INTERFACE_KEY =
			ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_INTERFACE_KEY;

		private const string COMPONENT_FEDERATION_CLASS_NAME_KEY =
			ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_FEDERATION_CLASS_NAME_KEY;

		private const string COMPONENT_IS_HIDDEN_METHOD_KEY = ServiceConstants.METHOD_SKIP_CLIENT_GENERATION_KEY;

		private const string SCHEMA_QUALIFIED_NAME_KEY =
			ServiceConstants.MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME;

		private const string SCHEMA_IS_OPTIONAL_KEY = ServiceConstants.SCHEMA_IS_OPTIONAL_KEY;
		private const string SCHEMA_OPTIONAL_TYPE_NAME_KEY = ServiceConstants.SCHEMA_OPTIONAL_TYPE_NAME_KEY;

		private readonly string _serviceNamespaceClassName;
		private readonly string _serviceName;
		private readonly CodeCompileUnit _targetUnit;

		private string TargetClassName => $"{_serviceName}Client";
		private string TargetParameterClassName => GetTargetParameterClassName(_serviceName);
		private string TargetExtensionClassName => $"ExtensionsFor{_serviceName}Client";

		private const string PARAMETER_STRING = "Parameter";
		private const string CLIENT_NAMESPACE = "Beamable.Server.Clients";
		private const string LIST_BASE_PREFIX = "System.Collections.Generic.List";
		private const string NULLABLE_BASE = "System.Nullable<{0}>";

		private static readonly Dictionary<string, string> OpenApiCSharpFullNameMap = new()
		{
			{ "int16", typeof(short).FullName },
			{ "int32", typeof(int).FullName },
			{ "int64", typeof(long).FullName },
			{ "integer", typeof(int).FullName },
			{ "float", typeof(float).FullName },
			{ "double", typeof(double).FullName },
			{ "decimal", typeof(decimal).FullName },
			{ "number", typeof(double).FullName },
			{ "boolean", typeof(bool).FullName },
			{ "date", typeof(DateTime).FullName },
			{ "date-time", typeof(DateTime).FullName },
			{ "uuid", typeof(Guid).FullName },
			{ "byte", typeof(byte).FullName },
			{ "string", typeof(string).FullName }
		};

		private static readonly Dictionary<string, string> OpenApiCSharpNameMap = new()
		{
			{ "int16", "short" },
			{ "int32", "int" },
			{ "int64", "long" },
			{ "integer", "int" },
			{ "float", "float" },
			{ "double", "double" },
			{ "decimal", "decimal" },
			{ "number", "double" },
			{ "boolean", "bool" },
			{ "uuid", typeof(Guid).FullName },
			{ "byte", "byte" },
			{ "string", "string" }
		};

		private static readonly List<string> CallableMethodsToGenerate = new()
		{
			nameof(CallableAttribute), nameof(ServerCallableAttribute), nameof(ClientCallableAttribute), nameof(AdminOnlyCallableAttribute)
		};

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
				document.Extensions.TryGetValue(ServiceConstants.MICROSERVICE_CLASS_TYPE_KEY,
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
			parameterClass.TypeAttributes =
				TypeAttributes.NotPublic | TypeAttributes.Sealed;

			targetClass.Comments.Add(
				new CodeCommentStatement(
					$"<summary> A generated client for <see cref=\"{_serviceNamespaceClassName}\"/> </summary",
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

		private void AddMethods(CodeTypeDeclaration targetClass, CodeTypeDeclaration parameterClass,
			OpenApiDocument document)
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
					if (!operation.Extensions.TryGetValue(ServiceConstants.OPERATION_CALLABLE_METHOD_TYPE_KEY,
						    out var methodTypeExt) || methodTypeExt is not OpenApiString methodType ||
					    !CallableMethodsToGenerate.Contains(methodType.Value))
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
					
					var requestSchema = requestType.Schema.GetEffective(document);
					var parameters = requestSchema.Properties.ToDictionary(itemKey => itemKey.Key,
						itemValue => itemValue.Value.GetEffective(document));
					AddCallableMethod(targetClass, methodName, parameters, responseType, addedParameters);
				}
			}

			foreach ((string paramType, string paramName) in addedParameters)
			{
				AddParameterClass(parameterClass, paramType, paramName);
			}
		}

		void AddSpecificFederationsInterfaces(CodeTypeDeclaration targetClass, OpenApiDocument document)
		{
			const string federatedComponent = ServiceConstants.MICROSERVICE_FEDERATED_COMPONENTS_V2_KEY;
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

					bool isFederatedLogin =
						fedInterfaceString.Value.Equals(typeof(IFederatedLogin<>).GetSanitizedFullName());
					bool isFederatedInventory =
						fedInterfaceString.Value.Equals(typeof(IFederatedInventory<>).GetSanitizedFullName());

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
			IDictionary<string, OpenApiSchema> parameters, OpenApiMediaType returnMediaType,
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
				var paramType = GetParsedType(schema, true);
				var paramName = key;
				paramsTypeName.TryAdd(paramType, GetParameterClassName(schema));
				CodeTypeReference param = new CodeTypeReference(paramType)
				{
					Options = CodeTypeReferenceOptions.GenericTypeParameter
				};
				genMethod.Parameters.Add(new CodeParameterDeclarationExpression(param, paramName));

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

			string returnTypeString;
			OpenApiString qualifiedNameString = null;
			bool hasQualifiedName = returnMediaType.Extensions.TryGetValue(SCHEMA_QUALIFIED_NAME_KEY, out var qualifiedExtension) &&
			                   qualifiedExtension is OpenApiString;
			if (hasQualifiedName)
			{
				qualifiedNameString = (OpenApiString)qualifiedExtension;
				returnTypeString = qualifiedNameString.Value;
			}
			else
			{
				returnTypeString = GetParsedType(returnMediaType.Schema);
			}


			string genericPromiseType = $"Beamable.Common.Promise<{returnTypeString}>";
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

			string parsedType = hasQualifiedName ? qualifiedNameString.Value : GetParsedType(returnMediaType.Schema, true);
			var requestInvokeExpr = new CodeMethodInvokeExpression(
				new CodeMethodReferenceExpression(
					new CodeThisReferenceExpression(),
					"Request",
					new CodeTypeReference[] { new(parsedType) }),
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

		private string GetParsedType(OpenApiSchema schema, bool useFullName = false)
		{
			var (typeName, isNullable, isOptional) = ResolveTypeInfo(schema);
			var nameBase = GetTypeNameBase(schema, isNullable);

			if (useFullName && !isNullable && !isOptional)
			{
				return GetFullTypeName(schema, typeName);
			}

			return FormatTypeName(nameBase, typeName);
		}

		private (string typeName, bool isNullable, bool isOptional) ResolveTypeInfo(OpenApiSchema schema)
		{
			bool isOptional = schema.Extensions.TryGetValue(SCHEMA_IS_OPTIONAL_KEY, out var extension) &&
			                  extension is OpenApiBoolean { Value: true };

			bool isNullable = schema.Nullable && !(schema.Type == "string" && string.IsNullOrEmpty(schema.Type));

			string typeName = GetBaseTypeName(schema);

			return (typeName, isNullable, isOptional);
		}

		private string GetTypeNameBase(OpenApiSchema schema, bool isNullable)
		{
			if (schema.Extensions.TryGetValue(SCHEMA_OPTIONAL_TYPE_NAME_KEY, out var optionalNameExt) &&
			    optionalNameExt is OpenApiString optionalName)
			{
				return optionalName.Value;
			}

			return isNullable ? NULLABLE_BASE : "{0}";
		}

		private string GetBaseTypeName(OpenApiSchema schema)
		{
			var mapToUse = OpenApiCSharpNameMap; // Default to short names
			string valueToFind = !string.IsNullOrEmpty(schema.Format) ? schema.Format : schema.Type;

			if (mapToUse.TryGetValue(valueToFind, out string typeValue))
			{
				return typeValue;
			}

			return schema.Type switch
			{
				"array" => GetArrayTypeName(schema),
				_ => GetObjectType(schema)
			};
		}

		private string GetArrayTypeName(OpenApiSchema schema)
		{
			return schema.Items.Format == "byte"
				? $"{GetParsedType(schema.Items, true)}[]"
				: $"{LIST_BASE_PREFIX}<{GetParsedType(schema.Items)}>";
		}

		private static string GetObjectType(OpenApiSchema schema)
		{
			if (schema.Reference != null && !string.IsNullOrEmpty(schema.Reference.Id))
			{
				return schema.Reference.Id;
			}

			return schema.Extensions.TryGetValue(SCHEMA_QUALIFIED_NAME_KEY, out var extension) &&
			       extension is OpenApiString value
				? value.Value
				: "object";
		}

		private string GetFullTypeName(OpenApiSchema schema, string typeName)
		{
			if (OpenApiCSharpFullNameMap.TryGetValue(schema.Format ?? schema.Type, out string fullName))
			{
				return fullName;
			}

			return typeName;
		}

		private string FormatTypeName(string nameBase, string typeName)
		{
			return nameBase.Contains("{0}")
				? string.Format(nameBase, typeName)
				: nameBase;
		}

		private string GetParameterClassName(OpenApiSchema schema, bool addParameterString = true)
		{
			var (typeName, isNullable, _) = ResolveTypeInfo(schema);
			var nameBase = GetParameterTypeNameBase(schema, isNullable);
			var parameterClassName = GetParameterTypeName(schema);

			return BuildParameterClassName(addParameterString, nameBase, parameterClassName);
		}

		private string GetParameterTypeNameBase(OpenApiSchema schema, bool isNullable)
		{
			if (schema.Extensions.TryGetValue(SCHEMA_OPTIONAL_TYPE_NAME_KEY, out var optionalNameExt) &&
			    optionalNameExt is OpenApiString optionalName)
			{
				return optionalName.Value.Replace("<", "_").Replace(">", "");
			}

			return isNullable ? "System_Nullable_{0}" : "{0}";
		}

		private string GetParameterTypeName(OpenApiSchema schema)
		{
			return (schema.Type, schema.Format) switch
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
				("array", _) => GetParameterArrayTypeName(schema),
				_ => GetObjectType(schema)
			};
		}

		private string GetParameterArrayTypeName(OpenApiSchema schema)
		{
			return schema.Items.Format == "byte"
				? $"System_Array_{GetParameterClassName(schema.Items, false)}"
				: $"{LIST_BASE_PREFIX}_{GetParameterClassName(schema.Items, false)}";
		}

		private string BuildParameterClassName(bool addParameterString, string nameBase, string parameterClassName)
		{
			var sb = new StringBuilder();
			if (addParameterString) sb.Append(PARAMETER_STRING);
			sb.Append(nameBase.Contains("{0}")
				? string.Format(nameBase, parameterClassName)
				: nameBase);
			return sb.ToString().Replace(".", "_");
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
				// CodeDom code generated with the string value of primal types are created with a @ added as prefix,
				// we need to remove it so Unity can compile
				return source;
			}
		}

		public void GenerateCSharpCode(string fileName)
		{
			File.WriteAllText(fileName, GetCSharpCodeString());
		}
	}
}
