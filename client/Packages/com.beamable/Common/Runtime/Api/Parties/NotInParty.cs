// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

using System;

namespace Beamable.Experimental.Api.Parties
{
	/// <summary>
	/// Exception thrown when making requests to <see cref="Beamable.Player.PlayerParty"/> when a player is not in a <see cref="Party"/>.
	/// </summary>
	public class NotInParty : Exception { }
}
