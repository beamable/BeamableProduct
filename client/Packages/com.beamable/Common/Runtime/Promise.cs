#if UNITY_WEBGL
#define DISABLE_THREADING
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Beamable.Common.Runtime.Collections;

#if !DISABLE_BEAMABLE_ASYNCMETHODBUILDER

namespace System.Runtime.CompilerServices
{
	public sealed class AsyncMethodBuilderAttribute : Attribute
	{
		public AsyncMethodBuilderAttribute(Type taskLike) { }
	}
}
#endif

namespace Beamable.Common
{
	/// <summary>
	/// This type defines the base for the %Beamable %Promise.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public abstract class PromiseBase
	{
		protected Action<Exception> errbacks;

		public bool HadAnyErrbacks
		{
			protected set;
			get;
		}

		protected Exception err;
		protected object _lock = new object();

#if DISABLE_THREADING
      protected bool done { get; set; }
#else
		private int _doneSignal = 0; // https://stackoverflow.com/questions/29411961/c-sharp-and-thread-safety-of-a-bool
		protected bool done
		{
			get => (System.Threading.Interlocked.CompareExchange(ref _doneSignal, 1, 1) == 1);
			set
			{
				if (value) System.Threading.Interlocked.CompareExchange(ref _doneSignal, 1, 0);
				else System.Threading.Interlocked.CompareExchange(ref _doneSignal, 0, 1);
			}
		}
#endif

		public static readonly Unit Unit = new Unit();

		public static Promise<Unit> SuccessfulUnit => Promise<Unit>.Successful(Unit);

		public bool IsCompleted => done;

		private static event PromiseEvent OnPotentialUncaughtError;

		public static bool HasUncaughtErrorHandler => OnPotentialUncaughtError != null;

		/// <summary>
		/// Set error handlers for uncaught promise errors. Beamable has a default handler set in its API initialization.
		/// </summary>
		/// <param name="handler">The new error handler.</param>
		/// <param name="replaceExistingHandlers">When TRUE, will replace all previously set handlers. When FALSE, will add the given handler.</param>
		public static void SetPotentialUncaughtErrorHandler(PromiseEvent handler, bool replaceExistingHandlers = true)
		{
			// This overwrites it everytime, blowing away any other listeners.
			if (replaceExistingHandlers)
			{
				OnPotentialUncaughtError = handler;
			}
			else // This allows someone to override the functionality.
			{
				OnPotentialUncaughtError += handler;
			}
		}

		protected void InvokeUncaughtPromise()
		{
			OnPotentialUncaughtError?.Invoke(this, err);
		}
	}

	public delegate void PromiseEvent(PromiseBase promise, Exception err);

	public interface ITaskLike<TResult, TSelf> : ICriticalNotifyCompletion
		where TSelf : ITaskLike<TResult, TSelf>
	{
		TResult GetResult();

		bool IsCompleted
		{
			get;
		}

		TSelf GetAwaiter();
	}

	public static class ITaskLikeExtensions
	{
		public static Promise<TResult> ToPromise<TResult, TSelf>(this ITaskLike<TResult, TSelf> taskLike)
			where TSelf : ITaskLike<TResult, TSelf>
		{
			var promise = new Promise<TResult>();
			taskLike.UnsafeOnCompleted(() => promise.CompleteSuccess(taskLike.GetResult()));
			return promise;
		}
	}

	/// <summary>
	/// This type defines the %Beamable %Promise.
	///
	/// A promise is an object that may produce a single value some time in the future:
	/// either a resolved value, or a reason that it’s not resolved (e.g., a network error occurred).
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/learning-fundamentals">Learning Fundamentals</a> documentation
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	///
	[AsyncMethodBuilder(typeof(PromiseAsyncMethodBuilder<>))]
	public class Promise<T> : PromiseBase, ICriticalNotifyCompletion
	{
		private Action<T> _callbacks;
		private T _val;

		/// <summary>
		/// Call to set the value and resolve the %Promise
		/// </summary>
		/// <param name="val"></param>
		public void CompleteSuccess(T val)
		{
			lock (_lock)
			{
				if (done)
				{
					return;
				}

				_val = val;
				done = true;
				try
				{
					_callbacks?.Invoke(val);
				}
				catch (Exception e)
				{
					BeamableLogger.LogException(e);
				}

				_callbacks = null;
				errbacks = null;
			}
		}

