using System;
using System.Collections.Generic;
using System.Linq;
using Beamable;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.EasyFeatures;
using UnityEngine;
using Beamable.Server.Clients;

namespace Beamable.EasyFeature.GameSpecificPlayerSystemArchitecture
{
	/// <summary>
	/// Enum defining the local client's draft state.
	/// </summary>
	public enum DraftStates
	{
		NotReady,
		AcceptDecline,
		WaitingForAcceptance,
		Banning,
		WaitingBanEnd,
		WaitingTurnToPick,
		Picking,
		WaitingDraftEnd,
		Ready,
		Declined,
	}

	/// <summary>
	/// This script is a fully functioning end-to-end testing facilitator that:
	/// <list type="bullet">
	/// <item>Has the ability to fake a match start with 10-logged users from the Unity Editor.</item>
	/// <item>Can render current draft states with a basic IMGUI-based UI.</item>
	/// <item>Allows selecting which player's "point of view" we are rendering.</item>
	/// <item>Demonstrates two separate ways of utilizing <see cref="BeamContext"/>: a Dependency-Injection way and a Explicit Initialization way.</item> 
	/// </list>
	///
	/// Primarily, this showcases how you can leverage <see cref="BeamContext"/> to mitigate the usual "death-by-a-thousand-cuts" problem in developing, testing and validating
	/// meta-game features that require multiple running instances to properly test (imagine that, for a 10-player draft, you would need 10 running instances to validate that it
	/// is working 100%).
	/// <para/>
	/// The ability to easily set something like this up can shave days, if not weeks, of development time over a project's lifecycle. And, with Beamable, it is trivial to setup.
	/// <para/>
	/// Keep in mind that, in a real project, you would never implement both <see cref="MatchStartSystemV1"/> and <see cref="MobaDraftPlayerSystem"/>. Ideally, you would pick one of 
	/// these approaches to managing the lifecycle of your systems and stick to it. Since these have advantages and disadvantages to them, Beamable enables and encourages you to
	/// think which is best for the context of your project/team and go with that. 
	/// </summary>
	public class MobaDraftLocalTestFeatureControl : MonoBehaviour
	
	{
		/// <summary>
		/// This <see cref="BeamContext"/> is used as a pretend server. To implement a draft, we'd need a "Beamable Webhook/API as Content" setup to be called by our backend
		/// whenever a match is found, but before the clients are notified of the match. So we use a <see cref="BeamContext"/> to fake the Webhook call that would set up the
		/// draft system's data.
		/// </summary>
		public BeamContext FakeServer;

		/// <summary>
		/// Array of <see cref="BeamContext"/> representing each logged user that will participate in the draft.
		/// These are automatically injected with <see cref="MobaDraftPlayerSystem"/> via to <see cref="LoMobaDraftPlayerSystemegisterService"/>.
		/// This means that, tied to each of the authenticated users inside this context, we have a <see cref="MobaDraftPlayerSystem"/>.
		/// <para/>
		/// This <see cref="MobaDraftPlayerSystem"/> is only used if <see cref="UseBeamContextVersion"/> is set to true.
		/// Otherwise, we use the <see cref="MatchStartSystems"/> array.
		/// </summary>
		[SerializeField] private BeamContext[] ActiveContexts;

		/// <summary>
		/// A <see cref="ContentRef{TContent}"/> to the <see cref="ContentObject"/> that defines the draft rules (see <see cref="DraftSimType"/> for details on said rules).
		/// </summary>
		public DraftSimTypeRef DraftRulesRef;

		/// <summary>
		/// When 0 or greater, the index for the player currently being rendered. -1, to render all players sequentially (easily achievable with IMGUI).
		/// </summary>
		public int RenderingPlayerOverride = -1;

		//TODO: Commment
		public BeamableViewGroup FeatureViewGroup;
		public MobaDraftAcceptanceView AcceptanceView;
		public MobaDraftView DraftView;


