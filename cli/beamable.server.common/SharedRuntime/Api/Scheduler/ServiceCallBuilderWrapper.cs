using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Pooling;
using Beamable.Common.Scheduler;
using Beamable.Serialization.SmallerJSON;
using Beamable.Server;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Beamable.Server
{

	public interface IServiceCallBuilderGen<T, TTask, TResponse> where T : Microservice
	{
		/// <summary>
		/// Pick a C#MS method to execute as the <see cref="Job.action"/>.
		/// The <see cref="expr"/> must be an expression that takes a <see cref="T"/> instance
		/// and selects a method that is <see cref="ServerCallableAttribute"/>.
		/// </summary>
		/// <param name="expr">
		/// An expression that takes an instance of <see cref="T"/> and selects a
		/// <see cref="ServerCallableAttribute"/> method. When the method is executed, if it takes
		/// arguments, then those arguments need to passed as additional parameters. All parameters
		/// will be serialized to JSON when the <see cref="Job"/> is saved, and stored
		/// in the <see cref="ServiceAction.body"/> field. It is invalid to schedule a call to a method
		/// with a return value.
		/// <code>
		/// .Run(service => service.HelloWorld, "message")
		/// </code>
		/// <para>
		/// The selected method must at least implement the <see cref="CallableAttribute"/>, and
		/// should enforce that the '*' be present. The '*' scope indicates that the request
		/// originated from a Beamable server, or some source that has elevated privileges. 
		/// The <see cref="ServerCallableAttribute"/> handles
		/// that authorization assertion.
		/// </para>
		/// </param>
		/// <returns>
		/// The continuation for the service call creation flow. 
		/// </returns>
		TResponse Run(Expression<Func<T, Action>> expr);

		/// <inheritdoc cref="Run(System.Linq.Expressions.Expression{System.Func{T,System.Action}})"/>
		TResponse Run(Expression<Func<T, Func<TTask>>> expr);

		/// <inheritdoc cref="Run(System.Linq.Expressions.Expression{System.Func{T,System.Action}})"/>
		TResponse Run<TArg1>(
			Expression<Func<T, Func<TArg1, TTask>>> expr,
			TArg1 arg);

		/// <inheritdoc cref="Run(System.Linq.Expressions.Expression{System.Func{T,System.Action}})"/>
		TResponse Run<TArg1, TArg2>(
			Expression<Func<T, Func<TArg1, TArg2, TTask>>> expr,
			TArg1 arg1,
			TArg2 arg2);

		/// <inheritdoc cref="Run(System.Linq.Expressions.Expression{System.Func{T,System.Action}})"/>
		TResponse Run<
			TArg1,
			TArg2,
			TArg3
		>(
			Expression<Func<T, Func<
				TArg1,
				TArg2,
				TArg3,
				TTask>>> expr,
			TArg1 arg1,
			TArg2 arg2,
			TArg3 arg3
			);

		/// <inheritdoc cref="Run(System.Linq.Expressions.Expression{System.Func{T,System.Action}})"/>
		TResponse Run<
			TArg1,
			TArg2,
			TArg3,
			TArg4
		>(
			Expression<Func<T, Func<
				TArg1,
				TArg2,
				TArg3,
				TArg4,
				TTask>>> expr,
			TArg1 arg1,
			TArg2 arg2,
			TArg3 arg3,
			TArg4 arg4
		);

		/// <inheritdoc cref="Run(System.Linq.Expressions.Expression{System.Func{T,System.Action}})"/>
		TResponse Run<
			TArg1,
			TArg2,
			TArg3,
			TArg4,
			TArg5
		>(
			Expression<Func<T, Func<
				TArg1,
				TArg2,
				TArg3,
				TArg4,
				TArg5,
				TTask>>> expr,
			TArg1 arg1,
			TArg2 arg2,
			TArg3 arg3,
			TArg4 arg4,
			TArg5 arg5
		);
	}

	/// <summary>
	/// This exists to summarize the <see cref="IServiceCallBuilderGen"/> interface for methods
	/// that return <see cref="Task"/> and <see cref="Promise"/>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TResponse"></typeparam>
	public interface IServiceCallBuilder<T, TResponse>
			: IServiceCallBuilderGen<T, Promise, TResponse>
			, IServiceCallBuilderGen<T, Task, TResponse>
		where T : Microservice
	{

	}

	/// <summary>
	/// A utility to resolve a C#MS <see cref="ServerCallableAttribute"/> method.
	/// </summary>
	/// <typeparam name="T">The type of the <see cref="Microservice"/> that has the method to execute.</typeparam>
	public interface IServiceCallBuilder<T> : IServiceCallBuilder<T, ISchedulerBuilderTrigger>
		where T : Microservice
	{

	}

	public class ServiceCallBuilderWrapper<T> : IServiceCallBuilder<T>
		where T : Microservice
	{
		private readonly SchedulerBuilder _builder;
		private readonly ServiceCallBuilder<T> _serviceCallBuilder;
		private readonly Action<ServiceAction> _setAction;

		public ServiceCallBuilderWrapper(SchedulerBuilder builder, ServiceCallBuilder<T> serviceCallBuilder, Action<ServiceAction> setAction)
		{
			_builder = builder;
			_serviceCallBuilder = serviceCallBuilder;
			_setAction = setAction;
		}

		public ISchedulerBuilderTrigger Run(Expression<Func<T, Func<Promise>>> expr)
		{
			var call = _serviceCallBuilder.Run(expr);
			_setAction(call);
			return _builder;
		}

		public ISchedulerBuilderTrigger Run(Expression<Func<T, Action>> expr)
		{
			var call = _serviceCallBuilder.Run(expr);
			_setAction(call);
			return _builder;
		}

		public ISchedulerBuilderTrigger Run(Expression<Func<T, Func<Task>>> expr)
		{
			_setAction(_serviceCallBuilder.Run(expr));
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1>(Expression<Func<T, Func<TArg1, Task>>> expr, TArg1 arg)
		{
			_setAction(_serviceCallBuilder.Run(expr, arg));
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2>(Expression<Func<T, Func<TArg1, TArg2, Task>>> expr, TArg1 arg1, TArg2 arg2)
		{
			_setAction(_serviceCallBuilder.Run(expr, arg1, arg2));
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2, TArg3>(Expression<Func<T, Func<TArg1, TArg2, TArg3, Task>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			_setAction(_serviceCallBuilder.Run(expr, arg1, arg2, arg3));
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2, TArg3, TArg4>(Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, Task>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3,
			TArg4 arg4)
		{
			_setAction(_serviceCallBuilder.Run(expr, arg1, arg2, arg3, arg4));
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2, TArg3, TArg4, TArg5>(Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Task>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3,
			TArg4 arg4, TArg5 arg5)
		{
			_setAction(_serviceCallBuilder.Run(expr, arg1, arg2, arg3, arg4, arg5));
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1>(Expression<Func<T, Func<TArg1, Promise>>> expr,
			TArg1 arg)
		{
			var call = _serviceCallBuilder.Run(expr, arg);
			_setAction(call);
			return _builder;
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2>(Expression<Func<T, Func<TArg1, TArg2, Promise>>> expr, TArg1 arg1, TArg2 arg2)
		{
			throw new NotImplementedException();
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2, TArg3>(Expression<Func<T, Func<TArg1, TArg2, TArg3, Promise>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3)
		{
			throw new NotImplementedException();
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2, TArg3, TArg4>(Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, Promise>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3,
			TArg4 arg4)
		{
			throw new NotImplementedException();
		}

		public ISchedulerBuilderTrigger Run<TArg1, TArg2, TArg3, TArg4, TArg5>(Expression<Func<T, Func<TArg1, TArg2, TArg3, TArg4, TArg5, Promise>>> expr, TArg1 arg1, TArg2 arg2, TArg3 arg3,
			TArg4 arg4, TArg5 arg5)
		{
			throw new NotImplementedException();
		}
	}
}
