// this file was copied from nuget package Beamable.Common@4.2.0-PREVIEW.RC3
// https://www.nuget.org/packages/Beamable.Common/4.2.0-PREVIEW.RC3

namespace Beamable.Common.Api
{
	public interface IHasBeamableRequester
	{
		/// <summary>
		/// Access the <see cref="IBeamableRequester"/>
		/// </summary>
		IBeamableRequester Requester { get; }
	}
}
