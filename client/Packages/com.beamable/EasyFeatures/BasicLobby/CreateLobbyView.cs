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
		[SerializeField] private int _enrichOrder;
		[SerializeField] private BeamableViewGroup _viewGroup;
		[SerializeField] private LobbyFeatureControl _featureControl;
		
		[Header("Components")]
		[SerializeField] private MultiToggleComponent _typesToggle;
		[SerializeField] private MultiToggleComponent _accessToggle;
		[SerializeField] private TMP_InputField _name;
		[SerializeField] private TMP_InputField _description;
		[SerializeField] private Button _confirmButton;
		[SerializeField] private Button _cancelButton;
		[SerializeField] private Button _backButton;

		private IDependencies _system;
		
		public int GetEnrichOrder() => _enrichOrder;

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
			_typesToggle.Setup(_system.GameTypes.Select(type => type.ContentName).ToList(), OnGameTypeSelected, _system.SelectedGameType);
			_accessToggle.Setup(_system.AccessOptions.Select(pair => pair.Key).ToList(), OnAccessOptionSelected, _system.SelectedAccessOption);
			
			_name.SetTextWithoutNotify(_system.Name);
			_description.SetTextWithoutNotify(_system.Description);
			
			_name.onValueChanged.RemoveListener(OnNameChanged);
			_name.onValueChanged.AddListener(OnNameChanged);
			
			_description.onValueChanged.RemoveListener(OnDescriptionChanged);
			_description.onValueChanged.AddListener(OnDescriptionChanged);
			
			_confirmButton.onClick.RemoveListener(ConfirmButtonClicked);
			_confirmButton.onClick.AddListener(ConfirmButtonClicked);
			_confirmButton.interactable = _system.ValidateConfirmButton();
			
			_cancelButton.onClick.RemoveListener(CancelButtonClicked);
			_cancelButton.onClick.AddListener(CancelButtonClicked);
			
			_backButton.onClick.RemoveListener(CancelButtonClicked);
			_backButton.onClick.AddListener(CancelButtonClicked);
		}

		private void CancelButtonClicked()
		{
			_system.ResetData();
			_featureControl.OpenMainView();
		}

		private void ConfirmButtonClicked()
		{
			_system.ConfirmButtonClicked();
		}

		private void OnNameChanged(string value)
		{
			_system.Name = value;
			_confirmButton.interactable = _system.ValidateConfirmButton();
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
