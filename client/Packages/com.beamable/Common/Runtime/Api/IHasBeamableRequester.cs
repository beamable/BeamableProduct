// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC4
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC4

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
