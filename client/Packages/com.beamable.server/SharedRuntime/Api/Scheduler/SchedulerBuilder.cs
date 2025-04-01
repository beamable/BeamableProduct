// this file was copied from nuget package Beamable.Server.Common@4.2.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Server.Common/4.2.0-PREVIEW.RC4

using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Common.Scheduler;
using System;
using System.Collections.Generic;

namespace Beamable.Server
{

	public interface ISchedulerBuilderSetup
	{
		/// <summary>
		/// Configure the <see cref="Job"/> to run a C#MS function as the <see cref="Job.action"/>
		/// </summary>
		/// <param name="useLocal">
		/// When running C#MS functions, the <see cref="useLocal"/> flag will indicate if
		/// the scheduled action should be forced to run against the C#MS that scheduled
		/// the job, or always be forced to use the deployed C#MS.
		///
		/// <para>
		/// If this code is executed on a deployed C#MS, then by default, the local C#MS is
		/// the deployed C#MS, and therefor, the field value is irrelevant. 
		/// </para>
		/// <para>
		/// However, when developing locally, if the value is set to false, then the scheduled
		/// action will be sent to the remote C#MS. This can be confusing, because your local C#MS
		/// will not receive the action.
		/// </para>
		/// </param>
		/// <typeparam name="T">
		/// The type of <see cref="Microservice"/> that has the <see cref="ServerCallableAttribute"/> method you
		/// want to call when the <see cref="Job"/> executes.
		/// </typeparam>
		/// <returns>
		/// 
		/// </returns>
		IServiceCallBuilder<T> Microservice<T>(bool useLocal = true) where T : Microservice;

		/// <summary>
		/// Configure the <see cref="Job"/> to run an HTTP request as the <see cref="Job.action"/>
		/// </summary>
		/// <returns></returns>
		HttpCallBuilderWrapper Http();
	}

	public interface ISchedulerBuilderTrigger
	{
		/// <summary>
		/// <para>
		/// <b> If you are unfamiliar with CRON, consider using the <see cref="OnCron(Func{ICronInitial, ICronComplete})"/> function instead.</b>
		/// </para>
		/// Schedules the <see cref="Job"/> on a CRON schedule.
		/// 
		/// <inheritdoc cref="CronEvent.cronExpression"/>
		///
		/// <para>
		/// </para>
		/// </summary>
		/// <param name="cronExpression"></param>
		/// <returns></returns>
		ISchedulerBuilderFinal OnCron(string cronExpression);

		/// <summary>
		/// Schedules the <see cref="Job"/> on a CRON schedule.
		/// </summary>
		/// <param name="cronBuilder">
		/// A builder that will guide you through creating a valid NCronTab expression.

		/// <example>
		/// <code>
		/// c => c.AtSecond(0)
		///	      .AtMinute(0)
		///	      .AtHour(12)
		///	      .EveryDay()
		/// </code>
		/// </example>
		/// <example>
		/// <code>
		/// c => c.Daily()
		/// </code>
		/// </example>
		/// </param>
		/// <returns></returns>
		ISchedulerBuilderFinal OnCron(Func<ICronInitial, ICronComplete> cronBuilder);

		/// <inheritdoc cref="OnCron(Func{ICronInitial, ICronComplete})"/>
		ISchedulerBuilderFinal OnCron(Func<ICronInitial, string> cronBuilder);

		/// <summary>
		/// Schedules the <see cref="Job"/> to run at a specific time in UTC
		/// </summary>
		/// <param name="executeAt">The UTC time the job will execute</param>
		/// <returns></returns>
		ISchedulerBuilderFinal OnExactDate(DateTime executeAt);

		/// <summary>
		/// Schedules the <see cref="Job"/> to run later by some given time span.
		/// </summary>
		/// <param name="timespan">The amount of time into the future the Job should run.</param>
		/// <returns></returns>
		ISchedulerBuilderFinal After(TimeSpan timespan);

