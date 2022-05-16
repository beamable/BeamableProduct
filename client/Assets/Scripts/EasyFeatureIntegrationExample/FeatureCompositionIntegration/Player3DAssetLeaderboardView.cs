using Beamable;
using Beamable.EasyFeatures;
using Beamable.EasyFeatures.BasicLeaderboard;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyFeaturesIntegrationExamples.FeatureCompositionIntegration
{
	public class Player3DAssetLeaderboardView : MonoBehaviour, ISyncBeamableView
	{
		public GameObject PlayerAsset;

		public float RotateSpeedIfGreaterOrEqualTo50;
		public float RotateSpeedIfLessThan50;

		public Coroutine RotatingCoroutine;
		
		public int GetEnrichOrder() => int.MaxValue;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var leaderboardViewDeps = currentContext.ServiceProvider
			                                        .GetService<BasicLeaderboardView.ILeaderboardDeps>();
		
			var maxScore = Beamable.Common.Constants.Features.Leaderboards.TEST_DATA_MAX_SCORE;
			var playerScore = leaderboardViewDeps.PlayerScore;
			
			var normalizedScore = playerScore / maxScore;
			
			if(RotatingCoroutine != null)
				StopCoroutine(RotatingCoroutine);

			var speed = normalizedScore >= .5 ? RotateSpeedIfGreaterOrEqualTo50 : RotateSpeedIfLessThan50;
			RotatingCoroutine = StartCoroutine(Rotate(PlayerAsset, speed));

			IEnumerator Rotate(GameObject toRotate, float rotateSpeed)
			{
				while (true)
				{
					toRotate.transform.localEulerAngles += Vector3.up * rotateSpeed;
					yield return null;
				}
			}					
		}
	}
}
