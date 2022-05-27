using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures.Components;
using Beamable.Experimental.Api.Lobbies;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.EasyFeatures.BasicLobby
{
	public class CreateLobbyView : MonoBehaviour, ISyncBeamableView
	{
		public interface IDependencies : IBeamableViewDeps
		{
			bool IsVisible { get; set; }
			List<SimGameType> GameTypes { get; }
			int SelectedGameTypeIndex { get; set; }
			Dictionary<string, LobbyRestriction> AccessOptions { get; }
			int SelectedAccessOption { get; set; }
			string Name { get; set; }
			string Description { get; set; }
			bool ValidateConfirmButton();
			void ResetData();
			Promise<Lobby> CreateLobby();
		}

		[Header("View Configuration")]
		public int EnrichOrder;
		public LobbyFeatureControl FeatureControl;
		
		[Header("Components")]
		public MultiToggleComponent TypesToggle;

		public MultiToggleComponent AccessToggle;
		public TMP_InputField Name;
		public TMP_InputField Description;
		public Button ConfirmButton;
		public Button CancelButton;
		public Button BackButton;

		private IDependencies _system;
		public BeamContext _userContext;

		public int GetEnrichOrder() => EnrichOrder;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			_userContext = managedPlayers.GetSinglePlayerContext();
			_system = _userContext.ServiceProvider.GetService<IDependencies>();

			gameObject.SetActive(_system.IsVisible);

			// We don't need to perform anything in case if view is not visible. Visibility is controlled by a feature control script.
			if (!_system.IsVisible)
			{
				return;
			}

			// Setting up all components
			TypesToggle.Setup(_system.GameTypes.Select(type => type.ContentName).ToList(), OnGameTypeSelected,
			                  _system.SelectedGameTypeIndex);
			AccessToggle.Setup(_system.AccessOptions.Select(pair => pair.Key).ToList(), OnAccessOptionSelected,
			                   _system.SelectedAccessOption);

			Name.SetTextWithoutNotify(_system.Name);
			Description.SetTextWithoutNotify(_system.Description);

			Name.onValueChanged.ReplaceOrAddListener(OnNameChanged);
			Description.onValueChanged.ReplaceOrAddListener(OnDescriptionChanged);
			ConfirmButton.onClick.ReplaceOrAddListener(CreateLobbyButtonClicked);
			ConfirmButton.interactable = _system.ValidateConfirmButton();
			CancelButton.onClick.ReplaceOrAddListener(CancelButtonClicked);
			BackButton.onClick.ReplaceOrAddListener(CancelButtonClicked);
		}

		private void CancelButtonClicked()
		{
			_system.ResetData();
			FeatureControl.OpenMainView();
		}

		private async void CreateLobbyButtonClicked()
		{
			FeatureControl.ShowOverlayedLabel("Creating Lobby...");

			try
			{
				Lobby lobby = await _system.CreateLobby();
				FeatureControl.HideOverlay();
				if (lobby != null)
				{
					_system.ResetData();
					FeatureControl.OpenLobbyView(lobby);
				}
			}
			catch (Exception e)
			{
				if (e is PlatformRequesterException pre)
				{
					FeatureControl.ShowErrorWindow(pre.Error.error);
				}
			}
		}

		private void OnNameChanged(string value)
		{
			_system.Name = value;
			ConfirmButton.interactable = _system.ValidateConfirmButton();
		}

		private void OnDescriptionChanged(string value)
		{
			_system.Description = value;
		}

		private void OnAccessOptionSelected(int optionId)
		{
			if (optionId == _system.SelectedAccessOption)
			{
				return;
			}

			_system.SelectedAccessOption = optionId;
		}

		private void OnGameTypeSelected(int optionId)
		{
			if (optionId == _system.SelectedGameTypeIndex)
			{
				return;
			}

			_system.SelectedGameTypeIndex = optionId;
		}
	}
}
