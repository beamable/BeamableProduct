using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Reflection;
using Beamable.Common.Runtime;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Common.XmlDocs;
using beamable.tooling.common.Microservice;
using microservice.Common;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Reflection;
using ZLogger;

namespace Beamable.Tooling.Common.OpenAPI;

/// <summary>
/// Generates OpenAPI documentation for a microservice.
/// </summary>
public class ServiceDocGenerator
{
	private const string JSON_CONTENT_TYPE = "application/json";
	private const string SCOPE = "scope";
	private const string USER = "user";
	private const string V2_COMPONENT_INTERFACE = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_INTERFACE_KEY;
	private const string V2_COMPONENT_FEDERATION_ID = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_FEDERATION_ID_KEY;
	private const string V2_COMPONENT_FEDERATION_CLASS_NAME = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_FEDERATION_CLASS_NAME_KEY;
	private const string SCHEMA_IS_OPTIONAL_KEY = Constants.Features.Services.SCHEMA_IS_OPTIONAL_KEY;
	private const string SCHEMA_OPTIONAL_TYPE_KEY = Constants.Features.Services.SCHEMA_OPTIONAL_TYPE_NAME_KEY;
	

	private static OpenApiSecurityScheme _userSecurityScheme = new OpenApiSecurityScheme
	{
		In = ParameterLocation.Header,
		Type = SecuritySchemeType.Http,
		Description = "Bearer authentication with an player access token in the Authorization header.",
		Scheme = "bearer",
		BearerFormat = "Bearer <Access Token>"
	};

	private static OpenApiSecurityScheme _scopeSecurityScheme = new OpenApiSecurityScheme
	{
		Type = SecuritySchemeType.ApiKey, Name = "X-DE-SCOPE", In = ParameterLocation.Header, Description = "Customer and project scope. This should contain the '<customer-id>.<project-id>'.",
	};