		/// <summary>
		/// Flag that defines we are ready to start a match --- waits for all fake users to be logged in and ready to go before being set to true. 
		/// </summary>
		[SerializeField] private bool ReadyToStart;

		/// <summary>
		/// Flag set to true when clicking on the "Fake Force Match Found"-button. Let's all logged users start the draft process as they would through the webhook.
		/// </summary>
		[SerializeField] private bool MatchFound;

		/// <summary>
		/// List of fake int character ids for us to draft.
		/// </summary>
		[SerializeField] private List<int> CharacterIds = Enumerable.Range(1, 20).ToList();

		/// <summary>
		/// Scroll position of the IMGUI-based UI.
		/// </summary>
		private Vector2 _scrollPos;

		/// <summary>
		/// Async Awake Unity-Event-Function that:
		/// <list type="number">
		/// <item>Initializes the <see cref="FakeServer"/> context.</item>
		/// <item>Resolves the <see cref="DraftRulesRef"/> so that we know the amount of players to run the draft with.</item>
		/// <item>Initializes the <see cref="ActiveContexts"/> and <see cref="MatchStartSystems"/> arrays based on the resolved <see cref="DraftRulesRef"/>.</item>
		/// <item>Create the <see cref="ActiveContexts"/> and wait for the login of each user to be made.</item>
		/// <item>After each user is logged in, initialize the <see cref="MatchStartSystemV1"/>s explicitly with the required information.</item>
		/// <item>After each user is logged in, also checks if it is the last user that we were expecting to login. If it is, we set the test script as <see cref="ReadyToStart"/>.</item>
		/// </list>
		///
		/// This process is a simple way of initializing an arbitrary number of <see cref="BeamContext"/> and Beamable Users that you can use to easily create an in-Editor environment
		/// for testing complex multiple user features including but not limited to:
		///
		/// <list type="bullet">
		/// <item>Draft systems and other Match Preparation systems (Character select screens, challenge accept/refusal, character match setup (like runes in League), etc...).</item>
		/// <item>Custom Group Systems (Guild levels, guild ranks, guild voting systems, etc...), Lobbies and party systems, etc...</item>
		/// <item>So on and so forth...</item>
		/// </list>
		/// </summary>
		private async void Awake()
		{
			FakeServer = BeamContext.Default;

			var draftSimType = await DraftRulesRef.Resolve();
			var playerCount = draftSimType.TeamSize * 2;
			ActiveContexts = new BeamContext[playerCount];

			for (var i = 0; i < playerCount; i++)
			{
				ActiveContexts[i] = BeamContext.ForPlayer($"Player-{i}");
				ActiveContexts[i].OnUserLoggedIn += _ => ReadyToStart = ActiveContexts.All(ctx => ctx.AuthorizedUser.IsAssigned);
			}

			FeatureViewGroup = FindObjectOfType<BeamableViewGroup>();
			AcceptanceView = FindObjectOfType<MobaDraftAcceptanceView>();
			DraftView = FindObjectOfType<MobaDraftView>();
			
			FeatureViewGroup.RebuildManagedViews(new ISyncBeamableView[]{ AcceptanceView, DraftView });
		}

		/// <summary>
		/// Update Unity event function that handles polling of the draft system.
		/// This manual polling will be made unnecessary once we enable Microservices to send messages to clients like our own platform does.
		/// </summary>
		private void Update()
		{
			// Do nothing if the required systems for the test are not yet initialized.
			if (!ReadyToStart) return;

			// Gets all MatchStartSystems that were initialized via BeamContext's Dependency injection framework.
			var matchServices = ActiveContexts.Select(c => c.ServiceProvider.GetService<MobaDraftPlayerSystem>()).ToList();

			// Update the MatchFound to be true only if all players have already found a match
			// This is just to make it easier to synchronize the render the IMGUI UI prior to the match being started.
			MatchFound = matchServices.Any(s => s.CurrentDraftState != DraftStates.NotReady);
		}

