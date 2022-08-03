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
			public string PlayerId { get; set; }
			public bool IsReady { get; set; }
			public string Team { get; set; }
			public int Index { get; set; }
			public float Height => 150.0f;
		}

		[Header("Components")]
		public GameObject Filled;
		public GameObject Empty;
		public TextMeshProUGUI Name;
		public TextMeshProUGUI Team;

		public void SetupEmpty()
		{
			Empty.SetActive(true);
			Filled.SetActive(false);
		}

		public void SetupFilled(string playerName, string team, bool isReady)
		{
			string ready = isReady ? "READY" : "NOT READY";
			
			Name.text = $"{playerName} - {ready}";
			Team.text = team;
			
			Empty.SetActive(false);
			Filled.SetActive(true);
		}
	}
}
