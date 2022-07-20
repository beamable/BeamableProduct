using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Beamable.EasyFeatures;
using Beamable.EasyFeatures.BasicLeaderboard;

namespace EasyFeaturesIntegrationExamples.BasicIntegration
{
	public class BasicIntegrationBoot : MonoBehaviour
	{
		public GameObject LeaderboardPrefab;

		private BasicLeaderboardFeatureControl _leaderboardInstance;

		public void OpenLeaderboardEasyFeature()
		{
			_leaderboardInstance = Instantiate(LeaderboardPrefab).GetComponent<BasicLeaderboardFeatureControl>();
			_leaderboardInstance.gameObject.SetActive(true);
			_leaderboardInstance.Run();
		}
	}
}