		/// <summary>
		/// Call to throw an exception and resolve the %Promise
		/// </summary>
		/// <param name="val"></param>
		public void CompleteError(Exception ex)
		{
			lock (_lock)
			{
				if (done)
				{
					return;
				}

				err = ex;
				done = true;

				try
				{
					if (!HadAnyErrbacks)
					{
						InvokeUncaughtPromise();
					}
					else
					{
						errbacks?.Invoke(ex);
					}
				}
				catch (Exception e)
				{
					BeamableLogger.LogException(e);
				}

				_callbacks = null;
				errbacks = null;
			}
		}

		/// <summary>
		/// Call to register a success completion handler callback for the %Promise
		/// </summary>
		/// <param name="val"></param>
		public Promise<T> Then(Action<T> callback)
		{
			lock (_lock)
			{
				if (done)
				{
					if (err == null)
					{
						try
						{
							callback(_val);
						}
						catch (Exception e)
						{
							BeamableLogger.LogException(e);
						}
					}
				}
				else
				{
					_callbacks += callback;
				}
			}

			return this;
		}

		/// <summary>
		/// Call to register a failure completion handler callback for the %Promise
		/// </summary>
		/// <param name="val"></param>
		public Promise<T> Error(Action<Exception> errback)
		{
			lock (_lock)
			{
				HadAnyErrbacks = true;
				if (done)
				{
					if (err != null)
					{
						try
						{
							errback(err);
						}
						catch (Exception e)
						{
							BeamableLogger.LogException(e);
						}
					}
				}
				else
				{
					errbacks += errback;
				}
			}

			return this;
		}

		/// <summary>
		/// Takes a promise of type A, and returns a promise of
		/// type B with a conversion applied.
		/// </summary>
		/// <param name="callback"></param>
		/// <typeparam name="TU"></typeparam>
		/// <returns></returns>
		public Promise<TU> Map<TU>(Func<T, TU> callback)
		{
			var result = new Promise<TU>();
			Then(value =>
				{
					try
					{
						var nextResult = callback(value);
						result.CompleteSuccess(nextResult);
					}
					catch (Exception ex)
					{
						result.CompleteError(ex);
					}
				})
				.Error(ex => result.CompleteError(ex));
			return result;
		}

		/// <summary>
		/// Takes a promise of type A, and returns a promise of
		/// type B with a conversion applied and the promise hierarchy is flattened.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="factory"></param>
		/// <typeparam name="PromiseU"></typeparam>
		/// <typeparam name="U"></typeparam>
		/// <returns></returns>
		public PromiseU FlatMap<PromiseU, U>(Func<T, PromiseU> callback, Func<PromiseU> factory)
			where PromiseU : Promise<U>
		{
			var pu = factory();
			FlatMap(callback)
				.Then(pu.CompleteSuccess)
				.Error(pu.CompleteError);
			return pu;
		}

		/// <summary>
		/// Takes a promise of type A, and returns a promise of
		/// type B with a conversion applied and the promise hierarchy is flattened.
		/// </summary>
		/// <param name="callback"></param>
		/// <typeparam name="TU"></typeparam>
		/// <returns></returns>
		public Promise<TU> FlatMap<TU>(Func<T, Promise<TU>> callback)
		{
			var result = new Promise<TU>();
			Then(value =>
			{
				try
				{
					callback(value)
						.Then(valueInner => result.CompleteSuccess(valueInner))
						.Error(ex => result.CompleteError(ex));
				}
				catch (Exception ex)
				{
					result.CompleteError(ex);
				}
			}).Error(ex =>
			{
				result.CompleteError(ex);
			});
			return result;
		}

		/// <summary>
		/// Call to set the value and resolve the %Promise
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static Promise<T> Successful(T value)
		{
			return new Promise<T> {done = true, _val = value};
		}

		/// <summary>
		/// Call to throw an exception and resolve the %Promise
		/// </summary>
		/// <param name="err"></param>
		/// <returns></returns>
		public static Promise<T> Failed(Exception err)
		{
			return new Promise<T> {done = true, err = err};
		}

