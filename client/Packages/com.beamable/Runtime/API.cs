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
    public class API : IBeamableAPI
    {
        private static Promise<IBeamableAPI> _instance;

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

        private static Promise<IBeamableAPI> ApiFactory()
        {
            const int SECONDS_UNTIL_RETRY = 2;

            var waitForRetryTime = new WaitForSecondsRealtime(SECONDS_UNTIL_RETRY);

            IEnumerator WaitThenTryAgain(Action cb)
            {
                yield return waitForRetryTime;
                cb();
            }

            var api = new API();
            return api.Initialize().RecoverWith(ex =>
            {
                if (!(ex is NoConnectivityException)) throw ex;

                Debug.LogWarning($"Beamable requires an internet connection to initialize the first session. Will retry in {SECONDS_UNTIL_RETRY} seconds...");

                // wait for a few seconds, and then try again...
                if (!ServiceManager.CanResolve<CoroutineService>())
                {
                    Debug.LogError("Coroutine service hasn't initialized yet, so a retry cannot be rescheduled.");
                    throw ex;
                }

                var p = new Promise<Unit>();
                ServiceManager
                    .Resolve<CoroutineService>()
                    .StartCoroutine(WaitThenTryAgain(() => p.CompleteSuccess(PromiseBase.Unit)));

                return p.FlatMap(_ =>
                {
                    Object.Destroy(api._gameObject);
                    api._platform?.Dispose();

                    Debug.LogWarning("Retrying internet connection for initial session.");
                    return ApiFactory();
                });
            });
        }

#if UNITY_EDITOR && UNITY_2019_1_OR_NEWER
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        static void Init()
        {
            _instance = null;
        }

        private PlatformService _platform;
        private GameObject _gameObject;
#if BEAMABLE_PURCHASING
        private IBeamablePurchaser _beamablePurchaser;
#endif

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/announcements-feature">Announcements</a> feature.
        /// </summary>
        public AnnouncementsService AnnouncementService => _platform.Announcements;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/content-feature">Content</a> feature.
        /// </summary>
        public ContentService ContentService => _platform.ContentService;

        public ISdkEventService SdkEventService { get; } = new SdkEventService();

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/inventory-feature">Inventory</a> feature.
        /// </summary>
        public InventoryService InventoryService => _platform.Inventory;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/leaderboards-feature">Leaderboards</a> feature.
        /// </summary>
        public LeaderboardService LeaderboardService => _platform.Leaderboard;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/accounts-feature">Accounts</a> feature.
        /// </summary>
        public IAuthService AuthService => _platform.Auth;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/analytics-feature">Analytics</a> feature.
        /// </summary>
        public IAnalyticsTracker AnalyticsTracker => _platform.Analytics;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/analytics-feature">Analytics</a> feature.
        /// </summary>
        public StatsService StatsService => _platform.Stats;

        /// <summary>
        /// Obsolete. Use StatsService instead.
        /// </summary>
        [Obsolete("Use " + nameof(StatsService) + " instead.")]
        public StatsService Stats => StatsService;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/tournaments-feature">Tournaments</a> feature.
        /// </summary>
        public ITournamentApi TournamentsService => _platform.Tournaments;

        /// <summary>
        /// Obsolete. Use TournamentsService instead.
        /// </summary>
        [Obsolete("Use " + nameof(TournamentsService) + " instead.")]
        public ITournamentApi Tournaments => TournamentsService;

        /// <summary>
        /// Entry point for the SessionService.
        /// </summary>
        public SessionService SessionService => _platform.Session;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/mail-feature">Mail</a> feature.
        /// </summary>
        public MailService MailService => _platform.Mail;

        /// <summary>
        /// Entry point for the PushService.
        /// </summary>
        public PushService PushService => _platform.Push;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/store-feature">Store</a> feature.
        /// </summary>
        public PaymentService PaymentService => _platform.Payments;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/store-feature">Store</a> feature.
        /// </summary>
        public CommerceService CommerceService => _platform.Commerce;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/events-feature">Events</a> feature.
        /// </summary>
        public EventsService EventsService => _platform.Events;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/cloud-saving-feature">Cloud Saving</a> feature.
        /// </summary>
        public CloudSavingService CloudSavingService => _platform.CloudSaving;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/cloud-saving-feature">Cloud Saving</a> feature.
        /// </summary>
        public ICloudDataApi TrialDataService => _platform.CloudDataService;

        /// <summary>
        /// Entry point for the <a target="_blank" href="https://docs.beamable.com/docs/groups-feature">Groups</a> feature.
        /// </summary>
        public GroupsService GroupsService => _platform.Groups;

        /// <summary>
        /// Entry point for the PlatformRequester.
        /// </summary>
        public IBeamableRequester Requester => _platform.Requester;

        /// <summary>
        /// Entry point for the INotificationService.
        /// </summary>
        public INotificationService NotificationService => _platform.Notification;

        /// <summary>
        /// Entry point for the IBeamablePurchaser.
        /// </summary>
        public Promise<IBeamablePurchaser> BeamableIAP => _platform.InitializedBeamableIAP;

        /// <summary>
        /// Entry point for the User.
        /// </summary>
        public User User => _platform.User;

        /// <summary>
        /// Entry point for the AccessToken.
        /// </summary>
        public AccessToken Token => _platform.AccessToken;

        /// <summary>
        /// Entry point for the IConnectivityService.
        /// </summary>
        public IConnectivityService ConnectivityService => _platform.ConnectivityService;

        /// <summary>
        /// Entry point for %experimental %labs / %beta features
        /// </summary>
        public IExperimentalAPI Experimental { get; private set; }

        /// <summary>
        /// Invoked after the active User has changed
        /// </summary>
        public event Action<User> OnUserChanged;

        /// <summary>
        /// Invoked after the active User has logged out
        /// </summary>
        public event Action<User> OnUserLoggingOut;
//
        private Promise<IBeamableAPI> Initialize()
        {
	        PromiseExtensions.SetupDefaultHandler();


            // Build default game object
            _gameObject = new GameObject("Beamable");
            Object.DontDestroyOnLoad(_gameObject);

            // Initialize platform
            ConfigDatabase.Init();
            //Flush cache that wasn't created with this version of the game.
            OfflineCache.FlushInvalidCache();
            // Register services
            ServiceManager.DisableEditorResolvers();
            var coroutineService = MonoBehaviourServiceContainer<CoroutineService>.CreateComponent(_gameObject);

            ServiceManager.Provide(coroutineService);

            ServiceManager.ProvideWithDefaultContainer(SessionConfiguration.Instance.CustomParameterProvider);
            ServiceManager.ProvideWithDefaultContainer(SessionConfiguration.Instance.DeviceOptions);

            ServiceManager.ProvideWithDefaultContainer(ContentConfiguration.Instance.ParameterProvider);
            ServiceManager.ProvideWithDefaultContainer<IAuthSettings>(AccountManagementConfiguration.Instance);



            _platform = new PlatformService(debugMode: false, withLocalNote: false);
            Experimental = new ExperimentalAPI(_platform);

            ServiceManager.ProvideWithDefaultContainer(_platform);
            ServiceManager.Provide(new BeamableResolver(this));


#if BEAMABLE_PURCHASING
            ServiceManager.Provide(new UnityBeamableIAPServiceResolver());
#endif

            return _platform.Initialize("en").Map<IBeamableAPI>(_ => this);
        }

        IEnumerator DeferPromise(Action action)
        {
            yield return Yielders.EndOfFrame;
            action();
        }

        public void ClearDeviceUsers() => _platform.ClearDeviceUsers();

        public Promise<Unit> ApplyToken(TokenResponse tokenResponse)
        {
            if (User != null)
            {
                OnUserLoggingOut?.Invoke(User);
            }

            return _platform.SaveToken(tokenResponse)
               .FlatMap(_ => _platform.ReloadUser())
               .FlatMap(user => _platform.StartNewSession().Map(_ => user))
               .Then(user => OnUserChanged?.Invoke(user))
               .Map(_ => PromiseBase.Unit);
        }

        public Promise<ISet<UserBundle>> GetDeviceUsers()
        {
            return _platform.GetDeviceUsers();
        }

        public void RemoveDeviceUser(TokenResponse token)
        {
            _platform.RemoveDeviceUsers(token);
        }

        public void UpdateUserData(User user)
        {
            _platform.SetUser(user);
            OnUserChanged?.Invoke(user);
        }

        public void TearDown()
        {
            var monoBehaviour = _gameObject.AddComponent<CoroutineBehaviour>();
            monoBehaviour.StartCoroutine(TearDownCoroutine());
        }

        private IEnumerator TearDownCoroutine()
        {
            // Create new empty scene so existing scenes can be unloaded and leave this one standing
            SceneManager.CreateScene(Guid.NewGuid().ToString());

            // Shut down all scenes
            var count = SceneManager.sceneCount;
            var ops = new List<AsyncOperation>();
            for (var i = 0; i < count; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                ops.Add(SceneManager.UnloadSceneAsync(scene));
            }

            for (var i = 0; i < count; i++)
            {
                yield return ops[i];
            }

            // Reboot
            _instance = null;
            Object.Destroy(_gameObject);
            SceneManager.LoadScene(0, LoadSceneMode.Single);
        }

        private class CoroutineBehaviour : MonoBehaviour
        {
        }
    }

    public class BeamableResolver : IServiceResolver<API>
    {
        private API _app;

        public BeamableResolver(API app)
        {
            _app = app;
        }

        public bool CanResolve()
        {
            return _app != null;
        }

        public bool Exists()
        {
            return _app != null;
        }

        public API Resolve()
        {
            return _app;
        }

        public void OnTeardown()
        {
            _app.TearDown();
            _app = null;
            ServiceManager.Remove(this);
        }
    }
}
