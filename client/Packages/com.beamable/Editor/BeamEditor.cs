using Beamable.AccountManagement;
using Beamable.Api;
using Beamable.Api.Caches;
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Assistant;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Config;
using Beamable.Console;
using Beamable.Content;
using Beamable.Editor;
using Beamable.Editor.Alias;
using Beamable.Editor.Assistant;
using Beamable.Editor.Config;
using Beamable.Editor.Content;
using Beamable.Editor.Modules.Account;
using Beamable.Editor.Modules.EditorConfig;
using Beamable.Editor.Realms;
using Beamable.Editor.Reflection;
using Beamable.Editor.ToolbarExtender;
using Beamable.Editor.Toolbox.Models;
using Beamable.Editor.UI;
using Beamable.Inventory.Scripts;
using Beamable.Reflection;
using Beamable.Serialization;
using Beamable.Serialization.SmallerJSON;
using Beamable.Sessions;
using Beamable.Shop;
using Beamable.Sound;
using Beamable.Theme;
using Beamable.Tournaments;
using Beamable.UI.Buss;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.VersionControl;
using UnityEditor.VspAttribution.Beamable;
using UnityEngine;
using static Beamable.Common.Constants;
using Debug = UnityEngine.Debug;
using Logger = Beamable.Common.Spew.Logger;
using Task = System.Threading.Tasks.Task;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.Compilation;
#endif

namespace Beamable
{
	[InitializeOnLoad, BeamContextSystem]
	public static class BeamEditor
	{
		public static CoreConfiguration CoreConfiguration { get; private set; }
		public static ReflectionCache EditorReflectionCache { get; private set; }
		public static IBeamHintGlobalStorage HintGlobalStorage { get; private set; }
		public static IBeamHintPreferencesManager HintPreferencesManager { get; private set; }
		public static bool IsInitialized { get; private set; }

		public static IDependencyBuilder BeamEditorContextDependencies;

		static BeamEditor()
		{
			Initialize();
		}