		void ICriticalNotifyCompletion.UnsafeOnCompleted(Action continuation)
		{
			Then(_ => continuation());
			Error(_ => continuation());
		}

		void INotifyCompletion.OnCompleted(Action continuation)
		{
			((ICriticalNotifyCompletion)this).UnsafeOnCompleted(continuation);
		}

		/// <summary>
		/// Get the result of the <see cref="Promise"/>.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public T GetResult()
		{
			if (err != null)
				throw err;
			return _val;
		}

		/// <summary>
		/// Get the awaiter of the <see cref="Promise"/>.
		/// </summary>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public Promise<T> GetAwaiter()
		{
			return this;
		}
	}

	/// <summary>
	/// This type defines the %Beamable %SequencePromise.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class SequencePromise<T> : Promise<IList<T>>
	{
		private Action<SequenceEntryException> _entryErrorCallbacks;
		private Action<SequenceEntrySuccess<T>> _entrySuccessCallbacks;

		private ConcurrentBag<SequenceEntryException> _errors = new ConcurrentBag<SequenceEntryException>();
		private ConcurrentBag<SequenceEntrySuccess<T>> _successes = new ConcurrentBag<SequenceEntrySuccess<T>>();

		private ConcurrentDictionary<int, object> _indexToResult = new ConcurrentDictionary<int, object>();

		public int SuccessCount => _successes.Count;
		public int ErrorCount => _errors.Count;
		public int Total => _errors.Count + _successes.Count;

		public int Count
		{
			get;
		}

		public float Ratio => HasProcessedAllEntries ? 1 : Total / (float)Count;
		public bool HasProcessedAllEntries => Total == Count;

		public IEnumerable<T> SuccessfulResults => _successes.Select(s => s.Result);

		public SequencePromise(int count)
		{
			Count = count;
			if (Count == 0)
			{
				CompleteSuccess();
			}
		}

		public SequencePromise<T> OnElementError(Action<SequenceEntryException> handler)
		{
			foreach (var existingError in _errors)
			{
				handler?.Invoke(existingError);
			}

			_entryErrorCallbacks += handler;
			return this;
		}

		public SequencePromise<T> OnElementSuccess(Action<SequenceEntrySuccess<T>> handler)
		{
			foreach (var success in _successes)
			{
				handler?.Invoke(success);
			}

			_entrySuccessCallbacks += handler;
			return this;
		}

		public void CompleteSuccess()
		{
			base.CompleteSuccess(SuccessfulResults.ToList());
		}

		public void ReportEntryError(SequenceEntryException exception)
		{
			if (_indexToResult.ContainsKey(exception.Index) || exception.Index >= Count) return;

			_errors.Add(exception);
			_indexToResult.TryAdd(exception.Index, exception);
			_entryErrorCallbacks?.Invoke(exception);

			CompleteError(exception.InnerException);
		}

		public void ReportEntrySuccess(SequenceEntrySuccess<T> success)
		{
			if (_indexToResult.ContainsKey(success.Index) || success.Index >= Count) return;

			_successes.Add(success);
			_indexToResult.TryAdd(success.Index, success);
			_entrySuccessCallbacks?.Invoke(success);

			if (HasProcessedAllEntries)
			{
				CompleteSuccess();
			}
		}

		public void ReportEntrySuccess(int index, T result) =>
			ReportEntrySuccess(new SequenceEntrySuccess<T>(index, result));

		public void ReportEntryError(int index, Exception err) =>
			ReportEntryError(new SequenceEntryException(index, err));
	}

	// Do not add doxygen comments to "public static class Promise" because
	// it confuses this with the doxygen output with "public class Promise" - srivello
	[AsyncMethodBuilder(typeof(PromiseAsyncMethodBuilder))]
	public class Promise : Promise<Unit>
	{
		public void CompleteSuccess() => CompleteSuccess(PromiseBase.Unit);

