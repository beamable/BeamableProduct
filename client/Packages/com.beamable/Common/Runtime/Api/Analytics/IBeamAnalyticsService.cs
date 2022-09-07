using System.Collections.Generic;

namespace Beamable.Api.Analytics
{
	/// <summary>
	/// Analytics service.
	/// This service provides an API to communicate with the Platform
	/// </summary>
	public interface IBeamAnalyticsService
	{
		/// <summary>
		/// Sends a single analytics event.
		/// </summary>
		/// <param name="eventRequest">Event request.</param>
		void SendAnalyticsEvent(AnalyticsEventRequest eventRequest);

		/// <summary>
		/// Sends the analytics event batch.
		/// This method also groups batches by gamertag, and issues a request for each
		/// </summary>
		/// <param name="eventBatch">Event batch.</param>
		public void SendAnalyticsEventBatch(List<AnalyticsEventRequest> eventBatch);
	}
}
