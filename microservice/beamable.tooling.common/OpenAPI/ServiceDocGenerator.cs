using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Common.XmlDocs;
using beamable.tooling.common.Microservice;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Reflection;
using Beamable.Common.Dependencies;
using Beamable.Common.Semantics;
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
	private const string SCHEMA_SEMANTIC_TYPE_NAME_KEY = Constants.Features.Services.SCHEMA_SEMANTIC_TYPE_NAME_KEY;


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
	
	
	
	
	public OpenApiDocument Generate(StartupContext startupCtx, IDependencyProvider rootProvider)
	{

		var telemetryAttributes = new List<TelemetryAttributeDescriptor>();
		if (rootProvider.CanBuildService<SingletonDependencyList<ITelemetryAttributeProvider>>())
		{
			var attributeProviders = rootProvider.GetService<SingletonDependencyList<ITelemetryAttributeProvider>>();
			for (var i = attributeProviders.Elements.Length - 1; i >= 0; i --)
			{
				telemetryAttributes.AddRange(attributeProviders.Elements[i].GetDescriptors());
			}
			telemetryAttributes = telemetryAttributes.GroupBy(a => a.name, a => a).Select(g =>
			{
				var first = g.First();
				return new TelemetryAttributeDescriptor
				{
					name = g.Key,
					description = first.description,
					type = first.type,
					level = first.level,
					source = g.Aggregate(TelemetryAttributeSource.NONE, (source, desc) => source | desc.source)
				};
			}).ToList();
		}
		
		var uniqueAssemblies = startupCtx.routeSources
			.Select(t => t.InstanceType.Assembly)
			.Distinct()
			.ToList();

		var allTypes = uniqueAssemblies
			.SelectMany(asm => asm.GetExportedTypes())
			.ToArray();
		
		var extraSchemas = LoadDotnetDeclaredSchemasFromTypes(allTypes, out var missingAttributes)
			.Select(t => t.type)
			.ToArray();
		if (missingAttributes.Count > 0)
		{
			var typesWithErr = string.Join(",", missingAttributes.Select(t => t.Name));
			throw new Exception(
				$"Types [{typesWithErr}] in microservice {startupCtx.attributes.MicroserviceName} should have {nameof(BeamGenerateSchemaAttribute)} as they are used as fields of a type with {nameof(BeamGenerateSchemaAttribute)}.");
		}

		var coll = RouteSourceUtil.BuildRoutes(startupCtx, startupCtx.args);
		var methods = coll.Methods.ToList();
		var doc = new OpenApiDocument
		{
			Info = new OpenApiInfo { Title = startupCtx.attributes.MicroserviceName, Version = "0.0.0" },
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>(), SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme> { [SCOPE] = _scopeSecurityScheme, [USER] = _userSecurityScheme, }
			}
		};
		doc.Extensions = new Dictionary<string, IOpenApiExtension>();


		var oapiTelemetryAttributes = new OpenApiArray();
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_TELEMETRY_ATTRIBUTES_KEY, oapiTelemetryAttributes);
		var enumSourceValues = Enum.GetValues(typeof(TelemetryAttributeSource))
			.Cast<TelemetryAttributeSource>()
			.ToList();
		foreach (var attr in telemetryAttributes)
		{
			var sources = new OpenApiArray();
			var attrSourceArray = enumSourceValues
				.Where(flag => flag != TelemetryAttributeSource.NONE && attr.source.HasFlag(flag))
				.Select(flag => flag.ToString())
				.ToArray();
			foreach (var source in attrSourceArray)
			{
				sources.Add(new OpenApiString(source));
			}
			var oapiAttr = new OpenApiObject
			{
				["name"] = new OpenApiString(attr.name),
				["description"] = new OpenApiString(attr.description),
				["type"] = new OpenApiString(attr.type.GetDisplayName()),
				["sources"] = sources,
				["level"] = new OpenApiInteger((int)attr.level),
			};
			oapiTelemetryAttributes.Add(oapiAttr);
		}
		
		
		var interfaces = startupCtx.routeSources
			.Select(t => t.InstanceType)
			.SelectMany(t => t.GetInterfaces())
			.ToList();
		// var interfaces = microserviceType.GetInterfaces();
		var apiComponents = new OpenApiArray();
		var v2ApiComponents = new OpenApiArray();
		
		var routeSources = new OpenApiArray();
		foreach (var routeSource in startupCtx.routeSources)
		{
			routeSources.Add(new OpenApiObject
			{
				[Constants.Features.Services.MICROSERVICE_ROUTE_SOURCE_FIELD_ROUTE_PREFIX] = new OpenApiString(routeSource.RoutePrefix),
				[Constants.Features.Services.MICROSERVICE_ROUTE_SOURCE_FIELD_CLIENT_PREFIX] = new OpenApiString(routeSource.ClientNamespacePrefix),
			});
		}

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


		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_ROUTE_SOURCES_KEY, routeSources);
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_KEY, apiComponents);
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_V2_KEY, v2ApiComponents);
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_CLASS_TYPE_KEY, new OpenApiString(startupCtx.attributes.MicroserviceName));

		// We add to the list of schemas all complex types in method signatures and all extra schemas that were given to us (usually, this is any type that has BeamGenerateSchemaAttribute -- but we can pass
		// in any serializable type here that follows microservice serialization rules).
		var allTypesFromRoutes = SchemaGenerator.FindAllTypesForOAPI(methods).ToList();
		allTypesFromRoutes.AddRange(extraSchemas.Select(ex => new SchemaGenerator.OAPIType(null, ex)));
		foreach (var oapiType in allTypesFromRoutes)
		{
			// We check because the same type can both be an extra type (declared via BeamGenerateSchema) AND be used in a signature; so we de-duplicate the concatenated lists.
			// If all usages of this type (within a sub-graph of types starting from a ServiceMethod) is set to NOT generate the client code, we won't.
			// Otherwise, even if just a single usage of the type wants the client code to be generated, we do generate it.
			// That's what this thing does.
			var type = oapiType.Type;
			var key = SchemaGenerator.GetQualifiedReferenceName(type);
			bool shouldSkipCodeGen = oapiType.ShouldSkipClientCodeGeneration();
			if (doc.Components.Schemas.TryGetValue(key, out var existingSchema))
			{
				existingSchema.AddExtension(Constants.Features.Services.METHOD_SKIP_CLIENT_GENERATION_KEY, new OpenApiBoolean(shouldSkipCodeGen));

				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Tried to add Schema more than once. Type={type.FullName}, SchemaKey={key}, WillSGenClient={!shouldSkipCodeGen}");
			}
			else
			{
				// Convert the type into a schema, then set this schema's client-code generation extension based on whether the OAPI type so our code-gen pipelines can decide whether to output it. 
				var schema = SchemaGenerator.Convert(type);
				schema.AddExtension(Constants.Features.Services.METHOD_SKIP_CLIENT_GENERATION_KEY, new OpenApiBoolean(shouldSkipCodeGen));

				BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"Adding Schema to Microservice OAPI docs. Type={type.FullName}, WillGenClient={!shouldSkipCodeGen}");
				doc.Components.Schemas.Add(key, schema);
			}
		}

		var methodsSkippedForClientCodeGen = new List<string>();
		foreach (var method in methods)
		{
			var callableAttrs = method.Method.GetCustomAttributes(typeof(CallableAttribute), true);

			Log.Trace("Adding to Docs method {MethodName}", method.Method.Name);
			var comments = DocsLoader.GetMethodComments(method.Method);
			var parameterNameToComment = comments.Parameters.ToDictionary(kvp => kvp.Name, kvp => kvp.Text);

			var returnType = GetTypeFromPromiseOrTask(method.Method.ReturnType);
			
			OpenApiSchema openApiSchema = SchemaGenerator.Convert(returnType, 0);
			
			var returnJson = new OpenApiMediaType { Schema = openApiSchema };
			if (openApiSchema.Reference != null && !doc.Components.Schemas.ContainsKey(openApiSchema.Reference.Id))
			{
				returnJson.Extensions.Add(Constants.Features.Services.MICROSERVICE_EXTENSION_BEAMABLE_TYPE_ASSEMBLY_QUALIFIED_NAME, new OpenApiString(returnType.GetGenericSanitizedFullName()));
			}
			
			

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
				AdditionalPropertiesAllowed = false,
			};
			
			// This schema should be excluded from client-code generation if its from a federation.
			requestSchema.AddExtension(Constants.Features.Services.METHOD_SKIP_CLIENT_GENERATION_KEY, new OpenApiBoolean(method.IsFederatedCallbackMethod));
			var openApiTags = new List<OpenApiTag> { new OpenApiTag { Name = method.Tag } };
			var operation = new OpenApiOperation
			{
				Responses = new OpenApiResponses { ["200"] = response }, Description = comments.Remarks, Summary = comments.Summary, Tags = openApiTags,
			};
			
			for (var i = 0; i < method.ParameterInfos.Count; i++)
			{
				Type parameterType = method.ParameterInfos[i].ParameterType;
				var parameterSchema = SchemaGenerator.Convert(parameterType, 0);
				var parameterName = method.ParameterNames[i];
				var parameterSource = method.ParameterSources[parameterName];
				
				if (parameterNameToComment.TryGetValue(parameterName, out var comment))
				{
					parameterSchema.Description = comment ?? "";
				}

				bool isNullable = Nullable.GetUnderlyingType(parameterType) != null;
				bool isOptional = parameterType.IsAssignableTo(typeof(Optional));
				parameterSchema.Nullable = isNullable;
				parameterSchema.Extensions.Add(SCHEMA_IS_OPTIONAL_KEY, new OpenApiBoolean(isOptional));

				switch (parameterSource)
				{
					case ParameterSource.Body:
						if (parameterSchema.Reference != null && !doc.Components.Schemas.ContainsKey(parameterSchema.Reference.Id))
						{
							requestSchema.Properties[parameterName] = SchemaGenerator.Convert(parameterType, 1, true);
						}
						else
						{
							requestSchema.Properties[parameterName] = parameterSchema;
						}
				

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
						break;
					case ParameterSource.Injection:
						// don't do anything, we should not include this in the api contract because
						//  the argument will be satisfied locally.
						break;
				}
				
			}


		
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
						[JSON_CONTENT_TYPE] = new() { Schema = new OpenApiSchema { Type = "object", Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = requestSchemaName } } }
					}
				};
			}


			var pathItem = new OpenApiPathItem { Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Post] = operation } };
			var securityReq = new OpenApiSecurityRequirement { [_scopeSecuritySchemeReference] = new List<string>() };
			operation.Security.Add(securityReq);
			if (method.RequireAuthenticatedUser)
			{
				securityReq[_userSecuritySchemeReference] = new List<string>();
			}


			var shouldSkipClientCodeGeneration = callableAttrs.Length > 0 && ((CallableAttribute)callableAttrs[0]).Flags.HasFlag(CallableFlags.SkipGenerateClientFiles);
			shouldSkipClientCodeGeneration |= method.IsFederatedCallbackMethod; 
			if (shouldSkipClientCodeGeneration)
			{
				methodsSkippedForClientCodeGen.Add(method.Path);
			}

			pathItem.Extensions.Add(Constants.Features.Services.METHOD_SKIP_CLIENT_GENERATION_KEY, new OpenApiBoolean(shouldSkipClientCodeGeneration));
			pathItem.Extensions.Add(Constants.Features.Services.PATH_CALLABLE_METHOD_NAME_KEY, new OpenApiString(method.Method.Name));
			pathItem.Extensions.Add(Constants.Features.Services.PATH_CALLABLE_METHOD_CLIENT_PREFIX_KEY, new OpenApiString(method.ClientNamespacePrefix));
			doc.Paths.Add("/" + method.Path, pathItem);
		}

		var skippedForClientCodeGenArray = new OpenApiArray();
		skippedForClientCodeGenArray.AddRange(methodsSkippedForClientCodeGen.Select(item => new OpenApiString(item)));
		doc.Extensions.Add(Constants.Features.Services.MICROSERVICE_METHODS_TO_SKIP_GENERATION_KEY, skippedForClientCodeGenArray);

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

		var pulledInTypes = SchemaGenerator.FindAllTypesForOAPI(output.Select(t => new SchemaGenerator.OAPIType(null, t.Item1)))
			.Select(et => et.Type)
			.ToList();
		var pulledInTypesFiltered = pulledInTypes.GroupBy(t => t.GetCustomAttribute<BeamGenerateSchemaAttribute>() != null).ToDictionary(g => g.Key, g => g.ToArray());
		missingAttributes = pulledInTypesFiltered.TryGetValue(false, out var pulledInTypesMissingAttributes) ? pulledInTypesMissingAttributes.ToList() : new();

		// Add the ones that do have the output
		if (pulledInTypesFiltered.TryGetValue(true, out var pulledInTypesWithAttributes))
			output = pulledInTypesWithAttributes.Select(t => (t, t.GetCustomAttribute<BeamGenerateSchemaAttribute>())).ToList();

		return output;
	}
}


public static class DocGenExtensions
{
    
	/// <summary>
	/// Generates OpenAPI documentation for a specified microservice type.
	/// </summary>
	/// <typeparam name="TMicroservice">The type of the microservice to generate documentation for.</typeparam>
	/// <param name="adminRoutes">The administrative routes associated with the microservice.</param>
	/// <returns>An OpenApiDocument containing the generated documentation.</returns>
	public static OpenApiDocument Generate<TMicroservice>(this ServiceDocGenerator generator, IDependencyProvider provider) where TMicroservice : Microservice
	{
		var attr = typeof(TMicroservice).GetCustomAttribute(typeof(MicroserviceAttribute)) as MicroserviceAttribute;
		var startupContext = new StartupContext
		{
			routeSources = new BeamRouteSource[]
			{
				new BeamRouteSource
				{
					InstanceType = typeof(TMicroservice)
				}
			},
			attributes = attr,
			args = provider.GetService<IMicroserviceArgs>()
		};
		return generator.Generate(startupContext, provider);
	}
}
