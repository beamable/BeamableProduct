using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
			public int Index { get; set; }
		}

		[Header("Components")]
		public TextMeshProUGUI Name;
		public TextMeshProUGUI Users;
		public GameObject SelectionMark;
		public Button Button;

		private Action<LobbiesListEntryPresenter> _onLobbySelected;

		public void Setup(Data data, Action<LobbiesListEntryPresenter> onLobbySelected)
		{
			Name.text = data.Name;
			Users.text = $"{data.CurrentPlayers}/{data.MaxPlayers}";
			_onLobbySelected = onLobbySelected;

			Button.onClick.RemoveListener(OnClick);
			Button.onClick.AddListener(OnClick);
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
