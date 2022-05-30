using UnityEngine;

namespace Beamable.EasyFeatures.BasicParty
{
	public enum PartyAccess
	{
		Private,
		Public,
	}
	
	public class PartyData : MonoBehaviour
	{
		public string PartyId
		{
			get;
			private set;
		}
		
		public PartyAccess Access
		{
			get;
			private set;
		}
		
		public int MaxPlayers
		{
			get;
			private set;
		}
	}
}
