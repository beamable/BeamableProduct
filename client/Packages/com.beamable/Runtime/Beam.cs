using Beamable.AccountManagement;
using Beamable.Api;
using Beamable.Api.Analytics;
using Beamable.Api.Announcements;
using Beamable.Api.Auth;
using Beamable.Api.Caches;
using Beamable.Api.CloudData;
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
using Beamable.Api.Tournaments;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Announcements;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.CloudData;
using Beamable.Common.Api.Content;
using Beamable.Common.Api.Inventory;
using Beamable.Common.Api.Leaderboards;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Api.Tournaments;
using Beamable.Common.Assistant;
using Beamable.Common.Dependencies;
using Beamable.Common.Reflection;
using Beamable.Config;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Experimental.Api.Calendars;
using Beamable.Experimental.Api.Chat;
using Beamable.Experimental.Api.Matchmaking;
using Beamable.Experimental.Api.Sim;
using Beamable.Experimental.Api.Social;
using Beamable.Player;
using Beamable.Reflection;
using Beamable.Sessions;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;

namespace Beamable
{
	/// <summary>
	/// Beam is a bootstrapping class for Beamable. It managers registration for the dependency injection,
	/// and provides a few helper methods for Unity wide operations.
	/// </summary>
	[Preserve]
	public static class Beam
	{
		/// <summary>
		/// This is the default <see cref="IDependencyBuilder"/>.
		/// By default, this will contain most of the Beamable services, but it <b> wont </b> contain
		/// any context specific services like the <see cref="IBeamableRequester"/> or the <see cref="BeamContext"/>.
		/// Those services get provided by the <see cref="BeamContext"/> itself.
		/// <para>
		/// You can register your own types by creating a static method, marking it with the <see cref="RegisterBeamableDependenciesAttribute"/>,
		/// and having it accept a single parameter of <see cref="IDependencyBuilder"/>. The instance you are given is the <see cref="DependencyBuilder"/>.
		/// </para>
		/// </summary>
		public static IDependencyBuilder DependencyBuilder;

		public static ReflectionCache ReflectionCache;
		public static IBeamHintGlobalStorage RuntimeGlobalStorage;

