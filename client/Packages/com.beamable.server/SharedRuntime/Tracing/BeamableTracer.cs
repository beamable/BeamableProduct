using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace beamable.server.Tracing
{

	public interface IBeamableTracer
	{
		IBeamTrace Start(string name);
		IBeamMetricCounter<T> GetCounter<T>(string name, string unit=null, string description=null) where T : struct;
	}

	public interface IBeamTrace : IDisposable
	{
		public void SetTag(string tagName, object value);
		public void SetTags(IEnumerable<KeyValuePair<string, object>> tags);
		public void RecordException(Exception ex);
	}

	public interface IBeamMetricCounter<T> where T : struct
	{
		void Add(T value);
	}
}
