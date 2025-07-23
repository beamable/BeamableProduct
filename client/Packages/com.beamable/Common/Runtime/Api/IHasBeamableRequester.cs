// this file was copied from nuget package Beamable.Common@5.1.0-PREVIEW.RC1
// https://www.nuget.org/packages/Beamable.Common/5.1.0-PREVIEW.RC1

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
