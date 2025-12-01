// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System.Collections.Generic;

namespace Beamable.Api.Analytics
{

	/// <summary>
	/// Sample custom event.
	/// This subclasses the abstract class CoreEvent
	/// </summary>
	public class SampleCustomEvent : CoreEvent
	{

		/// <summary>
		/// Initializes a new instance of the <see cref="SampleCustomEvent"/> class.
		/// </summary>
		/// <param name="foo">Foo.</param>
		/// <param name="bar">Bar.</param>
		public SampleCustomEvent(string foo, string bar)
			: base("sample", "sample_custom_event", new Dictionary<string, object>
			{
				["foo"] = foo,
				["bar"] = bar,
				["hello_world"] = "Hello World."
			})
		{
		}
	}
}
