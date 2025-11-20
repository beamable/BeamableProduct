// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

﻿using System.Collections.Generic;

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
		void SendAnalyticsEventBatch(List<AnalyticsEventRequest> eventBatch);

		/// <summary>
		/// Prepares request based on analytics event.
		/// </summary>
		/// <param name="analyticsEvent">Event to convert.</param>
		/// <returns>Event request ready to send.</returns>
		AnalyticsEventRequest BuildRequest(IAnalyticsEvent analyticsEvent);
	}
}
