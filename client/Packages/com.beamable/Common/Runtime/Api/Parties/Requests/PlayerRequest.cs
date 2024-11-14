// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC6

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
