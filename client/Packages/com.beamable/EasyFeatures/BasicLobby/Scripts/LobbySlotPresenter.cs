using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class LobbySlotPresenter : MonoBehaviour
	{
		public struct ViewData
		{
			public string PlayerId;
			public bool IsReady;
		}

		public class PoolData : PoolableScrollView.IItem
		{
			public ViewData ViewData { get; set; }
			public float Height { get; set; }
			public int Index { get; set; }
		}

		[Header("Components")]
		public GameObject Filled;
		public GameObject Empty;
		public TextMeshProUGUI Name;
		public GameObject ReadyButton;
		public GameObject NotReadyButton;
		public Button AdminButton;

		public void SetupEmpty()
		{
			Empty.SetActive(true);
			Filled.SetActive(false);
		}
		
		public void SetupFilled(string playerName, bool isReady, bool isAdmin, Action onAdminButtonClicked)
		{
			Name.text = playerName;
			
			ReadyButton.gameObject.SetActive(isReady);
			NotReadyButton.gameObject.SetActive(!isReady);

			AdminButton.onClick.ReplaceOrAddListener(onAdminButtonClicked.Invoke);
			AdminButton.gameObject.SetActive(isAdmin);
			
			Empty.SetActive(false);
			Filled.SetActive(true);
		}
	}
}
