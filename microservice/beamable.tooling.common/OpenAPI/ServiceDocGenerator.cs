using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Server;
using Beamable.Server.Common;
using Beamable.Server.Common.XmlDocs;
using beamable.tooling.common.Microservice;
using microservice.Common;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Beamable.Tooling.Common.OpenAPI;

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
		Type = SecuritySchemeType.ApiKey,
		Name = "X-DE-SCOPE",
		In = ParameterLocation.Header,
		Description = "Customer and project scope. This should contain the '<customer-id>.<project-id>'.",
	};

	private static OpenApiSecurityScheme _userSecuritySchemeReference = new OpenApiSecurityScheme
	{
		Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = USER }
	};
	private static OpenApiSecurityScheme _scopeSecuritySchemeReference = new OpenApiSecurityScheme
	{
		Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SCOPE }
	};
	
	public OpenApiDocument Generate<TMicroservice>(AdminRoutes adminRoutes) where TMicroservice: Microservice
	{
		var attr = typeof(TMicroservice).GetCustomAttribute(typeof(MicroserviceAttribute)) as MicroserviceAttribute;
		return Generate(typeof(TMicroservice), attr, adminRoutes);
	}
	
	public OpenApiDocument Generate(Type microserviceType, MicroserviceAttribute attribute, AdminRoutes adminRoutes)
	{
		if (!microserviceType.IsAssignableTo(typeof(Microservice)))
		{
			throw new ArgumentException($"must be a subtype of {nameof(Microservice)}", nameof(microserviceType));
		}

		var coll = RouteTableGeneration.BuildRoutes(microserviceType, attribute, adminRoutes, _ => null);
		var methods = coll.Methods.ToList();
		var doc = new OpenApiDocument {
			Info = new OpenApiInfo
			{
				Title = attribute.MicroserviceName,
				Version = "0.0.0"
			},
			Paths = new OpenApiPaths(),
			Components = new OpenApiComponents
			{
				Schemas = new Dictionary<string, OpenApiSchema>(),
				SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
				{
					[SCOPE] = _scopeSecurityScheme,
					[USER] = _userSecurityScheme,
				}
			}
		};
		

		var paths = new OpenApiPaths();
		var allTypes = SchemaGenerator.FindAllComplexTypes(methods).ToList();

		
		foreach (var type in allTypes)
		{
			var schema = SchemaGenerator.Convert(type);
			doc.Components.Schemas.Add(type.Name, schema);
		}

		foreach (var method in methods)
		{
			// TODO: handle docs

			var comments = DocsLoader.GetMethodComments(method.Method);
			var parameterNameToComment = comments.Parameters.ToDictionary(kvp => kvp.Name, kvp => kvp.Text);
			
			var returnType = GetTypeFromPromiseOrTask(method.Method.ReturnType);
			
			var returnJson = new OpenApiMediaType
			{
				Schema = SchemaGenerator.Convert(returnType, 0)
			};
			var response = new OpenApiResponse()
			{
				Description = comments.Returns,
			};
			if (!IsEmptyResponseType(returnType))
			{
				response.Content = new Dictionary<string, OpenApiMediaType> { [JSON_CONTENT_TYPE] = returnJson };
			}

			var requestSchema = new OpenApiSchema
			{
				Properties = new Dictionary<string, OpenApiSchema>(),
				Required = new SortedSet<string>()
			};
			for (var i = 0; i < method.ParameterInfos.Count; i++)
			{
				var parameterSchema = SchemaGenerator.Convert(method.ParameterInfos[i].ParameterType, 0);
				var parameterName = method.ParameterNames[i];

				if (parameterNameToComment.TryGetValue(parameterName, out var comment))
				{
					parameterSchema.Description = comment;
				}
				requestSchema.Properties[parameterName] = parameterSchema;
				if (!method.ParameterInfos[i].ParameterType.IsAssignableTo(typeof(Optional)))
				{
					requestSchema.Required.Add(parameterName);
				}
			}

			var requestJson = new OpenApiMediaType { Schema = requestSchema };
			var operation = new OpenApiOperation
			{
				
				Responses = new OpenApiResponses
				{
					["200"] = response
				},
				Description = comments.Remarks,
				Summary = comments.Summary,
				Tags = new List<OpenApiTag>
				{
					new OpenApiTag{ Name = method.Tag }
				}
			};
			if (method.ParameterInfos.Count > 0)
			{
				operation.RequestBody = new OpenApiRequestBody
				{
					Content = new Dictionary<string, OpenApiMediaType> { [JSON_CONTENT_TYPE] = requestJson }
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
		
		return doc;
	}

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

	public static bool IsEmptyResponseType(Type type)
	{
		var isPromise = type == typeof(Promise);
		var isTask = type == typeof(Task);
		var isVoid = type == typeof(void);
		return isPromise || isTask || isVoid;
	}

	// public static OpenApiPathItem Convert(ServiceMethod method)
	// {
	// 	var item = new OpenApiPathItem();
	//
	// 	var returnType = method.Method.ReturnType;
	// 	
	// 	
	// 	var operation = new OpenApiOperation { RequestBody = new OpenApiRequestBody { Content = new Dictionary<string, OpenApiMediaType>
	// 			{
	// 				["application/json"] = new OpenApiMediaType { Schema = new OpenApiSchema() }
	// 			}
	// 		}
	// 	};
	// 	// operation.
	// 	item.AddOperation(OperationType.Post, operation);
	// 	return item;
	// } 
}