		/// <summary>
		/// Unity OnGUI event function for rendering the state of the draft.
		/// You could easily make a prefab-based flow following the same principles.
		/// </summary>
		private void OnGUI()
		{
			// If we are not ready to start (if all players required for the test are not logged in), we wait until they are.
			if (!ReadyToStart)
			{
				GUILayout.Label("Initializing Fake Server and Logging in Players...");
				return;
			}

			// Once we are ready to start, we render a button that, when clicked, will replicate the "Match Found"-webhook + "Match Found" callback we
			// would get from the Matchmaking service.
			if (!MatchFound)
			{
				if (GUILayout.Button("Force Match Found (Fake)")) StartMatch();
				return;
			}

			// Render some buttons to make it easy to swap rendered players. Pay attention to the ID numbers of the rendered players, you can see changes as you cycle
			// through the logged user's point of views. You can also clearly see this during the pick phase of the draft.
			GUILayout.BeginHorizontal();
			if (GUILayout.Button($"Render - All")) RenderingPlayerOverride = -1;
			for (var i = 0; i < ActiveContexts.Length; i++)
			{
				if (GUILayout.Button($"Render - {i}"))
					RenderingPlayerOverride = i;
			}

			GUILayout.EndHorizontal();

			// Starts a scroll view area for us to render all players' point of view (or just the selected one). 
			_scrollPos = GUILayout.BeginScrollView(_scrollPos);

			// If we want to look at a single player, render only the given player.
			if (RenderingPlayerOverride != -1)
			{
				// Keep rendering overrides within the render-able clients.
				RenderingPlayerOverride = Mathf.Clamp(RenderingPlayerOverride, 0, ActiveContexts.Length - 1);
				var renderingContext = ActiveContexts[RenderingPlayerOverride];
				_ = FeatureViewGroup.EnrichWithPlayerCodes(new List<string>() {renderingContext.PlayerCode});
				AcceptanceView.RenderOnGUI();
				DraftView.RenderOnGUI();
				
				// Close the scroll view and early out.
				GUILayout.EndScrollView();
				return;
			}

			// If we are not rendering a specific-player's point of view, render all players sequentially.
			foreach (var ctx in ActiveContexts)
			{
				_ = FeatureViewGroup.EnrichWithPlayerCodes(new List<string>() {ctx.PlayerCode});
				AcceptanceView.RenderOnGUI();
				DraftView.RenderOnGUI();
			}

			GUILayout.EndScrollView();
		}

		/// <summary>
		/// Cleans up all <see cref="BeamContext"/>s when this script is destroyed.
		/// </summary>
		private async void OnDestroy()
		{
			await FakeServer.ClearPlayerAndStop();
			foreach (var activeContext in ActiveContexts) await activeContext.ClearPlayerAndStop();
		}

		/// <summary>
		/// Helper function to "pretend" that a Matchmaking callback was received and that it had a Webhook pointing to the OnMatchMade function of the MatchPreparationService Microservice. 
		/// </summary>
		private async void StartMatch()
		{
			var playerIds = ActiveContexts.Select(ctx => ctx.PlayerId).ToArray();
			var matchId = Guid.NewGuid().ToString();

			await FakeServer.Microservices().GetClient<MatchPreparationServiceClient>()
			                .OnMatchMade(matchId, DraftRulesRef.Id, playerIds);

			MatchFound = true;

			// Gets the MatchStartSystemV2 for all logged players
			var matchServices = ActiveContexts.Select(c => c.ServiceProvider.GetService<MobaDraftPlayerSystem>()).ToList();

			// KickOff the MatchStartProcess in the client for each of those players
			foreach (var matchStartSystemV2 in matchServices) 
				matchStartSystemV2.StartMatchProcessKickOff(matchId, DraftRulesRef.Id, CharacterIds);
		}
	}
}
