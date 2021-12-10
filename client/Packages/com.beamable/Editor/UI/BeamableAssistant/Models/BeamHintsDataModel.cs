using Beamable.Editor.BeamableAssistant.Components;
using Beamable.Editor.Content.Components;
using Common.Runtime.BeamHints;
using Editor.BeamableAssistant;
using Editor.BeamableAssistant.BeamHints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Experimental.UIElements;

namespace Beamable.Editor.BeamableAssistant.Models
{
	[System.Serializable]
	public class BeamHintsDataModel
	{
		private IBeamHintGlobalStorage _hintGlobalStorage;
		private IBeamHintPreferencesManager _hintPreferencesManager;
		
		[SerializeField] public List<BeamHintHeader> SelectedHints;
		[SerializeField] public List<string> SelectedDomains;
		[SerializeField] public List<BeamHintHeader> DisplayingHints;
		[SerializeField] public List<string> SortedDomainsInStorage;
		[SerializeField] public string CurrentFilter;

		public BeamHintsDataModel()
		{
			SelectedHints = new List<BeamHintHeader>();
			SelectedDomains = new List<string>();
			SortedDomainsInStorage = new List<string>();
			DisplayingHints = new List<BeamHintHeader>();
		}

		public void SetGlobalStorage(IBeamHintGlobalStorage beamHintGlobalStorage)
		{
			_hintGlobalStorage = beamHintGlobalStorage;
		}
		
		public void SetPreferencesManager(IBeamHintPreferencesManager beamHintPreferencesManager)
		{
			_hintPreferencesManager = beamHintPreferencesManager;
		}
		
		public void RefreshDomainsFromHints(IEnumerable<BeamHint> storage)
		{
			// Gets all domains in the current storage
			SortedDomainsInStorage = storage.Select(hint => hint.Header.Domain).ToList();
			SortedDomainsInStorage.Sort();
		}

		public void RefreshDisplayingHints(IEnumerable<BeamHint> storage, List<string> domains)
		{
			var perDomainHints = storage.Where(hint => domains.Contains(hint.Header.Domain)).ToList();
			
			// Handle Display/Ignored hints based on stored preferences inside this editor.
			_hintPreferencesManager.RebuildPerHintPreferences();
			_hintPreferencesManager.SplitHintsByVisibilityState(perDomainHints, out var toDisplayHints, out _);
			
			// Apply text based filter
			var filteredHints = toDisplayHints.Where(hint => {

				var isEmptyFilter = string.IsNullOrEmpty(CurrentFilter);
				var matchId = !isEmptyFilter && hint.Header.Id.ToLower().Contains(CurrentFilter.ToLower());

				return matchId || isEmptyFilter;
			});
			
			// Display only hints that pass through the preferences filter.
			DisplayingHints = filteredHints.Select(hint=> hint.Header).ToList();
		}

		public void SelectDomains(List<string> domainsToSelect)
		{
			RefreshDomainsFromHints(_hintGlobalStorage);
			
			var selectedDomains = domainsToSelect.Count == 0 ? SortedDomainsInStorage : domainsToSelect;
			RefreshDisplayingHints(_hintGlobalStorage, selectedDomains);
			SelectedDomains = selectedDomains;
		}
		
		public BeamHint GetHint(BeamHintHeader header)
		{
			return _hintGlobalStorage.All.First(hint => hint.Header.Equals(header));
		}

		public void FilterDisplayedBy(string searchText)
		{
			CurrentFilter = searchText;
			RefreshDisplayingHints(_hintGlobalStorage, SelectedDomains);
		}
	}
	
	

	[Serializable]
	public sealed class BeamHintDomainTreeViewItem : TreeViewItem
	{
		public readonly string FullDomain;
		
		public BeamHintDomainTreeViewItem(int id, int depth, string fullDomain, string displayName) : base(id, depth, displayName)
		{
			FullDomain = fullDomain;
		}
	}
}
