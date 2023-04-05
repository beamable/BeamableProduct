using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server.Common;
using microservice.Extensions;
using Newtonsoft.Json;
using Serilog;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Server
{
   public struct ServiceMethodProvider
   {
      public Type instanceType;
      public Func<RequestContext, object> factory;
      public string pathPrefix;
   }

   public static class ServiceMethodHelper
   {
      public static ServiceMethodCollection Scan(MicroserviceAttribute serviceAttribute, ICallableGenerator[] generators, params ServiceMethodProvider[] serviceMethodProviders)
      {
         var output = new List<ServiceMethod>();
         foreach (var provider in serviceMethodProviders)
         {
            output.AddRange(ScanType(serviceAttribute, provider));
            foreach (var gen in generators)
            {
	            output.AddRange(gen.ScanType(serviceAttribute, provider));
            }
         }
         return new ServiceMethodCollection(output);
      }


      public static ServiceMethod CreateMethod(
	      MicroserviceAttribute serviceAttribute,
	      ServiceMethodProvider provider,
	      string path,
	      string tag,
	      bool requiredUser,
	      HashSet<string> requiredScopes,
	      MethodInfo method)
      {
	      var swaggerCategoryAttribute = method.GetCustomAttribute<SwaggerCategoryAttribute>();
	      if (swaggerCategoryAttribute != null)
	      {
		      tag = swaggerCategoryAttribute.CategoryName.FirstCharToUpperRestToLower();
	      }

	      var serializerAttribute = method.GetCustomAttribute<CustomResponseSerializationAttribute>();
#pragma warning disable 618
	      IResponseSerializer responseSerializer =
		      new DefaultResponseSerializer(serviceAttribute.UseLegacySerialization);
#pragma warning restore 618
	      if (serializerAttribute != null)
	      {
		      responseSerializer = new CustomResponseSerializer(serializerAttribute);
	      }

	      GenerateMethodExecution(method,
		      out var executor,
		      out var deserializers,
		      out var namedDeserializers,
		      out var parameterNames,
		      out var parameters);

	      var serviceMethod = new ServiceMethod
	      {
		      ParameterInfos = parameters.ToList(),
		      InstanceFactory = provider.factory,
		      ParameterNames = parameterNames,
		      ParameterDeserializers = namedDeserializers,
		      RequiredScopes = requiredScopes,
		      RequireAuthenticatedUser = requiredUser,
		      Path = path,
		      Deserializers = deserializers,
		      Method = method,
		      Executor = executor,
		      ResponseSerializer = responseSerializer,
		      Tag = tag
	      };
	      return serviceMethod;
      }

	   private static void GenerateMethodExecution(MethodInfo method, 
	      out MethodInvocation executor,
	      out List<ParameterDeserializer> deserializers,
	      out Dictionary<string, ParameterDeserializer> namedDeserializers,
	      out List<string> parameterNames,
	      out ParameterInfo[] parameters
	      )
      {
	      parameters = method.GetParameters();
	      deserializers = new List<ParameterDeserializer>();
	      namedDeserializers = new Dictionary<string, ParameterDeserializer>(); // parameter name -> deserializer
	      parameterNames = new List<string>();
	      foreach (var parameter in parameters)
	      {
		      var pType = parameter.ParameterType;
		      var parameterAttribute = parameter.GetCustomAttribute<ParameterAttribute>();
		      var parameterName = parameterAttribute?.ParameterNameOverride ?? parameter.Name;

		      ParameterDeserializer deserializer = (json) =>
		      {
			      if (typeof(string) == pType)
			      {
				      return json.Substring(1, json.Length - 2); // peel off the quotes
			      }

			      var deserializeObject =
				      JsonConvert.DeserializeObject(json, pType, UnitySerializationSettings.Instance);
			      return deserializeObject;
		      };
		      if (namedDeserializers.ContainsKey(parameterName))
		      {
			      throw new BeamableMicroserviceException(
				      $"parameter name is duplicated name=[{parameterName}] method=[{method.Name}]")
			      {
				      ErrorCode = BeamableMicroserviceException.kBMS_ERROR_CODE_DUPLICATED_PARAMTER_NAME
			      };
		      }

		      parameterNames.Add(parameterName);
		      namedDeserializers.Add(parameterName, deserializer);
		      deserializers.Add(deserializer);
	      }

	      var resultType = method.ReturnType;

	      if (resultType.IsSubclassOf(typeof(Promise<Unit>)))
		      resultType = typeof(Promise<Unit>);

	      if ((resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Promise<>)))
	      {
		      executor = (target, args) =>
		      {
			      var promiseObject = method.Invoke(target, args);
			      var promiseMethod = typeof(BeamableTaskExtensions).GetMethod(
				      nameof(BeamableTaskExtensions.TaskFromPromise), BindingFlags.Static | BindingFlags.Public);

			      return (Task)promiseMethod.MakeGenericMethod(resultType.GetGenericArguments()[0])
				      .Invoke(null, new[] { promiseObject });
		      };
	      }
	      else
	      {
		      var isAsync = null != method.GetCustomAttribute<AsyncStateMachineAttribute>();

		      if (isAsync)
		      {
			      executor = (target, args) =>
			      {
				      var task = (Task)method.Invoke(target, args);
				      return task;
			      };
		      }
		      else
		      {
			      executor = (target, args) =>
			      {
				      var invocationResult = method.Invoke(target, args);
				      return Task.FromResult(invocationResult);
			      };
		      }
	      }
      }

      private static List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
      {
         var type = provider.instanceType;
         var output = new List<ServiceMethod>();

         Log.Debug(Logs.SCANNING_CLIENT_PREFIX + type.Name);

         var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
         foreach (var method in allMethods)
         {
            var attribute = method.GetCustomAttribute<CallableAttribute>();
            if (attribute == null) continue;

            var tag = provider.pathPrefix == "admin/" ? "Admin" : "Uncategorized";
            var swaggerCategoryAttribute = method.GetCustomAttribute<SwaggerCategoryAttribute>();
            if (swaggerCategoryAttribute != null)
            {
               tag = swaggerCategoryAttribute.CategoryName.FirstCharToUpperRestToLower();
            }
            
            var servicePath = attribute.PathName;
            if (string.IsNullOrEmpty(servicePath))
            {
               servicePath = method.Name;
            }

            servicePath = provider.pathPrefix + servicePath;

            var requiredScopes = attribute.RequiredScopes;
            var requiredUser = attribute.RequireAuthenticatedUser;

            Log.Debug("Found {method} for {path}", method.Name, servicePath);
            var serviceMethod = CreateMethod(
	            serviceAttribute,
	            provider,
	            servicePath,
	            tag,
	            requiredUser,
	            requiredScopes,
	            method);
            
            if (output.Select(sm => sm.Path).Contains(servicePath))
               throw new BeamableMicroserviceException($"Overloaded Callables are not currently supported in C#MS! Class={method.DeclaringType.Name} Method={method.Name}")
                  { ErrorCode = BeamableMicroserviceException.kBMS_ERROR_CODE_OVERLOADED_METHOD_UNSUPPORTED };
            
            output.Add(serviceMethod);
         }

         return output;
      }
   }
}
