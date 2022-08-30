using Beamable.Api.Open.Accounts;
using Beamable.Api.Open.Announcements;
using Beamable.Api.Open.Auth;
using Beamable.Api.Open.Beamo;
using Beamable.Api.Open.Calendars;
using Beamable.Api.Open.Chatv2;
using Beamable.Api.Open.Cloudsaving;
using Beamable.Api.Open.Commerce;
using Beamable.Api.Open.EventPlayers;
using Beamable.Api.Open.Events;
using Beamable.Api.Open.Groups;
using Beamable.Api.Open.GroupUsers;
using Beamable.Api.Open.Inventory;
using Beamable.Api.Open.Leaderboards;
using Beamable.Api.Open.Mail;
using Beamable.Api.Open.Matchmaking;
using Beamable.Api.Open.Notification;
using Beamable.Api.Open.Payments;
using Beamable.Api.Open.Push;
using Beamable.Api.Open.Realms;
using Beamable.Api.Open.Social;
using Beamable.Api.Open.Stats;
using Beamable.Api.Open.Tournaments;
using Beamable.Common;
using Beamable.Common.Dependencies;

namespace Beamable.Api.Open
{
	[BeamContextSystem]
	public class OpenApiSystem
	{
		[RegisterBeamableDependencies(Constants.SYSTEM_DEPENDENCY_ORDER)]
		public static void RegisterOpenApis(IDependencyBuilder builder)
		{
			builder.AddSingleton<IAccountsApiBasicApi, AccountsApiBasicApi>();
			builder.AddSingleton<IAccountsApiObjectApi, AccountsApiObjectApi>();
			builder.AddSingleton<IAnnouncementsApiBasicApi, AnnouncementsApiBasicApi>();
			builder.AddSingleton<IAnnouncementsApiObjectApi, AnnouncementsApiObjectApi>();
			builder.AddSingleton<IAuthApiBasicApi, AuthApiBasicApi>();
			builder.AddSingleton<IBeamoApiBasicApi, BeamoApiBasicApi>();
			builder.AddSingleton<ICalendarsApiObjectApi, CalendarsApiObjectApi>();
			builder.AddSingleton<IChatv2ApiObjectApi, Chatv2ApiObjectApi>();
			builder.AddSingleton<ICloudsavingApiBasicApi, CloudsavingApiBasicApi>();
			builder.AddSingleton<ICommerceApiBasicApi, CommerceApiBasicApi>();
			builder.AddSingleton<ICommerceApiObjectApi, CommerceApiObjectApi>();
			builder.AddSingleton<IEventPlayersApiObjectApi, EventPlayersApiObjectApi>();
			builder.AddSingleton<IEventsApiBasicApi, EventsApiBasicApi>();
			builder.AddSingleton<IEventsApiObjectApi, EventsApiObjectApi>();
			builder.AddSingleton<IGroupsApiObjectApi, GroupsApiObjectApi>();
			builder.AddSingleton<IGroupUsersApiObjectApi, GroupUsersApiObjectApi>();
			builder.AddSingleton<IInventoryApiBasicApi, InventoryApiBasicApi>();
			builder.AddSingleton<IInventoryApiObjectApi, InventoryApiObjectApi>();
			builder.AddSingleton<ILeaderboardsApiBasicApi, LeaderboardsApiBasicApi>();
			builder.AddSingleton<ILeaderboardsApiObjectApi, LeaderboardsApiObjectApi>();
			builder.AddSingleton<IMailApiBasicApi, MailApiBasicApi>();
			builder.AddSingleton<IMailApiObjectApi, MailApiObjectApi>();
			builder.AddSingleton<IMatchmakingApiObjectApi, MatchmakingApiObjectApi>();
			builder.AddSingleton<INotificationApiBasicApi, NotificationApiBasicApi>();
			builder.AddSingleton<IPaymentsApiBasicApi, PaymentsApiBasicApi>();
			builder.AddSingleton<IPaymentsApiObjectApi, PaymentsApiObjectApi>();
			builder.AddSingleton<IPushApiBasicApi, PushApiBasicApi>();
			builder.AddSingleton<IRealmsApiBasicApi, RealmsApiBasicApi>();
			builder.AddSingleton<ISocialApiBasicApi, SocialApiBasicApi>();
			builder.AddSingleton<IStatsApiBasicApi, StatsApiBasicApi>();
			builder.AddSingleton<IStatsApiObjectApi, StatsApiObjectApi>();
			builder.AddSingleton<ITournamentsApiBasicApi, TournamentsApiBasicApi>();
			builder.AddSingleton<ITournamentsApiObjectApi, TournamentsApiObjectApi>();
		}
	}
}
