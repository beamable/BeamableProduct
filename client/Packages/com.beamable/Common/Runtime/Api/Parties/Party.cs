using Beamable.Common.Player;
using System;
using System.Collections.Generic;

namespace Beamable.Experimental.Api.Parties
{
	[Serializable]
	public class Party : DefaultObservable
	{
		/// <summary>
		/// The id of the lobby. Use this id when making requests for a particular lobby via <see cref="IPartyApi"/>
		/// </summary>
		public string partyId;

		/// <summary>
		/// String version of the `Access` property.
		/// </summary>
		public string access;

		/// <summary>
		/// PlayerId of a player who created the party.
		/// </summary>
		public string host;

		/// <summary>
		/// Max number of players this party can hold.
		/// </summary>
		public int maxPlayers;

		/// <summary>
		/// List of <see cref="PartyPlayer"/> who are currently active in the party.
		/// </summary>
		public List<PartyPlayer> players;

		/// <summary>
		/// Either "Private" of "Public" representing who can join the <see cref="Party"/>
		/// </summary>
		public PartyAccess Access => (PartyAccess)Enum.Parse(typeof(PartyAccess), access);

		/// <summary>
		/// Update the state of the current party with the data from another party instance.
		/// This will trigger the observable callbacks.
		/// </summary>
		/// <param name="updatedState">The latest copy of the party</param>
		public void Set(Party updatedState)
		{
			partyId = updatedState?.partyId;
			access = updatedState?.access;
			host = updatedState?.host;
			maxPlayers = updatedState?.maxPlayers ?? 0;
			players = updatedState?.players;
			TriggerUpdate();
		}
	}
}
