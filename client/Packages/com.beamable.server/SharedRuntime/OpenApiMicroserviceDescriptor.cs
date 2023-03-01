using Beamable.Common;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using Beamable.Server.Editor;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedRuntime
{
	public class MicroserviceArgument
	{
		public string name;
		public Type type;
	}
	public class MicroserviceEndPointInfo
	{
		public CallableAttribute callableAttribute;
		public string methodName;
		public Type returnType;
		public IList<MicroserviceArgument> arguments;
	}
	public class OpenApiMicroserviceDescriptor : IDescriptor
	{
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

		public OpenApiMicroserviceDescriptor(string openApi)
		{
			_openApi = openApi;
		}

		public bool Build()
		{
			try
			{
				var array = (ArrayDict)Json.Deserialize(_openApi);
				if (array["info"] is ArrayDict info)
				{
					Name = info["title"] as string;
					try
					{
						// Type = Type.GetType($"Beamable.Microservices.{Name}");
					}
					catch
					{
					}
					var paths = array["paths"] as ArrayDict;
					var endPoints = paths.Keys.ToList();
					BeamableLogger.Log(Name + " with endpoints:\n\t-" + string.Join("\n\t-",endPoints));
					
					_methods = new List<MicroserviceEndPointInfo>(endPoints.Count);
					foreach (string key in endPoints)
					{
						var endPoint = paths[key] as ArrayDict;
						var post = endPoint["post"] as ArrayDict;
						var response =
							(ArrayDict)((ArrayDict)((ArrayDict)((ArrayDict)((ArrayDict)post["responses"])["200"])["content"])[
								"application/json"])["schema"];
						var responseTypeString = response["title"] as string;
						var responseType = Type.GetType(responseTypeString);
						var name = key.Replace("/", string.Empty);
						name = $"{char.ToUpperInvariant(name[0])}{name[1..]}";

						var methodInfo = new MicroserviceEndPointInfo() {callableAttribute = new CallableAttribute(pathnameOverride:key,requireAuthenticatedUser:true), returnType = responseType, methodName = name};
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
	}
}
