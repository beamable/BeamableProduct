// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405141737
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405141737

﻿using System;

namespace Beamable.Experimental.Api.Parties
{
	/// <summary>
	/// Request payload to be used whenever player action in <see cref="Party"/> is needed.
	/// This includes kicking, promoting or inviting a player to the <see cref="Party"/>.
	/// </summary>
	[Serializable]
	public class PlayerRequest
	{
		/// <summary>
		/// The id of the player on which action is meant to be taken.
		/// </summary>
		public string playerId;

		public PlayerRequest(string playerId)
		{
			this.playerId = playerId;
		}
	}
}
