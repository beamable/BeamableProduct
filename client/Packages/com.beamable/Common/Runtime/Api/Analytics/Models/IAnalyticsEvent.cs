// this file was copied from nuget package Beamable.Common@4.1.5
// https://www.nuget.org/packages/Beamable.Common/4.1.5

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
