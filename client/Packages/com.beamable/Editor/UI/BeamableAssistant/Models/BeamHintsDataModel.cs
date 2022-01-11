using Beamable.Common.Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Beamable.Editor.Assistant
{
	/// <summary>
	/// A serializable data model to store, across domain reloads, the current state of the <see cref="BeamableAssistantWindow"/>'s Hint display. 
	/// </summary>
	[Serializable]
	public class BeamHintsDataModel
	{
		private IBeamHintGlobalStorage _hintGlobalStorage;
		private IBeamHintPreferencesManager _hintPreferencesManager;

		/// <summary>
		/// Current list of all Hints that have their details expanded. 
		/// </summary>
		[SerializeField] public List<BeamHintHeader> DetailsOpenedHints;

		/// <summary>
		/// Current list of all selected domains in the domain tree.
		/// </summary>
		[SerializeField] public List<string> SelectedDomains;

		/// <summary>
		/// Current list of all hints being displayed.
		/// </summary>
		[SerializeField] public List<BeamHintHeader> DisplayingHints;

		/// <summary>
		/// Current list of all domains, sorted by their value, that can be found in the current <see cref="DisplayingHints"/>. 
		/// </summary>
		[SerializeField] public List<string> SortedDomainsInStorage;

		/// <summary>
		/// Current text filter applied to existing hints in <see cref="_hintGlobalStorage"/> to generate the <see cref="DisplayingHints"/>.
		/// </summary>
		[SerializeField] public string CurrentFilter;

		public BeamHintsDataModel()
		{
			DetailsOpenedHints = new List<BeamHintHeader>();
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

		/// <summary>
		/// Refreshes the <see cref="SortedDomainsInStorage"/> from a given list of hints.
		/// </summary>
		/// <param name="storage">Typically, this will be the <see cref="IBeamHintGlobalStorage.All"/>. But if we need we can regenerate these from any collection of <see cref="BeamHint"/>s.</param>
		public void RefreshDomainsFromHints(IEnumerable<BeamHint> storage)
		{
			// Gets all domains in the current storage
			SortedDomainsInStorage = storage.Select(hint => hint.Header.Domain).ToList();
			SortedDomainsInStorage.Sort();
		}

		/// <summary>
		/// Updates the current <see cref="DisplayingHints"/> based on the <see cref="SelectedDomains"/>, <see cref="CurrentFilter"/> and visibility preferences
		/// stored in <see cref="IBeamHintPreferencesManager"/> to the given <see cref="storage"/>.  
		/// </summary>
		public void RefreshDisplayingHints(IEnumerable<BeamHint> storage, List<string> domains)
		{
			var perDomainHints = storage.Where(hint => domains.Contains(hint.Header.Domain)).ToList();

			// Handle Display/Ignored hints based on stored preferences inside this editor.
			_hintPreferencesManager.RebuildPerHintPreferences();
			_hintPreferencesManager.SplitHintsByVisibilityPreferences(perDomainHints, out var toDisplayHints, out _);

			// Apply text based filter
			var filteredHints = toDisplayHints.Where(hint =>
			{
				var isEmptyFilter = string.IsNullOrEmpty(CurrentFilter);
				var matchId = !isEmptyFilter && hint.Header.Id.ToLower().Contains(CurrentFilter.ToLower());

				return matchId || isEmptyFilter;
			});

			// Display only hints that pass through the preferences filter.
			DisplayingHints = filteredHints.Select(hint => hint.Header).ToList();
		}

		/// <summary>
		/// Selects a group of domains and updates <see cref="DisplayingHints"/> accordingly.
		/// </summary>
		public void SelectDomains(List<string> domainsToSelect)
		{
			RefreshDomainsFromHints(_hintGlobalStorage);

			var selectedDomains = domainsToSelect.Count == 0 ? SortedDomainsInStorage : domainsToSelect;
			RefreshDisplayingHints(_hintGlobalStorage, selectedDomains);
			SelectedDomains = selectedDomains;
		}

		/// <summary>
		/// Gets the full <see cref="BeamHint"/> from a <see cref="BeamHintHeader"/>. 
		/// </summary>
		public BeamHint GetHint(BeamHintHeader header) => _hintGlobalStorage.GetHint(header);

		/// <summary>
		/// Updates <see cref="CurrentFilter"/> and <see cref="DisplayingHints"/> based on the given <paramref name="searchText"/>.  
		/// </summary>
		public void FilterDisplayedBy(string searchText)
		{
			CurrentFilter = searchText;
			RefreshDisplayingHints(_hintGlobalStorage, SelectedDomains);
		}

		/// <summary>
		/// Gets the <see cref="BeamHintNotificationPreference"/> for the given <paramref name="header"/>.
		/// </summary>
		public BeamHintNotificationPreference GetHintNotificationValue(BeamHintHeader header)
		{
			var hint = GetHint(header);
			return _hintPreferencesManager.GetHintNotificationPreferences(hint);
		}

		/// <summary>
		/// Updates the <see cref="IBeamHintPreferencesManager"/> with a new <see cref="BeamHintNotificationPreference"/> for the given header <paramref name="header"/>. 
		/// </summary>
		/// <param name="header">The hint whose notification preference we want to update.</param>
		/// <param name="evtNewValue">True will set it to <see cref="BeamHintNotificationPreference.NotifyAlways"/>. False to <see cref="BeamHintNotificationPreference.NotifyNever"/>.</param>
		public void SetHintNotificationValue(BeamHintHeader header, bool evtNewValue)
		{
			var hint = GetHint(header);
			var state = evtNewValue ? BeamHintNotificationPreference.NotifyAlways : BeamHintNotificationPreference.NotifyNever;
			_hintPreferencesManager.SetHintNotificationPreferences(hint, state);
		}

		/// <summary>
		/// Gets the <see cref="BeamHintPlayModeWarningPreference"/> for the given <paramref name="header"/>.
		/// </summary>
		public BeamHintPlayModeWarningPreference GetHintPlayModeWarningState(BeamHintHeader header)
		{
			var hint = GetHint(header);
			var playModeWarningState = _hintPreferencesManager.GetHintPlayModeWarningPreferences(hint);
			return playModeWarningState;
		}

		/// <summary>
		/// Updates the <see cref="IBeamHintPreferencesManager"/> with a new <see cref="BeamHintPlayModeWarningPreference"/> for the given header <paramref name="header"/>. 
		/// </summary>
		public void SetHintPreferencesValue(BeamHintHeader header, BeamHintPlayModeWarningPreference newPreference)
		{
			var hint = GetHint(header);
			_hintPreferencesManager.SetHintPlayModeWarningPreferences(hint, newPreference);
		}
	}

	/// <summary>
	/// Serializable Domain Tree view item that holds both the entire domain as well as its current substring (display name). 
	/// </summary>
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
