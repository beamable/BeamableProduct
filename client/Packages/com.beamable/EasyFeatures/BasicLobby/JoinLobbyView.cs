using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures.Components;
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
			int SelectedLobbyIndex { get; }
			string NameFilter { get; }
			int CurrentPlayersFilter { get; }
			int MaxPlayersFilter { get; }
			List<SimGameType> GameTypes { get; }
			List<LobbiesListEntryPresenter.Data> LobbiesData { get; }
			void ApplyFilter(string name);
			void ApplyFilter(string name, int currentPlayers, int maxPlayers);
			Promise ConfigureData();
			void OnLobbySelected(int obj);
			bool CanJoinLobby();
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
		
		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			var ctx = managedPlayers.GetSinglePlayerContext();
			_system = ctx.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(_system.IsVisible);

			// We don't need to perform anything in case if view is not visible. Visibility is controlled by a feature control script.
			if (!_system.IsVisible)
			{
				return;
			}
			
			// Setting up all components
			TypesToggle.Setup(_system.GameTypes.Select(type => type.ContentName).ToList(), OnGameTypeSelected, _system.SelectedGameTypeIndex);
			
			// TODO: wrap this in some helper
			FilterField.onEndEdit.RemoveListener(OnFilterApplied);
			FilterField.onEndEdit.AddListener(OnFilterApplied);
			
			ClearFilterButton.onClick.RemoveListener(ClearButtonClicked);
			ClearFilterButton.onClick.AddListener(ClearButtonClicked);
			
			JoinLobbyButton.onClick.RemoveListener(JoinLobbyButtonClicked);
			JoinLobbyButton.onClick.AddListener(JoinLobbyButtonClicked);
			JoinLobbyButton.interactable = _system.CanJoinLobby();
			
			BackButton.onClick.RemoveListener(BackButtonClicked);
			BackButton.onClick.AddListener(BackButtonClicked);
			
			FilterField.SetTextWithoutNotify(_system.NameFilter);
			
			LobbiesList.ClearPooledRankedEntries();
			LobbiesList.Setup(_system.LobbiesData, OnLobbySelected);
			LobbiesList.RebuildPooledLobbiesEntries();
			
			NoLobbiesIndicator.SetActive(_system.LobbiesData.Count == 0);
		}

		private void BackButtonClicked()
		{
			OnLobbySelected(-1);
			FeatureControl.OpenMainView();
		}

		private void JoinLobbyButtonClicked()
		{
			
		}

		private void OnLobbySelected(int lobbyId)
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
			
			OnLobbySelected(-1);
			
			_system.SelectedGameTypeIndex = optionId;
			
			NoLobbiesIndicator.SetActive(false);
			
			LoadingIndicator.Toggle(true);
			await _system.ConfigureData();
			LoadingIndicator.Toggle(false);
			
			await ViewGroup.Enrich();
		}
	}
}
