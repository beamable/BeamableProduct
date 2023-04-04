using Beamable.Server;
using Beamable.Server.Common;
using beamable.tooling.common.Microservice;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace Beamable.Tooling.Common.OpenAPI;

public class ServiceDocGenerator
{
	private const string JSON_CONTENT_TYPE = "application/json";

	public OpenApiDocument Generate<TMicroservice>() where TMicroservice: Microservice
	{
		var attr = typeof(TMicroservice).GetCustomAttribute(typeof(MicroserviceAttribute)) as MicroserviceAttribute;
		return Generate(typeof(TMicroservice), attr);
	}
	
	public OpenApiDocument Generate(Type microserviceType, MicroserviceAttribute attribute)
	{
		if (!microserviceType.IsAssignableTo(typeof(Microservice)))
		{
			throw new ArgumentException($"must be a subtype of {nameof(Microservice)}", nameof(microserviceType));
		}

		var coll = RouteTableGeneration.BuildRoutes(microserviceType, attribute, _ => null);
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
				Schemas = new Dictionary<string, OpenApiSchema>()
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
			// TODO: handle auth
			var returnJson = new OpenApiMediaType
			{
				Schema = SchemaGenerator.Convert(method.Method.ReturnType, 0)
			};

			var requestSchema = new OpenApiSchema
			{
				Properties = new Dictionary<string, OpenApiSchema>(),
				Required = new SortedSet<string>()
			};
			for (var i = 0; i < method.ParameterInfos.Count; i++)
			{
				// TODO: handle enums
				var parameterSchema = SchemaGenerator.Convert(method.ParameterInfos[i].ParameterType, 0);
				var parameterName = method.ParameterNames[i];
				requestSchema.Properties[parameterName] = parameterSchema;
				requestSchema.Required.Add(parameterName);
			}

			var requestJson = new OpenApiMediaType { Schema = requestSchema };
			var operation = new OpenApiOperation
			{
				RequestBody = new OpenApiRequestBody
				{
					Content = new Dictionary<string, OpenApiMediaType> { [JSON_CONTENT_TYPE] = requestJson }
				},
				Responses = new OpenApiResponses
				{
					["200"] = new OpenApiResponse
					{
						Description = "",
						Content = new Dictionary<string, OpenApiMediaType>
						{
							[JSON_CONTENT_TYPE] = returnJson
						}
					}
				}
			};

			var pathItem = new OpenApiPathItem { Operations = new Dictionary<OperationType, OpenApiOperation> { [OperationType.Post] = operation } };
			doc.Paths.Add("/" + method.Path, pathItem);

		}
		
		return doc;
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
