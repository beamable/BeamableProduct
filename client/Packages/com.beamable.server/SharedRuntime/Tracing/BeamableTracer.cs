using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace beamable.server.Tracing
{

	public interface IBeamableTracer
	{
		/// <summary>
		/// Begin an Open Telemetry trace.
		/// You must dispose the returned <see cref="IBeamTrace"/> to end the trace.
		/// Consider a `using` statement. 
		/// </summary>
		/// <param name="name">The name of the trace</param>
		/// <returns>A <see cref="IBeamTrace"/> object that can store additional meta information.</returns>
		IBeamTrace Start(string name);
		
		/// <summary>
		/// Retrieve a <see cref="IBeamMetricCounter{T}"/> for the given name.
		/// The first time this method is invoked per <see cref="name"/> value, a new instance will be created.
		/// However, every subsequent invocation per <see cref="name"/> value will return the initial instance.
		///
		/// The <see cref="unit"/> and <see cref="description"/> fields applied on the first invocation will stay permanent. 
		/// </summary>
		/// <param name="name">The name of the metric. This should be unique.</param>
		/// <param name="unit"></param>
		/// <param name="description"></param>
		/// <typeparam name="T">Must be a struct like type that is countable, like long, int, or double.</typeparam>
		/// <returns>A <see cref="IBeamMetricCounter{T}"/> instance</returns>
		IBeamMetricCounter<T> GetCounter<T>(string name, string unit=null, string description=null) where T : struct;
	}

	public interface IBeamTrace : IDisposable
	{
		/// <summary>
		/// Set a tag value for the trace. 
		/// </summary>
		/// <param name="tagName">The name of the tag; should be unique. </param>
		/// <param name="value">The value of the tag. It can be a string, or simple object such as int, long, or bool. Complex types won't be supported. </param>
		public void SetTag(string tagName, object value);
		
		/// <summary>
		/// Set many tags as key value pairs. 
		/// </summary>
		/// <param name="tags">A set of <see cref="KeyValuePair{TKey,TValue}"/></param>
		public void SetTags(IEnumerable<KeyValuePair<string, object>> tags);
		
		/// <summary>
		/// Mark the trace in an error state. 
		/// </summary>
		/// <param name="ex">The exception that caused the trace to fail.</param>
		public void RecordException(Exception ex);
	}

	public interface IBeamMetricCounter<T> where T : struct
	{
		/// <summary>
		/// adds the given value to the metric value. 
		/// </summary>
		/// <param name="value">How much to increment the metric by</param>
		/// <param name="tags">An optional set of tags for the increase event</param>
		void Add(T value, params KeyValuePair<string, object>[] tags);
	}

	public static class BeamMetricCounterExtensions
	{
		/// <summary>
		/// Increment the given counter by 1.
		/// </summary>
		/// <param name="counter"></param>
		/// <param name="tags">An optional set of tags for the event</param>
		public static void Increment(this IBeamMetricCounter<int> counter, params KeyValuePair<string, object>[] tags) => counter.Add(1, tags);
		
		/// <summary>
		/// Increment the given counter by 1.
		/// </summary>
		/// <param name="counter"></param>
		/// <param name="tags">An optional set of tags for the event</param>
		public static void Increment(this IBeamMetricCounter<long> counter, params KeyValuePair<string, object>[] tags) => counter.Add(1, tags);
	}
}
