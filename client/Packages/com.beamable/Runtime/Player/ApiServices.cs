// unset

using Beamable;
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
using Beamable.Api.Leaderboard;
using Beamable.Api.Mail;
using Beamable.Api.Notification;
using Beamable.Api.Payments;
using Beamable.Api.Sessions;
using Beamable.Api.Stats;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.CloudData;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Api.Tournaments;
using Beamable.Common.Player;
using Beamable.Content;
using Beamable.Experimental;
using Beamable.Experimental.Api.Calendars;
using Beamable.Experimental.Api.Chat;
using Beamable.Experimental.Api.Matchmaking;
using Beamable.Experimental.Api.Sim;
using Beamable.Experimental.Api.Social;
using System;
using System.Collections.Generic;

namespace Beamable.Player
{
	public class ApiServices : IBeamableAPI
	{

		public class ExperimentalApiServices : IExperimentalAPI
		{
			private readonly BeamContext _ctx;

			public ExperimentalApiServices(BeamContext ctx)
			{
				_ctx = ctx;
			}
			public ChatService ChatService => _ctx.ServiceProvider.GetService<ChatService>();
			public GameRelayService GameRelayService  => _ctx.ServiceProvider.GetService<GameRelayService>();
			public MatchmakingService MatchmakingService  => _ctx.ServiceProvider.GetService<MatchmakingService>();
			public SocialService SocialService  => _ctx.ServiceProvider.GetService<SocialService>();
			public CalendarsService CalendarService => _ctx.ServiceProvider.GetService<CalendarsService>();
		}

		private readonly BeamContext _ctx;
		public User User => _ctx.AuthorizedUser;
		public AccessToken Token => _ctx.AccessToken;

#pragma warning disable CS0067
		public event Action<User> OnUserChanged;
		public event Action<User> OnUserLoggingOut;
#pragma warning restore CS0067


		private ExperimentalApiServices _experimentalApiServices;
		public IExperimentalAPI Experimental => _experimentalApiServices;
		public AnnouncementsService AnnouncementService => _ctx.ServiceProvider.GetService<AnnouncementsService>();
		public IAuthService AuthService => _ctx.ServiceProvider.GetService<IAuthService>();

		public CloudSavingService CloudSavingService => _ctx.ServiceProvider.GetService<CloudSavingService>();
		public ContentService ContentService=> _ctx.ServiceProvider.GetService<ContentService>();
		public InventoryService InventoryService => _ctx.ServiceProvider.GetService<InventoryService>();
		public LeaderboardService LeaderboardService => _ctx.ServiceProvider.GetService<LeaderboardService>();
		public IBeamableRequester Requester => _ctx.ServiceProvider.GetService<IBeamableRequester>();
		public StatsService StatsService=> _ctx.ServiceProvider.GetService<StatsService>();
		public StatsService Stats => _ctx.ServiceProvider.GetService<StatsService>();
		public SessionService SessionService => _ctx.ServiceProvider.GetService<SessionService>();
		public IAnalyticsTracker AnalyticsTracker => _ctx.ServiceProvider.GetService<IAnalyticsTracker>();
		public MailService MailService => _ctx.ServiceProvider.GetService<MailService>();
		public PushService PushService => _ctx.ServiceProvider.GetService<PushService>();
		public CommerceService CommerceService => _ctx.ServiceProvider.GetService<CommerceService>();
		public PaymentService PaymentService => _ctx.ServiceProvider.GetService<PaymentService>();
		public GroupsService GroupsService => _ctx.ServiceProvider.GetService<GroupsService>();
		public EventsService EventsService => _ctx.ServiceProvider.GetService<EventsService>();
		public Promise<IBeamablePurchaser> BeamableIAP => null;
		public IConnectivityService ConnectivityService => _ctx.ServiceProvider.GetService<IConnectivityService>();
		public INotificationService NotificationService => _ctx.ServiceProvider.GetService<INotificationService>();
		public ITournamentApi TournamentsService => _ctx.ServiceProvider.GetService<ITournamentApi>();
		public ICloudDataApi TrialDataService => _ctx.ServiceProvider.GetService<ICloudDataApi>();
		public ITournamentApi Tournaments => _ctx.ServiceProvider.GetService<ITournamentApi>();
		public ISdkEventService SdkEventService => _ctx.ServiceProvider.GetService<ISdkEventService>();

		public void UpdateUserData(User user)
		{
			throw new NotImplementedException();
		}

		public Promise<ISet<UserBundle>> GetDeviceUsers()
		{
			throw new NotImplementedException();
		}

		public void RemoveDeviceUser(TokenResponse token)
		{
			throw new NotImplementedException();
		}

		public Promise<Unit> ApplyToken(TokenResponse response)
		{
			throw new NotImplementedException();
		}

		public ApiServices(BeamContext ctx)
		{
			_ctx = ctx;
			_experimentalApiServices = new ExperimentalApiServices(ctx);

			// TODO: Implement missing methods and events
		}
	}
}
