using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.BasicLeaderboard;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasyFeaturesIntegrationExamples.FeatureCompositionIntegration
{
	public class FeatureCompositionBoot : MonoBehaviour
	{
		public void OnEnable()
		{
			var loadSceneAsync = SceneManager.LoadSceneAsync("FeatureCompositionIntegrationScene");
			loadSceneAsync.completed += _ =>
			{
				var playerAssetLeaderboardView = FindObjectOfType<Player3DAssetLeaderboardView>();
				var leaderboardFeatureControl = FindObjectOfType<BasicLeaderboardFeatureControl>();
				leaderboardFeatureControl.LeaderboardViewGroup.ManagedViews.Add(playerAssetLeaderboardView);
				leaderboardFeatureControl.gameObject.SetActive(true);
				leaderboardFeatureControl.Run();
			};
		}
	}
}
