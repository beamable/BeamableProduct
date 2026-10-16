using Beamable.Api.Analytics.Batch;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;

namespace Beamable.Api.Analytics
{
	/// <summary>Registers one tracker singleton and aliases its interface across SDK assembly boundaries.</summary>
	public static class AnalyticsDependencyRegistration
	{
		public static void Register(IDependencyBuilder builder)
		{
			builder.AddSingleton<IAnalyticsBatchStorage, PlayerPrefsBatchStorage>();
			builder.AddSingleton<AnalyticsTracker>(provider => new AnalyticsTracker(
				provider.GetService<IBeamAnalyticsService>(), provider.GetService<CoroutineService>(),
				30, 10, provider.GetService<IAnalyticsBatchStorage>()));
			builder.AddSingleton<IAnalyticsTracker>(provider => provider.GetService<AnalyticsTracker>());
		}
	}
}
