using Beamable.Common.Content;
using Beamable.EasyFeatures.Components;
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
			int SelectedGameType { get; set; }
			Dictionary<string, bool> AccessOptions { get; }
			int SelectedAccessOption { get; set; }
			string Name { get; set; }
			string Description { get; set; }
			bool ValidateConfirmButton();
			void ConfirmButtonClicked();
			void ResetData();
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
			TypesToggle.Setup(_system.GameTypes.Select(type => type.ContentName).ToList(), OnGameTypeSelected, _system.SelectedGameType);
			AccessToggle.Setup(_system.AccessOptions.Select(pair => pair.Key).ToList(), OnAccessOptionSelected, _system.SelectedAccessOption);
			
			Name.SetTextWithoutNotify(_system.Name);
			Description.SetTextWithoutNotify(_system.Description);
			
			Name.onValueChanged.RemoveListener(OnNameChanged);
			Name.onValueChanged.AddListener(OnNameChanged);
			
			Description.onValueChanged.RemoveListener(OnDescriptionChanged);
			Description.onValueChanged.AddListener(OnDescriptionChanged);
			
			ConfirmButton.onClick.RemoveListener(ConfirmButtonClicked);
			ConfirmButton.onClick.AddListener(ConfirmButtonClicked);
			ConfirmButton.interactable = _system.ValidateConfirmButton();
			
			CancelButton.onClick.RemoveListener(CancelButtonClicked);
			CancelButton.onClick.AddListener(CancelButtonClicked);
			
			BackButton.onClick.RemoveListener(CancelButtonClicked);
			BackButton.onClick.AddListener(CancelButtonClicked);
		}

		private void CancelButtonClicked()
		{
			_system.ResetData();
			FeatureControl.OpenMainView();
		}

		private void ConfirmButtonClicked()
		{
			_system.ConfirmButtonClicked();
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
			if (optionId == _system.SelectedGameType)
			{
				return;
			}
			
			_system.SelectedGameType = optionId;
		}
	}
}
