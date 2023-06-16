using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Scheduler;
using System;
using System.Collections.Generic;

namespace Beamable.Server
{

	public interface ISchedulerBuilderSetup
	{
		ServiceCallBuilderWrapper<T> Microservice<T>(bool useLocal = true) where T : Microservice;
		HttpCallBuilderWrapper Http();

	}

	public interface ISchedulerBuilderTrigger
	{
		ISchedulerBuilderFinal OnCron(string cronExpression);
		// ISchedulerBuilderFinal OnCron(ICronComponent builder);
		ISchedulerBuilderFinal OnCron(Func<ICronMinutes, string> cronBuilder);
		ISchedulerBuilderFinal OnExactDate(DateTime executeAt);
		ISchedulerBuilderFinal WithRetryPolicy(RetryPolicy policy);

		ISchedulerBuilderFinal WithRetryPolicy(int? maxRetryCount = null, int? retryDelayMs = null,
			bool? useExponentialBackoff = null);
	}

	public interface ISchedulerBuilderFinal : ISchedulerBuilderTrigger
	{
		Promise<Job> Save(string name, string source=null);
	}
	
	public class SchedulerBuilder : ISchedulerBuilderSetup, ISchedulerBuilderFinal
	{
		private readonly BeamScheduler _scheduler;

		private ISchedulableAction _action;
		private List<ISchedulerTrigger> _triggers = new List<ISchedulerTrigger>();
		private RetryPolicy _retry = new RetryPolicy();

		public SchedulerBuilder(BeamScheduler scheduler)
		{
			_scheduler = scheduler;
		}

		public ServiceCallBuilderWrapper<T> Microservice<T>(bool useLocal = true) where T : Microservice
		{
			var builder = new ServiceCallBuilder<T>(useLocal, _scheduler.SchedulerContext);
			return new ServiceCallBuilderWrapper<T>(this, builder, action => _action = action);
		}
		
		public HttpCallBuilderWrapper Http()
		{
			return new HttpCallBuilderWrapper(this, http => _action = http);
		}

		public ISchedulerBuilderFinal OnCron(string cronExpression)
		{
			var trigger = new CronEvent(cronExpression);
			_triggers.Add(trigger);
			return this;
		}

		public ISchedulerBuilderFinal OnCron(Func<ICronMinutes, string> cronBuilder)
		{
			throw new NotImplementedException();
			// return OnCron(cronBuilder(CronBuilder.OnSecond(0) ));
		}

		public ISchedulerBuilderFinal OnExactDate(DateTime executeAt)
		{
			var trigger = new ExactTimeEvent(executeAt);
			_triggers.Add(trigger);
			return this;
		}

		public ISchedulerBuilderFinal WithRetryPolicy(RetryPolicy policy)
		{
			_retry = policy;
			return this;
		}
		public ISchedulerBuilderFinal WithRetryPolicy(int? maxRetryCount=null, int? retryDelayMs=null, bool? useExponentialBackoff=null)
		{
			if (maxRetryCount.HasValue)
			{
				_retry.maxRetryCount = maxRetryCount.Value;
			}

			if (retryDelayMs.HasValue)
			{
				_retry.retryDelayMs = retryDelayMs.Value;
			}

			if (useExponentialBackoff.HasValue)
			{
				_retry.useExponentialBackoff = useExponentialBackoff.Value;
			}
			
			return this;
		}


		public async Promise<Job> Save(string name, string source=null)
		{
			if (string.IsNullOrEmpty(source))
			{
				source = _scheduler.SchedulerContext.ServiceName;
			}
			return await _scheduler.CreateJob(name, source, _action, _triggers.ToArray(), _retry);
		}
	}
	

	public static class SchedulerExtensions
	{
		public static ISchedulerBuilderSetup Schedule(this BeamScheduler scheduler)
		{
			return new SchedulerBuilder(scheduler);
		}
	}
}
