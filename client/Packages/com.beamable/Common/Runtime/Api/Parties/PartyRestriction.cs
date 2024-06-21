// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

namespace Beamable.Experimental.Api.Parties
{
	/// <summary>
	/// Value used to determine who is able to join a <see cref="Party"/>.
	/// </summary>
	public enum PartyRestriction
	{
		Unrestricted,
		InviteOnly
	}
}
