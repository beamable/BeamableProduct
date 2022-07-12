using Beamable.UI.Scripts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class MatchmakingSlotPresenter : MonoBehaviour
	{
		public class ViewData : PoolableScrollView.IItem
		{
			public float FoldedHeight => 150.0f;
			public float UnfoldedHeight => 300.0f;
			
			public string PlayerId { get; set; }
			public string Team { get; set; }
			public float Height { get; set; }
			public int Index { get; set; }
			public bool IsUnfolded { get; set; }
		}

		[Header("Components")]
		public GameObject Filled;
		public GameObject Empty;
		public TextMeshProUGUI Name;
		public TextMeshProUGUI Team;
		public GameObject ButtonsGroup;
		public Button AdminButton;
		public Button KickButton;

		public void SetupEmpty()
		{
			Empty.SetActive(true);
			Filled.SetActive(false);
			ButtonsGroup.SetActive(false);
		}

		public void SetupFilled(string playerName,
		                        string team,
		                        bool isAdmin,
		                        bool isUnfolded,
		                        Action onAdminButtonClicked,
		                        Action onKickButtonClicked)
		{
			Name.text = playerName;
			Team.text = team;
			
			KickButton.onClick.ReplaceOrAddListener(onKickButtonClicked.Invoke);
			AdminButton.onClick.ReplaceOrAddListener(onAdminButtonClicked.Invoke);

			Empty.SetActive(false);
			Filled.SetActive(true);
			ButtonsGroup.SetActive(isUnfolded);
			AdminButton.interactable = isAdmin;
		}
	}
}
