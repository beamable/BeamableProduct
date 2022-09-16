using Beamable.UI.Scripts;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicSocial
{
	public class FriendSlotPresenter : MonoBehaviour
	{
		public struct ViewData
		{
			public string PlayerId;
			public string PlayerName;
			public string Description;
			public Sprite Avatar;
		}
		
		public class PoolData : PoolableScrollView.IItem
		{
			public ViewData ViewData { get; set; }
			public int Index { get; set; }
			public float Height { get; set; }
		}
	}
}
