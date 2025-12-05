using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Common.Dependencies;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Beamable.Server
{

	/// <summary>
	/// Delegate representing a method that deserializes parameters from JSON format.
	/// </summary>
	/// <param name="json">The JSON string containing the parameter data.</param>
	/// <returns>The deserialized parameter object.</returns>
	public delegate object ParameterDeserializer(string json);

	/// <summary>
	/// Delegate representing an asynchronous method invocation.
	/// </summary>
	/// <param name="target">The object on which to invoke the method.</param>
	/// <param name="args">The arguments to pass to the method.</param>
	/// <returns>A task representing the asynchronous execution of the method.</returns>
	public delegate Task MethodInvocation(object target, object[] args);

	/// <summary>
	/// Represents a service method with its associated metadata and behavior.
	/// </summary>
	public class ServiceMethod
	{
		/// <summary>
		/// Whether or not this is a method that is meant to be called by the beamable backend as part of one of our federated flows (<see cref="FederatedLoginCallableGenerator"/>).
		/// </summary>
		public bool IsFederatedCallbackMethod { get; set; }
		
		/// <summary>
		/// The tag associated with the service method.
		/// </summary>
		public string Tag { get; set; }

		/// <summary>
		/// The path associated with the service method.
		/// </summary>
		public string Path { get; set; }
		
		/// <summary>
		/// A namespace associated with the service method.
		/// </summary>
		public string ClientNamespacePrefix { get; set; }

		/// <summary>
		/// Factory function to create an instance of the service method's target.
		/// </summary>
		public ServiceMethodInstanceFactory InstanceFactory;

		/// <summary>
		/// The set of required scopes for accessing the service method.
		/// </summary>
		public HashSet<string> RequiredScopes { get; set; }

		/// <summary>
		/// Indicates whether an authenticated user is required to access the service method.
		/// </summary>
		public bool RequireAuthenticatedUser { get; set; }

		/// <summary>
		/// List of parameter information for the service method.
		/// </summary>
		public List<ParameterInfo> ParameterInfos { get; set; }

		/// <summary>
		/// The method to be invoked for the service.
		/// </summary>
		public MethodInfo Method { get; set; }

		/// <summary>
		/// List of parameter deserializers for the service method.
		/// </summary>
		public List<ParameterDeserializer> Deserializers;

		/// <summary>
		/// List of parameter names for the service method.
		/// </summary>
		public List<string> ParameterNames { get; set; }

		/// <summary>
		/// Dictionary mapping parameter names to their corresponding deserializers.
		/// </summary>
		public Dictionary<string, ParameterDeserializer> ParameterDeserializers;

		/// <summary>
		/// A map from parameter name to the source of the parameter
		/// </summary>
		public Dictionary<string, ParameterSource> ParameterSources = new Dictionary<string, ParameterSource>();

		/// <summary>
		/// The method invocation delegate for the service method.
		/// </summary>
		public MethodInvocation Executor;

		/// <summary>
		/// The response serializer for the service method.
		/// </summary>
		public IResponseSerializer ResponseSerializer;

		/// <summary>
		/// Gets the arguments for the method invocation from the provided parameter provider.
		/// </summary>
		private object[] GetArgs(RequestContext ctx, IParameterProvider parameterProvider, IDependencyProvider provider)
		{
			try
			{
				var args = parameterProvider.GetParameters(this, provider);
				return args;
			}
			catch (Exception ex ) when (ex is not MicroserviceException)
			{
				throw new BadInputException(ctx?.Body, ex);
			}
		}

		/// <summary>
		/// Executes the service method with the provided request context and parameter provider.
		/// </summary>
		public async Task<object> Execute(MicroserviceRequestContext ctx, IParameterProvider parameterProvider)
		{
			var instanceData = InstanceFactory(ctx);
			var target = instanceData.instance;
			var args = GetArgs(ctx, parameterProvider, instanceData.provider);
			
			try
			{
				var task = Executor(target, args);
				if (task != null)
				{
					await task;

					var resultProperty = task.GetType().GetProperty("Result");
					var result =
						resultProperty
							.GetValue(
								task); // TODO: XXX It stinks that there is active reflection going on the callpath

					if (result is string strResult)
					{
						return strResult; // If the data is already in a string format, then just use that.
					}

					return result;
				}
				else
				{
					return "";
				}
			}
			finally
			{
				if (target is Microservice microservice)
				{
					await microservice.DisposeMicroservice();
				}
			}
		}
	}
}
