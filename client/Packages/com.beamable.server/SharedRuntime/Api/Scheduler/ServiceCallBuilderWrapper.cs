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

namespace Beamable.Server
{

	public class ServiceCallBuilderWrapper<T> where T : Microservice
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
		
		public ISchedulerBuilderTrigger Run<TArg1>(Expression<Func<T, Func<TArg1, Promise>>> expr, 
			TArg1 arg)
		{
			var call = _serviceCallBuilder.Run(expr, arg);
			_setAction(call);
			return _builder;
		}
	}
}
