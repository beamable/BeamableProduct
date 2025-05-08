// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

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