		/// <summary>
		/// Create a <see cref="SequencePromise{T}"/> from List of <see cref="Promise{T}"/>
		/// </summary>
		/// <param name="promises"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static SequencePromise<T> ObservableSequence<T>(IList<Promise<T>> promises)
		{
			var result = new SequencePromise<T>(promises.Count);

			if (promises == null || promises.Count == 0)
			{
				result.CompleteSuccess();
				return result;
			}

			for (var i = 0; i < promises.Count; i++)
			{
				var index = i;
				promises[i].Then(reply =>
				{
					result.ReportEntrySuccess(new SequenceEntrySuccess<T>(index, reply));

					if (result.Total == promises.Count)
					{
						result.CompleteSuccess();
					}
				}).Error(err =>
				{
					result.ReportEntryError(new SequenceEntryException(index, err));
					result.CompleteError(err);
				});
			}

			return result;
		}

		/// <summary>
		/// Create a <see cref="Promise"/> of List from a List of <see cref="Promise"/>s.
		/// </summary>
		/// <param name="promises"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Promise<List<T>> Sequence<T>(IList<Promise<T>> promises)
		{
			var result = new Promise<List<T>>();
			var replies = new ConcurrentDictionary<int, T>();

			if (promises == null || promises.Count == 0)
			{
				result.CompleteSuccess(replies.Values.ToList());
				return result;
			}

			for (var i = 0; i < promises.Count; i++)
			{
				var index = i;

				promises[i].Then(reply =>
				{
					replies.TryAdd(index, reply);

					if (replies.Count == promises.Count)
					{
						result.CompleteSuccess(replies.Values.ToList());
					}
				}).Error(err => result.CompleteError(err));
			}

			return result;
		}

		/// <summary>
		/// Create Sequence <see cref="Promise"/> from an array of <see cref="Promise"/>s.
		/// </summary>
		/// <param name="promises"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Promise<List<T>> Sequence<T>(params Promise<T>[] promises)
		{
			return Sequence((IList<Promise<T>>)promises);
		}

		/// <summary>
		/// Given a list of promise generator functions, process the whole list, but serially.
		/// Only one promise will be active at any given moment.
		/// </summary>
		/// <param name="generators"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns>A single promise of Unit to represent the completion of the processing. Any other side effects need to be handled separately</returns>
		public static Promise<Unit> ExecuteSerially<T>(List<Func<Promise<T>>> generators)
		{
#if DISABLE_THREADING // unity doesn't supporting System.Threading
         // use a tail recursion approach. It'll stink for massive lists, but at least it works for small ones
         if (generators.Count == 0)
         {
            return PromiseBase.SuccessfulUnit;
         }
         var first = generators.First();
         var rest = generators.Skip(1).ToList();
         return first().FlatMap(_ => ExecuteSerially<T>(rest));
#else
			async System.Threading.Tasks.Task Execute()
			{
				for (var i = 0; i < generators.Count; i++)
				{
					var generator = generators[i];
					var promise = generator();
					await promise;
				}
			}

			return Execute().ToPromise();
#endif
		}

		private interface IAtomicInt
		{
			int Value
			{
				get;
			}

			void Increment();
			void Decrement();
		}

#if DISABLE_THREADING
      private class AtomicInt : IAtomicInt
      {
         public int Value { get; private set; }
         public void Increment()
         {
            Value++;
         }

         public void Decrement()
         {
            Value--;
         }
      }
#else
		private class AtomicInt : IAtomicInt
		{
			private int v;

			public int Value => System.Threading.Interlocked.CompareExchange(ref v, 0, 0);

			public void Increment()
			{
				System.Threading.Interlocked.Increment(ref v);
			}

			public void Decrement()
			{
				System.Threading.Interlocked.Decrement(ref v);
			}
		}
#endif

