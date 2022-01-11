using System;
using System.Collections;
using System.Collections.Generic;
using Beamable.AccountManagement;
using Beamable.Api;
using Beamable.Api.Analytics;
using Beamable.Api.Announcements;
using Beamable.Api.Auth;
using Beamable.Api.Caches;
using Beamable.Common;
using Beamable.Content;
using Beamable.Config;
using Beamable.Coroutines;
using Beamable.Api.Commerce;
using Beamable.Api.Inventory;
using Beamable.Api.Leaderboard;
using Beamable.Api.Payments;
using Beamable.Api.Stats;
using Beamable.Api.CloudSaving;
using Beamable.Service;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Beamable.Api.Connectivity;
using Beamable.Api.Events;
using Beamable.Api.Groups;
using Beamable.Api.Mail;
using Beamable.Api.Notification;
using Beamable.Api.Sessions;
using Beamable.Common.Api;
using Beamable.Common.Api.CloudData;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Api.Tournaments;
using Beamable.Common.Player;
using Beamable.Experimental;
using Beamable.Player;
using Beamable.Sessions;
#if BEAMABLE_PURCHASING
using Beamable.Purchasing;
#endif

namespace Beamable
{
    /// <summary>
    /// This type defines the %Client main entry point for the main %Beamable features.
    ///
    /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
    ///
    /// #### Related Links
    /// - See Beamable.API script reference
    ///
    /// ![img beamable-logo]
    ///
    /// </summary>
    public interface IBeamableAPI
    {
        User User { get; }
        AccessToken Token { get; }

        event Action<User> OnUserChanged;
        event Action<User> OnUserLoggingOut;


        IExperimentalAPI Experimental { get; }
        AnnouncementsService AnnouncementService { get; }
        IAuthService AuthService { get; }
        CloudSavingService CloudSavingService { get; }
        ContentService ContentService { get; }
        InventoryService InventoryService { get; }
        LeaderboardService LeaderboardService { get; }
        IBeamableRequester Requester { get; }
        StatsService StatsService { get; }

        [Obsolete("Use " + nameof(StatsService) + " instead.")]
        StatsService Stats { get; }

        SessionService SessionService { get; }
        IAnalyticsTracker AnalyticsTracker { get; }
        MailService MailService { get; }
        PushService PushService { get; }
        CommerceService CommerceService { get; }
        PaymentService PaymentService { get; }
        GroupsService GroupsService { get; }
        EventsService EventsService { get; }
        Promise<IBeamablePurchaser> BeamableIAP { get; }
        IConnectivityService ConnectivityService { get; }
        INotificationService NotificationService { get; }
        ITournamentApi TournamentsService { get; }
        ICloudDataApi TrialDataService { get; }

        [Obsolete("Use " + nameof(TournamentsService) + " instead.")]
        ITournamentApi Tournaments { get; }

        ISdkEventService SdkEventService { get; }

        void UpdateUserData(User user);
        Promise<ISet<UserBundle>> GetDeviceUsers();
        void RemoveDeviceUser(TokenResponse token);
        void ClearDeviceUsers();
        Promise<Unit> ApplyToken(TokenResponse response);
    }

