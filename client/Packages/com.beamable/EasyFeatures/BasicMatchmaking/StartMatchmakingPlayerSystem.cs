using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures.Basicmatchmaking;
using Beamable.Experimental.Api.Matchmaking;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.EasyFeatures.BasicMatchmaking
{
	public class StartMatchmakingPlayerSystem : StartMatchmakingView.IDependencies
	{
		public event Action<MatchmakingState> OnStateChanged;
		
		public BeamContext BeamContext { get; }
		
		public bool IsVisible { get; set; }
		public List<SimGameType> GameTypes { get; set; }
		public int SelectedGameTypeIndex { get; set; }
		public int TimeoutSeconds { get; set; }
		public bool InProgress { get; set; }

		private MatchmakingState _matchmakingState;
		public MatchmakingState MatchmakingState
		{
			get => _matchmakingState;
			set
			{
				_matchmakingState = value;
				OnStateChanged?.Invoke(_matchmakingState);
			}
		}

		public StartMatchmakingPlayerSystem(BeamContext beamContext)
		{
			BeamContext = beamContext;
		}
		
		public void Setup(List<SimGameType> gameTypes, int timeoutSeconds)
		{
			GameTypes = gameTypes;
			SelectedGameTypeIndex = 0;
			TimeoutSeconds = timeoutSeconds;
			InProgress = false;
		}

		public async Promise StartMatchmaking()
		{
			InProgress = true;

			MatchmakingHandle matchmakingHandle = await BeamContext.Api.Experimental.MatchmakingService.StartMatchmaking(
				GameTypes[SelectedGameTypeIndex].Id, OnUpdate, OnReady, OnTimeout,
				TimeSpan.FromSeconds(TimeoutSeconds));

			RegisterMatchmakingHandle(matchmakingHandle);
		}

		private void RegisterMatchmakingHandle(MatchmakingHandle matchmakingHandle)
		{
			 MatchmakingState = matchmakingHandle.State;
		}

		private void OnUpdate(MatchmakingHandle matchmakingHandle)
		{
			RegisterMatchmakingHandle(matchmakingHandle);
			Debug.Log("OnUpdate");
		}

		private void OnReady(MatchmakingHandle matchmakingHandle)
		{
			RegisterMatchmakingHandle(matchmakingHandle);
			InProgress = false;
			Debug.Log("OnReady");
		}

		private void OnTimeout(MatchmakingHandle matchmakingHandle)
		{
			RegisterMatchmakingHandle(matchmakingHandle);
			InProgress = false;
			Debug.Log("OnTimeout");
		}
	}
}
