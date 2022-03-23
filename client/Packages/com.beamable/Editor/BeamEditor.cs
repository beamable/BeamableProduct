using Beamable.AccountManagement;
using Beamable.Api;
using Beamable.Api.Auth;
using Beamable.Api.Caches;
using Beamable.Api.Connectivity;
using Beamable.Avatars;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.Content;
using Beamable.Common.Assistant;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Config;
using Beamable.Console;
using Beamable.Content;
using Beamable.Editor;
using Beamable.Editor.Alias;
using Beamable.Editor.Assistant;
using Beamable.Editor.Content;
using Beamable.Editor.Modules.Account;
using Beamable.Editor.Modules.EditorConfig;
using Beamable.Editor.Realms;
using Beamable.Editor.Reflection;
using Beamable.Editor.ToolbarExtender;
using Beamable.Inventory.Scripts;
using Beamable.Reflection;
using Beamable.Sessions;
using Beamable.Shop;
using Beamable.Sound;
using Beamable.Theme;
using Beamable.Tournaments;
using Beamable.UI.Buss;
using Core.Platform.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using static Beamable.Common.Constants;
using Logger = Beamable.Common.Spew.Logger;
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

			EditorAPI.Instance.Then(_ => { });

			// Initializes the Config database
			// This solves the same problem that the try/catch block around the ModuleConfigurations solves.
			try
			{
				ConfigDatabase.Init();
			}
			catch (FileNotFoundException e)
			{
				if (e.FileName == ConfigDatabase.GetConfigFileName())
				{
					Logger.DoSpew("Config File not found during initialization dodged!");
					EditorApplication.delayCall += Initialize;
					return;
				}
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

			// Also initializes the Reflection Cache system with it's IBeamHintGlobalStorage instance
			// (that gets propagated down to any IReflectionSystem that also implements IBeamHintProvider).
			// Finally, calls the Generate Reflection cache
			EditorReflectionCache.SetStorage(HintGlobalStorage);
			EditorReflectionCache.GenerateReflectionCache(coreConfiguration.AssembliesToSweep);

			// Hook up editor play-mode-warning feature.
			void OnPlayModeStateChanged(PlayModeStateChange change)
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
							BeamableAssistantWindow.ShowWindow();
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
			                                                                                                 null) {RequestTimeoutMs = $"{30 * 1000}"}
			);
			BeamEditorContextDependencies.AddSingleton(provider => provider.GetService<IPlatformRequester>() as IHttpRequester);
			BeamEditorContextDependencies.AddSingleton(provider => provider.GetService<IPlatformRequester>() as PlatformRequester);

			BeamEditorContextDependencies.AddSingleton<IEditorAuthApi>(provider => new EditorAuthService(provider.GetService<IPlatformRequester>()));
			BeamEditorContextDependencies.AddSingleton(provider => new ContentIO(provider.GetService<IPlatformRequester>()));
			BeamEditorContextDependencies.AddSingleton(provider => new ContentPublisher(provider.GetService<IPlatformRequester>(), provider.GetService<ContentIO>()));
			BeamEditorContextDependencies.AddSingleton(provider => new AliasService(provider.GetService<IHttpRequester>()));
			BeamEditorContextDependencies.AddSingleton(provider => new RealmsService(provider.GetService<PlatformRequester>()));

			BeamEditorContextDependencies.AddSingleton(_ => EditorReflectionCache);
			BeamEditorContextDependencies.AddSingleton(_ => HintGlobalStorage);
			BeamEditorContextDependencies.AddSingleton(_ => HintPreferencesManager);

			var hintReflectionSystem = GetReflectionSystem<BeamHintReflectionCache.Registry>();
			foreach (var globallyAccessibleHintSystem in hintReflectionSystem.GloballyAccessibleHintSystems)
				BeamEditorContextDependencies.AddSingleton(globallyAccessibleHintSystem.GetType(), () => globallyAccessibleHintSystem);

			IsInitialized = true;

			// Initialize toolbar
			BeamableToolbarExtender.LoadToolbarExtender();

			// Set flag of FacebookImporter
			BeamableFacebookImporter.SetFlag();

			async void Init()
			{
				await BeamEditorContext.Default.InitializePromise;
				Debug.Log($"Initialized Default [{BeamEditorContext.Default.PlayerCode}] - " +
				          $"[{BeamEditorContext.Default.ServiceScope.GetService<PlatformRequester>().Cid}] - " +
				          $"[{BeamEditorContext.Default.ServiceScope.GetService<PlatformRequester>().Pid}]");
			}

			Init();
		}

		public static T GetReflectionSystem<T>() where T : IReflectionSystem => EditorReflectionCache.GetFirstSystemOfType<T>();

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		// ReSharper disable once RedundantAssignment
		public static void GetBeamHintSystem<T>(ref T foundProvider) where T : IBeamHintSystem
		{
			var hintReflectionSystem = GetReflectionSystem<BeamHintReflectionCache.Registry>();
			foundProvider = hintReflectionSystem.GloballyAccessibleHintSystems.Where(a => a is T).Cast<T>().FirstOrDefault();
		}

		[RegisterBeamableDependencies(), System.Diagnostics.Conditional("UNITY_EDITOR")]
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
	}

	public class BeamEditorContext
	{
		public const string EDITOR_PLAYER_CODE_TEMPLATE = "editor.{0}.";

		public static Dictionary<string, BeamEditorContext> EditorContexts = new Dictionary<string, BeamEditorContext>();
		public static List<BeamEditorContext> All => EditorContexts.Values.ToList();
		public static BeamEditorContext Default => Instantiate(string.Format(EDITOR_PLAYER_CODE_TEMPLATE, "0"));

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

		public CustomerView CurrentCustomer;
		public RealmView CurrentRealm;
		public RealmView ProductionRealm;
		public EditorUser CurrentUser;
		
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

			// Load up the current Configuration data
			ConfigDatabase.TryGetString("alias", out var alias);
			var cid = ConfigDatabase.GetString("cid");
			var pid = ConfigDatabase.GetString("pid");
			var platform = ConfigDatabase.GetString("platform");
			AliasHelper.ValidateAlias(alias);
			AliasHelper.ValidateCid(cid);

			// Initialize the requester configuration data so we can attempt a login.
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Cid = cid;
			requester.Pid = pid;
			requester.Host = platform;

			// Attempts to login with recovery.
			// TODO: use newly added recover with extension with these timings new int[] { 2, 2, 4, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10 };
			var accessTokenStorage = ServiceScope.GetService<AccessTokenStorage>();
			InitializePromise = accessTokenStorage.LoadTokenForCustomer(cid).FlatMap(token =>
			{
				if (token != null) return Login(token);

				requester.Token = null; // show state as logged out.
				return Promise.Success;
			}).ToPromise();
		}
		
		public Promise Login(string email, string password)
		{
			var accessTokenStorage = ServiceScope.GetService<AccessTokenStorage>();
			var authService = ServiceScope.GetService<IEditorAuthApi>();
			var requester = ServiceScope.GetService<PlatformRequester>();
			return authService.Login(email, password, customerScoped: true).FlatMap(tokenRes =>
			{
				var token = new AccessToken(accessTokenStorage, requester.Cid, null, tokenRes.access_token, tokenRes.refresh_token, tokenRes.expires_in);
				// use this token.
				return ApplyToken(token);
			}).ToPromise();
		}

		public async Promise Login(AccessToken token)
		{
			var realmService = ServiceScope.GetService<RealmsService>();
			await ApplyToken(token)
				.FlatMap(_ =>
				{
					return realmService
					       .GetRealm()
					       .Recover(ex =>
					       {
						       if (ex is RealmServiceException err)
						       {
							       // there is no realm.
							       return null;
						       }

						       throw ex;
					       })
					       .FlatMap(realm => realm == null ? Promise.Success : SwitchRealm(realm));
				});
		}

		public Promise<Unit> SwitchRealm(RealmView realm)
		{
			return SwitchRealm(realm.FindRoot(), realm?.Pid);
		}

		public async Promise<Unit> SwitchRealm(RealmView game, string pid)
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

			//SaveConfig(Alias, pid, cid: game.Cid);

			await ServiceScope.GetService<ContentIO>().FetchManifest();
			var realms = await ServiceScope.GetService<RealmsService>().GetRealms(game);

			var set = EditorPrefHelper
			          .GetMap(REALM_PREFERENCE)
			          .Set($"{game.Cid}.{game.Pid}", pid)
			          .Save();

			var realm = realms.FirstOrDefault(r => string.Equals(r.Pid, pid));

			CurrentRealm = realm;
			ProductionRealm = game;
			OnRealmChange?.Invoke(realm);

			return PromiseBase.Unit;
		}

		Promise<Unit> ApplyToken(AccessToken token)
		{
			return token.SaveAsCustomerScoped().FlatMap(_ => InitializeWithToken(token)).ToUnit();
		}

		private Promise InitializeWithToken(AccessToken token)
		{
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Token = token;

			// TODO: This call may fail because we're getting a customer scoped token now..
			var authService = ServiceScope.GetService<IEditorAuthApi>();
			var realmsService = ServiceScope.GetService<RealmsService>();

			return realmsService
			       .GetCustomerData()
			       .Map(data =>
			       {
				       CurrentCustomer = data;
				       SaveConfig(data.Alias, requester.Pid, cid: data.Cid);
				       OnCustomerChange?.Invoke(data);
				       return Promise.Success;
			       })
			       .FlatMap(_ => authService.GetUserForEditor()
			                                .Map(user =>
			                                {
				                                CurrentUser = user;
				                                OnUserChange?.Invoke(CurrentUser);
				                                return CurrentUser;
			                                })
			                                .RecoverWith(ex =>
			                                {
				                                if (ex is PlatformRequesterException err && err.Status == 403)
				                                {
					                                return authService.GetUser().Map(user2 =>
					                                {
						                                CurrentUser = new EditorUser(user2);
						                                OnUserChange?.Invoke(CurrentUser);
						                                return CurrentUser;
					                                });
				                                }
				                                else throw ex;
			                                })
			                                .Error(err => { Logout(); })
			       )
			       .ToPromise();
		}
		
		public void Logout()
		{
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.DeleteToken();
			CurrentUser = null;
			OnUserChange?.Invoke(null);
			BeamableEnvironment.ReloadEnvironment();
		}

		
		public void SaveConfig(string alias, string pid, string host = null, string cid = "", string containerPrefix = null)
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

			string path = "Assets/Beamable/Resources/config-defaults.txt";
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
				Directory.CreateDirectory("Assets/Beamable/Resources/");
				
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

			// Initialize the requester configuration data so we can attempt a login.
			var requester = ServiceScope.GetService<PlatformRequester>();
			requester.Cid = cid;
			requester.Pid = pid;
			requester.Host = host;
		}
	}
}
