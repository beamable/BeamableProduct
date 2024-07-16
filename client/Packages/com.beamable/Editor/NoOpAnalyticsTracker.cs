using Beamable.Api.Analytics;

namespace Beamable.Editor
{
	public class NoOpAnalyticsTracker : IAnalyticsTracker
	{
		public void TrackEvent(IAnalyticsEvent analyticsEvent, bool sendImmediately)
		{
			// no-op
		}

		public void UpdateBatchSettings(BatchSettings batchSettings, bool flush = true)
		{
			// no-op
		}

		public void RevertBatchSettings(bool flush = true)
		{
			// no-op
		}
	}
}
