using Beamable.Api.Analytics.Batch;
using Beamable.Common.Dependencies;
using Beamable.Common.Spew;
using Beamable.Coroutines;
using System.Collections.Generic;

namespace Beamable.Api.Analytics
{

	/// <summary>
	/// This type defines the %Client main entry point for the %Analytics feature.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-4.0/unity/user-reference/beamable-services/bi/analytics-overview/">Analytics</a> feature documentation
	/// - See Beamable.API script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class SecureAnalyticsTracker : AnalyticsTracker
	{
		/// <summary>
		/// The storage key used to persist batches to disk.
		/// </summary>
		private const string _storageKey = "analytics_event_batch";

		/// <summary>
		/// The default batch capacity threshold for expiration.
		/// </summary>
		private const int _defaultBatchCapacity = 100;

		/// <summary>
		/// The default batch timeout in seconds for expiration.
		/// </summary>
		private const double _defaultBatchTimeout = 60f;

		private List<AnalyticsEventRequest> _earlyRequests = new List<AnalyticsEventRequest>();
		private IBeamAnalyticsService _analyticsService;

		/// <summary>
		/// The Batch Manager responsible for managing analytics batches.
		/// </summary>
		private SecurePersistentBatchManager<AnalyticsEventRequest> _secureBatchManager;

		private BatchSettings _defaultBatchSettings;
		private bool usingDefaultSettings = true;

		/// <summary>
		/// Initialize the specified platformServer, customerId, projectId, batchTimeoutSeconds and batchMaxSize.
		/// Analytics Events can be tracked before initialization, but will not be sent until initialization occurs.
		/// </summary>
		/// <param name="batchTimeoutSeconds">Batch timeout seconds before expiration.</param>
		/// <param name="batchMaxSize">Batch max size before expiration.</param>
		public SecureAnalyticsTracker(IBeamAnalyticsService analyticsService, CoroutineService coroutineService, int batchTimeoutSeconds, int batchMaxSize) : base(analyticsService, coroutineService, batchTimeoutSeconds, batchMaxSize)
		{
			_secureBatchManager = new SecurePersistentBatchManager<AnalyticsEventRequest>(coroutineService, _storageKey, _defaultBatchCapacity, _defaultBatchTimeout);

			_analyticsService = analyticsService;

			// Set up default settings
			_defaultBatchSettings.HeartbeatInterval = 1f;
			_defaultBatchSettings.TimeoutSeconds = batchTimeoutSeconds;
			_defaultBatchSettings.MaxSize = batchMaxSize;

			_secureBatchManager.OnBatchExpired += OnBatchExpired;
			if (usingDefaultSettings)
				UpdateBatchSettings(_defaultBatchSettings);

			// Add Saved events into batch now that we have a proper player id to assign them to.
			AnalyticsLogger.LogFormat("Backfilling early requests: {0} total", _earlyRequests.Count);
			for (var i = 0; i < _earlyRequests.Count; i++)
			{
				var request = _earlyRequests[i];
				_secureBatchManager.Add(request);
			}
			_earlyRequests.Clear();


			_secureBatchManager.Flush();
			_secureBatchManager.Start();
		}

		public new void TrackEvent(IAnalyticsEvent analyticsEvent, bool sendImmediately = false)
		{
			var analyticsEventRequest = _analyticsService.BuildRequest(analyticsEvent);

			if (sendImmediately)
			{
				AnalyticsLogger.LogFormat("AnalyticsTracker.TrackEvent: Immediate payload={0}", analyticsEventRequest.Payload);
				_analyticsService.SendAnalyticsEvent(analyticsEventRequest);
			}
			else
			{
				AnalyticsLogger.LogFormat("AnalyticsTracker.TrackEvent: Batching payload={0}", analyticsEventRequest.Payload);
				_secureBatchManager.Add(analyticsEventRequest);
			}
		}

		public new void UpdateBatchSettings(BatchSettings batchSettings, bool flush = true)
		{
			UpdateBatchSettingsInternal(batchSettings, flush, false);
		}

		private void UpdateBatchSettingsInternal(BatchSettings batchSettings, bool flush, bool isDefault)
		{
			usingDefaultSettings = isDefault;
			_secureBatchManager.SetCapacity(batchSettings.MaxSize);
			_secureBatchManager.SetTimeoutSeconds(batchSettings.TimeoutSeconds);
			_secureBatchManager.SetHeartbeat(batchSettings.HeartbeatInterval);

			if (flush)
			{
				_secureBatchManager.Flush();
			}
		}

		public new void RevertBatchSettings(bool flush = true)
		{
			UpdateBatchSettingsInternal(_defaultBatchSettings, flush, true);
		}

		/// <summary>
		/// Raises the batch expired event.
		/// </summary>
		/// <param name="batchList">Batch list.</param>
		private void OnBatchExpired(List<AnalyticsEventRequest> batchList)
		{
			AnalyticsLogger.LogFormat("AnalyticsTracker.OnBatchExpired: Sending {0} events.", batchList.Count);
			_analyticsService.SendAnalyticsEventBatch(batchList);
		}
	}
	[BeamContextSystem]
	public class DependencyRegistration
	{
		[RegisterBeamableDependencies]
		public static void Register(IDependencyBuilder builder)
		{
			builder.ReplaceSingleton<IAnalyticsTracker, SecureAnalyticsTracker>();
			builder.ReplaceSingleton<AnalyticsTracker, SecureAnalyticsTracker>(provider => new SecureAnalyticsTracker(
					                                                                   provider.GetService<IBeamAnalyticsService>(),
					                                                                   provider.GetService<CoroutineService>(), 30, 10));
		}
	}
}
