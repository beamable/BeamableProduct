using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.Basicmatchmaking
{
	public class StartMatchmakingView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
			List<SimGameType> GameTypes { get; }
			int SelectedGameTypeIndex { get; set; }
			bool InProgress { get; set; }
			Promise StartMatchmaking();
		}

		[Header("View Configuration")]
		public int EnrichOrder;

		[Header("Components")]
		public MultiToggleComponent TypesToggle;
		public Button StartMatchmakingButton;

		[Header("Callbacks")]
		public UnityEvent OnStartMatchmakingRequestSent;

		public Action<string> OnError;

		public int GetEnrichOrder() => EnrichOrder;

		protected IDependencies System;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			System = ctx.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(System.IsVisible);

			// Setting up all components
			TypesToggle.Setup(System.GameTypes.Select(gameType => gameType.name).ToList(), OnGameTypeSelected,
			                  System.SelectedGameTypeIndex);

			StartMatchmakingButton.onClick.ReplaceOrAddListener(StartMatchmakingButtonClicked);
		}

		private void OnGameTypeSelected(int optionId)
		{
			if (optionId == System.SelectedGameTypeIndex)
			{
				return;
			}

			System.SelectedGameTypeIndex = optionId;
		}

		private async void StartMatchmakingButtonClicked()
		{
			if (System.InProgress)
			{
				return;
			}

			try
			{
				OnStartMatchmakingRequestSent?.Invoke();
				await System.StartMatchmaking();
			}
			catch (Exception e)
			{
				if (e is PlatformRequesterException pre)
				{
					OnError?.Invoke(pre.Error.error);
					System.InProgress = false;
				}
			}
		}
	}
}
