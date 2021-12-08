using Beamable.Api;
using Beamable.Api.AdvertisingIdentifier;
using Beamable.Api.Auth;
using Beamable.Api.Connectivity;
using Beamable.Api.Notification;
using Beamable.Api.Payments;
using Beamable.Api.Sessions;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using Beamable.Config;
using Beamable.Player;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable
{

	/// <summary>
	/// <para>
	/// The <see cref="BeamContext"/> represents a player's session and is equipped with all required services.
	/// <see cref="BeamContext"/> instances are not tied to scene memory. Instead, <see cref="BeamContext"/> instances live in a static way.
	/// However, if a <see cref="BeamableBehaviour"/> is connected to a <see cref="BeamContext"/>, then the <see cref="BeamableBehaviour"/>'s lifecycle
	/// will control the `<see cref="BeamContext"/>'s lifecycle.
	/// Additionally, the <see cref="PlayerCode"/> variable is the unique id of the <see cref="BeamContext"/>.
	/// </para>
	///
	/// <para>
	/// From Monobehaviours or Unity components, you should use the <see cref="ForContext(UnityEngine.Component)"/> method to access a <see cref="BeamContext"/>.
	/// The <see cref="ForContext(UnityEngine.Component)"/> will give you the closest context in the Unity object heirarchy from your script's location, or give you
	/// <see cref="Default"/> if no context exists. You can add <see cref="BeamableBehaviour"/> components to add a context to a GameObject.
	///
	/// If you want to access an instance without using a context sensitive approach, you should use the <see cref="Instantiate"/> method.
	/// Finally, you can always use <see cref="Default"/>.
	/// </para>
	///
	/// <para>
	/// Remember, there will only ever be one instance of <see cref="BeamContext"/> per <see cref="PlayerCode"/> value.
	/// The <see cref="Default"/> context has a <see cref="PlayerCode"/> value of an empty string.
	/// </para>
	/// </summary>
	[Serializable]
	public class BeamContext : IPlatformService, IGameObjectContext
	{
		/// <summary>
		/// The <see cref="PlayerCode"/> is the name of a player's slot on the device. The <see cref="Default"/> context uses an empty string,
		/// but you could use values like "player1" and "player2" to enable a feature like couch-coop.
		/// </summary>
		public string PlayerCode { get; private set; }

		public ObservableLong PlayerId = new ObservableLong();
		public ObservableUser UserData = new ObservableUser();

		public string Cid => _requester.Cid;

		public string Pid => _requester.Pid; // TODO: rename to rid

		public ObservableAccessToken AccessToken = new ObservableAccessToken();

		public IDependencyProvider ServiceProvider => _serviceScope;
		private IDependencyProviderScope _serviceScope;

		public IBeamableRequester Requester => ServiceProvider.GetService<IBeamableRequester>();

		public bool IsDisposed => _isDisposed;

		// Lazy initialization of services.
		[SerializeField]
		private PlayerAnnouncements _announcements;
		[SerializeField]
		private PlayerCurrencyGroup _currency;
		[SerializeField]
		private PlayerStats _playerStats;

		public PlayerAnnouncements Announcements =>
			_announcements?.IsInitialized ?? false
				? _announcements
				: (_announcements = ServiceProvider.GetService<PlayerAnnouncements>());

		public PlayerCurrencyGroup Currencies =>
			_currency?.IsInitialized ?? false
				? _currency
				: (_currency = ServiceProvider.GetService<PlayerCurrencyGroup>());

		public PlayerStats Stats =>
			_playerStats?.IsInitialized ?? false
				? _playerStats
				: (_playerStats = ServiceProvider.GetService<PlayerStats>());


		/// <summary>
		/// Each <see cref="BeamContext"/> has a set of components that need to live on a gameObject in the scene.
		/// </summary>
		public GameObject GameObject { get; private set; }

		private ApiServices _api;
		public ApiServices Api => _api ?? (_api = new ApiServices(this));

		private AccessTokenStorage _tokenStorage;
		private EnvironmentData _environment;
		private PlatformRequester _requester;

		private IAuthService _authService;
		private IConnectivityService _connectivityService;
		private NotificationService _notification;
		private IPubnubSubscriptionManager _pubnubSubscriptionManager;
		private IPubnubNotificationService _pubnubNotificationService;
		private ISessionService _sessionService;
		private IHeartbeatService _heartbeatService;
		private BeamableBehaviour _behaviour;

		private Promise _initPromise;

		private bool _isDisposed;

		public bool IsInitialized => _initPromise != null;

		private BeamContext(){}

		private void Init(string cid,
		                  string pid,
		                  string playerCode,
		                  BeamableBehaviour behaviour,
		                  IDependencyBuilder builder)
		{
			PlayerCode = playerCode;
			_isDisposed = false;

			var shouldCreateGob = behaviour == null; // if there is no behaviour, then we definately need one
			if (!shouldCreateGob) // but also, if the object needs to be preserved, then it also needs to be at root level
			{
				shouldCreateGob = behaviour.transform.parent != null && behaviour.DontDestroyContext.IsTruthy();
			}

			if (shouldCreateGob)
			{
				var gob = new GameObject($"Beamable {playerCode}");
				var nextBehaviour = gob.AddComponent<BeamableBehaviour>();

				if (string.IsNullOrEmpty(playerCode) || (behaviour.DontDestroyContext?.Value ?? false))
				{
					// the default context shouldn't destroy on load, unless again, it has already been specified.
					nextBehaviour.DontDestroyContext = new OptionalBoolean {HasValue = true, Value = true};
				}

				behaviour = nextBehaviour;
			}

			GameObject = behaviour.gameObject;

			builder = builder.Fork();
			builder.AddSingleton<PlatformRequester, PlatformRequester>(
				provider => new PlatformRequester(
					_environment.ApiUrl,
					provider.GetService<AccessTokenStorage>(),
					provider.GetService<IConnectivityService>()
				)
			);
			builder.AddSingleton<BeamableApiRequester>(
				provider => new BeamableApiRequester(
					_environment.ApiUrl,
					provider.GetService<AccessTokenStorage>(),
					provider.GetService<IConnectivityService>())
			);

			builder.AddSingleton<BeamContext>(this);
			builder.AddSingleton<IPlatformService>(this);
			builder.AddSingleton<IGameObjectContext>(this);
			builder.AddSingleton(new AccessTokenStorage(playerCode));
			_serviceScope = builder.Build();
			behaviour.Initialize(this);

			_tokenStorage = ServiceProvider.GetService<AccessTokenStorage>();
			_environment = ServiceProvider.GetService<EnvironmentData>();
			_authService = ServiceProvider.GetService<IAuthService>();

			_requester = ServiceProvider.GetService<PlatformRequester>();
			_requester.AuthService = _authService;
			_requester.Cid = cid;
			_requester.Pid = pid;

			_connectivityService = ServiceProvider.GetService<IConnectivityService>();
			_notification = ServiceProvider.GetService<NotificationService>();
			_pubnubSubscriptionManager = ServiceProvider.GetService<IPubnubSubscriptionManager>();
			_pubnubNotificationService = ServiceProvider.GetService<IPubnubNotificationService>();
			_sessionService = ServiceProvider.GetService<ISessionService>();
			_heartbeatService = ServiceProvider.GetService<IHeartbeatService>();
			_behaviour = ServiceProvider.GetService<BeamableBehaviour>();

			UserData.OnUpdated += () => PlayerId.Value = UserData.Value.id;

			_initPromise = InitializeUser();
		}


		private async Promise SaveToken(TokenResponse rsp)
		{
			ClearToken();
			_requester.Token = new AccessToken(_tokenStorage,
			                                   Cid,
			                                   Pid,
			                                   rsp.access_token,
			                                   rsp.refresh_token,
			                                   rsp.expires_in);
			AccessToken.Value = _requester.Token;

			// TODO: Where was this being used in the past?
			// _beamableApiRequester.Token = _requester.Token;

			await _requester.Token.Save();
		}

		private void ClearToken()
		{
			_requester.DeleteToken();
			AccessToken.Value = null;
		}

		private async Promise StartNewSession()
		{
			var adId = await AdvertisingIdentifier.GetIdentifier();
			await _sessionService.StartSession(UserData.Value, adId, _requester.Language);
		}

		private async Promise InitializeUser()
		{
			// Create a new account
			AccessToken.Value = _tokenStorage.LoadTokenForRealmImmediate(Cid, Pid);
			_requester.Token = AccessToken.Value;
			_requester.Language = "en"; // TODO: Put somewhere


			if (AccessToken.IsNullOrUnassigned)
			{
				var rsp = await _authService.CreateUser();
				await SaveToken(rsp);
				return;
			}

			// Refresh token
			if (AccessToken.Value.IsExpired)
			{
				try
				{
					var rsp = await _authService.LoginRefreshToken(AccessToken.Value.RefreshToken);
					await SaveToken(rsp);
				}
				catch (NoConnectivityException)
				{
					// this exception is valid.
				}
			}

			// Fetch User
			UserData.Value = await _authService.GetUser();

			// Subscribe pubnub stuff
			_pubnubSubscriptionManager.UnsubscribeAll();
			_pubnubSubscriptionManager.SubscribeToProvider();
			OnReloadUser?.Invoke();
			Debug.Log("Got User " + UserData.Value.id, GameObject);

			// Start Session
			await StartNewSession();

			// Register for notifications
			_notification.RegisterForNotifications();
			_heartbeatService.Start();

			// Check if we should initialize the
			if (ServiceProvider.CanBuildService<IBeamablePurchaser>())
			{
				await ServiceProvider.GetService<IBeamablePurchaser>().Initialize(ServiceProvider);
			}

		}


		/// <summary>
		/// Create or retrieve a <see cref="BeamContext"/> for the given <see cref="PlayerCode"/>. There is only one instance of a context per <see cref="PlayerCode"/>.
		/// A <see cref="BeamableBehaviour"/> is required because the context needs to attach specific Unity components to a GameObject, and the given <see cref="BeamableBehaviour"/>'s GameObject will be used.
		/// If no <see cref="BeamableBehaviour"/> is given, then a new GameObject will be instantiated at the root transform level named, "Beamable (playerCode)"
		/// </summary>
		/// <param name="beamable">A component that will invite other Beamable components to exist on its GameObject</param>
		/// <param name="playerCode">A named code that represents a player slot on the device. The <see cref="Default"/> context uses an empty string. </param>
		/// <returns></returns>
		public static BeamContext Instantiate(
			BeamableBehaviour beamable=null,
			string playerCode=null
			)
		{
			playerCode = playerCode ?? "";
			// get the cid & pid if not given
			var cid = ConfigDatabase.GetString("cid");
			var pid = ConfigDatabase.GetString("pid");


			// there should only be one context per playerCode.
			if (_playerCodeToContext.TryGetValue(playerCode, out var existingContext))
			{
				if (existingContext.IsDisposed)
				{
					existingContext.Init(cid, pid, playerCode, beamable, Beam.DependencyBuilder);
				}

				return existingContext;
			}


			// var ctx = new BeamContext(cid, pid, playerCode, beamable, Beam.DependencyBuilder);
			var ctx = new BeamContext();
			ctx.Init(cid, pid, playerCode, beamable, Beam.DependencyBuilder);
			_playerCodeToContext[playerCode] = ctx;
			return ctx;
		}

		private static Dictionary<string, BeamContext> _playerCodeToContext = new Dictionary<string, BeamContext>();

		/// <summary>
		/// A static <see cref="BeamContext"/> that uses a <see cref="PlayerCode"/> of an empty string.
		/// By default, the default context will persist between scene reloads. If you need to dispose it, you'll
		/// need to manually invoke <see cref="Dispose"/>
		/// </summary>
		public static BeamContext Default => Instantiate();

		/// <summary>
		/// Find the first <see cref="BeamableBehaviour.Context"/> in the parent lineage of the current component, or <see cref="BeamContext.Default"/> if no <see cref="BeamableBehaviour"/> components exist
		/// </summary>
		public static BeamContext ForContext(Component c) => c.GetBeamable();

		public static BeamContext ForContext(string playerCode="") => Instantiate(playerCode: playerCode);

		/// <summary>
		/// This method will tear down a <see cref="BeamContext"/> and notify all internal services that the context should be destroyed.
		/// All coroutines associated with the context will stop.
		/// No notifications will be received internal to the context.
		/// No services will be accessible from the <see cref="ServiceProvider"/> after <see cref="Dispose"/> has been called.
		/// <para>
		/// After a context has been disposed, if a call is made to <see cref="Instantiate"/> with the disposed context's <see cref="PlayerCode"/>,
		/// then the disposed instance will be reinitialized and rehydrated and returned to the <see cref="Instantiate"/>'s callsite.
		/// </para>
		/// <para>
		/// If you want to create a new player, see <see cref="Reset"/>
		/// </para>
		/// </summary>
		public async Promise Dispose()
		{
			if (_isDisposed) return;

			_isDisposed = true;
			OnShutdown?.Invoke();
			try
			{
				await _serviceScope.Dispose();
			}
			catch (NullReferenceException nullEx)
			{
				Debug.Log("I FOUND THE NULL REFERENCE EXCEPTION!!!!!" + nullEx.Message);
				throw;
			}
			catch (PromiseException promiseEx)
			{
				Debug.Log("I FOUND THE PROMISE REFERENCE EXCEPTION!!!!!" + promiseEx.Message);
				throw;
			}

			OnShutdownComplete?.Invoke();

		}

		/// <summary>
		/// Clear the authorization token for the <see cref="PlayerCode"/>, then internally calls <see cref="Dispose"/>, and finally re-initializes the context.
		/// After this method completes, there will be a new PlayerId associated with the player code.
		/// </summary>
		public async Promise Reset()
		{
			ClearToken();
			await Dispose();
			Instantiate(_behaviour, PlayerCode);
			await _initPromise;
		}

		public void ChangeTime()
		{
			TimeOverrideChanged?.Invoke();
		}

		long IUserContext.UserId => PlayerId.Value;

		public event Action OnShutdown;
		public event Action OnShutdownComplete;
		public event Action OnReloadUser;
		public event Action TimeOverrideChanged;

		User IPlatformService.User => UserData.Value;
		Promise<Unit> IPlatformService.OnReady => _initPromise;
		INotificationService IPlatformService.Notification => _notification;
		IPubnubNotificationService IPlatformService.PubnubNotificationService => _pubnubNotificationService;
		IConnectivityService IPlatformService.ConnectivityService => _connectivityService;
		IHeartbeatService IPlatformService.Heartbeat => _heartbeatService;
	}
}
