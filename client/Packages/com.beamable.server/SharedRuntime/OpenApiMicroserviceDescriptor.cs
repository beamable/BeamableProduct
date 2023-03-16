using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Beamable.Server
{
	public class OpenApiMicroserviceDescriptor : IMicroserviceApi
	{
		private readonly Type[] _types;
		private readonly string _openApi;
		public string Name { get; set; }
		public Type Type { get; set; }
		public bool HasValidationError { get; }
		public List<MicroserviceEndPointInfo> EndPoints { get; private set; }

		public OpenApiMicroserviceDescriptor(string openApi)
		{
			_openApi = openApi;
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var types = new List<Type>();
			foreach (var assembly in assemblies)
			{
				types.AddRange(assembly.GetTypes());
			}
			_types = types.ToArray();
			HasValidationError = !Build();
		}

		bool Build()
		{
			try
			{
				var array = (ArrayDict)Json.Deserialize(_openApi);
				if (array["info"] is ArrayDict info)
				{
					Name = info["title"] as string;
					Type = TryFindServiceType();
					var paths = array["paths"] as ArrayDict;
					var endPoints = paths.Keys.ToList();

					EndPoints = new List<MicroserviceEndPointInfo>(endPoints.Count);
					foreach (string key in endPoints)
					{
						if(key.StartsWith("/admin/")) // TODO Configure that maybe?
							continue;
						var endPoint = (ArrayDict)paths[key];
						var post = (ArrayDict) endPoint?["post"];
						if (post == null)
							continue;
						var methodInfo = ReadEndPointInfo(post, key);
						EndPoints.Add(methodInfo);
					}
				}
			}
			catch (Exception e)
			{
				BeamableLogger.LogException(e);
				return false;
			}

			return true;
		}

		private MicroserviceEndPointInfo ReadEndPointInfo(ArrayDict post, string name)
		{
			var description = post["summary"] as string;
			Type responseType = GetResponseType(post);
			name = name.Replace("/", string.Empty);
			name = $"{char.ToUpperInvariant(name[0])}{name.Substring(1)}";
			var arguments = GetArgumentsList(post);

			return new MicroserviceEndPointInfo()
			{
				description = description,
				callableAttribute =
					new CallableAttribute(pathnameOverride: name.StartsWith("/") ? name.Substring(1) : name,
					                      requireAuthenticatedUser: true),
				returnType = responseType,
				methodName = name,
				parameters = arguments
			};
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private IList<MicroserviceArgument> GetArgumentsList(ArrayDict post)
		{
			var result = new List<MicroserviceArgument>();
			var argContent = post.JsonPath("requestBody.content") as ArrayDict;
			if (argContent == null || argContent.Count == 0)
				return result;
			var properties = argContent.JsonPath("application/json.schema.properties") as ArrayDict;
			foreach (string property in properties.Keys)
			{
				var type = ReadType((ArrayDict)properties[property]);
				result.Add(new MicroserviceArgument(){name=property,type = type});
			}
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Type GetResponseType(ArrayDict post)
		{
			var schema = post.JsonPath("responses.200.content.application/json.schema") as ArrayDict;
			var responseType = ReadType(schema);
			return responseType;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Type ReadType(ArrayDict schema)
		{
			var typeString = schema["title"] as string;
			var hasTypeString = !string.IsNullOrWhiteSpace(typeString);
			if (!hasTypeString)
			{
				typeString = schema["$ref"] as string;
				typeString = typeString?.Replace("#/components/schemas/", string.Empty);
				hasTypeString = !string.IsNullOrWhiteSpace(typeString);
			}
			var type = schema["type"] as string;
			if (type != null && type.Equals("array"))
			{
				typeString = typeString.Replace("System.Array_", string.Empty);
				typeString = $"{typeString}[]";
			}
			var containsUnderscore = hasTypeString && typeString.Contains("_");

			if(containsUnderscore)
			{
				var typesStrings = typeString.Split('_');
				if (typesStrings.Length == 2)
				{
					var genericType = GetType(typesStrings[0] + "`1");
					var argumentType = GetType(typesStrings[1]);
					var responseGenericType = genericType.MakeGenericType(argumentType);
					return responseGenericType;
				}
			}
			var responseType = GetType(typeString);
			return responseType;
		}

		private Type GetType(string type)
		{
			if (string.IsNullOrWhiteSpace(type)) return typeof(void);
			var resultType = Type.GetType(type) ?? _types.FirstOrDefault(type1 => type1.FullName.Equals(type));
			return resultType;
		}

		private Type TryFindServiceType()
		{
			foreach (var type in _types)
				if (typeof(Microservice).IsAssignableFrom(type) &&
				    type.Name != null && type.Name.Equals(Name))
					return type;

			return null;
		}
	}
}
