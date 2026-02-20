using System.Reflection;
using System.Runtime.CompilerServices;
using Beamable.Common;
using Beamable.Server.Common;
using microservice.Common;
using microservice.Extensions;
using Newtonsoft.Json;

using System.Text;
using Beamable.Common.Dependencies;
using beamable.tooling.common.Microservice;
using ZLogger;
using static Beamable.Common.Constants.Features.Services;

namespace Beamable.Server
{
	public class ServiceMethodInstanceData : ServiceMethodInstanceData<object>
	{
	}

	public class ServiceMethodInstanceData<T> where T : class
	{
		public IDependencyProvider provider;
		public T instance;
	}

	public delegate ServiceMethodInstanceData ServiceMethodInstanceFactory(MicroserviceRequestContext context);
	
	/// <summary>
	/// Represents a provider for service methods.
	/// </summary>
	public struct ServiceMethodProvider
	{
		/// <summary>
		/// The instance type associated with the service methods.
		/// </summary>
		public Type instanceType;

		/// <summary>
		/// The factory function to create service method instances.
		/// </summary>
		public ServiceMethodInstanceFactory factory;

		/// <summary>
		/// The path prefix for the service methods.
		/// </summary>
		public string pathPrefix;

		/// <summary>
		/// The client code prefix for the service methods.
		/// </summary>
		public string clientPrefix;
	}

	/// <summary>
	/// Helper class for managing service methods.
	/// </summary>
   public static class ServiceMethodHelper
   {
	   /// <summary>
	   /// Scans for service methods based on various providers and generators.
	   /// </summary>
      public static ServiceMethodCollection Scan(IMicroserviceAttributes serviceAttribute, params ServiceMethodProvider[] serviceMethodProviders)
      {
         var output = new List<ServiceMethod>();
         foreach (var provider in serviceMethodProviders)
         {
            output.AddRange(ScanType(serviceAttribute, provider));
            if (provider.instanceType == typeof(AdminRoutes))
            {
	            continue;
            }
            output.AddRange(ScanTypeFederation(serviceAttribute, provider));
         }
         return new ServiceMethodCollection(output);
      }
	   
	   private static List<ServiceMethod> ScanTypeFederation(IMicroserviceAttributes serviceAttribute, ServiceMethodProvider provider)
	   {
		   var type = provider.instanceType;
		   var output = new List<ServiceMethod>();

		   var interfaces = type.GetInterfaces();

		   var methodToPathMap = new Dictionary<string, string>
		   {
			   [nameof(IFederatedGameServer<DummyThirdParty>.CreateGameServer)] = "servers",
			   [nameof(IFederatedPlayerInit<DummyThirdParty>.CreatePlayer)] = "player",
			   [nameof(IFederatedInventory<DummyThirdParty>.GetInventoryState)] = "inventory/state",
			   [nameof(IFederatedInventory<DummyThirdParty>.StartInventoryTransaction)] = "inventory/put",
			   [nameof(IFederatedLogin<DummyThirdParty>.Authenticate)] = "authenticate",
		   };

		   foreach (var interfaceType in interfaces)
		   {
			   if (!interfaceType.IsGenericType) continue;
			   if (!typeof(IFederation).IsAssignableFrom(interfaceType.GetGenericTypeDefinition())) continue;

			   var map = type.GetInterfaceMap(interfaceType);
			   var federatedType = interfaceType.GetGenericArguments()[0];
			   var identity = Activator.CreateInstance(federatedType) as IFederationId;

			   var federatedNamespace = identity.GetUniqueName();
			   for (var i = 0 ; i < map.TargetMethods.Length; i ++)
			   {
				   var method = map.TargetMethods[i];
				   var interfaceMethod = map.InterfaceMethods[i];
				   var attribute = method.GetCustomAttribute<CallableAttribute>(true);
				   if (attribute != null) continue;

				   if (!methodToPathMap.TryGetValue(interfaceMethod.Name, out var pathName))
				   {
					   var err = $"Unable to map method name to path part. name=[{interfaceMethod.Name}]";
					   throw new Exception(err);
				   }
				   var path = $"{federatedNamespace}/{pathName}";
				   var tag = federatedNamespace;

				   var serviceMethod = ServiceMethodHelper.CreateMethod(
					   serviceAttribute,
					   provider,
					   path,
					   tag,
					   false,
					   new HashSet<string>(new []{"*"}),
					   method,
					   true);

				   Log.Debug("Found Federated method. FederatedPath={FederatedPath}, MethodName={MethodName}", path, serviceMethod.Method.Name);
				   output.Add(serviceMethod);
			   }
		   }

		   return output;
	   }

