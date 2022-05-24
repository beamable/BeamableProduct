using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbySlotPresenter : MonoBehaviour
	{
		public struct Data
		{
			public bool IsOccupied;
			public string Name;
			public bool IsReady;
		}

		public class PoolData : PoolableScrollView.IItem
		{
			public Data Data { get; set; }
			public float Height { get; set; }
			public int Index { get; set; }
		}

		[Header("Components")]
		public GameObject Filled;
		public GameObject Empty;
		public TextMeshProUGUI Name;
		public Button ReadyButton;
		public Button NotReadyButton;
		public Button AdminButton;

		public void SetupEmpty()
		{
			Empty.SetActive(true);
			Filled.SetActive(false);
		}
		
		public void SetupFilled(string playerName, bool isReady, bool isAdmin, Action onReadyButtonClicked, Action onNotReadyButtonClicked, Action onAdminButtonClicked)
		{
			Name.text = playerName;
			
			ReadyButton.onClick.RemoveListener(onReadyButtonClicked.Invoke);
			ReadyButton.onClick.AddListener(onReadyButtonClicked.Invoke);
			ReadyButton.gameObject.SetActive(isReady);
			
			NotReadyButton.onClick.RemoveListener(onNotReadyButtonClicked.Invoke);
			NotReadyButton.onClick.AddListener(onNotReadyButtonClicked.Invoke);
			NotReadyButton.gameObject.SetActive(!isReady);

			AdminButton.onClick.RemoveListener(onAdminButtonClicked.Invoke);
			AdminButton.onClick.AddListener(onAdminButtonClicked.Invoke);
			AdminButton.gameObject.SetActive(isAdmin);
			
			Empty.SetActive(false);
			Filled.SetActive(true);
		}
	}
}
