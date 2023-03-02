using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using Beamable.Server.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharedRuntime
{
	public class MicroserviceArgument
	{
		public string name;
		public Type type;

		public override string ToString() => $"{type.FullName} {name}";
	}

	public class MicroserviceEndPointInfo
	{
		public CallableAttribute callableAttribute;
		public string methodName;
		public Type returnType;
		public IList<MicroserviceArgument> parameters;
		public string description;
	}

	public class OpenApiMicroserviceDescriptor : IDescriptor
	{
		private readonly Type[] _types;
		private readonly string _openApi;
		private List<MicroserviceEndPointInfo> _methods;
		public string Name { get; set; }
		public string AttributePath { get; set; }
		public Type Type { get; set; }
		public string ContainerName { get; set; }
		public string ImageName { get; set; }
		public ServiceType ServiceType => ServiceType.MicroService;
		public bool HasValidationError { get; }
		public bool HasValidationWarning { get; }
		public List<MicroserviceEndPointInfo> Methods => _methods;

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
			HasValidationError = HasValidationWarning = !Build();
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

					_methods = new List<MicroserviceEndPointInfo>(endPoints.Count);
					foreach (string key in endPoints)
					{
						if(key.StartsWith("/admin/")) // TODO Configure that maybe?
							continue;
						if (paths[key] is not ArrayDict endPoint)
							continue;
						if (endPoint["post"] is not ArrayDict post)
							continue;
						var methodInfo = ReadEndPointInfo(post, key);
						_methods.Add(methodInfo);
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
			if (string.IsNullOrWhiteSpace(typeString))
			{
				typeString = schema["$ref"] as string;
				typeString = typeString?.Replace("#/components/schemas/", string.Empty);
			}
			var type = schema["type"] as string;
			if (type != null && type.Equals("array"))
			{
				typeString = typeString.Replace("System.Array_", string.Empty);
				typeString = $"{typeString}[]";
			}
			var responseType = string.IsNullOrWhiteSpace(typeString)
				? typeof(void)
				: Type.GetType(typeString);
			if (responseType == null)
			{
				responseType = _types.FirstOrDefault(type1 => type1.FullName.Equals(typeString));
			}
			return responseType;
		}

		private Type TryFindServiceType()
		{
			foreach (var type in _types)
				if (typeof(Microservice).IsAssignableFrom(type) &&
				    type.Name != null && type.Name.Equals(Name))
					return type;

			return null;
		}

		public string GetFullName()
		{
			if (Type != null)
				return Type.FullName;
			return $"Beamable.Microservices.{Name}";
		}
	}
}
