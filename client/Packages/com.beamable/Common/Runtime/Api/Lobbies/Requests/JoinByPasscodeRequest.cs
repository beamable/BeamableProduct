// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Lobbies
{
	/// <summary>
	/// Request payload for joining a <see cref="Lobby"/> by passcode.
	/// </summary>
	[Serializable]
	public class JoinByPasscodeRequest
	{
		/// <summary>
		/// Passcode of the <see cref="Lobby"/> to join.
		/// </summary>
		public string passcode;

		/// <summary>
		/// List of <see cref="Tag"/> to associate to the joined player in the <see cref="Lobby"/>.
		/// </summary>
		public List<Tag> tags;

		public JoinByPasscodeRequest(string passcode, List<Tag> tags)
		{
			this.passcode = passcode;
			this.tags = tags;
		}
	}
}