	private static OpenApiSecurityScheme _userSecuritySchemeReference = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = USER } };
	private static OpenApiSecurityScheme _scopeSecuritySchemeReference = new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SCOPE } };

	/// <summary>
	/// Generates OpenAPI documentation for a specified microservice type.
	/// </summary>
	/// <typeparam name="TMicroservice">The type of the microservice to generate documentation for.</typeparam>
	/// <param name="adminRoutes">The administrative routes associated with the microservice.</param>
	/// <returns>An OpenApiDocument containing the generated documentation.</returns>
	public OpenApiDocument Generate<TMicroservice>(AdminRoutes adminRoutes) where TMicroservice : Microservice
	{
		var attr = typeof(TMicroservice).GetCustomAttribute(typeof(MicroserviceAttribute)) as MicroserviceAttribute;
		var extraSchemas = LoadDotnetDeclaredSchemasFromTypes(typeof(TMicroservice).Assembly.GetExportedTypes(), out _).Select(t => t.type).ToArray();
		return Generate(typeof(TMicroservice), attr, adminRoutes, false, extraSchemas);
	}

	/// <summary>
	/// Generates OpenAPI documentation for a specified microservice type.
	/// </summary>
	/// <param name="microserviceType">The type of the microservice to generate documentation for.</param>
	/// <param name="attribute">The MicroserviceAttribute associated with the microservice.</param>
	/// <param name="adminRoutes">The administrative routes associated with the microservice.</param>
	/// <param name="forClientCodeGeneration">Should be true if you want the OAPI-spec that will be used for client code generating (does not generate the doc for any endpoints coming from federation and respect <see cref="CallableFlags.SkipGenerateClientFiles"/>).</param>
	/// <param name="extraSchemas">List of types to add to the schema list of the OAPI doc.</param>
	/// <returns>An OpenApiDocument containing the generated documentation.</returns>
	public OpenApiDocument Generate(Type microserviceType, MicroserviceAttribute attribute, AdminRoutes adminRoutes, bool forClientCodeGeneration = false, Type[] extraSchemas = null)
	{
		extraSchemas ??= Array.Empty<Type>();

		if (!microserviceType.IsAssignableTo(typeof(Microservice)))
		{
			throw new ArgumentException($"must be a subtype of {nameof(Microservice)}", nameof(microserviceType));
		}

		var coll = RouteTableGeneration.BuildRoutes(microserviceType, attribute, adminRoutes, _ => null);
		var methods = coll.Methods.ToList();
		for (var i = methods.Count - 1; i >= 0; i--)
		{
			// IF the caller is asking for the Client-Code-Gen spec, we skip out the federated and hidden methods.
			
			if (forClientCodeGeneration)
			{
				var method = methods[i];
				var isFederatedMethod = method.IsFederatedCallbackMethod;
				if (!isFederatedMethod)
				{
					continue;
				}

				Log.Debug("Removing federated callback {MethodName} that made through the check",
					method.Method.Name);
				methods.RemoveAt(i);
			}
		}
		
		var doc = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = attribute.MicroserviceName, Version = "0.0.0" },
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>(), SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme> { [SCOPE] = _scopeSecurityScheme, [USER] = _userSecurityScheme, }
			}
		};
		doc.Extensions = new Dictionary<string, IOpenApiExtension>();

		var interfaces = microserviceType.GetInterfaces();
		var apiComponents = new OpenApiArray();
		var v2ApiComponents = new OpenApiArray();

		foreach (Type it in interfaces)
		{
			// Skip non-generic types while we look for IFederation-derived implementations
			if (!it.IsGenericType) 
				continue;

			// Make sure we found an IFederation interface
			if (!it.GetGenericTypeDefinition().GetInterfaces().Contains(typeof(IFederation)))
				continue;

			// Get the cleaned-up type name (IFederatedGameServer`1 => IFederatedGameServer) 
			var typeName = it.GetGenericTypeDefinition().Name;
			typeName = typeName.Substring(0, typeName.IndexOf("`", StringComparison.Ordinal));
			
			// Get the IFederationId 
			var federatedType = it.GetGenericArguments()[0];
			if (Activator.CreateInstance(federatedType) is IFederationId identity)
			{
				string federationId = identity?.GetUniqueName();
				apiComponents.Add(new OpenApiString($"{typeName}/{federationId}"));
				v2ApiComponents.Add(new OpenApiObject
				{
					[V2_COMPONENT_INTERFACE] = new OpenApiString(it.GetGenericTypeDefinition().GetSanitizedFullName()),
					[V2_COMPONENT_FEDERATION_ID] = new OpenApiString(federationId),
					[V2_COMPONENT_FEDERATION_CLASS_NAME] = new OpenApiString(federatedType.FullName),
				});
			}
		}
		
		
		
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_KEY, apiComponents);
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_KEY, v2ApiComponents);
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_CLASS_TYPE_KEY, new OpenApiString(microserviceType.ToString()));

		// We add to the list of schemas all complex types in method signatures and all extra schemas that were given to us (usually, this is any type that has BeamGenerateSchemaAttribute -- but we can pass
		// in any serializable type here that follows microservice serialization rules).
		var allTypes = SchemaGenerator.FindAllComplexTypes(methods).ToList();
		foreach (var type in allTypes.Concat(extraSchemas))
		{
			var schema = SchemaGenerator.Convert(type);
			BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Adding Schema to Microservice OAPI docs. Type={type.FullName}" );
			
			// We check because the same type can both be an extra type (declared via BeamGenerateSchema) AND be used in a signature; so we de-duplicate the concatenated lists.
			var key = SchemaGenerator.GetQualifiedReferenceName(type);
			if(!doc.Components.Schemas.ContainsKey(key))
				doc.Components.Schemas.Add(key, schema);
			else
				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Tried to add Schema more than once. Type={type.FullName}, SchemaKey={key}");
		}
		var hiddenMethods = new List<string>();
		foreach (var method in methods)
		{
			var callableAttrs = method.Method.GetCustomAttributes(typeof(CallableAttribute), true);
			
			Log.Debug("Adding to Docs method {MethodName}", method.Method.Name);
			var comments = DocsLoader.GetMethodComments(method.Method);
			var parameterNameToComment = comments.Parameters.ToDictionary(kvp => kvp.Name, kvp => kvp.Text);

			var returnType = GetTypeFromPromiseOrTask(method.Method.ReturnType);

			OpenApiSchema openApiSchema = SchemaGenerator.Convert(returnType, 0);
			if (openApiSchema.Reference != null && !doc.Components.Schemas.ContainsKey(openApiSchema.Reference.Id))
			{
				var schema = SchemaGenerator.Convert(returnType);
				doc.Components.Schemas.Add(openApiSchema.Reference.Id, schema);
			}
			var returnJson = new OpenApiMediaType { Schema = openApiSchema };
			var response = new OpenApiResponse() { Description = comments.Returns ?? "", };
			if (!IsEmptyResponseType(returnType))
			{
				response.Content = new Dictionary<string, OpenApiMediaType> { [JSON_CONTENT_TYPE] = returnJson };
			}

			var requestSchemaName = method.Path.Replace("/", "_") + "RequestArgs";
			var requestSchema = new OpenApiSchema
			{
				Properties = new Dictionary<string, OpenApiSchema>(),
				Required = new SortedSet<string>(),
				Type = "object",
				// Title = requestSchemaName,
				AdditionalPropertiesAllowed = false
			};
			for (var i = 0; i < method.ParameterInfos.Count; i++)
			{
				Type parameterType = method.ParameterInfos[i].ParameterType;
				var parameterSchema = SchemaGenerator.Convert(parameterType, 0);
				var parameterName = method.ParameterNames[i];

				if (parameterNameToComment.TryGetValue(parameterName, out var comment))
				{
					parameterSchema.Description = comment ?? "";
				}

				bool isNullable = Nullable.GetUnderlyingType(parameterType) != null;
				bool isOptional = parameterType.IsAssignableTo(typeof(Optional));
				parameterSchema.Nullable = isNullable;
				parameterSchema.Extensions.Add(SCHEMA_IS_OPTIONAL_KEY, new OpenApiBoolean(isOptional));
				requestSchema.Properties[parameterName] = parameterSchema;
				
				if (!isOptional)
				{
					requestSchema.Required.Add(parameterName);
				}
				else
				{
					var optionalTypeValue = new OpenApiString(parameterType.IsGenericType
						? parameterType.GetGenericTypeDefinition().FullName.Replace("`1", "<{0}>")
						: parameterType.FullName);
					
					parameterSchema.Extensions.Add(SCHEMA_OPTIONAL_TYPE_KEY, optionalTypeValue);
				}
			}

			var openApiTags = new List<OpenApiTag> { new OpenApiTag { Name = method.Tag } };

			var operation = new OpenApiOperation
			{
				Responses = new OpenApiResponses { ["200"] = response },
				Description = comments.Remarks,
				Summary = comments.Summary,
				Tags = openApiTags,
			};
			if (callableAttrs.Length > 0 && method.Tag != "Admin")
			{
				operation.Extensions.Add(Constants.Features.Services.OPERATION_CALLABLE_METHOD_TYPE_KEY,
					new OpenApiString(callableAttrs[0].GetType().Name));
			}

			if (method.ParameterInfos.Count > 0)
			{
				doc.Components.Schemas.Add(requestSchemaName, requestSchema);
				operation.RequestBody = new OpenApiRequestBody
				{
					Content = new Dictionary<string, OpenApiMediaType>
					{
						[JSON_CONTENT_TYPE] = new()
						{
							Schema = new OpenApiSchema
							{
								Type = "object",
								Reference = new OpenApiReference
								{
									Type = ReferenceType.Schema, Id = requestSchemaName
								}
							}
						}
					}
				};
			}


			var pathItem = new OpenApiPathItem
			{
				Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Post] = operation }
			};
			var securityReq = new OpenApiSecurityRequirement { [_scopeSecuritySchemeReference] = new List<string>() };
			operation.Security.Add(securityReq);
			if (method.RequireAuthenticatedUser)
			{
				securityReq[_userSecuritySchemeReference] = new List<string>();
			}
			

			var isHidden = callableAttrs.Length > 0 && ((CallableAttribute)callableAttrs[0]).Flags.HasFlag(CallableFlags.SkipGenerateClientFiles);
			if (isHidden)
			{
				hiddenMethods.Add(method.Path);
			}
			
			pathItem.Extensions.Add(Constants.Features.Services.METHOD_SKIP_CLIENT_GENERATION_KEY, new OpenApiBoolean(isHidden));
			doc.Paths.Add("/" + method.Path, pathItem);
		}
		
		var hiddenMethodsOapiArray = new OpenApiArray();
		hiddenMethodsOapiArray.AddRange(hiddenMethods.Select(item => new OpenApiString(item)));
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_METHODS_TO_SKIP_GENERATION_KEY, hiddenMethodsOapiArray);

		var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		doc = new OpenApiStringReader().Read(outputString, out var diag);

		return doc;
	}

	/// <summary>
	/// Generates OpenAPI documentation for a non-microservice assembly. It'll generate a document containing only the given <paramref name="schemas"/> (<see cref="BeamGenerateSchemaAttribute"/>).
	/// </summary>
	/// <returns>An OpenApiDocument containing the generated documentation.</returns>
	public OpenApiDocument Generate(Assembly ownerAssembly, IEnumerable<Type> schemas)
	{
		var doc = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = ownerAssembly.GetName().Name, Version = "0.0.0" },
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>(), SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme> { [SCOPE] = _scopeSecurityScheme, [USER] = _userSecurityScheme, }
			}
		};
		doc.Extensions = new Dictionary<string, IOpenApiExtension>();

		// Generate the list of schemas
		foreach (var type in schemas)
		{
			var schema = SchemaGenerator.Convert(type);
			BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Adding Schema to Microservice OAPI docs. Type={type.FullName}");
			doc.Components.Schemas.Add(SchemaGenerator.GetQualifiedReferenceName(type), schema);
		}

		var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
		doc = new OpenApiStringReader().Read(outputString, out var diag);

		return doc;
	}

	/// <summary>
	/// Retrieves the inner type from a Task or Promise type.
	/// </summary>
	/// <param name="type">The original type, potentially wrapped in Task or Promise.</param>
	/// <returns>The inner type if wrapped, or the original type if not.</returns>
	public static Type GetTypeFromPromiseOrTask(Type type)
	{
		if (type.IsGenericType)
		{
			var isPromise = type.GetGenericTypeDefinition() == typeof(Promise<>);
			var isTask = type.GetGenericTypeDefinition() == typeof(Task<>);
			if (isPromise || isTask)
			{
				var genericType = type.GetGenericArguments();
				return genericType[0];
			}
		}

		return type;
	}

	/// <summary>
	/// Determines whether a given type represents an empty response type.
	/// </summary>
	/// <param name="type">The type to check.</param>
	/// <returns><c>true</c> if the type represents an empty response, otherwise <c>false</c>.</returns>
	public static bool IsEmptyResponseType(Type type)
	{
		var isPromise = type == typeof(Promise);
		var isTask = type == typeof(Task);
		var isVoid = type == typeof(void);
		return isPromise || isTask || isVoid;
	}


	/// <summary>
	/// Given a list of types, finds the subset of types that have a <see cref="BeamGenerateSchemaAttribute"/> on them.
	/// </summary>
	public static List<(Type type, BeamGenerateSchemaAttribute attribute)> LoadDotnetDeclaredSchemasFromTypes(IEnumerable<Type> allTypes, out List<Type> missingAttributes)
	{
		var output = new List<(Type, BeamGenerateSchemaAttribute)>();
		foreach (var type in allTypes)
		{
			var attribute = type.GetCustomAttribute<BeamGenerateSchemaAttribute>();
			if (attribute == null) continue;

			output.Add((type, attribute));
		}

		var pulledInTypes = SchemaGenerator.FindAllComplexTypes(output.Select(t => t.Item1)).ToList();
		var pulledInTypesFiltered = pulledInTypes.GroupBy(t => t.GetCustomAttribute<BeamGenerateSchemaAttribute>() != null).ToDictionary(g => g.Key, g => g.ToArray());
		missingAttributes = pulledInTypesFiltered.TryGetValue(false, out var pulledInTypesMissingAttributes) ? pulledInTypesMissingAttributes.ToList() : new ();

		// Add the ones that do have the output
		if(pulledInTypesFiltered.TryGetValue(true, out var pulledInTypesWithAttributes))
			output = pulledInTypesWithAttributes.Select(t => (t, t.GetCustomAttribute<BeamGenerateSchemaAttribute>())).ToList();
		
		return output;
	}
}