    /// <summary>
    /// This type defines the %Client main entry point for %Beamable %Client features.
    ///
    /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
    ///
    /// #### Related Links
    /// - %AnalyticsTracker - See Beamable.Api.Analytics.IAnalyticsTracker and <a target="_blank" href="https://docs.beamable.com/docs/analytics-feature">Analytics</a> feature
    /// - %AnnouncementsService - See Beamable.Api.Announcements.AnnouncementsService and <a target="_blank" href="https://docs.beamable.com/docs/announcements-feature">Announcements</a> feature
    /// - %AuthService - See Beamable.Api.Auth.IAuthService and <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature
    /// - %CalendarsService - See Beamable.Experimental.Api.Chat.ChatService and <a target="_blank" href="https://docs.beamable.com/docs/chat-feature">Chat</a> feature
    /// - %ChatService - See Beamable.Experimental.Api.Calendars.CalendarsService and <a target="_blank" href="https://docs.beamable.com/docs/calendars-feature">Calendars</a> feature
    /// - %ConnectivityService - See Beamable.Api.Connectivity.IConnectivityService and <a target="_blank" href="https://docs.beamable.com/docs/connectivity-feature">Connectivity</a> feature
    /// - %CommerceService - See Beamable.Api.Commerce.CommerceService and <a target="_blank" href="https://docs.beamable.com/docs/store-feature">Store</a> feature
    /// - %CloudSavingService - See Beamable.Common.Api.CloudData.ICloudDataApi and <a target="_blank" href="https://docs.beamable.com/docs/cloud-save-feature">Cloud Save</a> feature
    /// - %ContentService - See Beamable.Content.ContentServiceand <a target="_blank" href="https://docs.beamable.com/docs/content-feature">Content</a> feature
    /// - %EventsService - See Beamable.Api.Events.EventsService and <a target="_blank" href="https://docs.beamable.com/docs/events-feature">Events</a> feature
    /// - %GroupsService - See Beamable.Api.Groups.GroupsService and <a target="_blank" href="https://docs.beamable.com/docs/groups-feature">Groups</a> feature
    /// - %InventoryService - See Beamable.Api.Inventory.InventoryService and <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature
    /// - %LeaderboardsService - See Beamable.Api.Leaderboard.LeaderboardService and <a target="_blank" href="https://docs.beamable.com/docs/leaderboards-feature">Leaderboards</a> feature
    /// - %MailService - See Beamable.Api.Mail.MailService and <a target="_blank" href="https://docs.beamable.com/docs/mail-feature">Mail</a> feature
    /// - %Multiplayer - See Beamable.Experimental.Api.Matchmaking.MatchmakingService  and <a target="_blank" href="https://docs.beamable.com/docs/multiplayer-feature">Multiplayer</a> feature
    /// - %Multiplayer - See Beamable.Experimental.Api.Sim.SimClient and <a target="_blank" href="https://docs.beamable.com/docs/multiplayer-feature">Multiplayer</a> feature
    /// - %Notifications - See Beamable.Api.Notification.NotificationService and <a target="_blank" href="https://docs.beamable.com/docs/friends-feature">Friends</a> feature
    /// - %PaymentService - See Beamable.Api.Payments.PaymentService and <a target="_blank" href="https://docs.beamable.com/docs/store-feature">Store</a> feature
    /// - %SocialService - See Beamable.Experimental.Api.Social.SocialService and <a target="_blank" href="https://docs.beamable.com/docs/friends-feature">Friends</a> feature
    /// - %StatsService - See Beamable.Api.Stats.StatsService  and <a target="_blank" href="https://docs.beamable.com/docs/stats-feature">Stats</a> feature
    /// - %TournamentsService - See Beamable.Common.Api.Tournaments.ITournamentApi and <a target="_blank" href="https://docs.beamable.com/docs/tournaments-feature">Tournaments</a> feature
    /// - %TrialDataService - See Beamable.Common.Api.CloudData.ICloudDataApi  and <a target="_blank" href="https://docs.beamable.com/docs/cloud-save-feature">Cloud Save</a> feature
    ///
    /// ### Example
    /// This demonstrates example usage.
    ///
    /// ```
    ///
    /// private async void MyClientMethod()
    /// {
    ///
    ///   var beamableAPI = await Beamable.API.Instance;
    ///
    ///   // Example usage
    ///   var announcementsService = beamableAPI.AnnouncementsService;
    ///   var result = await announcementsService.GetCurrent();
    ///
    ///   // Others...
    ///   var analyticsTracker = beamableAPI.AnalyticsTracker;
    ///   var authService = beamableAPI.AuthService;
    ///   var cloudSavingService = beamableAPI.CloudSavingService;
    ///   var commerceService = beamableAPI.CommerceService;
    ///   var connectivityService = beamableAPI.ConnectivityService;
    ///   var contentService = beamableAPI.ContentService ;
    ///   var eventsService = beamableAPI.EventsService;
    ///   var groupsService = beamableAPI.GroupsService;
    ///   var inventoryService = beamableAPI.InventoryService;
    ///   var leaderboardService = beamableAPI.LeaderboardService;
    ///   var mailService = beamableAPI.MailService;
    ///   var paymentService = beamableAPI.PaymentService;
    ///   var pushService = beamableAPI.PushService;
    ///   var sessionService = beamableAPI.SessionService;
    ///   var statsService = beamableAPI.StatsService;
    ///   var tournamentsService = beamableAPI.TournamentsService;
    ///   var trialDataService = beamableAPI.TrialDataService;
    ///
    /// }
    ///
    /// ```
    ///
    ///
    /// #### Alternative API Links
    /// - See Beamable.Server.IBeamableServices for the main %Microservice script reference
    ///
    ///
    /// ![img beamable-logo]
    ///
    /// </summary>
    public class API //: IBeamableAPI
    {
        private static Promise<IBeamableAPI> _instance;

        /// <summary>
        /// This is deprecated. Please use BeamContext.Default
        /// </summary>
        public static Promise<IBeamableAPI> Instance
        {
            get {
	            return BeamContext.Default.OnReady.Map<IBeamableAPI>(_ => BeamContext.Default.Api);

                // if (_instance != null)
                // {
                //     return _instance;
                // }
                //
                // _instance = ApiFactory();
                // return _instance;
            }

            // SHOULD ONLY BE USED BY LOCAL TEST CODE.
#if UNITY_EDITOR
            set => _instance = value;
#endif
        }

        private API()
        {

        }
    }
}
