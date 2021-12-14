using Beamable.Api;
using Beamable.Api.AdvertisingIdentifier;
using Beamable.Api.Auth;
using Beamable.Api.Connectivity;
using Beamable.Api.Notification;
using Beamable.Api.Payments;
using Beamable.Api.Sessions;
using Beamable.Api.Stats;
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
using UnityEngine.Assertions;

namespace Beamable
{

	public interface IObservedPlayer : IUserContext
	{
		PlayerStats Stats { get; }
	}

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
	public class BeamContext : IPlatformService, IGameObjectContext, IObservedPlayer
	{

		#region Internal State
		/// <summary>
		/// The <see cref="PlayerCode"/> is the name of a player's slot on the device. The <see cref="Default"/> context uses an empty string,
		/// but you could use values like "player1" and "player2" to enable a feature like couch-coop.
		/// </summary>
		public string PlayerCode { get; private set; }

		/// <summary>
		/// The User that this context is authenticated with. Any web-calls that are made from this <see cref="BeamContext"/> are made by this User
		/// </summary>
		public ObservableUser AuthorizedUser = new ObservableUser();

		/// <summary>
		/// The <see cref="IDependencyProvider"/> is a collection of all services required to provide a Beamable SDK full funcitonality
		/// </summary>
		public IDependencyProvider ServiceProvider => _serviceScope;

		/// <summary>
		/// If the <see cref="Dispose"/> method has been run, this property will return true. Once a context is disposed, you shouldn't use
		/// it anymore, and the <see cref="ServiceProvider"/> will throw exceptions if you do.
		/// You can re-initialize the context by using <see cref="ForContext(UnityEngine.Component)"/>
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		/// Each <see cref="BeamContext"/> has a set of components that need to live on a gameObject in the scene.
		/// </summary>
		public GameObject GameObject => _gob ? _gob : _parent.GameObject;

		public bool IsInitialized => _initPromise != null;

		private IDependencyProviderScope _serviceScope;
		private bool _isDisposed;
		private GameObject _gob;
		private Promise _initPromise;
		private BeamContext _parent;
		private HashSet<BeamContext> _children = new HashSet<BeamContext>();


		#endregion

		#region Service Accessors

		public long PlayerId {
			get {
				var userContext = ServiceProvider.GetService<IUserContext>();
				if (userContext == this) return AuthorizedUser?.Value?.id ?? 0;
				return userContext.UserId;
			}
		}

		public string Cid => _requester.Cid;

		public string Pid => _requester.Pid; // TODO: rename to rid

		public AccessToken AccessToken => _requester.Token;

		public IBeamableRequester Requester => ServiceProvider.GetService<IBeamableRequester>();


		[SerializeField]
		private PlayerAnnouncements _announcements;
		[SerializeField]
		private PlayerCurrencyGroup _currency;
		[SerializeField]
		private PlayerStats _playerStats;

		public PlayerAnnouncements Announcements =>
			_announcements?.IsInitialized ?? false
				? _announcements
				: (_announcements = _serviceScope.GetService<PlayerAnnouncements>());

		public PlayerCurrencyGroup Currencies =>
			_currency?.IsInitialized ?? false
				? _currency
				: (_currency = _serviceScope.GetService<PlayerCurrencyGroup>());

		public PlayerStats Stats =>
			_playerStats?.IsInitialized ?? false
				? _playerStats
				: (_playerStats = _serviceScope.GetService<PlayerStats>());


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


		// Lazy initialization of services.
		// [SerializeField]
		// private PlayerAnnouncements _announcements;
		// [SerializeField]
		// private PlayerCurrencyGroup _currency;
		// [SerializeField]
		// private PlayerStats _playerStats;
		//
		// public PlayerAnnouncements Announcements =>
		// 	_announcements?.IsInitialized ?? false
		// 		? _announcements
		// 		: (_announcements = ServiceProvider.GetService<PlayerAnnouncements>());
		//
		// public PlayerCurrencyGroup Currencies =>
		// 	_currency?.IsInitialized ?? false
		// 		? _currency
		// 		: (_currency = ServiceProvider.GetService<PlayerCurrencyGroup>());
		//
		// public PlayerStats Stats =>
		// 	_playerStats?.IsInitialized ?? false
		// 		? _playerStats
		// 		: (_playerStats = ServiceProvider.GetService<PlayerStats>());

		// public IObservedPlayer Me => this;
		#endregion

		#region events

		public event Action OnShutdown;
		public event Action OnShutdownComplete;
		public event Action OnReloadUser;
		public event Action TimeOverrideChanged; // TODO: What to do with the time override?

		public event Action<User> OnUserLoggingOut;
		public event Action<User> OnUserLoggedIn;

		#endregion


		private BeamContext()
		{
			AuthorizedUser.OnDataUpdated += user => OnUserLoggedIn?.Invoke(user);
		}

