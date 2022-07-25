using Beamable;
using Beamable.Common.Leaderboards;
using NewTestingTool.Attributes;
using NewTestingTool.Core;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class Tester : Testable
{
	[SerializeField] private LeaderboardRef _leaderboardRef = null;
	
	private BeamContext _ctx;
	
	[TestRule(1)]
	private async Task<TestResult> Init()
	{
		try 
		{ 		
			_ctx = BeamContext.Default;
			await _ctx.OnReady;
		}
		catch (Exception e)
		{
			TestableDebug.LogError(e);
			return TestResult.Failed;
		}

		return TestResult.Passed;
	}
	
	[TestRule(2, 100)]
	private async Task<TestResult> SetScore(double score)
	{
		try
		{
			await _ctx.Api.LeaderboardService.SetScore(_leaderboardRef.Id, score);
		}
		catch (Exception e)
		{
			TestableDebug.LogError(e);
			return TestResult.Failed;
		}
		return TestResult.Passed;
	}
	
	[TestRule(3, 50)]
	[TestRule(3, 100)]
	private async Task<TestResult> IncrementScore(double score)
	{
		try
		{
			await _ctx.Api.LeaderboardService.IncrementScore(_leaderboardRef.Id, score);
		}
		catch (Exception e)
		{
			TestableDebug.LogError(e);
			return TestResult.Failed;
		}
		return TestResult.Passed;
	}
}

