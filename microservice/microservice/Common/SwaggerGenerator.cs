using Beamable.Common;
using Beamable.Server;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace microservice.Common
{
	public static class SwaggerGenerator
	{
		private const string SCOPE = "scope";
		private const string USER = "user";
		private const string OBJECT = "object";
		private const string INTEGER = "integer";
		private const string NUMBER = "number";
		private const string STRING = "string";
		private const string BOOLEAN = "boolean";
		private const string ARRAY = "array";
		private const string RESPONSE_200 = "200";
		private const string JSON_CONTENT_TYPE = "application/json";

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

		private static Dictionary<Type, string> _typeToSwaggerName = new Dictionary<Type, string>
		{
			[typeof(ushort)] = INTEGER,
			[typeof(short)] = INTEGER,
			[typeof(int)] = INTEGER,
			[typeof(uint)] = INTEGER,
			[typeof(long)] = INTEGER,
			[typeof(ulong)] = INTEGER,
			[typeof(ulong)] = INTEGER,
			[typeof(float)] = NUMBER,
			[typeof(double)] = NUMBER,
			[typeof(string)] = STRING,
			[typeof(bool)] = BOOLEAN
		};

		private static Dictionary<Type, OpenApiSchema> _typeToSchema = new Dictionary<Type, OpenApiSchema>();
		private static Dictionary<OpenApiSchema, Type> _schemaToType = new Dictionary<OpenApiSchema, Type>();

		private static Dictionary<BeamableMicroService, OpenApiDocument> _serviceToDoc =
		   new Dictionary<BeamableMicroService, OpenApiDocument>();

		private static string GetJsonTypeName(Type type)
		{
			if (type.IsArray || typeof(IList).IsAssignableFrom(type))
			{
				return ARRAY;
			}
			if (_typeToSwaggerName.TryGetValue(type, out var name))
			{
				return name;
			}

			return OBJECT;
		}

		private static List<OpenApiSecurityRequirement> GenerateSecurityOptions()
		{
			return new List<OpenApiSecurityRequirement>
		 {
			new OpenApiSecurityRequirement()
			{
			   [new OpenApiSecurityScheme {Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = USER}}] =
				  new List<string>(),
			   [new OpenApiSecurityScheme {Reference = new OpenApiReference {Type = ReferenceType.SecurityScheme, Id = SCOPE}}] =
				  new List<string>()
			}
		 };
		}

		private static IList<OpenApiTag> GetTags(ServiceMethod method)
		{
			return new List<OpenApiTag> { new OpenApiTag { Name = method.Tag } };
		}

		private static OpenApiPathItem GeneratePathItem(ServiceMethod method)
		{
			var pathItem = new OpenApiPathItem();
			var op = new OpenApiOperation();

			op.Tags = GetTags(method);

			XmlDocsHelper.TryGetComments(method.Method, out var comments);
			op.Summary = comments?.Summary;
			op.Description = comments?.Remarks;

			var body = new OpenApiRequestBody();
			op.RequestBody = body;
			var jsonInput = new OpenApiMediaType();
			body.Content = new Dictionary<string, OpenApiMediaType>
			{
				[JSON_CONTENT_TYPE] = jsonInput
			};
			var jsonSchema = new OpenApiSchema();
			jsonInput.Schema = jsonSchema;

			op.Security = GenerateSecurityOptions();

			if (method.ParameterNames.Count == 0)
			{
				body.Content = new Dictionary<string, OpenApiMediaType>();
			}

			var parameterComments = comments.Parameters?.ToDictionary(x => x.Name, x => x.Text) ?? new Dictionary<string, string>();
			string bodyDescription = "";
			for (var i = 0; i < method.ParameterNames.Count; i++)
			{
				var parameterName = method.ParameterNames[i];
				var parameterInfo = method.ParameterInfos[i];
				var parameterSchema = CreateSchema(parameterInfo.ParameterType);

				if (parameterComments.TryGetValue(parameterName, out var desc))
				{
					bodyDescription += $"* _{parameterName}_: {desc}\n";
				}
				else
				{
					bodyDescription += $"* _{parameterName}_: \n";
				}

				jsonSchema.Properties.Add(parameterName, parameterSchema);
			}

			body.Description = bodyDescription;

			// TODO: pull responses from xml docs and add here.
			op.Responses = new OpenApiResponses
			{
				[RESPONSE_200] = new OpenApiResponse
				{
					Description = comments?.Returns ?? "",
					Content = new Dictionary<string, OpenApiMediaType>
					{
						[JSON_CONTENT_TYPE] = new OpenApiMediaType
						{
							Schema = CreateSchema(method.Method.ReturnType)
						}
					}
				}
			};

			pathItem.Operations[OperationType.Post] = op;
			return pathItem;
		}

		private static void RewriteOperationResponseAsReference(OpenApiOperation op)
		{
			if (!op.Responses.TryGetValue(RESPONSE_200, out var response))
				return;
			if (!response.Content.TryGetValue(JSON_CONTENT_TYPE, out var jsonResponse))
				return;
			if (!_schemaToType.TryGetValue(jsonResponse.Schema, out var existingType))
				return;
			if (!_typeToSchema.TryGetValue(existingType, out var existingSchema))
				return;

			if (existingSchema == null) return;
			var referenceSchema = new OpenApiSchema
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.Schema,
					Id = existingSchema.Title
				}
			};
			jsonResponse.Schema = referenceSchema;
		}

		private static void RewriteOperationRequestAsReference(OpenApiOperation op)
		{
			if (!op.RequestBody.Content.TryGetValue(JSON_CONTENT_TYPE, out var opType))
				return;

			var schema = opType.Schema;
			foreach (var kvp in schema.Properties)
			{
				var property = kvp.Value;
				if (!_schemaToType.TryGetValue(property, out var existingType))
					continue;
				if (!_typeToSchema.TryGetValue(existingType, out var existingSchema))
					continue;

				if (existingSchema == null) continue;
				var referenceSchema = new OpenApiSchema
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Schema,
						Id = existingSchema.Title
					}
				};
				schema.Properties[kvp.Key] = referenceSchema;
			}
		}

		private static OpenApiPaths GeneratePaths(IEnumerable<ServiceMethod> serviceMethods)
		{
			var paths = new OpenApiPaths();
			foreach (var method in serviceMethods)
			{
				var pathItem = GeneratePathItem(method);
				paths.Add("/" + method.Path, pathItem);
			}

			foreach (var path in paths.Values)
			{
				foreach (var op in path.Operations.Values)
				{
					RewriteOperationResponseAsReference(op);
					RewriteOperationRequestAsReference(op);
				}
			}
			return paths;
		}

		private static OpenApiSchema CreateSchema(Type type)
		{
			if (typeof(void).IsAssignableFrom(type) || type == null)
			{
				return new OpenApiSchema();
			}

			if (typeof(Task).IsAssignableFrom(type))
			{
				return CreateSchema(type.GenericTypeArguments.Length == 1
				   ? type.GenericTypeArguments[0]
				   : typeof(object));
			}

			if (_typeToSchema.TryGetValue(type, out var existingSchema))
			{
				var tmp = new OpenApiSchema
				{
					Reference = new OpenApiReference
					{
						Type = ReferenceType.Schema,
						Id = existingSchema.Title
					}
				};

				if (type == typeof(bool))
					tmp.Default = new OpenApiBoolean(false);

				return tmp;
			}

			var jsonType = GetJsonTypeName(type);
			var schema = new OpenApiSchema
			{
				Title = type.GetTypeString(),
				Type = jsonType
			};

			void HandleArray()
			{
				// need to get element type.
				var elementType = type.GetElementType();
				if (elementType == null && type.GenericTypeArguments.Length == 1)
				{
					elementType = type.GenericTypeArguments[0];
				}
				// TODO: there are unhandled array cases.
				schema.Items = CreateSchema(elementType);
			}

			void HandleObject()
			{
				// TODO: Handle [SerializeField] and special json considerations.
				var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
				schema.Properties = new Dictionary<string, OpenApiSchema>();
				foreach (var field in fields)
				{
					var fieldSchema = CreateSchema(field.FieldType);

					if (field.FieldType == typeof(bool))
						fieldSchema.Default = new OpenApiBoolean(false);

					schema.Properties.Add(field.Name, fieldSchema);
				}
			}

			switch (jsonType)
			{
				case OBJECT:
					_typeToSchema.Add(type, schema); // avoid recursion hell.
					_schemaToType.Add(schema, type);
					HandleObject();
					break;
				case ARRAY:
					HandleArray();
					break;
				case BOOLEAN:
					schema.Default = new OpenApiBoolean(false);
					break;
			}

			return schema;
		}

		public static void InvalidateSwagger(BeamableMicroService service)
		{
			if (_serviceToDoc.TryGetValue(service, out var _))
			{
				_serviceToDoc.Remove(service);
			}
		}

		public static OpenApiDocument GenerateDocument(BeamableMicroService service)
		{
			if (_serviceToDoc.TryGetValue(service, out var existing))
			{
				return existing;
			}

			var paths = GeneratePaths(service.ServiceMethods.Methods);
			Dictionary<string, OpenApiSchema> schemas = new Dictionary<string, OpenApiSchema>();
			foreach (var kvp in _typeToSchema)
			{
				var key = kvp.Value.Title;
				if (!schemas.ContainsKey(key))
				{
					schemas.Add(key, kvp.Value);
				}
			}
			var document = new OpenApiDocument
			{
				Info = new OpenApiInfo
				{
					Version = "0.0.0", // TODO: make this settable, somehow
					Title = service.MicroserviceName,
					// TODO: Use description
				},
				Servers = new List<OpenApiServer>
			{
			   new OpenApiServer
			   {
				  Url = service.PublicHost
			   }
			},
				Paths = paths,
				Components = new OpenApiComponents
				{
					Schemas = schemas,
					SecuritySchemes = new Dictionary<string, OpenApiSecurityScheme>
					{
						[SCOPE] = _scopeSecurityScheme,
						[USER] = _userSecurityScheme,
					}
				}
			};

			_serviceToDoc[service] = document;
			return document;
		}

		public static string GetDocJson(OpenApiDocument doc)
		{
			var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
			return outputString;
		}
	}
}