		/// <summary>
		/// Given a list of promise generator functions, process the list, but in a rolling fashion.
		/// </summary>
		/// <param name="maxProcessSize"></param>
		/// <param name="generators"></param>
		/// <param name="stopWhen"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static SequencePromise<T> ExecuteRolling<T>(int maxProcessSize,
		                                                   List<Func<Promise<T>>> generators,
		                                                   Func<bool> stopWhen = null)
		{
			var current = new AtomicInt();
			var running = new AtomicInt();

			var completePromise = new SequencePromise<T>(generators.Count);

			object locker = generators;

			void ProcessUpToLimit()
			{
				lock (locker)
				{
					var runningCount = running.Value;
					var currentCount = current.Value;

					while (runningCount < maxProcessSize && currentCount < generators.Count)
					{
						if (stopWhen != null && stopWhen())
						{
							break;
						}

						var index = currentCount;
						var generator = generators[index];

						current.Increment();
						running.Increment();
						var promise = generator();

						promise.Then(result =>
						{
							running.Decrement();
							completePromise.ReportEntrySuccess(index, result);
							ProcessUpToLimit();
						});

						promise.Error(err =>
						{
							running.Decrement();
							completePromise.ReportEntryError(index, err);
							ProcessUpToLimit();
						});

						runningCount = running.Value;
						currentCount = current.Value;
					}
				}
			}

			ProcessUpToLimit();
			return completePromise;
		}