		static Beam()
		{
			ReflectionCache = new ReflectionCache();
#if UNITY_EDITOR
			RuntimeGlobalStorage = new BeamHintGlobalStorage();
#endif

			var reflectionSystemObjects = Resources.LoadAll<ReflectionSystemObject>("ReflectionSystems")
												   .Where(system => system.Enabled)
												   .ToList();
			reflectionSystemObjects.Sort((reflectionSys1, reflectionSys2) => reflectionSys1.Priority.CompareTo(reflectionSys2.Priority));

			// Inject them into the ReflectionCache system in the correct order.
			foreach (var reflectionSystemObject in reflectionSystemObjects)
			{
				ReflectionCache.RegisterTypeProvider(reflectionSystemObject.TypeProvider);
				ReflectionCache.RegisterReflectionSystem(reflectionSystemObject.System);
			}

			// Also initializes the Reflection Cache system with it's IBeamHintGlobalStorage instance when in the editor. When not in the editor, the storage should really not
			// be used and
			// Finally, calls the Generate Reflection cache
#if UNITY_EDITOR
			ReflectionCache.SetStorage(RuntimeGlobalStorage);
#endif
			ReflectionCache.GenerateReflectionCache(CoreConfiguration.Instance.AssembliesToSweep);

			// Set the default promise error handlers
			PromiseExtensions.SetupDefaultHandler();

			// The config-database is what sits inside of config-defaults
			try
			{
				ConfigDatabase.Init();
			}
			catch (FileNotFoundException)
			{
				Debug.LogError("Failed to find 'config-defaults' file. This should never be seen here. If you do, please file a bug-report.");
			}

			// Flush cache that wasn't created with this version of the game.
			OfflineCache.FlushInvalidCache();

			// register all services that are not context specific.
			DependencyBuilder = new DependencyBuilder();
			DependencyBuilder.AddComponentSingleton<CoroutineService>();
			DependencyBuilder.AddComponentSingleton<NotificationService>();
			DependencyBuilder.AddComponentSingleton<BeamableBehaviour>();
			DependencyBuilder.AddComponentSingleton<PubnubSubscriptionManager>(
				(manager, provider) => manager.Initialize(provider.GetService<IPlatformService>()));
			DependencyBuilder.AddSingleton<IBeamableRequester, PlatformRequester>(
				provider => provider.GetService<PlatformRequester>());
			DependencyBuilder.AddSingleton(BeamableEnvironment.Data);
			DependencyBuilder.AddSingleton<IUserContext>(provider => provider.GetService<IPlatformService>());
			DependencyBuilder.AddSingleton<IConnectivityService, ConnectivityService>();
			DependencyBuilder.AddSingleton<IAuthService, AuthService>();
			DependencyBuilder.AddScoped<IInventoryApi, InventoryService>(
				provider => provider.GetService<InventoryService>());
			DependencyBuilder.AddSingleton<IAnnouncementsApi, AnnouncementsService>();
			DependencyBuilder.AddSingleton<ISessionService, SessionService>();
			DependencyBuilder.AddSingleton<CloudSavingService>();
			DependencyBuilder.AddSingleton<IBeamableFilesystemAccessor, PlatformFilesystemAccessor>();
			DependencyBuilder.AddSingleton<ContentService>();
			DependencyBuilder.AddSingleton<IContentApi>(provider => provider.GetService<ContentService>());
			DependencyBuilder.AddScoped<InventoryService>();
			DependencyBuilder.AddScoped<StatsService>(provider =>
														  new StatsService(
															  provider.GetService<IPlatformService>(),
															  provider.GetService<PlatformRequester>(),
															  provider,
															  UnityUserDataCache<Dictionary<string, string>>
																  .CreateInstance));
			DependencyBuilder.AddSingleton<AnalyticsTracker>(provider =>
																 new AnalyticsTracker(
																	 provider.GetService<IPlatformService>(),
																	 provider.GetService<PlatformRequester>(),
																	 provider.GetService<CoroutineService>(), 30, 10)
			);
			DependencyBuilder.AddSingleton<IAnalyticsTracker>(provider => provider.GetService<AnalyticsTracker>());
			DependencyBuilder.AddSingleton<MailService>();
			DependencyBuilder.AddSingleton<PushService>();
			DependencyBuilder.AddSingleton<CommerceService>();
			DependencyBuilder.AddSingleton<CloudDataService>();
			DependencyBuilder.AddSingleton<ICloudDataApi>(provider => provider.GetService<CloudDataService>());
			DependencyBuilder.AddSingleton<CloudDataApi>(provider => provider.GetService<CloudDataService>());
			DependencyBuilder.AddSingleton<PaymentService>();
			DependencyBuilder.AddSingleton<GroupsService>();
			DependencyBuilder.AddSingleton<EventsService>();
			DependencyBuilder.AddSingleton<ITournamentApi>(p => p.GetService<TournamentService>());
			DependencyBuilder.AddSingleton<TournamentService>();
			DependencyBuilder.AddSingleton<ChatService>();
			DependencyBuilder.AddSingleton<LeaderboardService>(provider =>
																   new LeaderboardService(
																	   provider.GetService<IPlatformService>(),
																	   provider.GetService<IBeamableRequester>(),
																	   provider,
																	   UnityUserDataCache<RankEntry>.CreateInstance
																   ));
			DependencyBuilder.AddSingleton<GameRelayService>();
			DependencyBuilder.AddSingleton<MatchmakingService>(provider => new MatchmakingService(
																   provider.GetService<IPlatformService>(),
																   // the matchmaking service needs a special instance of the beamable api requester
																   provider.GetService<IBeamableApiRequester>())
			);
			DependencyBuilder.AddSingleton<SocialService>();
			DependencyBuilder.AddSingleton<CalendarsService>();
			DependencyBuilder.AddSingleton<AnnouncementsService>();
			DependencyBuilder.AddSingleton<IHeartbeatService, Heartbeat>();
			DependencyBuilder.AddSingleton<ISdkEventService, SdkEventService>();
			DependencyBuilder.AddSingleton<PubnubNotificationService>();
			DependencyBuilder.AddSingleton<IPubnubNotificationService, PubnubNotificationService>();
			DependencyBuilder.AddSingleton<IPubnubSubscriptionManager>(
				provider => provider.GetService<PubnubSubscriptionManager>());
			DependencyBuilder.AddSingleton<INotificationService>(
				provider => provider.GetService<NotificationService>());
			DependencyBuilder.AddSingleton<ApiServices>();

			DependencyBuilder.AddSingleton<Promise<IBeamablePurchaser>>(provider => new Promise<IBeamablePurchaser>());
			DependencyBuilder.AddSingleton<PlayerAnnouncements>();
			DependencyBuilder.AddScoped<PlayerStats>();
			DependencyBuilder.AddScoped<PlayerInventory>();

			// register module configurations. XXX: move these registrations into their own modules?
			DependencyBuilder.AddSingleton(SessionConfiguration.Instance.DeviceOptions);
			DependencyBuilder.AddSingleton(SessionConfiguration.Instance.CustomParameterProvider);
			DependencyBuilder.AddSingleton(ContentConfiguration.Instance.ParameterProvider);
			DependencyBuilder.AddSingleton(CoreConfiguration.Instance);
			DependencyBuilder.AddSingleton<IAuthSettings>(AccountManagementConfiguration.Instance);

			ReflectionCache.GetFirstSystemOfType<BeamReflectionCache.Registry>().LoadCustomDependencies(DependencyBuilder);
			//LoadCustomDependencies();
		}