		static void Initialize()
		{
			if (IsInitialized) return;

			// Attempts to load all Module Configurations --- If they fail, we delay BeamEditor initialization until they don't fail.
			// The ONLY fail case is:
			//   - On first import or "re-import all", Resources and AssetDatabase don't know about the existence of these instances when this code runs for a couple of frames.
			//   - Empirically, we noticed this takes 2~3 attempts (frames) until this is done. So it's an acceptable and unnoticeable wait.
			// Doing this loading in this manner and making our windows delay their initialization until this is initialized (see BeamableAssistantWindow.OnEnable), we can
			// never have to care about this UnityEditor problem in our code that actually does things and we can have a guarantee that these will never throw.
			CoreConfiguration coreConfiguration;
			try
			{
				coreConfiguration = CoreConfiguration = CoreConfiguration.Instance;
				_ = AccountManagementConfiguration.Instance;
				_ = AvatarConfiguration.Instance;
				_ = BussConfiguration.OptionalInstance;
				_ = ConsoleConfiguration.Instance;
				_ = ContentConfiguration.Instance;
				_ = EditorConfiguration.Instance;
				_ = InventoryConfiguration.Instance;
				_ = SessionConfiguration.Instance;
				_ = ShopConfiguration.Instance;
				_ = SoundConfiguration.Instance;
				_ = ThemeConfiguration.Instance;
				_ = TournamentsConfiguration.Instance;
			}
			// Solves a specific issue on first installation of package ---
			catch (ModuleConfigurationNotReadyException)
			{
				EditorApplication.delayCall += Initialize;
				return;
			}

			// Ensures we have the latest assembly definitions and paths are all correctly setup.
			CoreConfiguration.OnValidate();

			// Apply the defined configuration for how users want to uncaught promises (with no .Error callback attached) in Beamable promises.
			if (!Application.isPlaying)
			{
				var promiseHandlerConfig = CoreConfiguration.Instance.DefaultUncaughtPromiseHandlerConfiguration;
				switch (promiseHandlerConfig)
				{
					case CoreConfiguration.EventHandlerConfig.Guarantee:
					{
						if (!PromiseBase.HasUncaughtErrorHandler)
							PromiseExtensions.RegisterBeamableDefaultUncaughtPromiseHandler();

						break;
					}
					case CoreConfiguration.EventHandlerConfig.Replace:
					case CoreConfiguration.EventHandlerConfig.Add:
					{
						PromiseExtensions.RegisterBeamableDefaultUncaughtPromiseHandler(promiseHandlerConfig == CoreConfiguration.EventHandlerConfig.Replace);
						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			// Reload the current environment data
			BeamableEnvironment.ReloadEnvironment();

			// Initializes the Config database
			// This solves the same problem that the try/catch block around the ModuleConfigurations solves.
			bool TryInitConfigDatabase(bool allowRetry = true)
			{
				try
				{
					ConfigDatabase.Init();
					return true;
				}
				catch (FileNotFoundException e)
				{
					if (e.FileName == ConfigDatabase.GetConfigFileName())
					{
						if (allowRetry)
						{
							BeamEditorContext.WriteConfig(string.Empty, string.Empty);
							return TryInitConfigDatabase(false);
						}
						else
						{
							Logger.DoSpew("Config File not found during initialization dodged!");
							EditorApplication.delayCall += Initialize;
							return false;
						}
					}

					throw;
				}
			}

			if (!TryInitConfigDatabase())
			{
				return;
			}

			// If we ever get to this point, we are guaranteed to run the initialization until the end so we...
			// Initialize Editor instances of Reflection and Assistant services
			EditorReflectionCache = new ReflectionCache();
			HintGlobalStorage = new BeamHintGlobalStorage();
			HintPreferencesManager = new BeamHintPreferencesManager(new List<BeamHintHeader>()
			{
				// insert hints that should auto-block, here. At the moment, there are none!
			});

			// Load up all Asset-based IReflectionSystem (injected via ReflectionSystemObject instances). This was made to solve a cross-package injection problem.
			// It doubles as a no-code way for users to inject their own IReflectionSystem into our pipeline.
			var reflectionCacheSystemGuids = BeamableAssetDatabase.FindAssets<ReflectionSystemObject>(
				coreConfiguration.ReflectionSystemPaths
				                 .Where(Directory.Exists)
				                 .ToArray());

			// Get ReflectionSystemObjects and sort them
			var reflectionSystemObjects = reflectionCacheSystemGuids.Select(reflectionCacheSystemGuid =>
			                                                        {
				                                                        var assetPath = AssetDatabase.GUIDToAssetPath(reflectionCacheSystemGuid);
				                                                        return AssetDatabase.LoadAssetAtPath<ReflectionSystemObject>(assetPath);
			                                                        })
			                                                        .Union(Resources.LoadAll<ReflectionSystemObject>("ReflectionSystems"))
			                                                        .Where(system => system.Enabled)
			                                                        .ToList();
			reflectionSystemObjects.Sort((reflectionSys1, reflectionSys2) => reflectionSys1.Priority.CompareTo(reflectionSys2.Priority));

			// Inject them into the ReflectionCache system in the correct order.
			foreach (var reflectionSystemObject in reflectionSystemObjects)
			{
				EditorReflectionCache.RegisterTypeProvider(reflectionSystemObject.TypeProvider);
				EditorReflectionCache.RegisterReflectionSystem(reflectionSystemObject.System);
			}

			// Add non-ScriptableObject-based Reflection-Cache systems into the pipeline.
			var contentReflectionCache = new ContentTypeReflectionCache();
			EditorReflectionCache.RegisterTypeProvider(contentReflectionCache);
			EditorReflectionCache.RegisterReflectionSystem(contentReflectionCache);

			// Also initializes the Reflection Cache system with it's IBeamHintGlobalStorage instance
			// (that gets propagated down to any IReflectionSystem that also implements IBeamHintProvider).
			// Finally, calls the Generate Reflection cache
			EditorReflectionCache.SetStorage(HintGlobalStorage);
			EditorReflectionCache.GenerateReflectionCache(coreConfiguration.AssembliesToSweep);

			// Hook up editor play-mode-warning feature.
			async void OnPlayModeStateChanged(PlayModeStateChange change)
			{
				if (!coreConfiguration.EnablePlayModeWarning) return;

				if (change == PlayModeStateChange.ExitingEditMode)
				{
					HintPreferencesManager.SplitHintsByPlayModeWarningPreferences(HintGlobalStorage.All, out var toWarnHints, out _);
					var hintsToWarnAbout = toWarnHints.ToList();
					if (hintsToWarnAbout.Count > 0)
					{
						var msg = string.Join("\n", hintsToWarnAbout.Select(hint => $"- {hint.Header.Id}"));

						var res = EditorUtility.DisplayDialogComplex("Beamable Assistant",
						                                             "There are pending Beamable Validations.\n" + "These Hints may cause problems during runtime:\n\n" + $"{msg}\n\n" +
						                                             "Do you wish to stop entering playmode and see these validations?", "Yes, I want to stop and go see validations.",
						                                             "No, I'll take my chances and don't bother me about these specific hints anymore.",
						                                             "No, I'll take my chances and don't bother me ever again about any hints.");

						if (res == 0)
						{
							EditorApplication.isPlaying = false;
							await BeamableAssistantWindow.Init();
						}
						else if (res == 1)
						{
							foreach (var hint in hintsToWarnAbout) HintPreferencesManager.SetHintPlayModeWarningPreferences(hint, BeamHintPlayModeWarningPreference.Disabled);
						}
						else if (res == 2)
						{
							coreConfiguration.EnablePlayModeWarning = false;
						}
					}
				}
			}

			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

			// Set up Globally Accessible Hint System Dependencies and then call init
			foreach (var hintSystem in GetReflectionSystem<BeamHintReflectionCache.Registry>().GloballyAccessibleHintSystems)
			{
				hintSystem.SetStorage(HintGlobalStorage);
				hintSystem.SetPreferencesManager(HintPreferencesManager);

				hintSystem.OnInitialized();
			}

			// Initialize BeamEditorContext dependencies
			BeamEditorContextDependencies = new DependencyBuilder();
			BeamEditorContextDependencies.AddSingleton(provider => new AccessTokenStorage(provider.GetService<BeamEditorContext>().PlayerCode));
			BeamEditorContextDependencies.AddSingleton<IPlatformRequester>(provider => new PlatformRequester(BeamableEnvironment.ApiUrl,
																											 provider.GetService<AccessTokenStorage>(),
																											 null,
																											 provider.GetService<OfflineCache>())
			{ RequestTimeoutMs = $"{30 * 1000}" }
			);
			BeamEditorContextDependencies.AddSingleton(provider => provider.GetService<IPlatformRequester>() as IHttpRequester);
			BeamEditorContextDependencies.AddSingleton(provider => provider.GetService<IPlatformRequester>() as PlatformRequester);
			BeamEditorContextDependencies.AddSingleton(provider => provider.GetService<IPlatformRequester>() as IBeamableRequester);

			BeamEditorContextDependencies.AddSingleton<IEditorAuthApi>(provider => new EditorAuthService(provider.GetService<IPlatformRequester>()));
			BeamEditorContextDependencies.AddSingleton(provider => new ContentIO(provider.GetService<IPlatformRequester>()));
			BeamEditorContextDependencies.AddSingleton(provider => new ContentPublisher(provider.GetService<IPlatformRequester>(), provider.GetService<ContentIO>()));
			BeamEditorContextDependencies.AddSingleton<AliasService>();
			BeamEditorContextDependencies.AddSingleton(provider => new RealmsService(provider.GetService<IPlatformRequester>()));

			BeamEditorContextDependencies.AddSingleton(_ => EditorReflectionCache);
			BeamEditorContextDependencies.AddSingleton(_ => HintGlobalStorage);
			BeamEditorContextDependencies.AddSingleton(_ => HintPreferencesManager);
			BeamEditorContextDependencies.AddSingleton<BeamableVsp>();

			BeamEditorContextDependencies.AddSingleton<IToolboxViewService, ToolboxViewService>();
			BeamEditorContextDependencies.AddSingleton<OfflineCache>(() => new OfflineCache(CoreConfiguration.Instance.UseOfflineCache));

			var hintReflectionSystem = GetReflectionSystem<BeamHintReflectionCache.Registry>();
			foreach (var globallyAccessibleHintSystem in hintReflectionSystem.GloballyAccessibleHintSystems)
				BeamEditorContextDependencies.AddSingleton(globallyAccessibleHintSystem.GetType(), () => globallyAccessibleHintSystem);

			// Set flag of SocialsImporter
			BeamableSocialsImporter.SetFlag();

			async void InitDefaultContext()
			{
				await BeamEditorContext.Default.InitializePromise;

#if BEAMABLE_DEVELOPER
				Debug.Log($"Initialized Default Editor Context [{BeamEditorContext.Default.PlayerCode}] - " +
				          $"[{BeamEditorContext.Default.ServiceScope.GetService<PlatformRequester>().Cid}] - " +
				          $"[{BeamEditorContext.Default.ServiceScope.GetService<PlatformRequester>().Pid}]");
#endif
				IsInitialized = true;

#if !DISABLE_BEAMABLE_TOOLBAR_EXTENDER
				// Initialize toolbar
				BeamableToolbarExtender.LoadToolbarExtender();
#endif
			}

			InitDefaultContext();
		}

		public static T GetReflectionSystem<T>() where T : IReflectionSystem => EditorReflectionCache.GetFirstSystemOfType<T>();

		[Conditional("UNITY_EDITOR")]
		// ReSharper disable once RedundantAssignment
		public static void GetBeamHintSystem<T>(ref T foundProvider) where T : IBeamHintSystem
		{
			var hintReflectionSystem = GetReflectionSystem<BeamHintReflectionCache.Registry>();
			foundProvider = hintReflectionSystem.GloballyAccessibleHintSystems.Where(a => a is T).Cast<T>().FirstOrDefault();
		}

		[RegisterBeamableDependencies(), Conditional("UNITY_EDITOR")]
		public static void ConditionallyRegisterBeamHintsAsServices(IDependencyBuilder builder)
		{
			foreach (var hintSystemConstructor in GetReflectionSystem<BeamHintReflectionCache.Registry>().BeamContextAccessibleHintSystems)
			{
				builder.AddSingleton(hintSystemConstructor.DeclaringType, () =>
				{
					var builtObj = (IBeamHintSystem)hintSystemConstructor.Invoke(null);
					builtObj.SetPreferencesManager(HintPreferencesManager);
					builtObj.SetStorage(HintGlobalStorage);

					builtObj.OnInitialized();
					return builtObj;
				});
			}
		}

		/// <summary>
		/// Utility function to delay an initialization call (from within any of Unity's callbacks) until we have initialized our default <see cref="BeamEditorContext"/>.
		/// This must be used to wrap any logic dependent on <see cref="BeamEditorContext"/> or <see cref="BeamEditor"/> systems that is being executed from within a unity event function that initializes things.
		/// These are: OnEnable, OnValidate, OnAfterDeserialize and others like it. Essentially, this guarantees our initialization has finished running, before the given action runs.
		/// <para/>
		/// This is especially used to handle first-import cases and several other edge-cases that happen when these unity event functions are called with our windows opened. In these cases, if we don't delay
		/// our windows cases, the following issues have arisen in the past:
		/// <list type="bullet">
		/// <item><see cref="BeamEditorContext.Default"/> is null; which should be impossible, but happens (probably has to do with DomainReloads)</item>
		/// <item>The window tries to make calls to a partially initialized <see cref="BeamEditorContext"/> and throws.</item>
		/// </list>
		/// </summary>
		/// <param name="onInitializationFinished">
		/// The that must be scheduled to run from a Unity callback, but is dependent on our initialization being done.
		/// </param>
		/// <param name="forceDelay">
		/// Whether or not we should force the call to be delayed. This is used to guarantee that the callback in <see cref="BeamEditorWindow{TWindow}.OnEnable"/> is
		/// called only after the <see cref="BeamEditorWindow{TWindow}.InitializedConfig"/> was set during the <see cref="BeamEditorWindow{TWindow}.InitBeamEditorWindow"/> flow.
		/// </param>
		public static void DelayedInitializationCall(Action onInitializationFinished, bool forceDelay, BeamEditorInitializedDelayClause customDelay = null)
		{
			var hasCustomDelay = customDelay != null;
			if (!IsInitialized || forceDelay || (hasCustomDelay && customDelay()))
			{
				EditorApplication.delayCall += () => DelayedInitializationCall(onInitializationFinished, false);
				return;
			}

			onInitializationFinished?.Invoke();
		}
	}

	public delegate bool BeamEditorInitializedDelayClause();

	public class BeamEditorContext
	{
		public const string EDITOR_PLAYER_CODE_TEMPLATE = "editor.{0}.";

		public static Dictionary<string, BeamEditorContext> EditorContexts = new Dictionary<string, BeamEditorContext>();
		public static List<BeamEditorContext> All => EditorContexts.Values.ToList();
		public static BeamEditorContext Default => Instantiate(string.Format(EDITOR_PLAYER_CODE_TEMPLATE, "0"));
		public static BeamEditorContext ForEditorUser(int idx) => Instantiate(string.Format(EDITOR_PLAYER_CODE_TEMPLATE, idx));
		public static BeamEditorContext ForEditorUser(string code) => Instantiate(code);

		public static bool ConfigFileExists { get; private set; }

		/// <summary>
		/// Create or retrieve a <see cref="BeamContext"/> for the given <see cref="PlayerCode"/>. There is only one instance of a context per <see cref="PlayerCode"/>.
		/// A <see cref="BeamableBehaviour"/> is required because the context needs to attach specific Unity components to a GameObject, and the given <see cref="BeamableBehaviour"/>'s GameObject will be used.
		/// If no <see cref="BeamableBehaviour"/> is given, then a new GameObject will be instantiated at the root transform level named, "Beamable (playerCode)"
		/// </summary>
		/// <param name="beamable">A component that will invite other Beamable components to exist on its GameObject</param>
		/// <param name="playerCode">A named code that represents a player slot on the device. The <see cref="Default"/> context uses an empty string. </param>
		/// <returns></returns>
		public static BeamEditorContext Instantiate(string playerCode = null, IDependencyBuilder dependencyBuilder = null)
		{
			dependencyBuilder = dependencyBuilder ?? BeamEditor.BeamEditorContextDependencies;
			playerCode = playerCode ?? string.Format(EDITOR_PLAYER_CODE_TEMPLATE, All.Count.ToString());

			// there should only be one context per playerCode.
			if (EditorContexts.TryGetValue(playerCode, out var existingContext))
			{
				if (existingContext.IsStopped)
				{
					existingContext.Init(playerCode, dependencyBuilder);
				}

				return existingContext;
			}

			var ctx = new BeamEditorContext();
			ctx.Init(playerCode, dependencyBuilder);
			EditorContexts[playerCode] = ctx;
			return ctx;
		}

		public string PlayerCode { get; private set; }
		public bool IsStopped { get; private set; }
		public bool IsAuthenticated => ServiceScope.GetService<PlatformRequester>().Token != null;

		public IDependencyProviderScope ServiceScope { get; private set; }
		public Promise InitializePromise { get; private set; }
		public ContentIO ContentIO => ServiceScope.GetService<ContentIO>();
		public IPlatformRequester Requester => ServiceScope.GetService<PlatformRequester>();

		public CustomerView CurrentCustomer;
		public RealmView CurrentRealm;
		public RealmView ProductionRealm;
		public EditorUser CurrentUser;

		public bool HasToken => Requester.Token != null;
		public bool HasCustomer => CurrentCustomer != null && !string.IsNullOrEmpty(CurrentCustomer.Cid);
		public bool HasRealm => CurrentRealm != null && !string.IsNullOrEmpty(CurrentRealm.Pid);

		public event Action<RealmView> OnRealmChange;
		public event Action<CustomerView> OnCustomerChange;
		public event Action<EditorUser> OnUserChange;

		public void Init(string playerCode, IDependencyBuilder builder)
		{
			PlayerCode = playerCode;
			IsStopped = false;

			builder = builder.Clone();
			builder.AddSingleton(this);

			var oldScope = ServiceScope;
			ServiceScope = builder.Build();
			oldScope?.Hydrate(ServiceScope);

			ConfigFileExists = ConfigDatabase.HasConfigFile(ConfigDatabase.GetConfigFileName());

			if (!ConfigFileExists)
			{
				SaveConfig(string.Empty, string.Empty, BeamableEnvironment.ApiUrl);
				Logout();
				InitializePromise = Promise.Success;
				return;
			}

			// Load up the current Configuration data
			ConfigDatabase.TryGetString("alias", out var alias);
			var cid = ConfigDatabase.GetString("cid");
			var pid = ConfigDatabase.GetString("pid");
			var platform = ConfigDatabase.GetString("platform");
			AliasHelper.ValidateAlias(alias);
			AliasHelper.ValidateCid(cid);

			if (string.IsNullOrEmpty(cid)) // with no cid, we cannot be logged in.
			{
				SaveConfig(string.Empty, string.Empty, BeamableEnvironment.ApiUrl);
				Logout();
				InitializePromise = Promise.Success;
				return;
			}

			// Initialize the requester configuration data so we can attempt a login.
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Cid = cid;
			requester.Pid = pid;
			requester.Host = platform;
			ServiceScope.GetService<BeamableVsp>().TryToEmitAttribution("login"); // this will no-op if the package isn't a VSP package.

			async Promise Initialize()
			{
				// Attempts to login with recovery.
				// TODO: use newly added recover with extension with these timings new int[] { 2, 2, 4, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10 };
				var accessTokenStorage = ServiceScope.GetService<AccessTokenStorage>();
				var accessToken = await accessTokenStorage.LoadTokenForCustomer(cid);

				if (accessToken == null)
				{
					requester.Token = null;
					await Promise.Success;
				}
				else
				{
					LoadLastAuthenticatedUserDataForToken(accessToken, pid, out CurrentUser, out CurrentCustomer, out CurrentRealm);

					if (CurrentUser == null || CurrentCustomer == null || CurrentRealm == null || accessToken.IsExpired)
						await Login(accessToken);
					else
					{
						// Set the token manually as we already have all the data we need be considered initialized (Serialized CurrentUser/CurrentCustomer/CurrentRealm data.
						requester.Token = accessToken;

						// Disable this warning as we do want to run this in the background silently.
#pragma warning disable CS4014
						Login(accessToken, pid);
#pragma warning restore CS4014
						await Promise.Success;
					}
				}
			}

			InitializePromise = Initialize();
		}

		public async Promise<Unit> LoginCustomer(string aliasOrCid, string email, string password)
		{
			var res = await ServiceScope.GetService<AliasService>().Resolve(aliasOrCid);
			var alias = res.Alias.GetOrElse("");
			var cid = res.Cid.GetOrThrow();

			// Set the config defaults to reflect the new Customer.
			SaveConfig(alias, null, BeamableEnvironment.ApiUrl, cid);

			// Attempt to get an access token.
			return await Login(email, password);
		}

		public async Promise Login(string email, string password)
		{
			var accessTokenStorage = ServiceScope.GetService<AccessTokenStorage>();
			var authService = ServiceScope.GetService<IEditorAuthApi>();
			var requester = ServiceScope.GetService<PlatformRequester>();
			var tokenRes = await authService.Login(email, password, customerScoped: true);
			var token = new AccessToken(accessTokenStorage, requester.Cid, null, tokenRes.access_token, tokenRes.refresh_token, tokenRes.expires_in);
			// use this token.
			await Login(token);
		}

		public async Promise Login(AccessToken token, string pid = null)
		{
			var realmService = ServiceScope.GetService<RealmsService>();
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Pid = pid;

			await ApplyToken(token);
			RealmView realm = null;
			try
			{
				realm = await realmService.GetRealm();
			}
			catch (Exception ex)
			{
				if (ex is RealmServiceException err)
				{
					// there is no realm.
					return;
				}
			}

			if (CurrentRealm != null)
				realm = CurrentRealm;

			if (realm == null)
			{
				var games = await realmService.GetGames();

				if (pid == null)
				{
					var realms = await realmService.GetRealms(games.First());
					realm = realms.First();
				}
				else
					realm = (await realmService.GetRealms(pid)).First(rv => rv.Pid == pid);
			}

			await (realm == null ? Promise.Success : SwitchRealm(realm));
			SaveConfig(CurrentCustomer.Alias, CurrentRealm.Pid, cid: CurrentCustomer.Cid);

			await requester.Token.SaveAsCustomerScoped();
			await SaveLastAuthenticatedUserDataForToken(token, CurrentUser, CurrentCustomer, CurrentRealm);
		}

		private void LoadLastAuthenticatedUserDataForToken(AccessToken token, string pid, out EditorUser authUserData, out CustomerView authCustomerData, out RealmView authRealmView)
		{
			var cid = token.Cid;

			var userSerializedData = PlayerPrefs.GetString($"{PlayerCode}{cid}.{pid}.auth_user_data", null);
			var customerSerializedData = PlayerPrefs.GetString($"{PlayerCode}{cid}.{pid}.auth_customer_data", null);
			var realmSerializedData = PlayerPrefs.GetString($"{PlayerCode}{cid}.{pid}.auth_realm_data", null);

			try
			{
				authUserData = DeserializeFromString<EditorUser>(userSerializedData);
				authCustomerData = DeserializeFromString<CustomerView>(customerSerializedData);
				authRealmView = DeserializeFromString<RealmView>(realmSerializedData);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				authUserData = null;
				authCustomerData = null;
				authRealmView = null;
			}
		}

		private static TData DeserializeFromString<TData>(string settings)
		{
			byte[] b = Convert.FromBase64String(settings);
			using (var stream = new MemoryStream(b))
			{
				var formatter = new BinaryFormatter();
				stream.Seek(0, SeekOrigin.Begin);
				return (TData)formatter.Deserialize(stream);
			}
		}

		private static string SerializeToString<TData>(TData settings)
		{
			using (var stream = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, settings);
				stream.Flush();
				stream.Position = 0;
				return Convert.ToBase64String(stream.ToArray());
			}
		}

		private Promise<Unit> SaveLastAuthenticatedUserDataForToken(AccessToken token, EditorUser authUserData, CustomerView authCustomerData, RealmView authRealmView)
		{
			var cid = token.Cid;
			var pid = authRealmView.Pid;

			var userSerializedData = SerializeToString(authUserData); // JsonUtility.ToJson(authUserData);
			var customerSerializedData = SerializeToString(authCustomerData); // JsonUtility.ToJson(authCustomerData);
			var realmSerializedData = SerializeToString(authRealmView); //JsonUtility.ToJson(authRealmView);
			PlayerPrefs.SetString($"{PlayerCode}{cid}.{pid}.auth_user_data", userSerializedData);
			PlayerPrefs.SetString($"{PlayerCode}{cid}.{pid}.auth_customer_data", customerSerializedData);
			PlayerPrefs.SetString($"{PlayerCode}{cid}.{pid}.auth_realm_data", realmSerializedData);

			return Promise.Success;
		}

		private void ClearLastAuthenticatedUserDataForToken(AccessToken token, string pid)
		{
			if (string.IsNullOrEmpty(pid)) return; // nothing to do if the pid is empty.
			var cid = token?.Cid;
			if (string.IsNullOrEmpty(cid)) return; // nothing to do if the cid is empty.

			PlayerPrefs.DeleteKey($"{PlayerCode}{cid}.{pid}.auth_user_data");
			PlayerPrefs.DeleteKey($"{PlayerCode}{cid}.{pid}.auth_customer_data");
			PlayerPrefs.DeleteKey($"{PlayerCode}{cid}.{pid}.auth_realm_data");
		}

		private async Promise ApplyToken(AccessToken token)
		{
			await token.SaveAsCustomerScoped();
			await InitializeWithToken(token);
		}

		private async Promise InitializeWithToken(AccessToken token)
		{
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Token = token;

			// TODO: This call may fail because we're getting a customer scoped token now..
			var authService = ServiceScope.GetService<IEditorAuthApi>();
			var realmsService = ServiceScope.GetService<RealmsService>();

			try
			{
				var data = CurrentCustomer = await realmsService.GetCustomerData();
				SaveConfig(data.Alias, requester.Pid, cid: data.Cid);
				OnCustomerChange?.Invoke(data);
			}
			catch
			{
				Logout();
			}

			try
			{
				var user = CurrentUser = await authService.GetUserForEditor();
				OnUserChange?.Invoke(CurrentUser);
			}
			catch (Exception ex)
			{
				if (ex is PlatformRequesterException err && err.Status == 403)
				{
					try
					{
						CurrentUser = new EditorUser(await authService.GetUser());
						OnUserChange?.Invoke(CurrentUser);
					}
					catch
					{
						Logout();
					}
				}
				else throw;
			}
		}

		public void Logout()
		{
			var requester = ServiceScope.GetService<PlatformRequester>();
			ClearLastAuthenticatedUserDataForToken(requester.Token, CurrentRealm?.Pid);
			requester.DeleteToken();
			CurrentUser = null;
			OnUserChange?.Invoke(null);
			BeamableEnvironment.ReloadEnvironment();
		}

		public static void WriteConfig(string alias, string pid, string host = null, string cid = "", string containerPrefix = null)
		{
			AliasHelper.ValidateAlias(alias);
			AliasHelper.ValidateCid(cid);

			if (string.IsNullOrEmpty(host))
			{
				host = BeamableEnvironment.ApiUrl;
			}

			var config = new ConfigData()
			{
				cid = cid,
				alias = alias,
				pid = pid,
				platform = host,
				socket = host,
				containerPrefix = containerPrefix
			};

			string path = ConfigDatabase.GetFullPath("config-defaults");
			var asJson = JsonUtility.ToJson(config, true);

			var writeConfig = true;
			if (File.Exists(path))
			{
				var existingJson = File.ReadAllText(path);
				if (string.Equals(existingJson, asJson))
				{
					writeConfig = false;
				}
			}

			if (writeConfig)
			{
				string directoryName = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}

				if (File.Exists(path))
				{
					var fileInfo = new FileInfo(path);
					fileInfo.IsReadOnly = false;
				}

				if (Provider.enabled)
				{
					var vcTask = Provider.Checkout(path, CheckoutMode.Asset);
					vcTask.Wait();
					if (!vcTask.success)
					{
						Debug.LogWarning($"Unable to checkout: {path}");
					}
				}

				File.WriteAllText(path, asJson);

				AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
				try
				{
					ConfigDatabase.Init();
				}
				catch (FileNotFoundException)
				{
					Debug.LogError("Failed to find 'config-defaults' file from EditorAPI.SaveConfig. This should never be seen here. If you do, please file a bug-report.");
				}

				AssetDatabase.Refresh();
			}
		}

		public void SaveConfig(string alias, string pid, string host = null, string cid = "", string containerPrefix = null)
		{
			if (string.IsNullOrEmpty(host))
			{
				host = BeamableEnvironment.ApiUrl;
			}

			WriteConfig(alias, pid, host, cid, containerPrefix);
			// Initialize the requester configuration data so we can attempt a login.
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Cid = cid;
			requester.Pid = pid;
			requester.Host = host;
		}

		#region Customer & User Creation and Management

		public async Promise CreateUser(string aliasOrCid, string customerEmail, string customerPassword)
		{
			var aliasService = ServiceScope.GetService<AliasService>();

			var res = await aliasService.Resolve(aliasOrCid);
			var alias = res.Alias.GetOrElse("");
			var cid = res.Cid.GetOrThrow();

			SaveConfig(alias, null, BeamableEnvironment.ApiUrl, cid);

			var authService = ServiceScope.GetService<IEditorAuthApi>();
			var accessTokenStorage = ServiceScope.GetService<AccessTokenStorage>();
			var requester = ServiceScope.GetService<PlatformRequester>();

			var newToken = await authService.CreateUser();

			var token = new AccessToken(accessTokenStorage, CurrentCustomer.Cid, CurrentRealm.Pid, newToken.access_token, newToken.refresh_token, newToken.expires_in);
			requester.Token = token;

			_ = await authService.RegisterDBCredentials(customerEmail, customerPassword);
			await Login(token);
		}

		public async Promise CreateCustomer(string alias, string gameName, string email, string password)
		{
			async Promise HandleNewCustomerAndUser(TokenResponse tokenResponse, string cid, string pid)
			{
				SaveConfig(alias, pid, null, cid);
				var accessTokenStorage = ServiceScope.GetService<AccessTokenStorage>();
				var token = new AccessToken(accessTokenStorage, cid, pid, tokenResponse.access_token,
				                            tokenResponse.refresh_token, tokenResponse.expires_in);
				CurrentRealm = null; // erase the current realm; if there is one..
				await Login(token, pid);
				await DoSilentContentPublish(true);
			}

			var customerName = alias; // TODO: For now...
			SaveConfig(null, null);
			var authService = ServiceScope.GetService<IEditorAuthApi>();

			var res = await authService.RegisterCustomer(email, password, gameName, customerName, alias);
			await HandleNewCustomerAndUser(res.token, res.cid.ToString(), res.pid);
		}

		public async Promise SendPasswordReset(string cidOrAlias, string email)
		{
			var aliasService = ServiceScope.GetService<AliasService>();
			var res = await aliasService.Resolve(cidOrAlias);
			var alias = res.Alias.GetOrElse("");
			var cid = res.Cid.GetOrThrow();

			SaveConfig(alias, null, BeamableEnvironment.ApiUrl, cid);
			var authService = ServiceScope.GetService<IEditorAuthApi>();
			await authService.IssuePasswordUpdate(email);
		}

		public async Promise SendPasswordResetCode(string code, string newPassword)
		{
			var authService = ServiceScope.GetService<IEditorAuthApi>();
			await authService.ConfirmPasswordUpdate(code, newPassword).ToUnit();
		}

		/// <summary>
		/// Force a publish operation, with no validation, with no UX popups. Log output will occur.
		/// </summary>
		/// <param name="force">Pass true to force all content to publish. Leave as false to only publish changed content.</param>
		/// <returns>A Promise of Unit representing the completion of the publish.</returns>
		private async Promise DoSilentContentPublish(bool force = false)
		{
			var contentPublisher = ServiceScope.GetService<ContentPublisher>();
			var clearPromise = force ? contentPublisher.ClearManifest() : Promise<Unit>.Successful(PromiseBase.Unit);
			await clearPromise;

			var publishSet = await contentPublisher.CreatePublishSet();
			await contentPublisher.Publish(publishSet, progress => { });

			var contentIO = ServiceScope.GetService<ContentIO>();
			await contentIO.FetchManifest();
		}

		#endregion

		#region Game & Realm Switching

		public Promise<string> GetRealmSecret()
		{
			// TODO this will only work if the current user is an admin.

			return Requester.Request<CustomerResponse>(Method.GET, "/basic/realms/admin/customer").Map(resp =>
			{
				var matchingProject = resp.customer.projects.FirstOrDefault(p => p.name.Equals(CurrentRealm.Pid));
				return matchingProject?.secret ?? "";
			});
		}

		public Promise SetGame(RealmView game)
		{
			if (game == null) return Promise.Failed(new Exception("Cannot set game to null")) as Promise;

			// we need to remember the last realm the user was on in this game.
			var hadSelectedPid = EditorPrefHelper
			                     .GetMap(REALM_PREFERENCE)
			                     .TryGetValue($"{CurrentCustomer.Cid}.{game.Pid}", out var existingPid);

			if (!hadSelectedPid)
				existingPid = game.Pid;

			SaveConfig(CurrentCustomer.Alias, existingPid, BeamableEnvironment.ApiUrl, CurrentCustomer.Cid);
			return SwitchRealm(game, existingPid);
		}

		public Promise SwitchRealm(RealmView realm)
		{
			return SwitchRealm(realm.FindRoot(), realm?.Pid);
		}

		public async Promise SwitchRealm(RealmView game, string pid)
		{
			if (game == null)
			{
				throw new Exception("Cannot switch to null game");
			}

			if (!game.IsProduction)
			{
				throw new Exception("Cannot switch to a game that isn't a production realm");
			}

			if (string.IsNullOrEmpty(pid))
			{
				throw new Exception("Cannot switch to a realm with a null pid");
			}

			var realms = await ServiceScope.GetService<RealmsService>().GetRealms(game);
			var set = EditorPrefHelper
			          .GetMap(REALM_PREFERENCE)
			          .Set($"{game.Cid}.{game.Pid}", pid)
			          .Save();

			var realm = realms.FirstOrDefault(r => string.Equals(r.Pid, pid));
			if (CurrentRealm == null || !CurrentRealm.Equals(realm))
			{
				CurrentRealm = realm;
				await SaveRealmInConfig();
				await ServiceScope.GetService<ContentIO>().FetchManifest();
				OnRealmChange?.Invoke(realm);
				ProductionRealm = game;
				return;
			}
			else
			{
				await ServiceScope.GetService<ContentIO>().FetchManifest();
			}

			ProductionRealm = game;
			await SaveRealmInConfig();
		}

		private async Promise SaveRealmInConfig()
		{
			// Ensure we save the current cached data for domain reloads.
			await SaveLastAuthenticatedUserDataForToken(Requester.Token, CurrentUser, CurrentCustomer, CurrentRealm);
			SaveConfig(CurrentCustomer.Alias, CurrentRealm.Pid, cid: CurrentCustomer.Cid);
		}

		#endregion

		#region TMP & Addressables Dependencies Check

#if BEAMABLE_DEVELOPER
		[MenuItem(MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES_BEAMABLE_DEVELOPER + "/Force Refresh Content (New)")]
		public static void ForceRefreshContent()
		{
			var contentIO = Default.ServiceScope.GetService<ContentIO>();
			// Do these in parallel to simulate startup behavior.
			_ = contentIO.BuildLocalManifest();
			_ = Default.CreateDependencies().GetResult();
		}
#endif

		public static bool HasDependencies()
		{
			var hasAddressables = AddressableAssetSettingsDefaultObject.GetSettings(false) != null;
			var hasTextmeshPro = TextMeshProImporter.EssentialsLoaded;

			return hasAddressables && hasTextmeshPro;
		}

		public async Promise CreateDependencies()
		{
			// import addressables...
			AddressableAssetSettingsDefaultObject.GetSettings(true);

			var contentIO = Default.ServiceScope.GetService<ContentIO>();
			await TextMeshProImporter.ImportEssentials();

			AssetDatabase.Refresh();
			contentIO.EnsureAllDefaultContent();

			ConfigManager.Initialize();

			if (IsAuthenticated)
			{
				var serverManifest = await contentIO.OnManifest;
				var hasNoContent = serverManifest.References.Count == 0;
				if (hasNoContent)
					await DoSilentContentPublish();
				else
					await PromiseBase.SuccessfulUnit;
			}
		}

		#endregion
	}

	[Serializable]
	public class ConfigData
	{
		public string cid;
		public string alias;
		public string pid;
		public string platform;
		public string socket;
		public string containerPrefix;
	}

	[Serializable]
	public class CustomerResponse
	{
		public CustomerDTO customer;
	}

	[Serializable]
	public class CustomerDTO
	{
		public List<ProjectDTO> projects;
	}

	[Serializable]
	public class ProjectDTO
	{
		public string name;
		public string secret;
	}
}
