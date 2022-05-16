#define ENABLE_FEATURE_EXTENSION_SAMPLE
using Beamable.Api.Leaderboard;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.EasyFeatures.BasicLeaderboard;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyFeaturesIntegrationExamples.FeatureExtensionIntegration
{

	[BeamContextSystem]
	public class FeatureExtensionIntegrationBoot : MonoBehaviour
	{

#if ENABLE_FEATURE_EXTENSION_SAMPLE
		[RegisterBeamableDependencies(Order = int.MaxValue)]
		public static void SetupBeamableDependencies(IDependencyBuilder builder)
		{
			builder.SetupUnderlyingSystemSingleton<SearchableLeaderboardPlayerSystem,
				BasicLeaderboardPlayerSystem,
				BasicLeaderboardView.ILeaderboardDeps>();
		}
#endif
	}


	public class SearchableLeaderboardPlayerSystem : BasicLeaderboardPlayerSystem
	{
		public string CurrentAliasFilter = "";
		
		public SearchableLeaderboardPlayerSystem(LeaderboardService leaderboardService, IUserContext ctx) : 
			base(leaderboardService, ctx) { }

		public override IEnumerable<BasicLeaderboardView.BasicLeaderboardViewEntry> Entries => 
			base.Entries.Where(e => string.IsNullOrEmpty(CurrentAliasFilter) || e.Alias.Contains(CurrentAliasFilter));
	}
}
