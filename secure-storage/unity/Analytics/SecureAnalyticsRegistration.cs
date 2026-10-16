#if UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || (!UNITY_EDITOR && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_IOS))
using Beamable.Api.Analytics.Batch;
using Beamable.Common.Dependencies;
using UnityEngine;

namespace Beamable.Security.Analytics
{
	/// <summary>Opt-in registration. Requires the accompanying SDK persistence patch.</summary>
	[BeamContextSystem]
	public static class SecureAnalyticsRegistration
	{
		[RegisterBeamableDependencies]
		public static void Register(IDependencyBuilder builder)
		{
			// Keep the SDK's single AnalyticsTracker and its IAnalyticsTracker alias.
			// Explicit factory: paths and primitive values are never resolved through DI.
			builder.ReplaceSingleton<IAnalyticsBatchStorage, SecureAnalyticsBatchStorage>(provider =>
				new SecureAnalyticsBatchStorage(Application.persistentDataPath, Application.identifier + ".analytics.v1"));
		}
	}
}
#endif
