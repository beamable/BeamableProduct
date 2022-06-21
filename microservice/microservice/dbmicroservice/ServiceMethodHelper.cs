using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Server.Common;
using LoxSmoke.DocXml;
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
      public static ServiceMethodCollection Scan(MicroserviceAttribute serviceAttribute, params ServiceMethodProvider[] serviceMethodProviders)
      {
         var output = new List<ServiceMethod>();
         foreach (var provider in serviceMethodProviders)
         {
            output.AddRange(ScanType(serviceAttribute, provider));
         }
         return new ServiceMethodCollection(output);
      }

      private static List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
      {
         var type = provider.instanceType;
         var output = new List<ServiceMethod>();

         Log.Debug(Logs.SCANNING_CLIENT_PREFIX + type.Name);

         var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
         foreach (var method in allMethods)
         {
            var closureMethod = method;
            var attribute = method.GetCustomAttribute<CallableAttribute>();
            if (attribute == null) continue;

            var tag = provider.pathPrefix == "admin/" ? "Admin" : "Uncategorized";
            var swaggerCategoryAttribute = method.GetCustomAttribute<SwaggerCategoryAttribute>();
            if (swaggerCategoryAttribute != null)
            {
               tag = swaggerCategoryAttribute.CategoryName.FirstCharToUpperRestToLower();
            }

            var serializerAttribute = method.GetCustomAttribute<CustomResponseSerializationAttribute>();
#pragma warning disable 618
            IResponseSerializer responseSerializer = new DefaultResponseSerializer(serviceAttribute.UseLegacySerialization);
#pragma warning restore 618
            if (serializerAttribute != null)
            {
               responseSerializer = new CustomResponseSerializer(serializerAttribute);
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

            var parameters = method.GetParameters();

            var deserializers = new List<ParameterDeserializer>();
            var namedDeserializers = new Dictionary<string, ParameterDeserializer>(); // parameter name -> deserializer
            var parameterNames = new List<string>();
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

                  var deserializeObject = JsonConvert.DeserializeObject(json, pType, UnitySerializationSettings.Instance);
                  return deserializeObject;
               };
               if (namedDeserializers.ContainsKey(parameterName))
               {
                  throw new BeamableMicroserviceException($"parameter name is duplicated name=[{parameterName}] method=[{method.Name}]")
                     { ErrorCode = BeamableMicroserviceException.kBMS_ERROR_CODE};
               }

               parameterNames.Add(parameterName);
               namedDeserializers.Add(parameterName, deserializer);
               deserializers.Add(deserializer);
            }
            
            MethodInvocation executor;

            var resultType = method.ReturnType;
            
            if (resultType.IsSubclassOf(typeof(Promise<Unit>)))
               resultType = typeof(Promise<Unit>);

            if ((resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Promise<>)))
            {
               executor = (target, args) =>
               {
                  var promiseObject = closureMethod.Invoke(target, args);
                  var promiseMethod = typeof(BeamableTaskExtensions).GetMethod(
                     nameof(BeamableTaskExtensions.TaskFromPromise), BindingFlags.Static | BindingFlags.Public);
                  
                  return (Task)promiseMethod.MakeGenericMethod(resultType.GetGenericArguments()[0])
                     .Invoke(null, new[] {promiseObject});
               };
            }
            else
            {
               var isAsync = null != method.GetCustomAttribute<AsyncStateMachineAttribute>();
               
               if (isAsync)
               {
                  executor = (target, args) =>
                  {
                     var task = (Task) closureMethod.Invoke(target, args);
                     return task;
                  };
               }
               else
               {
                  executor = (target, args) =>
                  {
                     var invocationResult = closureMethod.Invoke(target, args);
                     return Task.FromResult(invocationResult);
                  };
               }
            }

            var serviceMethod = new ServiceMethod
            {
               ParameterInfos = parameters.ToList(),
               InstanceFactory = provider.factory,
               ParameterNames = parameterNames,
               ParameterDeserializers = namedDeserializers,
               RequiredScopes = requiredScopes,
               RequireAuthenticatedUser = requiredUser,
               Path = servicePath,
               Deserializers = deserializers,
               Method = method,
               Executor = executor,
               ResponseSerializer = responseSerializer,
               Tag = tag
            };
            output.Add(serviceMethod);
         }

         return output;
      }
   }
}