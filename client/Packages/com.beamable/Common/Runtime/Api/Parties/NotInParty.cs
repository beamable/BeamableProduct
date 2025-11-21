// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿using System;

namespace Beamable.Experimental.Api.Parties
{
	/// <summary>
	/// Exception thrown when making requests to PlayerLobby when a player is not in a <see cref="Party"/>.
	/// </summary>
	public class NotInParty : Exception { }
}