		/// <summary>
		/// Non-editor systems should use the reflection system returned by this methods to do what they need.
		/// </summary>
		public static T GetReflectionSystem<T>() where T : IReflectionSystem => ReflectionCache.GetFirstSystemOfType<T>();

		/// <summary>
		/// Runs the <see cref="BeamContext.ClearPlayerAndStop"/> method on every <see cref="BeamContext"/> in memory.
		/// </summary>
		public static async Promise ClearAndStopAllContexts()
		{
			foreach (var ctx in BeamContext.All)
			{
				await ctx.ClearPlayerAndStop();
			}
		}

		/// <summary>
		/// Runs the <see cref="BeamContext.Stop"/> method on every <see cref="BeamContext"/> in memory.
		/// </summary>
		public static async Promise StopAllContexts()
		{
			foreach (var ctx in BeamContext.All)
			{
				await ctx.Stop();
			}
		}

		/// <summary>
		/// Unload every scene, and reload some requested scene.
		/// If no <paramref name="sceneQualifier"/> is given, then this will reload the current scene.
		/// If you are in the Unity Editor, this means the scene will reload to the state when you entered Playmode.
		/// Otherwise, if you are in a built game, this will reload the scene as it exists in the Project Build Settings.
		///
		/// This method won't deploy objects in the DontDestroyOnLoad scene.
		/// </summary>
		/// <param name="sceneQualifier">The string should either be a scene name, or the stringified int of a scene build index.</param>
		public static async Promise ResetToScene(string sceneQualifier = null)
		{
			var loadAction = GetLoadSceneFunction(sceneQualifier);

			var totalScenesRequired = 0;
			var scenesDestroyed = 0;
			var allScenesUnloaded = new Promise();

			Action<AsyncOperation> Check(Scene unloadedScene)
			{
				return (_) =>
				{
					scenesDestroyed++;
					if (scenesDestroyed != totalScenesRequired) return;

					allScenesUnloaded.CompleteSuccess();
				};
			}

			var autoScene = SceneManager.CreateScene("_autogeneratored_" + Guid.NewGuid().ToString());
			totalScenesRequired = SceneManager.sceneCount - 1;
			for (var i = 0; i < totalScenesRequired; i++)
			{
				var toDestroy = SceneManager.GetSceneAt(i);
				if (autoScene == toDestroy) continue;
				var op = SceneManager.UnloadSceneAsync(toDestroy);
				if (op == null)
				{
					Check(toDestroy)(null);
				}
				else op.completed += Check(toDestroy);
			}

			await allScenesUnloaded;
			loadAction();
		}

		private static Action GetLoadSceneFunction(string sceneQualifier = null)
		{
			Action loadAction = () =>
			{
				Debug.LogWarning("No scene could be identified to reload.");
			};
			if (sceneQualifier == null)
			{
				// load the activeScene, or if in Unity Editor, load
#if UNITY_EDITOR
				loadAction = () =>
				{
					UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
						"Temp/__Backupscenes/0.backup", new LoadSceneParameters(LoadSceneMode.Single));
				};
#else
				var activeScene = SceneManager.GetActiveScene();
				loadAction = () =>
				{
					SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
				};
#endif
			}
			else if (int.TryParse(sceneQualifier, out var buildIndex))
			{
				loadAction = () =>
				{
					SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
				};
			}
			else
			{
				loadAction = () =>
				{
					SceneManager.LoadScene(sceneQualifier, LoadSceneMode.Single);
				};
			}

			return loadAction;
		}
	}
}
