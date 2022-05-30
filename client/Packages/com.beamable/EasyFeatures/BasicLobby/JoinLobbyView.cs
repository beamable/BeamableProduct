﻿using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class JoinLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; }
			bool IsLoading { get; set; }
			int SelectedGameTypeIndex { get; set; }
			int? SelectedLobbyIndex { get; }
			string NameFilter { get; }
			int CurrentPlayersFilter { get; }
			int MaxPlayersFilter { get; }
			List<SimGameType> GameTypes { get; }
			List<LobbiesListEntryPresenter.ViewData> LobbiesData { get; }
			void ApplyFilter(string name);
			void ApplyFilter(string name, int currentPlayers, int maxPlayers);
			void OnLobbySelected(int? obj);
			bool CanJoinLobby();
			Promise JoinLobby();
			Promise GetLobbies();
		}

		[Header("View Configuration")]
		public int EnrichOrder;
		public BeamableViewGroup ViewGroup;

		[Header("Components")]
		public MultiToggleComponent TypesToggle;
		public GameObject NoLobbiesIndicator;
		public LobbiesListPresenter LobbiesList;
		public TMP_InputField FilterField;
		public Button ClearFilterButton;
		public Button JoinLobbyButton;
		public Button BackButton;

		[Header("Callbacks")]
		public UnityEvent OnGetLobbiesRequestSent;
		public UnityEvent OnGetLobbiesRequestReceived;
		public UnityEvent OnJoinLobbyRequestSent;
		public UnityEvent OnJoinLobbyRequestReceived;
		public UnityEvent OnBackButtonClicked;
		
		public Action<string> OnError;

		protected IDependencies System;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			BeamContext ctx = managedPlayers.GetSinglePlayerContext();
			System = ctx.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(System.IsVisible);

			// We don't need to perform anything in case if view is not visible. Visibility is controlled by a feature control script.
			if (!System.IsVisible)
			{
				return;
			}

			// Setting up all components
			TypesToggle.Setup(System.GameTypes.Select(gameType => gameType.name).ToList(), OnGameTypeSelected,
			                  System.SelectedGameTypeIndex);

			FilterField.onEndEdit.ReplaceOrAddListener(OnFilterApplied);
			ClearFilterButton.onClick.ReplaceOrAddListener(ClearButtonClicked);
			JoinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			JoinLobbyButton.interactable = System.CanJoinLobby();
			BackButton.onClick.ReplaceOrAddListener(BackButtonClicked);

			FilterField.SetTextWithoutNotify(System.NameFilter);

			LobbiesList.gameObject.SetActive(!System.IsLoading);
			NoLobbiesIndicator.SetActive(System.LobbiesData.Count == 0 && !System.IsLoading);

			if (System.IsLoading)
			{
				return;
			}
			
			LobbiesList.ClearPooledRankedEntries();
			LobbiesList.Setup(System.LobbiesData, OnLobbySelected);
			LobbiesList.RebuildPooledLobbiesEntries();
		}

		private async void JoinLobbyButtonClicked()
		{
			try
			{
				OnJoinLobbyRequestSent?.Invoke();
				await System.JoinLobby();
				OnJoinLobbyRequestReceived?.Invoke();
			}
			catch (Exception e)
			{
				OnError?.Invoke(e.Message);
				// if (e is PlatformRequesterException pre)
				// {
				//		OnError?.Invoke(pre.Error.error);
				// }
			}
		}

		private async void OnGameTypeSelected(int optionId)
		{
			if (optionId == System.SelectedGameTypeIndex)
			{
				return;
			}

			OnLobbySelected(null);

			System.SelectedGameTypeIndex = optionId;
			
			OnGetLobbiesRequestSent?.Invoke();
			System.IsLoading = true;
			await ViewGroup.Enrich();

			try
			{
				await System.GetLobbies();
				OnGetLobbiesRequestReceived?.Invoke();
				System.IsLoading = false;
				await ViewGroup.Enrich();
			}
			catch (Exception e)
			{
				OnError?.Invoke(e.Message);
				// if (e is PlatformRequesterException pre)
				// {
				//		OnError?.Invoke(pre.Error.error);
				// }
			}
		}

		private void BackButtonClicked()
		{
			OnLobbySelected(null);
			OnBackButtonClicked?.Invoke();
		}

		private void OnLobbySelected(int? lobbyId)
		{
			System.OnLobbySelected(lobbyId);
			JoinLobbyButton.interactable = System.CanJoinLobby();
		}

		private async void ClearButtonClicked()
		{
			System.ApplyFilter(String.Empty);
			await ViewGroup.Enrich();
		}

		private async void OnFilterApplied(string filter)
		{
			System.ApplyFilter(filter);
			await ViewGroup.Enrich();
		}
	}
}
