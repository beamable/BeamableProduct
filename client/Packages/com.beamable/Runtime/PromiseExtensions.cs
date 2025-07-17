using Beamable.Common;
using Beamable.Common.Runtime.Collections;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Beamable
{
	public static class PromiseExtensions
	{

		public static SequencePromise<T> ExecuteOnGameObjectRoutines<T>(int routineCount,
		                                                              CoroutineService coroutineService,
		                                                              List<Func<Promise<T>>> generators)
		{
			return ExecuteOnRoutines(
				routineCount: routineCount,
				coroutineExecutor: enumerator => coroutineService.StartCoroutine(enumerator),
				generators: generators);
		}
		
		/// <summary>
		/// This function will execute a series of promises.
		///
		/// A set of consuming routines will be spawned immediately as the function is invoked,
		/// and each routine will work on the next available promise in the given <see cref="generators"/> list.
		///
		/// When all the generators have been completed, the resulting sequence promise will complete.
		///
		/// </summary>
		/// <param name="routineCount">The number of consuming routines</param>
		/// <param name="coroutineExecutor">This method is not responsible for scheduling the routines, and relies on this proxy action to actually start the routine.
		/// This action should enumerate the routine until completion. Ideally, all the routines should be enumerated in parallel. </param>
		/// <param name="generators">The list of generators that will be invoked by this method. Each function callback will only be invoked once. </param>
		/// <typeparam name="T"></typeparam>
		/// <returns>
		/// A sequence promise where the inner list of values maps to the results of each promise generator.
		/// The order is maintained. 
		/// </returns>
		public static SequencePromise<T> ExecuteOnRoutines<T>(int routineCount,
		                                                    Action<IEnumerator> coroutineExecutor,
		                                                    List<Func<Promise<T>>> generators)
		{
			
			var seq = new SequencePromise<T>(generators.Count);
			var nextIndex = 0;
			for (var i = 0; i < routineCount; i++)
			{
				coroutineExecutor(Routine());
			}

			IEnumerator Routine()
			{
				while (nextIndex < generators.Count())
				{
					var index = nextIndex++;
					var generator = generators[index];
					var promise = generator();
					yield return promise.ToYielder();

					if (promise.IsFailed)
					{
						// Debug.LogWarning($"Failed promise index=[{index}]");
						promise.Error(ex =>
						{
							seq.ReportEntryError(index, ex);
						});
					}
					else
					{
						var result = promise.GetResult();
						seq.ReportEntrySuccess(index, result);
					}

				}

			}

			return seq;
		}

		
		private static IConcurrentDictionary<long, Task> _uncaughtTasks = new ConcurrentDictionary<long, Task>();

		public static Promise WaitForAllUncaughtHandlers()
		{
			var handler = Beam.GlobalScope.GetService<DefaultUncaughtPromiseQueue>();
			return handler.WaitForAllHandlers();
		}

		/// <summary>
		/// Registers Beamable's default Uncaught Promise Handler. This removes all other handlers
		/// </summary>
		public static void RegisterBeamableDefaultUncaughtPromiseHandler(bool replaceExistingHandlers = true)
		{
			PromiseBase.SetPotentialUncaughtErrorHandler(PromiseBaseOnPotentialOnPotentialUncaughtError, replaceExistingHandlers);
		}

		private static void PromiseBaseOnPotentialOnPotentialUncaughtError(PromiseBase promise, Exception ex)
		{
			var handler = Beam.GlobalScope.GetService<DefaultUncaughtPromiseQueue>();
			handler.Handle(promise, ex);
		}

		/// <summary>
		/// Returns a promise that will complete successfully in <paramref name="seconds"/>.
		/// Don't use this version when writing tests, instead use <see cref="WaitForSeconds{T}(Beamable.Common.Promise{T},float,CoroutineService)"/>.
		/// </summary>
		public static Promise<T> WaitForSeconds<T>(this Promise<T> promise, float seconds)
		{
			var result = new Promise<T>();
			IEnumerator Wait()
			{
				yield return Yielders.Seconds(seconds);
				promise.Then(x => result.CompleteSuccess(x));
			};

			BeamContext.Default.CoroutineService.StartCoroutine(Wait());

			return result;
		}

		/// <summary>
		/// Returns a promise that will complete successfully in <paramref name="seconds"/> by kicking off a coroutine via the given Coroutine <paramref name="service"/>.
		/// </summary>
		public static Promise<T> WaitForSeconds<T>(this Promise<T> promise, float seconds, CoroutineService service)
		{
			var result = new Promise<T>();
			IEnumerator Wait()
			{
				yield return Yielders.Seconds(seconds);
				promise.Then(x => result.CompleteSuccess(x));
			};

			service.StartCoroutine(Wait());
			return result;
		}

		/// <summary>
		/// This has the same behaviour as <see cref="RecoverWith{T}(Beamable.Common.Promise{T},System.Func{System.Exception,int,Beamable.Common.Promise{T}},float[],CoroutineService,System.Nullable{int})"/>.
		/// However, it's configured to automatically use the <see cref="BeamContext.Default"/>'s <see cref="CoroutineService"/>.
		/// </summary>
		public static Promise<T> RecoverWith<T>(this Promise<T> promise, Func<Exception, int, Promise<T>> callback, float[] falloffSeconds, int? maxRetries = null)
		{
			return RecoverWith(promise, callback, falloffSeconds, BeamContext.Default.CoroutineService, maxRetries);
		}

		/// <summary>
		/// Returns a promise configured to be attempted multiple times --- waiting for the amount of seconds defined by <paramref name="falloffSeconds"/> for each attempt. If <paramref name="maxRetries"/>
		/// is greater than the number of items in <paramref name="falloffSeconds"/>, it will reuse the final item of the array for every attempt over the array's size.
		/// </summary>
		/// <param name="promise">The promise to recover from in case of failure.</param>
		/// <param name="callback">A callback that returns a promise based on which attempt you are making and the error that happened in the previous attempt or original promise.</param>
		/// <param name="falloffSeconds">An array defining, for each attempt, the amount of seconds to wait before attempting again.</param>
		/// <param name="service">The <see cref="CoroutineService"/> that we'll use to wait before attempting again.</param>
		/// <param name="maxRetries">The maximum number of retry attempts we can make. If this number is larger than <paramref name="falloffSeconds"/>'s length, the final falloff value is used for
		/// every attempt over the length.</param>
		/// <typeparam name="T">The result value type of the promise.</typeparam>
		public static Promise<T> RecoverWith<T>(this Promise<T> promise, Func<Exception, int, Promise<T>> callback, float[] falloffSeconds, CoroutineService service, int? maxRetries = null)
		{
			var result = new Promise<T>();

			var attempt = -1;
			maxRetries = maxRetries ?? falloffSeconds.Length;

			promise.Then(value => result.CompleteSuccess(value))
				   .Error(HandleError);

			void HandleError(Exception err)
			{
				attempt += 1;
				if (attempt >= maxRetries)
				{
					result.CompleteError(err);
					return;
				}

				// Will reuse the last fall-off value in cases where maxRetries is larger than falloffSeconds.Length.
				var idx = Mathf.Clamp(attempt, 0, falloffSeconds.Length - 1);
				var delay = falloffSeconds[idx];
				var delayPromise = Promise.Success.WaitForSeconds(delay, service);
				_ = delayPromise.FlatMap(_ => callback(err, attempt)
											  .Then(v => result.CompleteSuccess(v))
											  .Error(HandleError)
										);
			}

			return result;
		}

		public static CustomYieldInstruction ToYielder<T>(this Promise<T> self)
		{
			return new PromiseYieldInstruction<T>(self);
		}

		public static void SetupDefaultHandler()
		{
			if (Application.isPlaying)
			{
				var promiseHandlerConfig = CoreConfiguration.Instance.DefaultUncaughtPromiseHandlerConfiguration;
				switch (promiseHandlerConfig)
				{
					case CoreConfiguration.EventHandlerConfig.Guarantee:
					{
						if (!PromiseBase.HasUncaughtErrorHandler)
							PromiseExtensions.RegisterBeamableDefaultUncaughtPromiseHandler();

						break;
					}
					case CoreConfiguration.EventHandlerConfig.Replace:
					case CoreConfiguration.EventHandlerConfig.Add:
					{
						PromiseExtensions.RegisterBeamableDefaultUncaughtPromiseHandler(promiseHandlerConfig == CoreConfiguration.EventHandlerConfig.Replace);
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}

	public class PromiseYieldInstruction<T> : CustomYieldInstruction
	{
		private readonly Promise<T> _promise;

		public PromiseYieldInstruction(Promise<T> promise)
		{
			_promise = promise;
		}

		public override bool keepWaiting => !_promise.IsCompleted;
	}

	public class DefaultUncaughtPromiseQueue
	{
		private readonly ICoroutineService _coroutineService;

		struct FailedPromise
		{
			public PromiseBase promise;
			public Exception exception;
			public Promise checkedPromise;
		}

		private Queue<FailedPromise> failedPromises;

		public DefaultUncaughtPromiseQueue(ICoroutineService coroutineService)
		{
			_coroutineService = coroutineService;
			failedPromises = new Queue<FailedPromise>();
			coroutineService.Run("uncaught-promise-dlq", Loop());
		}

		public void Handle(PromiseBase promise, Exception ex)
		{
			failedPromises.Enqueue(new FailedPromise
			{
				promise = promise,
				exception = ex,
			});
		}

		public Promise WaitForAllHandlers()
		{
			var p = new Promise();
			var timeoutAt = Time.realtimeSinceStartup + .5f; // half a second into the future
			IEnumerator Wait()
			{
				if (Time.realtimeSinceStartup > timeoutAt)
				{
					p.CompleteError(new Exception($"There are cascading failures in the uncaught promise handling. The {nameof(WaitForAllHandlers)} function has timed out."));
					yield break;
				}
				while (failedPromises.Count > 0)
				{
					yield return null;
				}
				p.CompleteSuccess();
			}
			_coroutineService.Run("uncaught-promise-dlq", Wait());
			return p;
		}

		IEnumerator Loop()
		{
			while (true)
			{
				while (failedPromises.Count > 0)
				{
					var failedPromise = failedPromises.Dequeue();
					if (failedPromise.promise.HadAnyErrbacks) continue;

					Beamable.Common.BeamableLogger.LogException(new UncaughtPromiseException(failedPromise.promise, failedPromise.exception));
				}
				yield return null;
			}
		}
	}
}