		/// <summary>
		/// Given a list of promise generator functions, process the list, but in batches of some size.
		/// The batches themselves will run one at a time. Every promise in the current batch must finish before the next batch can start.
		/// </summary>
		/// <param name="maxBatchSize"></param>
		/// <param name="generators"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns>A single promise of Unit to represent the completion of the processing. Any other side effects need to be handled separately</returns>
		public static Promise<Unit> ExecuteInBatch<T>(int maxBatchSize, List<Func<Promise<T>>> generators)
		{
			var batches = new List<List<Func<Promise<T>>>>();

			// create batches...
			for (var i = 0; i < generators.Count; i += maxBatchSize)
			{
				var start = i;
				var minBatchSize = generators.Count - start;
				var count = minBatchSize < maxBatchSize ? minBatchSize : maxBatchSize; // min()
				var batch = generators.GetRange(start, count);
				batches.Add(batch);
			}

			Promise<List<T>> ProcessBatch(List<Func<Promise<T>>> batch)
			{
				// start all generators in batch...
				return Promise.Sequence(batch.Select(generator => generator()).ToList());
			}

			// run each batch, serially...
			var batchRunners = batches.Select(batch => new Func<Promise<List<T>>>(() => ProcessBatch(batch))).ToList();

			return ExecuteSerially(batchRunners);
		}
	}

	/// <summary>
	/// This type defines the static %Beamable %SequenceEntryException.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class SequenceEntryException : Exception
	{
		public int Index
		{
			get;
		}

		public SequenceEntryException(int index, Exception inner) : base($"index[{index}]. {inner.Message}", inner)
		{
			Index = index;
		}
	}

	/// <summary>
	/// This type defines the static %Beamable %SequenceEntrySuccess.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class SequenceEntrySuccess<T>
	{
		public int Index
		{
			get;
			private set;
		}

		public T Result
		{
			get;
			private set;
		}

		public SequenceEntrySuccess(int index, T result)
		{
			Index = index;
			Result = result;
		}
	}

	/// <summary>
	/// This type defines the static %Beamable %PromiseExtensions.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public static class PromiseExtensions
	{
		public static Promise<T> Recover<T>(this Promise<T> promise, Func<Exception, T> callback)
		{
			var result = new Promise<T>();
			promise.Then(value => result.CompleteSuccess(value))
			       .Error(err => result.CompleteSuccess(callback(err)));
			return result;
		}

		public static Promise<T> RecoverWith<T>(this Promise<T> promise, Func<Exception, Promise<T>> callback)
		{
			var result = new Promise<T>();
			promise.Then(value => result.CompleteSuccess(value)).Error(err =>
			{
				try
				{
					var nextPromise = callback(err);
					nextPromise.Then(value => result.CompleteSuccess(value)).Error(errInner =>
					{
						result.CompleteError(errInner);
					});
				}
				catch (Exception ex)
				{
					result.CompleteError(ex);
				}
			});
			return result;
		}

#if !UNITY_WEBGL // webgl does not support the system.threading library
		/// <summary>
		/// Convert <see cref="Task"/> to <see cref="Promise{Unit}"/>.
		/// </summary>
		/// <param name="task"></param>
		/// <returns></returns>
		public static Promise<Unit> ToPromise(this System.Threading.Tasks.Task task)
		{
			var promise = new Promise<Unit>();

			async System.Threading.Tasks.Task Helper()
			{
				try
				{
					await task;
					promise.CompleteSuccess(PromiseBase.Unit);
				}
				catch (Exception ex)
				{
					promise.CompleteError(ex);
				}
			}

			var _ = Helper();

			return promise;
		}

		/// <summary>
		/// Convert <see cref="Task{T}"/> to <see cref="Promise{T}"/>.
		/// </summary>
		/// <param name="task"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static Promise<T> ToPromise<T>(this System.Threading.Tasks.Task<T> task)
		{
			var promise = new Promise<T>();

			async System.Threading.Tasks.Task Helper()
			{
				try
				{
					var result = await task;
					promise.CompleteSuccess(result);
				}
				catch (Exception ex)
				{
					promise.CompleteError(ex);
				}
			}

			var _ = Helper();

			return promise;
		}
#endif

		public static Promise<Unit> ToUnit<T>(this Promise<T> self)
		{
			return self.Map(_ => PromiseBase.Unit);
		}
	}

	/// <summary>
	/// This type defines the static %Beamable %UncaughtPromiseException.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class UncaughtPromiseException : Exception
	{
		public PromiseBase Promise
		{
			get;
		}

		public UncaughtPromiseException(PromiseBase promise, Exception ex) : base(
			$"Uncaught promise innerMsg=[{ex.Message}]", ex)
		{
			Promise = promise;
		}
	}

	/// <summary>
	/// This type defines the struct %Beamable %Unit.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Promise script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public readonly struct Unit { }

	/// <summary>
	/// https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md
	/// https://referencesource.microsoft.com/#mscorlib/system/runtime/compilerservices/AsyncMethodBuilder.cs
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class PromiseAsyncMethodBuilder<T>
	{
		private IAsyncStateMachine _stateMachine;
		private Promise<T> _promise = new Promise<T>(); // TODO: allocation.

		public static PromiseAsyncMethodBuilder<T> Create()
		{
			return new PromiseAsyncMethodBuilder<T>();
		}

		public void SetResult(T res)
		{
			_promise.CompleteSuccess(res);
		}

		public void SetException(Exception ex)
		{
			_promise.CompleteError(ex);
		}

		public void SetStateMachine(IAsyncStateMachine machine)
		{
			_stateMachine = machine;
		}

		public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
			where TAwaiter : INotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			if (_stateMachine == null)
			{
				_stateMachine = stateMachine;
				_stateMachine.SetStateMachine(stateMachine);
			}

			awaiter.OnCompleted(() =>
			{
				_stateMachine.MoveNext();
			});
		}

		public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
		                                                            ref TStateMachine stateMachine)
			where TAwaiter : ICriticalNotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			AwaitOnCompleted(ref awaiter, ref stateMachine);
		}

		public void Start<TStateMachine>(ref TStateMachine stateMachine)
			where TStateMachine : IAsyncStateMachine
		{
			stateMachine.MoveNext();
		}

		public Promise<T> Task => _promise;
	}

	public sealed class PromiseAsyncMethodBuilder
	{
		private IAsyncStateMachine _stateMachine;
		private Promise _promise = new Promise(); // TODO: allocation.

		public static PromiseAsyncMethodBuilder Create()
		{
			return new PromiseAsyncMethodBuilder();
		}

		public void SetResult()
		{
			_promise.CompleteSuccess(PromiseBase.Unit);
		}

		public void SetException(Exception ex)
		{
			_promise.CompleteError(ex);
		}

		public void SetStateMachine(IAsyncStateMachine machine)
		{
			_stateMachine = machine;
		}

		public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
			where TAwaiter : INotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			if (_stateMachine == null)
			{
				_stateMachine = stateMachine;
				_stateMachine.SetStateMachine(stateMachine);
			}

			awaiter.OnCompleted(() =>
			{
				_stateMachine.MoveNext();
			});
		}

		public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
		                                                            ref TStateMachine stateMachine)
			where TAwaiter : ICriticalNotifyCompletion
			where TStateMachine : IAsyncStateMachine
		{
			AwaitOnCompleted(ref awaiter, ref stateMachine);
		}

		public void Start<TStateMachine>(ref TStateMachine stateMachine)
			where TStateMachine : IAsyncStateMachine
		{
			stateMachine.MoveNext();
		}

		public Promise Task => _promise;
	}
}
