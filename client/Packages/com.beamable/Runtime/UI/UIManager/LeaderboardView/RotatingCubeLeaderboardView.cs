using System;
using System.Collections;
using System.Collections.Generic;
using Beamable;
using Beamable.Common;
using UnityEngine;

public class RotatingCubeLeaderboardView : MonoBehaviour, ISyncBeamableView
{
	public int EnrichOrder = int.MaxValue;
	public GameObject FakePlayerCharacter;
	public float RotationSpeed = 1f;

	private Coroutine rotatingCubeCoroutine;
	
	public BeamableViewGroup.PlayerCountMode SupportedMode => BeamableViewGroup.PlayerCountMode.SinglePlayerUI;
	public int GetEnrichOrder() => EnrichOrder;

	public void EnrichWithContext(BeamContext currentContext)
	{
		var leaderboardViewDeps = currentContext.ServiceProvider.GetService<LeaderboardView.ILeaderboardDeps>();

		var userIdx = leaderboardViewDeps.CurrentUserIndexInLeaderboard;
		if(userIdx == -1 && rotatingCubeCoroutine != null)
			StopCoroutine(rotatingCubeCoroutine);

		if (userIdx != -1)
			rotatingCubeCoroutine = StartCoroutine(RotateCube(RotationSpeed));

		IEnumerator RotateCube(float speed)
		{
			while (true)
			{
				FakePlayerCharacter.transform.RotateAround(FakePlayerCharacter.transform.position, Vector3.up, speed * Time.deltaTime);
				yield return null;
			}
		}
	}

	public void EnrichWithContext(BeamContext currentContext, int playerIndex)
    {
	    throw new System.NotImplementedException();
    }
}
