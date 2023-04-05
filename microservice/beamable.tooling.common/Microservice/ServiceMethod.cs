using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Beamable.Server
{

	public delegate object ParameterDeserializer(string json);

	public delegate Task MethodInvocation(object target, object[] args);

	public class ServiceMethod
	{
		public string Tag { get; init; }
		public string Path { get; init; }
		public Func<RequestContext, object> InstanceFactory;
		public HashSet<string> RequiredScopes { get; init; }
		public bool RequireAuthenticatedUser { get; init; }
		public List<ParameterInfo> ParameterInfos { get; init; }
		public MethodInfo Method { get; init; }
		public List<ParameterDeserializer> Deserializers;
		public List<string> ParameterNames { get; init; }
		public Dictionary<string, ParameterDeserializer> ParameterDeserializers;
		public MethodInvocation Executor;
		public IResponseSerializer ResponseSerializer;

		private object[] GetArgs(RequestContext ctx, IParameterProvider parameterProvider)
		{
			try
			{
				var args = parameterProvider.GetParameters(this);
				return args;
			}
			catch (Exception ex ) when (ex is not MicroserviceException)
			{
				throw new BadInputException(ctx?.Body, ex);
			}
		}

		public async Task<object> Execute(RequestContext ctx, IParameterProvider parameterProvider)
		{
			var args = GetArgs(ctx, parameterProvider);
			var target = InstanceFactory(ctx);
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
