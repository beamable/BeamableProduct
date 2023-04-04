// using System.Reflection;
//
// namespace Beamable.Server.Common.Microservice;
//
// public class MethodScanner
// {
// 	public static List<T> ScanType<T>(Type type, string pathPrefix, MicroserviceAttribute microserviceAttribute, Func<T> factory)
// 		where T : IServiceMethod
// 	{
// 		var output = new List<T>();
//
// 		// Log.Debug(Logs.SCANNING_CLIENT_PREFIX + type.Name);
//
// 		var allMethods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
// 		foreach (var method in allMethods)
// 		{
// 			var attribute = method.GetCustomAttribute<CallableAttribute>();
// 			if (attribute == null) continue;
//
// 			var tag = pathPrefix == "admin/" ? "Admin" : "Uncategorized";
// 			var swaggerCategoryAttribute = method.GetCustomAttribute<SwaggerCategoryAttribute>();
// 			if (swaggerCategoryAttribute != null)
// 			{
// 				tag = swaggerCategoryAttribute.CategoryName.FirstCharToUpperRestToLower();
// 			}
//             
// 			var servicePath = attribute.PathName;
// 			if (string.IsNullOrEmpty(servicePath))
// 			{
// 				servicePath = method.Name;
// 			}
//
// 			servicePath = pathPrefix + servicePath;
//
// 			var requiredScopes = attribute.RequiredScopes;
// 			var requiredUser = attribute.RequireAuthenticatedUser;
//
// 			// Log.Debug("Found {method} for {path}", method.Name, servicePath);
//
// 			var serviceMethod = factory();
// 			// var serviceMethod = CreateMethod(
// 			// 	serviceAttribute,
// 			// 	provider,
// 			// 	servicePath,
// 			// 	tag,
// 			// 	requiredUser,
// 			// 	requiredScopes,
// 			// 	method);
//    //          
// 			// if (output.Select(sm => sm.Path).Contains(servicePath))
// 			// 	throw new BeamableMicroserviceException($"Overloaded Callables are not currently supported in C#MS! Class={method.DeclaringType.Name} Method={method.Name}")
// 			// 		{ ErrorCode = BeamableMicroserviceException.kBMS_ERROR_CODE_OVERLOADED_METHOD_UNSUPPORTED };
//    //          
// 			output.Add(serviceMethod);
// 		}
//
// 		return output;
// 	}
// }
