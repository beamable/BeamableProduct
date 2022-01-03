// unset

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
using Beamable.Common.Dependencies;
using Beamable.Common.Player;
using Beamable.Config;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Experimental.Api.Calendars;
using Beamable.Experimental.Api.Chat;
using Beamable.Experimental.Api.Matchmaking;
using Beamable.Experimental.Api.Sim;
using Beamable.Experimental.Api.Social;
using Beamable.Player;
using Beamable.Sessions;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beamable
{
	/// <summary>
	/// Beam is a bootstrapping class for Beamable. It managers registration for the dependency injection,
	/// and provides a few helper methods for Unity wide operations.
	/// </summary>
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

		static Beam()
		{
			// Set the default promise error handlers
			PromiseExtensions.SetupDefaultHandler();

			// The config-database is what sits inside of config-defaults
			ConfigDatabase.Init();

			// Flush cache that wasn't created with this version of the game.
			OfflineCache.FlushInvalidCache();

			// register all services that are not context specific.
			DependencyBuilder = new DependencyBuilder()
			                    .AddComponentSingleton<CoroutineService>()
			                    .AddComponentSingleton<NotificationService>()
			                    .AddComponentSingleton<BeamableBehaviour>()
			                    .AddComponentSingleton<PubnubSubscriptionManager>(
				                    (manager, provider) => manager.Initialize(provider.GetService<IPlatformService>()))
			                    .AddSingleton<IBeamableRequester, PlatformRequester>(
				                    provider => provider.GetService<PlatformRequester>())
			                    .AddSingleton(BeamableEnvironment.Data)
			                    .AddSingleton<IUserContext>(provider => provider.GetService<IPlatformService>())
			                    .AddSingleton<IConnectivityService, ConnectivityService>()
			                    .AddSingleton<IAuthService, AuthService>()
			                    .AddScoped<IInventoryApi, InventoryService>( provider => provider.GetService<InventoryService>())
			                    .AddSingleton<IAnnouncementsApi, AnnouncementsService>()
			                    .AddSingleton<ISessionService, SessionService>()
			                    .AddSingleton<CloudSavingService>()
			                    .AddSingleton<IBeamableFilesystemAccessor, PlatformFilesystemAccessor>()
			                    .AddSingleton<ContentService>()
			                    .AddSingleton<IContentApi>( provider => provider.GetService<ContentService>())
			                    .AddScoped<InventoryService>()
			                    .AddScoped<StatsService>(provider =>
				                                             new StatsService(
					                                             provider.GetService<IPlatformService>(),
					                                             provider.GetService<PlatformRequester>(),
					                                             provider,
					                                             UnityUserDataCache<Dictionary<string, string>>
						                                             .CreateInstance))
			                    .AddSingleton<AnalyticsTracker>(provider =>
				                    new AnalyticsTracker(provider.GetService<IPlatformService>(),
				                                         provider.GetService<PlatformRequester>(),
				                                         provider.GetService<CoroutineService>(), 30, 10)
			                    )
			                    .AddSingleton<IAnalyticsTracker>(provider => provider.GetService<AnalyticsTracker>())
			                    .AddSingleton<MailService>()
			                    .AddSingleton<PushService>()
			                    .AddSingleton<CommerceService>()
			                    .AddSingleton<CloudDataService>()
			                    .AddSingleton<ICloudDataApi>(provider => provider.GetService<CloudDataService>())
			                    .AddSingleton<CloudDataApi>(provider => provider.GetService<CloudDataService>())
			                    .AddSingleton<PaymentService>()
			                    .AddSingleton<GroupsService>()
			                    .AddSingleton<EventsService>()
			                    .AddSingleton<ITournamentApi>(p => p.GetService<TournamentService>())
			                    .AddSingleton<TournamentService>()
			                    .AddSingleton<ChatService>()
			                    .AddSingleton<LeaderboardService>(provider =>
				                                                      new LeaderboardService(
					                                                      provider.GetService<IPlatformService>(),
					                                                      provider.GetService<IBeamableRequester>(),
					                                                      provider,
					                                                      UnityUserDataCache<RankEntry>.CreateInstance
					                                                      ))
			                    .AddSingleton<GameRelayService>()
			                    .AddSingleton<MatchmakingService>(provider => new MatchmakingService(
				                                                      provider.GetService<IPlatformService>(),
				                                                      // the matchmaking service needs a special instance of the beamable api requester
				                                                      provider.GetService<BeamableApiRequester>())
				                    )
			                    .AddSingleton<SocialService>()
			                    .AddSingleton<CalendarsService>()
			                    .AddSingleton<AnnouncementsService>()
			                    .AddSingleton<IHeartbeatService, Heartbeat>()
			                    .AddSingleton<ISdkEventService, SdkEventService>()
			                    .AddSingleton<PubnubNotificationService>()
			                    .AddSingleton<IPubnubNotificationService, PubnubNotificationService>()
			                    .AddSingleton<IPubnubSubscriptionManager>(
				                    provider => provider.GetService<PubnubSubscriptionManager>())
			                    .AddSingleton<INotificationService>(
				                    provider => provider.GetService<NotificationService>())
				;



			DependencyBuilder.AddSingleton<Promise<IBeamablePurchaser>>(provider => new Promise<IBeamablePurchaser>());
			DependencyBuilder.AddSingleton<PlayerAnnouncements>();
			// DependencyBuilder.AddScoped<PlayerCurrencyGroup>();
			DependencyBuilder.AddScoped<PlayerStats>();
			DependencyBuilder.AddScoped<PlayerInventory>();

			// register module configurations. XXX: move these registrations into their own modules?
			DependencyBuilder.AddSingleton(SessionConfiguration.Instance.DeviceOptions);
			DependencyBuilder.AddSingleton(SessionConfiguration.Instance.CustomParameterProvider);
			DependencyBuilder.AddSingleton(ContentConfiguration.Instance.ParameterProvider);
			DependencyBuilder.AddSingleton(CoreConfiguration.Instance);
			DependencyBuilder.AddSingleton<IAuthSettings>(AccountManagementConfiguration.Instance);

			LoadCustomDependencies();
		}

		/// <summary>
		/// Runs the <see cref="BeamContext.ClearAndDispose"/> method on every <see cref="BeamContext"/> in memory.
		/// </summary>
		public static async Promise ClearAndDisposeAllContexts()
		{
			foreach (var ctx in BeamContext.All)
			{
				await ctx.ClearAndDispose();
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
		public static async Promise ResetToScene(string sceneQualifier=null)
		{
			var loadAction = GetLoadSceneFunction(sceneQualifier);

			var totalScenesRequired = 0;
			var scenesDestroyed = 0;
			var allScenesUnloaded = new Promise();
			Action<AsyncOperation> Check(Scene unloadedScene)
			{
				return (_) => {
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


		private static void LoadCustomDependencies()
		{

			var registrations = new List<(RegisterBeamableDependenciesAttribute, MethodInfo)>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();


			foreach (var assembly in assemblies)
			{
				var asmName = assembly.GetName().Name;
				if ("Tests".Equals(asmName)) continue; // TODO think harder about this.
				try
				{
					foreach (var type in assembly.GetTypes())
					{
						var methodsWithAttr = type
						                      .GetMethods(BindingFlags.Static | BindingFlags.Public)
						                      .Where(m => {
							                      var methodParameters = m.GetParameters();
							                      return methodParameters.Length == 1 &&
							                             typeof(IDependencyBuilder).IsAssignableFrom(
								                             methodParameters[0].ParameterType);
						                      })
						                      .ToList();
						var possibleMethods = methodsWithAttr;
						foreach (var method in possibleMethods)
						{
							var attr = method.GetCustomAttribute<RegisterBeamableDependenciesAttribute>(false);
							if (attr == null) continue;

							registrations.Add((attr, method));
						}
					}
				}
				catch
				{
					// don't do anything.
				}
			}
			registrations.Sort( (a, b) => a.Item1.Order.CompareTo(b.Item1.Order));
			foreach (var registration in registrations)
			{
				registration.Item2.Invoke(null, new[] {Beam.DependencyBuilder});
			}
		}

		private static Action GetLoadSceneFunction(string sceneQualifier = null)
		{
			Action loadAction = () => { Debug.LogWarning("No scene could be identified to reload."); };
			if (sceneQualifier == null)
			{
				// load the activeScene, or if in Unity Editor, load
#if UNITY_EDITOR
				loadAction = () => {
					UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
						"Temp/__Backupscenes/0.backup", new LoadSceneParameters(LoadSceneMode.Single));
				};
#else
				var activeScene = SceneManager.GetActiveScene();
				loadAction = () => {
					SceneManager.LoadScene(activeScene.name, LoadSceneMode.Single);
				};
#endif
			}
			else if (int.TryParse(sceneQualifier, out var buildIndex))
			{
				loadAction = () => {
					SceneManager.LoadScene(buildIndex, LoadSceneMode.Single);
				};
			}
			else
			{
				loadAction = () => {
					SceneManager.LoadScene(sceneQualifier, LoadSceneMode.Single);
				};
			}

			return loadAction;
		}

	}
}
