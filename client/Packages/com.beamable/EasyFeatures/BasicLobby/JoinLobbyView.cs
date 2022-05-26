using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Lobbies;
using Beamable.UI.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class JoinLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; }
			int SelectedGameTypeIndex { get; set; }
			int? SelectedLobbyIndex { get; }
			string NameFilter { get; }
			int CurrentPlayersFilter { get; }
			int MaxPlayersFilter { get; }
			List<SimGameType> GameTypes { get; }
			List<Lobby> LobbiesData { get; }
			void ApplyFilter(string name);
			void ApplyFilter(string name, int currentPlayers, int maxPlayers);
			Promise GetLobbies();
			void OnLobbySelected(int? obj);
			bool CanJoinLobby();
			Promise JoinLobby(string lobbyId);
		}
		
		[Header("View Configuration")]
		public int EnrichOrder;
		public BeamableViewGroup ViewGroup;
		public LobbyFeatureControl FeatureControl;
		
		[Header("Components")]
		public MultiToggleComponent TypesToggle;
		public GameObjectToggler LoadingIndicator;
		public GameObject NoLobbiesIndicator;
		public LobbiesListPresenter LobbiesList;
		public TMP_InputField FilterField;
		public Button ClearFilterButton;
		public Button JoinLobbyButton;
		public Button BackButton;

		private IDependencies _system;
		private BeamContext _beamContext;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			_beamContext = managedPlayers.GetSinglePlayerContext();
			_system = _beamContext.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(_system.IsVisible);

			// We don't need to perform anything in case if view is not visible. Visibility is controlled by a feature control script.
			if (!_system.IsVisible)
			{
				return;
			}
			
			// Setting up all components
			TypesToggle.Setup(_system.GameTypes.Select(type => type.ContentName).ToList(), OnGameTypeSelected, _system.SelectedGameTypeIndex);
			
			// TODO: wrap this in some helper
			FilterField.onEndEdit.ReplaceOrAddListener(OnFilterApplied);
			ClearFilterButton.onClick.ReplaceOrAddListener(ClearButtonClicked);
			JoinLobbyButton.onClick.ReplaceOrAddListener(JoinLobbyButtonClicked);
			JoinLobbyButton.interactable = _system.CanJoinLobby();
			
			BackButton.onClick.ReplaceOrAddListener(BackButtonClicked);
			
			FilterField.SetTextWithoutNotify(_system.NameFilter);
			
			LobbiesList.ClearPooledRankedEntries();
			LobbiesList.Setup(_system.LobbiesData, OnLobbySelected);
			LobbiesList.RebuildPooledLobbiesEntries();
			
			NoLobbiesIndicator.SetActive(_system.LobbiesData.Count == 0);
		}

		private void BackButtonClicked()
		{
			OnLobbySelected(null);
			FeatureControl.OpenMainView();
		}

		private async void JoinLobbyButtonClicked()
		{
			if (_system.SelectedLobbyIndex == null)
			{
				return; 
			}
			
			FeatureControl.ShowOverlayedLabel("Joining lobby...");
			
			try
			{
				await _system.JoinLobby(_system.LobbiesData[_system.SelectedLobbyIndex.Value].lobbyId);
				FeatureControl.HideOverlay();
				if (_beamContext.Lobby.State != null)
				{
					FeatureControl.OpenLobbyView(_beamContext.Lobby.State);
				}
			}
			catch (Exception e)
			{
				FeatureControl.ShowErrorWindow(e.Message);
				// if (e is PlatformRequesterException pre)
				// {
				// 	FeatureControl.ShowErrorWindow(pre.Error.error);
				// }
			}
		}

		private void OnLobbySelected(int? lobbyId)
		{
			_system.OnLobbySelected(lobbyId);
			JoinLobbyButton.interactable = _system.CanJoinLobby();
		}

		private async void ClearButtonClicked()
		{
			_system.ApplyFilter(String.Empty);
			await ViewGroup.Enrich();
		}

		private async void OnFilterApplied(string filter)
		{
			_system.ApplyFilter(filter);
			await ViewGroup.Enrich();
		}

		private async void OnGameTypeSelected(int optionId)
		{
			if (optionId == _system.SelectedGameTypeIndex)
			{
				return;
			}
			
			OnLobbySelected(null);
			
			_system.SelectedGameTypeIndex = optionId;
			
			NoLobbiesIndicator.SetActive(false);
			
			LoadingIndicator.Toggle(true);
			await _system.GetLobbies();
			LoadingIndicator.Toggle(false);
			
			await ViewGroup.Enrich();
		}
	}
}