		/// <summary>
		/// A <see cref="BeamContext"/> is configured for one authorized user. If wish to change the user, you need to give it a new token.
		/// You can get <see cref="TokenResponse"/> values from the <see cref="IAuthService"/> by calling various log in methods.
		///
		/// This method will <i>mutate</i> the current <see cref="BeamContext"/> instance itself, and returned the mutated object.
		/// </summary>
		/// <param name="token"></param>
		/// <returns>The same instance, but now mutated for the new authorized user</returns>
		public async Promise<BeamContext> ChangeAuthorizedPlayer(TokenResponse token)
		{
			if (AuthorizedUser != null)
			{
				OnUserLoggingOut?.Invoke(AuthorizedUser);
			}

			await Dispose(); // tear down all the services, and do a total reboot with the new user.
			await SaveToken(token); // set the token so that it gets picked up on the next initialization
			var ctx = Instantiate(null, PlayerCode);

			Assert.AreEqual(ctx, this);
			return ctx;
		}


		/// <summary>
		/// Using the authorization associated with the current context, observe the public data of another player
		/// </summary>
		/// <param name="otherPlayerId"></param>
		public IObservedPlayer ObservePlayer(long otherPlayerId)
		{
			return Fork(builder => {
				builder
					.RemoveIfExists<IUserContext>()
					.AddScoped<IUserContext>(new SimpleUserContext(otherPlayerId))
					;
			});
		}

		private BeamContext Fork(Action<IDependencyBuilder> configure)
		{
			var ctx = new BeamContext();
			ctx._parent = this;
			ctx.AuthorizedUser = AuthorizedUser;
			_children.Add(ctx);

			var subScope = _serviceScope.Fork(configure);
			ctx._serviceScope = subScope;
			ctx.InitServices(Cid, Pid);
			return ctx;
		}

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

			_gob = behaviour.gameObject;
			builder = builder.Fork();

			RegisterServices(builder);

			var oldScope = _serviceScope;
			_serviceScope = builder.Build();
			oldScope?.Hydrate(_serviceScope);

			InitServices(cid, pid);
			_behaviour.Initialize(this);


			_initPromise = InitializeUser();
		}

		private void RegisterServices(IDependencyBuilder builder)
		{
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

			builder.AddSingleton<IBeamableAPI>(provider => Api);
			builder.AddSingleton<BeamContext>(this);
			builder.AddSingleton<IPlatformService>(this);
			builder.AddSingleton<IGameObjectContext>(this);
			builder.AddSingleton(new AccessTokenStorage(PlayerCode));
		}

		private void InitServices(string cid, string pid)
		{
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

			// TODO: Where was this being used in the past?
			// _beamableApiRequester.Token = _requester.Token;

			await _requester.Token.Save();
		}

		private void ClearToken()
		{
			_requester.DeleteToken();
		}

		private async Promise StartNewSession()
		{
			var adId = await AdvertisingIdentifier.GetIdentifier();
			await _sessionService.StartSession(AuthorizedUser.Value, adId, _requester.Language);
		}

		private async Promise InitializeUser()
		{
			// Create a new account
			_requester.Token = _tokenStorage.LoadTokenForRealmImmediate(Cid, Pid);
			_requester.Language = "en"; // TODO: Put somewhere

			if (AccessToken == null)
			{
				var rsp = await _authService.CreateUser();
				await SaveToken(rsp);
			} else if (AccessToken.IsExpired)
			{
				try
				{
					var rsp = await _authService.LoginRefreshToken(AccessToken.RefreshToken);
					await SaveToken(rsp);
				}
				catch (NoConnectivityException)
				{
					// this exception is valid.
				}
			}

			// Fetch User
			AuthorizedUser.Value = await _authService.GetUser();

			// Subscribe pubnub stuff
			_pubnubSubscriptionManager.UnsubscribeAll();
			_pubnubSubscriptionManager.SubscribeToProvider();
			OnReloadUser?.Invoke();

			// Start Session
			await StartNewSession();

			// Register for notifications
			_notification.RegisterForNotifications();
			_heartbeatService.Start();

			// Check if we should initialize the
			if (ServiceProvider.CanBuildService<IBeamablePurchaser>())
			{
				var purchaser = ServiceProvider.GetService<IBeamablePurchaser>();
				await purchaser.Initialize(ServiceProvider);
				ServiceProvider.GetService<Promise<IBeamablePurchaser>>().CompleteSuccess(purchaser);
			}

			OnReloadUser?.Invoke();

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

		public static BeamContext EditorContext => ForContext("editor");

		public static IEnumerable<BeamContext> All => _playerCodeToContext.Values;
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
			await _serviceScope.Dispose();
			OnShutdownComplete?.Invoke();
		}




		/// <summary>
		/// Clear the authorization token for the <see cref="PlayerCode"/>, then internally calls <see cref="Dispose"/>, and finally re-initializes the context.
		/// After this method completes, there will be a new PlayerId associated with the player code.
		/// </summary>
		public async Promise ClearAndDispose()
		{
			ClearToken();
			await Dispose();
			// Instantiate(null, PlayerCode);
			// await _initPromise;
		}

		public void ChangeTime()
		{
			TimeOverrideChanged?.Invoke();
		}

		long IUserContext.UserId => PlayerId;

		User IPlatformService.User => AuthorizedUser.Value;
		public Promise<Unit> OnReady => _initPromise;
		INotificationService IPlatformService.Notification => _notification;
		IPubnubNotificationService IPlatformService.PubnubNotificationService => _pubnubNotificationService;
		IConnectivityService IPlatformService.ConnectivityService => _connectivityService;
		IHeartbeatService IPlatformService.Heartbeat => _heartbeatService;
	}
}
