using Beamable.AccountManagement;
using Beamable.Api;
using Beamable.Api.Analytics;
using Beamable.Api.Announcements;
using Beamable.Api.Auth;
using Beamable.Api.Caches;
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
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Auth;
using Beamable.Common.Api.CloudData;
using Beamable.Common.Api.Notifications;
using Beamable.Common.Api.Tournaments;
using Beamable.Common.Player;
using Beamable.Config;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Experimental;
using Beamable.Player;
using Beamable.Service;
using Beamable.Sessions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
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
	/// This is the legacy way to access Beamable. It will still work, and internally maps to the new way.
	/// However, please use the new way, but accessing <see cref="BeamContext.Default"/>
	/// ![img beamable-logo]
	///
	/// </summary>
	public class API //: IBeamableAPI
	{
		private static Promise<IBeamableAPI> _instance;

		/// <summary>
		/// This is deprecated. Please use <see cref="BeamContext.Default"/>
		/// </summary>
		public static Promise<IBeamableAPI> Instance
		{
			get
			{
#if UNITY_EDITOR
	            if (_instance != null) return _instance;
#endif

				return BeamContext.Default.OnReady.Map<IBeamableAPI>(_ => BeamContext.Default.Api);
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
