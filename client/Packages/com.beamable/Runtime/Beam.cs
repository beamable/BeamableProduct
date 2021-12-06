// unset

using Beamable.Api;
using Beamable.Api.Analytics;
using Beamable.Api.Announcements;
using Beamable.Api.Auth;
using Beamable.Api.CloudSaving;
using Beamable.Api.Commerce;
using Beamable.Api.Connectivity;
using Beamable.Api.Events;
using Beamable.Api.Groups;
using Beamable.Api.Inventory;
using Beamable.Api.Mail;
using Beamable.Api.Notification;
using Beamable.Api.Payments;
using Beamable.Api.Sessions;
using Beamable.Api.Stats;
using Beamable.Api.Tournaments;
using Beamable.Common.Api;
using Beamable.Common.Api.Announcements;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.CloudData;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Api.Tournaments;
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Experimental.Api.Calendars;
using Beamable.Experimental.Api.Chat;
using Beamable.Experimental.Api.Matchmaking;
using Beamable.Experimental.Api.Sim;
using Beamable.Experimental.Api.Social;
using Beamable.Player;
using Beamable.Purchasing;
using Beamable.Service;
using Core.Platform.SDK;
using System.Collections.Generic;

namespace Beamable
{
	public static class Beam
	{
		public static IDependencyBuilder DependencyBuilder;
		static Beam()
		{
			// set the default promise error handlers
			PromiseExtensions.SetupDefaultHandler();

			// TODO: Use dependency cache to add an attribute where folks can add their own services. This will also allow us to support the BeamablePurchaser
			// register all services that are not context specific.
			DependencyBuilder = new DependencyBuilder()
			                    .AddComponentSingleton<CoroutineService>()
			                    .AddComponentSingleton<NotificationService>()
			                    .AddComponentSingleton<BeamableBehaviour>()
			                    .AddComponentSingleton<PubnubSubscriptionManager>(
				                    (manager, provider) => manager.Initialize(provider.GetService<IPlatformService>()))
			                    .AddSingleton<IBeamableRequester, PlatformRequester>(
				                    provider => provider.GetService<PlatformRequester>())
			                    .AddSingleton(ServiceManager.ResolveIfAvailable<IAuthSettings>())
			                    .AddSingleton(BeamableEnvironment.Data)
			                    .AddSingleton<IUserContext>(provider => provider.GetService<IPlatformService>())
			                    .AddSingleton<IConnectivityService, ConnectivityService>()
			                    .AddSingleton<IAuthService, AuthService>()
			                    .AddSingleton<IInventoryApi, InventoryService>()
			                    .AddSingleton<IAnnouncementsApi, AnnouncementsService>()
			                    .AddSingleton<ISessionService, SessionService>()
			                    .AddSingleton<CloudSavingService>()
			                    .AddSingleton<IBeamableFilesystemAccessor, PlatformFilesystemAccessor>()
			                    .AddSingleton<ContentService>()
			                    .AddSingleton<InventoryService>()
			                    .AddSingleton<StatsService>(provider =>
				                                                new StatsService(
					                                                provider.GetService<IPlatformService>(),
					                                                provider.GetService<PlatformRequester>(),
					                                                UnityUserDataCache<Dictionary<string, string>>
						                                                .CreateInstance))
			                    .AddSingleton<IAnalyticsTracker, AnalyticsTracker>(provider =>
				                    new AnalyticsTracker(provider.GetService<IPlatformService>(),
				                                         provider.GetService<PlatformRequester>(),
				                                         provider.GetService<CoroutineService>(), 30, 10)
			                    )
			                    .AddSingleton<MailService>()
			                    .AddSingleton<PushService>()
			                    .AddSingleton<CommerceService>()
			                    .AddSingleton<PaymentService>()
			                    .AddSingleton<GroupsService>()
			                    .AddSingleton<EventsService>()
			                    .AddSingleton<ITournamentApi, TournamentService>()
			                    .AddSingleton<ICloudDataApi, CloudDataApi>()
			                    .AddSingleton<ChatService>()
			                    .AddSingleton<GameRelayService>()
			                    .AddSingleton<MatchmakingService>()
			                    .AddSingleton<SocialService>()
			                    .AddSingleton<CalendarsService>()
			                    .AddSingleton<AnnouncementsService>()
			                    .AddSingleton<IHeartbeatService, Heartbeat>()
			                    .AddSingleton<ISdkEventService, SdkEventService>()
			                    .AddSingleton<PubnubNotificationService>()
			                    .AddSingleton<IPubnubNotificationService, PubnubNotificationService>()
			                    .AddSingleton<IPubnubSubscriptionManager>(
				                    provider => provider.GetService<PubnubSubscriptionManager>())
			                    .AddSingleton<INotificationService>(
				                    provider => provider.GetService<NotificationService>())
				;

			DependencyBuilder.AddSingleton<PlayerAnnouncements>();
			DependencyBuilder.AddSingleton<PlayerCurrencyGroup>();

		}
	}
}