	   /// <summary>
	   /// Creates a service method based on provided parameters.
	   /// </summary>
	   /// <param name="serviceAttribute">The microservice attribute associated with the service.</param>
	   /// <param name="provider">The service method provider.</param>
	   /// <param name="path">The path for the service method.</param>
	   /// <param name="tag">The tag for the service method.</param>
	   /// <param name="requiredUser">Indicates if a required user is needed.</param>
	   /// <param name="requiredScopes">The set of required scopes.</param>
	   /// <param name="method">The method information for the service method.</param>
	   /// <param name="isFederatedCallbackMethod">Whether or not this method is meant to be used only as part of a federated flow (<see cref="FederatedLoginCallableGenerator"/>).</param>
	   /// <returns>The created service method.</returns>
	   public static ServiceMethod CreateMethod(
	      IMicroserviceAttributes serviceAttribute,
	      ServiceMethodProvider provider,
	      string path,
	      string tag,
	      bool requiredUser,
	      HashSet<string> requiredScopes,
	      MethodInfo method,
	      bool isFederatedCallbackMethod = false)
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
		      out var namedParameterSources,
		      out var parameterNames,
		      out var parameters);

	      var serviceMethod = new ServiceMethod
	      {
		      ClientNamespacePrefix = provider.clientPrefix,
		      ParameterInfos = parameters.ToList(),
		      InstanceFactory = provider.factory,
		      ParameterNames = parameterNames,
		      ParameterDeserializers = namedDeserializers,
		      RequiredScopes = requiredScopes,
		      RequireAuthenticatedUser = requiredUser,
		      ParameterSources = namedParameterSources,
		      Path = path,
		      Deserializers = deserializers,
		      Method = method,
		      Executor = executor,
		      ResponseSerializer = responseSerializer,
		      Tag = tag,
		      IsFederatedCallbackMethod = isFederatedCallbackMethod,
	      };
	      return serviceMethod;
      }

	   private static void GenerateMethodExecution(MethodInfo method, 
	      out MethodInvocation executor,
	      out List<ParameterDeserializer> deserializers,
	      out Dictionary<string, ParameterDeserializer> namedDeserializers,
	      out Dictionary<string, ParameterSource> namedParameterSources,
	      out List<string> parameterNames,
	      out ParameterInfo[] parameters
	      )
      {
	      parameters = method.GetParameters();
	      deserializers = new List<ParameterDeserializer>();
	      namedParameterSources = new Dictionary<string, ParameterSource>();
	      namedDeserializers = new Dictionary<string, ParameterDeserializer>(); // parameter name -> deserializer
	      parameterNames = new List<string>();
	      foreach (var parameter in parameters)
	      {
		      var pType = parameter.ParameterType;
		      var parameterAttribute = parameter.GetCustomAttribute<ParameterAttribute>();
		      var parameterName = parameterAttribute?.ParameterNameOverride ?? parameter.Name;
		      namedParameterSources[parameterName] = parameterAttribute?.Source ?? ParameterSource.Body;
		      ParameterDeserializer deserializer;
		      if (typeof(string) == pType)
		      {
			      deserializer = DeserializeStringParameter;
		      }
		      else
		      {
			      deserializer = json => 
			      {
				      var deserializeObject =
					      JsonConvert.DeserializeObject(json, pType, UnitySerializationSettings.Instance);
				      return deserializeObject;
			      };
		      }
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
		      var isTaskBased = resultType.IsAssignableTo(typeof(Task));
		      if (isTaskBased)
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

	   /// <summary>
	   /// Deserializes string type parameter
	   /// </summary>
	   /// <param name="json"></param>
	   /// <returns></returns>
	   public static object DeserializeStringParameter(string json)
	   {
		   if (string.IsNullOrWhiteSpace(json))
			   return json;
		   // first try use SmallerJSON for handling escape chars passed from Unity
		   var smallerJson = Serialization.SmallerJSON.Json.Deserialize(json);

		   if (smallerJson is string result)
		   {
			   return result;
		   }

		   if (smallerJson is Serialization.SmallerJSON.ArrayDict dict)
		   {
			   return Serialization.SmallerJSON.Json.Serialize(dict, new StringBuilder());
		   }

		   // or just peel off the quotes
		   return json.Substring(1, json.Length - 2);
	   }

	   private static List<ServiceMethod> ScanType(IMicroserviceAttributes serviceAttribute, ServiceMethodProvider provider)
      {
         var type = provider.instanceType;
         var output = new List<ServiceMethod>();

         BeamableZLoggerProvider.LogContext.Value.ZLogDebug($"{Logs.SCANNING_CLIENT_PREFIX} {type.Name}");
        
         var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
         foreach (var method in allMethods)
         {
            var attribute = method.GetCustomAttribute<CallableAttribute>();
            if (attribute == null)
            {
	            BeamableZLoggerProvider.LogContext.Value.ZLogTrace($"Skipped {method.Name}");
	            continue;
            }

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

            servicePath = (provider.pathPrefix?.Trim('/') + '/' + servicePath.Trim('/')).Trim('/');

            var requiredScopes = attribute.RequiredScopes;
            var requiredUser = attribute.RequireAuthenticatedUser;

            BeamableZLoggerProvider.LogContext.Value.ZLogTrace($"Found {method.Name} for {servicePath}");

            
            var isAsync = null != method.GetCustomAttribute<AsyncStateMachineAttribute>();
            var isVoid = method.ReturnType == typeof(void);
            if (isAsync && isVoid)
            {
	            throw new BeamableMicroserviceException($"The following method is invalid, method=[{type.Name}.{method.Name}]. Callable methods in Beamable Microservices are not allowed to have the `async void` method signature. Consider using `async Promise` or `async Task` instead. ");
            }
            
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
