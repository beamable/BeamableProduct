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
			int CurrentlySelectedGameType { get; set; }
			string CurrentFilter { get; }
			List<SimGameType> GameTypes { get; }
			List<LobbiesListEntryPresenter.Data> LobbiesData { get; }
			Promise<List<LobbiesListEntryPresenter.Data>> FetchData();
			void ApplyFilter(string filter);
			Promise ConfigureData();
		}
		
		[Header("View Configuration")]
		[SerializeField] private int _enrichOrder;
		[SerializeField] private BeamableViewGroup _viewGroup;
		
		[Header("Components")]
		[SerializeField] private MultiToggleComponent _typesToggle;
		[SerializeField] private GameObjectToggler _loadingIndicator;
		[SerializeField] private GameObject _noLobbiesIndicator;
		[SerializeField] private LobbiesListPresenter _lobbiesList;
		[SerializeField] private TMP_InputField _filterField;
		[SerializeField] private Button _clearFilterButton;

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
			
			// Setting up all relevant components
			_typesToggle.Setup(_system.GameTypes.Select(type => type.ContentName).ToList(), OnOptionSelected, _system.CurrentlySelectedGameType);
			
			_filterField.onEndEdit.RemoveListener(OnFilterApplied);
			_filterField.onEndEdit.AddListener(OnFilterApplied);
			
			_clearFilterButton.onClick.RemoveListener(ClearButtonClicked);
			_clearFilterButton.onClick.AddListener(ClearButtonClicked);
			
			_filterField.SetTextWithoutNotify(_system.CurrentFilter);
			
			_lobbiesList.ClearPooledRankedEntries();
			_lobbiesList.Setup(_system.LobbiesData);
			_lobbiesList.RebuildPooledLobbiesEntries();
			
			_noLobbiesIndicator.SetActive(_system.LobbiesData.Count == 0);
		}

		private async void ClearButtonClicked()
		{
			_system.ApplyFilter(String.Empty);
			await _viewGroup.Enrich();
		}

		private async void OnFilterApplied(string filter)
		{
			_system.ApplyFilter(filter);
			await _viewGroup.Enrich();
		}

		private async void OnOptionSelected(int optionId)
		{
			if (optionId == _system.CurrentlySelectedGameType)
			{
				return;
			}
			
			_system.CurrentlySelectedGameType = optionId;
			
			_noLobbiesIndicator.SetActive(false);
			
			_loadingIndicator.Toggle(true);
			await _system.ConfigureData();
			_loadingIndicator.Toggle(false);
			
			await _viewGroup.Enrich();
		}
	}
}
