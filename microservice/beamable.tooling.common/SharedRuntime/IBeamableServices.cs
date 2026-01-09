using Beamable.Common.Scheduler;
using Beamable.Server.Api;
using Beamable.Server.Api.Analytics;
using Beamable.Server.Api.Announcements;
using Beamable.Server.Api.Calendars;
using Beamable.Server.Api.Chat;
using Beamable.Server.Api.CloudData;
using Beamable.Server.Api.Commerce;
using Beamable.Server.Api.Content;
using Beamable.Server.Api.Events;
using Beamable.Server.Api.Groups;
using Beamable.Server.Api.Inventory;
using Beamable.Server.Api.Leaderboards;
using Beamable.Server.Api.Mail;
using Beamable.Server.Api.Notifications;
using Beamable.Server.Api.Payments;
using Beamable.Server.Api.RealmConfig;
using Beamable.Server.Api.Social;
using Beamable.Server.Api.Stats;
using Beamable.Server.Api.Tournament;

namespace Beamable.Server
{
	/// <summary>
	/// This type defines the %Microservice main entry point for %Beamable %Microservice features.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Server.Microservice script reference
	///
	/// ### Example
	/// This demonstrates example usage from WITHIN a custom %Beamable %Microservice.
	///
	/// ```
	/// [ClientCallable]
	/// private async void MyMicroserviceMethod()
	/// {
	///
	///   // Example usage
	///   var announcementsService = Services.Announcements;
	///   var result = await announcementsService.GetCurrent();
	///
	///   // Others...
	///   var AuthService = Services.Auth;
	///   var CalendarsService = Services.Calendars;
	///   var ContentService = Services.Content;
	///   var EventsService = Services.Events;
	///   var GroupsService = Services.Groups;
	///   var InventoryService = Services.Inventory;
	///   var LeaderboardsService = Services.Leaderboards;
	///   var RealmConfigurationService = Services.RealmConfig;
	///   var SocialService = Services.Social;
	///   var StatsService = Services.Stats;
	///   var TournamentService = Services.Tournament;
	///   var TrialDataService = Services.TrialData;
	///
	/// }
	///
	/// ```
	///
	///
	/// #### Alternative API Links
	/// - See Beamable.API for the main %Client script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public interface IBeamableServices
	{
		IMicroserviceAnalyticsService Analytics { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/live-ops/announcements-overview/">Announcements</a> feature
		/// </summary>
		IMicroserviceAnnouncementsApi Announcements { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/identity/identity/">Identity</a> feature
		/// </summary>
		IMicroserviceAuthApi Auth { get; }

		/// <summary>
		/// %Microservice entry point for the Calendars feature
		/// </summary>
		IMicroserviceCalendarsApi Calendars { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/content/content-overview/-overview">Content</a> feature
		/// </summary>
		IMicroserviceContentApi Content { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/live-ops/events-overview/">Events</a> feature
		/// </summary>
		IMicroserviceEventsApi Events { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/social-networking/groups/">Groups</a> feature
		/// </summary>
		IMicroserviceGroupsApi Groups { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/game-economy/inventory-overview/">Inventory</a> feature
		/// </summary>
		IMicroserviceInventoryApi Inventory { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/social-networking/leaderboards/">Leaderboards</a> feature
		/// </summary>
		IMicroserviceLeaderboardsApi Leaderboards { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/live-ops/mail-overview/">Mail</a> feature
		/// </summary>
		IMicroserviceMailApi Mail { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/live-ops/notifications-overview/">Notification</a> feature
		/// </summary>
		IMicroserviceNotificationsApi Notifications { get; }

		/// <summary>
		/// %Microservice entry point for the Realm Configuration feature
		/// </summary>
		IMicroserviceRealmConfigService RealmConfig { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/social-networking/overview/">Social</a> feature
		/// </summary>
		IMicroserviceSocialApi Social { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/stats/">Stats</a> feature
		/// </summary>
		IMicroserviceStatsApi Stats { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/social-networking/tournaments/">Tournaments</a> feature
		/// </summary>
		IMicroserviceTournamentApi Tournament { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/beamable-services/profile-storage/cloud-save/">Cloud Saving</a> feature
		/// </summary>
		IMicroserviceCloudDataApi TrialData { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://beam-api.readme.io/reference/post_basic-commerce-catalog-legacy">Commerce</a> feature
		/// </summary>
		IMicroserviceCommerceApi Commerce { get; }

		/// <summary>
		/// %Microservice entry point for the <a target="_blank" href="https://beam-api.readme.io/reference/get_object-chatv2-objectid-rooms">Chat</a> feature
		/// </summary>
		IMicroserviceChatApi Chat { get; }

		/// <summary>
		/// %Microservice entry point for payment operations.
		/// </summary>
		IMicroservicePaymentsApi Payments { get; }

		/// <summary>
		/// A <see cref="BeamScheduler"/> that can be used to schedule jobs for execution later.
		/// </summary>
		BeamScheduler Scheduler { get; }
	}
}
