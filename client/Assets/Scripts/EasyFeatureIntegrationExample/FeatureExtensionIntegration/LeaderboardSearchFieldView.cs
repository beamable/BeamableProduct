using Beamable;
using Beamable.EasyFeatures;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace EasyFeaturesIntegrationExamples.FeatureExtensionIntegration
{
	public class LeaderboardSearchFieldView : MonoBehaviour, ISyncBeamableView
	{
		public BeamableViewGroup OwnerGroup;
		public TMP_InputField Filter;

		public int GetEnrichOrder() => int.MaxValue;

		public void EnrichWithContext(BeamContextGroup managedPlayers)
		{
			OwnerGroup = managedPlayers.Owner;
			
			var currentContext = managedPlayers.GetSinglePlayerContext();
			var searchableLeaderboard = currentContext.ServiceProvider
			                                          .GetService<SearchableLeaderboardPlayerSystem>();
			Filter.SetTextWithoutNotify(searchableLeaderboard.CurrentAliasFilter);
			
			// Setup listener
			Filter.onEndEdit.ReplaceOrAddListener(HandleFilterChanged);
		}

		public async void HandleFilterChanged(string newFilter)
		{
			var currentContext = OwnerGroup.AllPlayerContexts[0];
			var searchableLeaderboard = currentContext.ServiceProvider
			                                          .GetService<SearchableLeaderboardPlayerSystem>();
			searchableLeaderboard.CurrentAliasFilter = newFilter;
			await OwnerGroup.Enrich();
		}
	}
}
