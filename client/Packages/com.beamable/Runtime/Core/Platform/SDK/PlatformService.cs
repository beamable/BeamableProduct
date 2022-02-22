using Beamable.Api.Connectivity;
using Beamable.Api.Sessions;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Notifications;
using Beamable.Coroutines;
using System;

namespace Beamable.Api
{
	public interface IPlatformService : IUserContext
	{
		// XXX: This is a small subset of the PlatformService, only pulled as needed for testing purposes.

		event Action OnShutdown;

		event Action OnReloadUser;

		event Action TimeOverrideChanged;

		User User { get; }
		Promise<Unit> OnReady { get; }
		INotificationService Notification { get; }
		IPubnubNotificationService PubnubNotificationService { get; }
		IHeartbeatService Heartbeat { get; }
		string Cid { get; }
		string Pid { get; }

		string TimeOverride { get; set; }

		IConnectivityService ConnectivityService { get; }
		CoroutineService CoroutineService { get; }
	}
}