		/// <summary>
		/// Configure the <see cref="RetryPolicy"/> for the <see cref="Job"/>
		/// </summary>
		/// <param name="policy"></param>
		/// <returns></returns>
		ISchedulerBuilderFinal WithRetryPolicy(RetryPolicy policy);

		/// <summary>
		/// Configure the <see cref="RetryPolicy"/> for the <see cref="Job"/>
		/// </summary>
		/// <param name="maxRetryCount"> See <see cref="RetryPolicy.maxRetryCount"/> </param>
		/// <param name="retryDelayMs">See <see cref="RetryPolicy.retryDelayMs"/>  </param>
		/// <param name="useExponentialBackoff">See <see cref="RetryPolicy.useExponentialBackoff"/> </param>
		/// <returns></returns>
		ISchedulerBuilderFinal WithRetryPolicy(int? maxRetryCount = null, int? retryDelayMs = null,
			bool? useExponentialBackoff = null);
	}

	public interface ISchedulerBuilderFinal : ISchedulerBuilderTrigger
	{
		/// <summary>
		/// Finally commit the <see cref="Job"/> to be scheduled.
		/// </summary>
		/// <param name="name">
		/// The value for the <see cref="Job.name"/> field. This can be used to filter
		/// the job later with the <see cref="BeamScheduler.GetJobs"/> function.
		/// </param>
		/// <param name="source">
		/// The value for the <see cref="Job.source"/> field. This can be used to filter
		/// the job later with the <see cref="BeamScheduler.GetJobs"/> function.
		/// <para>
		/// By default, if no value is given, this will be the current C#MS service name.
		/// </para>
		/// </param>
		/// <returns>A scheduled <see cref="Job"/></returns>
		Promise<Job> Save(string name, string source = null);
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

		public IServiceCallBuilder<T> Microservice<T>(bool useLocal = true) where T : Microservice
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
			if (!CronValidation.TryValidate(cronExpression, out var message))
			{
				throw new CronInvalidException(cronExpression, message);
			}
			var trigger = new CronEvent(cronExpression);
			_triggers.Add(trigger);
			return this;
		}

		public ISchedulerBuilderFinal OnCron(Func<ICronInitial, string> cronBuilder)
		{
			var cb = new CronBuilder();
			var cronExpr = cronBuilder(cb);
			return OnCron(cronExpr);
		}


		public ISchedulerBuilderFinal OnCron(Func<ICronInitial, ICronComplete> cronBuilder)
		{
			var cb = new CronBuilder();
			var cronExpr = cronBuilder(cb).ToCron();
			return OnCron(cronExpr);
		}

		public ISchedulerBuilderFinal OnExactDate(DateTime executeAt)
		{
			var trigger = new ExactTimeEvent(executeAt);
			_triggers.Add(trigger);
			return this;
		}

		public ISchedulerBuilderFinal After(TimeSpan timespan) => OnExactDate(DateTime.UtcNow + timespan);

		public ISchedulerBuilderFinal WithRetryPolicy(RetryPolicy policy)
		{
			_retry = policy;
			return this;
		}
		public ISchedulerBuilderFinal WithRetryPolicy(int? maxRetryCount = null, int? retryDelayMs = null, bool? useExponentialBackoff = null)
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


		public async Promise<Job> Save(string name, string source = null)
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
		/// <summary>
		/// Create a <see cref="Job"/> that Beamable will execute later.
		/// A job will have 1 <see cref="Job.action"/> that can be an HTTP request, or C#MS request.
		/// The job will run anytime any of the <see cref="Job.triggers"/> execute.
		/// </summary>
		/// <param name="scheduler"></param>
		/// <returns>
		/// A <see cref="ISchedulerBuilderSetup"/> object that guides you to create a <see cref="Job"/>
		/// </returns>
		public static ISchedulerBuilderSetup Schedule(this BeamScheduler scheduler)
		{
			return new SchedulerBuilder(scheduler);
		}
	}
}
