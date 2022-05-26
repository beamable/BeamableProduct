using Beamable.Experimental.Api.Lobbies;
using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbiesListEntryPresenter : MonoBehaviour
	{
		public class PoolData : PoolableScrollView.IItem
		{
			public Lobby Data { get; set; }
			public float Height { get; set; }
			public int Index { get; set; }
		}

		[Header("Components")]
		public TextMeshProUGUI Name;
		public TextMeshProUGUI Users;
		public GameObject SelectionMark;
		public Button Button;

		private Action<LobbiesListEntryPresenter> _onLobbySelected;

		public void Setup(Lobby data, Action<LobbiesListEntryPresenter> onLobbySelected)
		{
			Name.text = data.name;
			Users.text = $"{data.players.Count}/{data.maxPlayers}";
			_onLobbySelected = onLobbySelected;

			Button.onClick.ReplaceOrAddListener(OnClick);
			SetSelected(false);
		}

		private void OnClick()
		{
			_onLobbySelected.Invoke(this);
		}

		public void SetSelected(bool value)
		{
			SelectionMark.SetActive(value);
		}
	}
}
