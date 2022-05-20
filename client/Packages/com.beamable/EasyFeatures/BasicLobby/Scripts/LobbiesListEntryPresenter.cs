using Beamable.UI.Scripts;
using TMPro;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbiesListEntryPresenter : MonoBehaviour
	{
		public struct Data
		{
			public string Name;
			public int CurrentPlayers;
			public int MaxPlayers;
		}
		
		public class PoolData : PoolableScrollView.IItem
		{
			public Data Data { get; set; }
			public float Height { get; set; }
		}

		[SerializeField] private TextMeshProUGUI _name;
		[SerializeField] private TextMeshProUGUI _users;

		public void Setup(Data data)
		{
			_name.text = data.Name;
			_users.text = $"{data.CurrentPlayers}/{data.MaxPlayers}";
		}
	}
}
