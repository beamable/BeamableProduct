using Beamable.Common.Scheduler;
using Beamable.Server.Api;
using Beamable.Server.Api.Announcements;
using Beamable.Server.Api.Calendars;
using Beamable.Server.Api.Chat;
using Beamable.Server.Api.Content;
using Beamable.Server.Api.Events;
using Beamable.Server.Api.Groups;
using Beamable.Server.Api.Inventory;
using Beamable.Server.Api.Leaderboards;
using Beamable.Server.Api.Mail;
using Beamable.Server.Api.Social;
using Beamable.Server.Api.Stats;
using Beamable.Server.Api.Tournament;
using Beamable.Server.Api.CloudData;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Api.Commerce;
using Beamable.Server.Api.Notifications;
using Beamable.Server.Api.Payments;
using Beamable.Server.Api.Push;

namespace Beamable.Server
{
   public class BeamableServices : IBeamableServices
   {
      public IMicroserviceAuthApi Auth { get; set; }
      public IMicroserviceStatsApi Stats { get; set; }
      public IMicroserviceContentApi Content { get; set; }
      public IMicroserviceInventoryApi Inventory { get; set; }
      public IMicroserviceLeaderboardsApi Leaderboards { get; set; }
      public IMicroserviceAnnouncementsApi Announcements { get; set; }
      public IMicroserviceCalendarsApi Calendars { get; set; }
      public IMicroserviceEventsApi Events { get; set; }
      public IMicroserviceGroupsApi Groups { get; set; }
      public IMicroserviceMailApi Mail { get; set; }
      public IMicroserviceNotificationsApi Notifications { get; set; }
      public IMicroserviceSocialApi Social { get; set; }
      public IMicroserviceTournamentApi Tournament { get; set; }
      public IMicroserviceCloudDataApi TrialData { get; set; }
      public IMicroserviceRealmConfigService RealmConfig { get; set; }
      public IMicroserviceCommerceApi Commerce { get; set; }
      public IMicroserviceChatApi Chat { get; set; }
      public IMicroservicePaymentsApi Payments { get; set; }
      public BeamScheduler Scheduler { get; set; }
      public IMicroservicePushApi Push { get; set; }
   }
}
