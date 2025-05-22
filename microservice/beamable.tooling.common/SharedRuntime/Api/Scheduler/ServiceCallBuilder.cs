using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Scheduler;
using Beamable.Serialization.SmallerJSON;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Beamable.Server
{

	public static class BeamSchedulerExtensions
	{

		public static ServiceCallBuilder<T> GetMicroserviceCallHelper<T>(this BeamScheduler scheduler)
			where T : Microservice
		{
			var builder = new ServiceCallBuilder<T>(true, scheduler.SchedulerContext);
			return builder;
		}
	}

	public class ServiceCallBuilder<T> : IServiceCallBuilder<T, ServiceAction>
		where T : Microservice
	{
		private readonly bool _useLocal;
		private readonly IBeamSchedulerContext _ctx;
		public ServiceCallBuilder(bool useLocal, IBeamSchedulerContext ctx)
		{
			_useLocal = useLocal;
			_ctx = ctx;
		}


		public ServiceAction Run(Expression<Func<T, Func<Promise>>> expr)
		{
			var call = CreateAction(expr);
			return call;
		}

		public ServiceAction Run(Expression<Func<T, Action>> expr)
		{
			var call = CreateAction(expr);
			return call;
		}

		public ServiceAction Run(Expression<Func<T, Func<Task>>> expr) => CreateAction(expr);

		public ServiceAction Run<TArg1>(Expression<Func<T, Func<TArg1, Task>>> expr, TArg1 arg)
		 => CreateAction(expr, arg);

		public ServiceAction Run<TArg1, TArg2>(Expression<Func<T, Func<TArg1, TArg2, Task>>> expr, TArg1 arg1, TArg2 arg2)
			=> CreateAction(expr, arg1, arg2);

		public ServiceAction Run<TArg1, TArg2, TArg3>(Expression<Func<T, Func<TArg1, TArg2, TArg3, Task>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3)
			=> CreateAction(expr, arg1, arg2, arg3);


		public ServiceAction Run<TArg1, TArg2, TArg3, TArg4>(Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, Task>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
			=> CreateAction(expr, arg1, arg2, arg3, arg4);


		public ServiceAction Run<TArg1, TArg2, TArg3, TArg4, TArg5>(Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4,
			TArg5 arg5)
			=> CreateAction(expr, arg1, arg2, arg3, arg4, arg5);


		public ServiceAction Run<TArg1>(Expression<Func<T, Func<TArg1, Promise>>> expr,
			TArg1 arg)
		{
			var call = CreateAction(expr, arg);
			return call;
		}

		public ServiceAction Run<TArg1, TArg2>(Expression<Func<T, Func<TArg1, TArg2, Promise>>> expr,
			TArg1 arg1,
			TArg2 arg2)
		{
			var call = CreateAction(expr, arg1, arg2);
			return call;
		}


		public ServiceAction Run<TArg1, TArg2, TArg3>(Expression<Func<T, Func<TArg1, TArg2, TArg3, Promise>>> expr,
			TArg1 arg1,
			TArg2 arg2,
			TArg3 arg3)
		{
			var call = CreateAction(expr, arg1, arg2, arg3);
			return call;
		}

		public ServiceAction Run<TArg1, TArg2, TArg3, TArg4>(
			Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, Promise>>> expr,
			TArg1 arg1,
			TArg2 arg2,
			TArg3 arg3,
			TArg4 arg4)
		{
			var call = CreateAction(expr, arg1, arg2, arg3, arg4);
			return call;
		}

		public ServiceAction Run<TArg1, TArg2, TArg3, TArg4, TArg5>(
			Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Promise>>> expr,
			TArg1 arg1,
			TArg2 arg2,
			TArg3 arg3,
			TArg4 arg4,
			TArg5 arg5)
		{
			var call = CreateAction(expr, arg1, arg2, arg3, arg4, arg5);
			return call;
		}

		private ServiceAction CreateAction(LambdaExpression expr, params object[] args)
		{
			var info = GetServiceMethodInfo(expr);
			var uri = GetUri(info.pathName);
			var json = GetBody(info.parameterNames, args);

			var call = new ServiceAction { method = Method.POST, body = json, uri = uri, routingKey = info.routingKey };
			return call;
		}

		private string GetBody(string[] parameterNames, params object[] args)
		{
			var dict = new ArrayDict();
			for (var i = 0; i < parameterNames.Length; i++)
			{
				dict.Add(parameterNames[i], args[i]);
			}

			return Json.Serialize(dict, new StringBuilder());
		}


		private string GetUri(string path)
		{
			var uri = BeamScheduler.Utility.GetServiceUrl(
				_ctx.Cid,
				_ctx.Pid,
				_ctx.ServiceName, path);

			return uri;
		}

		private ServiceMethodInfo GetServiceMethodInfo(LambdaExpression expression)
		{
			if (expression == null)
				throw new ArgumentNullException(nameof(expression));

			Expression body = expression.Body;
			while (body.NodeType == ExpressionType.Convert || body.NodeType == ExpressionType.ConvertChecked)
			{
				body = ((UnaryExpression)body).Operand;
			}

			if (body.NodeType != ExpressionType.Call)
				throw new ArgumentException("Invalid expression. Expected a method call.", nameof(expression));

			var expressionCall = (MethodCallExpression)body;
			var objectExpression = expressionCall.Object as ConstantExpression;
			if (objectExpression == null)
				throw new ArgumentException("Invalid expression. objectExpression is null.", nameof(expression));

			if (objectExpression.Type != typeof(MethodInfo))
				throw new ArgumentException("Invalid expression. Expected a methodInfo.", nameof(expression));
			var methodInfo = objectExpression.Value as MethodInfo;
			if (methodInfo == null)
				throw new ArgumentException("Invalid expression. methodInfo is null.", nameof(expression));

			var callable = methodInfo.GetCustomAttribute<CallableAttribute>(inherit: true);
			if (callable == null)
			{
				throw new ArgumentException("Invalid expression. Must point to a callable expression");
			}

			if (callable.RequireAuthenticatedUser)
			{
				throw new ArgumentException(
					"Invalid expression. Unfortunately, only callables that don't require users are supported. Consider using " +
					nameof(ServerCallableAttribute));
			}

			var pathName = string.IsNullOrEmpty(callable.PathName) ? methodInfo.Name : callable.PathName;

			var parameters = methodInfo.GetParameters();
			var parameterNames = new string[parameters.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				var parameterName = parameters[i].GetCustomAttribute<ParameterAttribute>()?.ParameterNameOverride;
				if (string.IsNullOrEmpty(parameterName))
				{
					parameterName = parameters[i].Name;
				}

				parameterNames[i] = parameterName;
			}
			bool hasLocalPrefix = !string.IsNullOrEmpty(_ctx.Prefix);
			var routingKey = _useLocal && hasLocalPrefix
				? new OptionalString($"micro_{_ctx.ServiceName}:{_ctx.Prefix}")
				: new OptionalString();

			return new ServiceMethodInfo { parameterNames = parameterNames, pathName = pathName, routingKey = routingKey };
		}

		struct ServiceMethodInfo
		{
			public string pathName;
			public string[] parameterNames;
			public OptionalString routingKey;
		}

	}
}
