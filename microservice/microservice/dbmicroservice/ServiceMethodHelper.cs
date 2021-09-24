using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Beamable.Server.Common;
using LoxSmoke.DocXml;
using microservice.Extensions;
using Newtonsoft.Json;
using Serilog;

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

      private static MethodInfo Convert(this MethodInfo method,params Type[] DeclaringTypeArguments)
      {
         var baseType = method.DeclaringType.GetGenericTypeDefinition().MakeGenericType(DeclaringTypeArguments);
         return MethodInfo.GetMethodFromHandle(method.MethodHandle, baseType.TypeHandle) as MethodInfo;
      }
      
      private static List<ServiceMethod> ScanType(MicroserviceAttribute serviceAttribute, ServiceMethodProvider provider)
      {
         var type = provider.instanceType;
         var output = new List<ServiceMethod>();

         Log.Debug("Scanning client methods for {typeName}", type.Name);

         var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
         foreach (var method in allMethods)
         {
            var closureMethod = method;
            Log.Debug("methodNameX {methodName}", method.Name);
            if (method.ReturnType.IsGenericType)
            {
               Log.Debug("resultTypeX1 {typeName}", method.ReturnType);
               var resultType = method.ReturnType.GetGenericArguments()[0];
               Log.Debug("resultTypeX2 {typeName}", resultType);
            }
            var attribute = closureMethod.GetCustomAttribute<ClientCallableAttribute>();
            if (attribute == null) continue;

            var tag = provider.pathPrefix == "admin/" ? "Admin" : "Uncategorized";
            var swaggerCategoryAttribute = closureMethod.GetCustomAttribute<SwaggerCategoryAttribute>();
            if (swaggerCategoryAttribute != null)
            {
               tag = swaggerCategoryAttribute.CategoryName.FirstCharToUpperRestToLower();
            }

            var serializerAttribute = closureMethod.GetCustomAttribute<CustomResponseSerializationAttribute>();
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
               servicePath = closureMethod.Name;
            }

            servicePath = provider.pathPrefix + servicePath;

            var requiredScopes = attribute.RequiredScopes;

            Log.Debug("Found {method} for {path}", closureMethod.Name, servicePath);

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
                  throw new Exception($"parameter name is duplicated name=[{parameterName}] method=[{closureMethod.Name}]");
               }

               parameterNames.Add(parameterName);
               namedDeserializers.Add(parameterName, deserializer);
               deserializers.Add(deserializer);
            }
            
            var isAsync = null != closureMethod.GetCustomAttribute<AsyncStateMachineAttribute>();

            MethodInvocation executor;

            if (isAsync)
            {
               executor = (target, args) =>
               {
                  var task = (Task)closureMethod.Invoke(target, args);
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

            var serviceMethod = new ServiceMethod
            {
               ParameterInfos = parameters.ToList(),
               InstanceFactory = provider.factory,
               ParameterNames = parameterNames,
               ParameterDeserializers = namedDeserializers,
               RequiredScopes = requiredScopes,
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