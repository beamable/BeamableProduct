using Beamable.Api;
using Beamable.Api.Analytics;
using Beamable.Api.Announcements;
using Beamable.Api.Auth;
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
using Beamable.Content;
using Beamable.Experimental;
using Beamable.Player;
using System;
using System.Collections.Generic;
#if BEAMABLE_PURCHASING
using Beamable.Purchasing;
#endif

namespace Beamable
{

	/// <summary>
	/// This interface represents a collection of Beamable APIs and data structures.
	/// This type defines the %Client main entry point for the main %Beamable features.
	///
	/// #### Related Links
	/// - See Beamable.API script reference
	///
	/// <inheritdoc cref="Beamable.Common.Docs.Logo"/>
	/// </summary>
	public interface IBeamableAPI
	{
		/// <summary>
		/// The currently signed in <see cref="User"/> for this <see cref="IBeamableAPI"/> instance.
		/// </summary>
		User User { get; }

		/// <summary>
		/// The <see cref="AccessToken"/> for the <see cref="User"/> object's account.
		/// </summary>
		AccessToken Token { get; }

		/// <summary>
		/// An event that will trigger anytime the <see cref="User"/> for this <see cref="IBeamableAPI"/> instance changes.
		/// It can change due to user log out, log in, account switch, or whenever a user attaches a new credential to their account.
		/// </summary>
		event Action<User> OnUserChanged;

		/// <summary>
		/// An event that will trigger anytime the <see cref="User"/> for this <see cref="IBeamableAPI"/> instance logs out.
		/// </summary>
		event Action<User> OnUserLoggingOut;

		/// <summary>
		/// Access experimental features of Beamable.
		/// <b> Services from this accessor may be subject to change </b>
		/// </summary>
		IExperimentalAPI Experimental { get; }

		/// <summary>
		/// Access the <see cref="AnnouncementService"/> for this player instance.
		/// </summary>
		AnnouncementsService AnnouncementService { get; }

		/// <summary>
		/// Access the <see cref="IAuthService"/> for this player instance.
		/// </summary>
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
	public class API
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
