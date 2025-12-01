// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Serialization;

namespace Beamable.Api.Analytics
{

	/// <summary>
	/// Analytics Event interface
	/// </summary>
	public interface IAnalyticsEvent : JsonSerializable.ISerializable
	{
		/// <summary>
		/// Gets the op code of the analytics event.
		/// </summary>
		/// <value>The op code.</value>
		string OpCode
		{
			get;
		}
	}
}
