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
using Serilog;
using System.Reflection;

namespace Beamable.Tooling.Common.OpenAPI;

/// <summary>
/// Generates OpenAPI documentation for a microservice.
/// </summary>
public class ServiceDocGenerator
{
	private const string JSON_CONTENT_TYPE = "application/json";
	private const string SCOPE = "scope";
	private const string USER = "user";

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
	/// <param name="excludeFederationCallbackEndpoints">When true, does not generate the doc for any endpoints coming from federation. Used primarily for code-generating in-SDK client code for Microservices.</param>
	/// <param name="extraSchemas">List of types to add to the schema list of the OAPI doc.</param>
	/// <returns>An OpenApiDocument containing the generated documentation.</returns>
	public OpenApiDocument Generate(Type microserviceType, MicroserviceAttribute attribute, AdminRoutes adminRoutes, bool excludeFederationCallbackEndpoints = false, Type[] extraSchemas = null)
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
			// Skip out all federated callbacks IF the caller of this function asked us to do so.
			if (excludeFederationCallbackEndpoints && methods[i].IsFederatedCallbackMethod)
			{
				Log.Debug("Removing federated callback {MethodName} that made through the check", methods[i].Method.Name);
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
				string componentName = $"{typeName}/{identity?.GetUniqueName()}";
				apiComponents.Add(new OpenApiString(componentName));
			}
		}

		const string federatedKey = Constants.Features.Services.MICROSERVICE_FEDERATED_COMPONENTS_KEY;
		doc.Extensions.Add(federatedKey, apiComponents);

		// We add to the list of schemas all complex types in method signatures and all extra schemas that were given to us (usually, this is any type that has BeamGenerateSchemaAttribute -- but we can pass
		// in any serializable type here that follows microservice serialization rules).
		var allTypes = SchemaGenerator.FindAllComplexTypes(methods).ToList();
		foreach (var type in allTypes.Concat(extraSchemas))
		{
			var schema = SchemaGenerator.Convert(type);
			Log.Debug("Adding Schema to Microservice OAPI docs. Type={TypeName}", type.FullName);
			doc.Components.Schemas.Add(SchemaGenerator.GetQualifiedReferenceName(type), schema);
		}

		foreach (var method in methods)
		{
			Log.Debug("Adding to Docs method {MethodName}", method.Method.Name);
			var comments = DocsLoader.GetMethodComments(method.Method);
			var parameterNameToComment = comments.Parameters.ToDictionary(kvp => kvp.Name, kvp => kvp.Text);

			var returnType = GetTypeFromPromiseOrTask(method.Method.ReturnType);

			var returnJson = new OpenApiMediaType { Schema = SchemaGenerator.Convert(returnType, 0) };
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
				var parameterSchema = SchemaGenerator.Convert(method.ParameterInfos[i].ParameterType, 0);
				var parameterName = method.ParameterNames[i];

				if (parameterNameToComment.TryGetValue(parameterName, out var comment))
				{
					parameterSchema.Description = comment ?? "";
				}

				requestSchema.Properties[parameterName] = parameterSchema;
				if (!method.ParameterInfos[i].ParameterType.IsAssignableTo(typeof(Optional)))
				{
					requestSchema.Required.Add(parameterName);
				}
			}

			var operation = new OpenApiOperation
			{
				Responses = new OpenApiResponses { ["200"] = response }, Description = comments.Remarks, Summary = comments.Summary, Tags = new List<OpenApiTag> { new OpenApiTag { Name = method.Tag } }
			};
			if (method.ParameterInfos.Count > 0)
			{
				doc.Components.Schemas.Add(requestSchemaName, requestSchema);
				operation.RequestBody = new OpenApiRequestBody
				{
					Content = new Dictionary<string, OpenApiMediaType>
					{
						[JSON_CONTENT_TYPE] = new OpenApiMediaType { Schema = new OpenApiSchema { Type = "object", Reference = new OpenApiReference { Type = ReferenceType.Schema, Id = requestSchemaName } } }
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

			doc.Paths.Add("/" + method.Path, pathItem);
		}

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
			Log.Debug("Adding Schema to Microservice OAPI docs. Type={TypeName}", type.FullName);
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
