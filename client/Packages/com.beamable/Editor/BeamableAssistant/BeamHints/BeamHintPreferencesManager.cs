using Beamable.Common.Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Beamable.Editor.Assistant
{
	/// <summary>
	/// Manages and persists <see cref="BeamHint"/> preferences. Can decide to display/ignore hints and persist this configuration this session or permanently. 
	/// </summary>
	public class BeamHintPreferencesManager : IBeamHintPreferencesManager
	{
		/// <summary>
		/// Key into both <see cref="SessionState"/> and <see cref="EditorPrefs"/> to store number of <see cref="BeamHint"/> states persisted in each of these.
		/// Used to allocate once at startup and avoid continuous resize allocations mid-loop.
		/// </summary>
		private const string VISIBILITY_HIDDEN_SAVED_COUNT = "BEAM_HINT_"+nameof(VISIBILITY_HIDDEN_SAVED_COUNT);

		/// <summary>
		/// Key into both <see cref="SessionState"/> and <see cref="EditorPrefs"/> to store the <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list
		/// of <see cref="BeamHintHeaders"/> states persisted in each prefs.
		/// </summary>
		private const string VISIBILITY_HIDDEN_SAVED = "BEAM_HINT_"+nameof(VISIBILITY_HIDDEN_SAVED);
		
		/// <summary>
		/// Key into both <see cref="SessionState"/> and <see cref="EditorPrefs"/> to store number of <see cref="BeamHint"/> states persisted in each of these.
		/// Used to allocate once at startup and avoid continuous resize allocations mid-loop.
		/// </summary>
		private const string PLAY_MODE_WARNING_DISABLED_SAVED_COUNT = "BEAM_HINT_"+nameof(PLAY_MODE_WARNING_DISABLED_SAVED_COUNT);

		/// <summary>
		/// Key into both <see cref="SessionState"/> and <see cref="EditorPrefs"/> to store the <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list
		/// of <see cref="BeamHintHeaders"/> states persisted in each prefs.
		/// </summary>
		private const string PLAY_MODE_WARNING_DISABLED_SAVED = "BEAM_HINT_"+nameof(PLAY_MODE_WARNING_DISABLED_SAVED);
		
		/// <summary>
		/// Key into <see cref="EditorPrefs"/> to store number of <see cref="BeamHint"/> states persisted to always notify.
		/// Used to allocate once at startup and avoid continuous resize allocations mid-loop.
		/// </summary>
		private const string NOTIFICATION_ALWAYS_SAVED_COUNT = "BEAM_HINT_"+nameof(NOTIFICATION_ALWAYS_SAVED_COUNT);

		/// <summary>
		/// Key into <see cref="EditorPrefs"/> to store the <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list of hints that are configured to always notify.
		/// </summary>
		private const string NOTIFICATION_ALWAYS_SAVED = "BEAM_HINT_"+nameof(NOTIFICATION_ALWAYS_SAVED);
		
		/// <summary>
		/// Key into <see cref="EditorPrefs"/> to store number of <see cref="BeamHint"/> states persisted to always notify.
		/// Used to allocate once at startup and avoid continuous resize allocations mid-loop.
		/// </summary>
		private const string NOTIFICATION_NEVER_SAVED_COUNT = "BEAM_HINT_"+nameof(NOTIFICATION_NEVER_SAVED_COUNT);

		/// <summary>
		/// Key into <see cref="EditorPrefs"/> to store the <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list of hints that are configured to always notify.
		/// </summary>
		private const string NOTIFICATION_NEVER_SAVED = "BEAM_HINT_"+nameof(NOTIFICATION_NEVER_SAVED);
		

		/// <summary>
		/// Current state of each <see cref="BeamHint"/>. Mapped by the <see cref="BeamHintHeader"/>.
		/// Any not found <see cref="BeamHintHeader"/> is presumed to be with the following state: <see cref="VisibilityState.Display"/>.
		/// </summary>
		private readonly Dictionary<BeamHintHeader, VisibilityState> _perHintVisibilityStates;

		/// <summary>
		/// List of all header's currently ignored in this <see cref="SessionState"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _sessionVisibilityIgnoredHints;

		/// <summary>
		/// List of all header's currently ignored in this <see cref="EditorPrefs"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _permanentlyVisibilityIgnoredHints;

		
		/// <summary>
		/// Current state of each <see cref="BeamHint"/>. Mapped by the <see cref="BeamHintHeader"/>.
		/// Any not found <see cref="BeamHintHeader"/> is presumed to be with the following state: <see cref="PlayModeWarningState.Enabled"/>.
		/// </summary>
		private readonly Dictionary<BeamHintHeader, PlayModeWarningState> _perHintPlayModeWarningStates;

		/// <summary>
		/// List of all header's currently disabled <see cref="PlayModeWarningState"/> in this <see cref="SessionState"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _sessionPlayModeWarningDisabledHints;

		/// <summary>
		/// List of all header's currently disabled <see cref="PlayModeWarningState"/> in this <see cref="EditorPrefs"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _permanentlyPlayModeWarningDisabledHints;
		
		
		/// <summary>
		/// Current state of each <see cref="BeamHint"/>. Mapped by the <see cref="BeamHintHeader"/>.
		/// Any not found <see cref="BeamHintHeader"/> is presumed to be with the following states:
		/// <para>
		///  - If <see cref="BeamHintType.Hint"/>, the default is <see cref="BeamHintNotificationState.NotifyOncePerSession"/>.
		/// </para>
		/// <para>
		///  - If <see cref="BeamHintType.Validation"/>, the default is <see cref="BeamHintNotificationState.NotifyOnContextObjectChanged"/>. This assumes that validation hints
		/// change their context objects if they ever update a hint. 
		/// </para>
		/// </summary>
		private readonly Dictionary<BeamHintHeader, BeamHintNotificationState> _perHintNotificationStates;
		
		/// <summary>
		/// List of all header's currently set to <see cref="BeamHintNotificationState.NotifyAlways"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _alwaysNotifyHints;
		
		/// <summary>
		/// List of all header's currently set to <see cref="BeamHintNotificationState.NotifyNever"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _neverNotifyHints;

		private readonly List<string> _hintsToPlayModeWarningByDefault;

		/// <summary>
		/// Creates a new <see cref="BeamHintPreferencesManager"/> instance you can use to manage <see cref="BeamHint"/> display/ignore preferences.
		/// </summary>
		public BeamHintPreferencesManager(List<string> playModeWarningByDefaultHints = null)
		{
			var sessionVisibilityPrefsCount = SessionState.GetInt(VISIBILITY_HIDDEN_SAVED_COUNT, 0);
			var hintVisibilityPrefsCount = EditorPrefs.GetInt(VISIBILITY_HIDDEN_SAVED_COUNT, 0);
			_perHintVisibilityStates = new Dictionary<BeamHintHeader, VisibilityState>(sessionVisibilityPrefsCount + hintVisibilityPrefsCount);

			_sessionVisibilityIgnoredHints = new List<BeamHintHeader>(sessionVisibilityPrefsCount);
			_permanentlyVisibilityIgnoredHints = new List<BeamHintHeader>(hintVisibilityPrefsCount);
			
			
			var sessionPlayModeWarningPrefsCount = SessionState.GetInt(PLAY_MODE_WARNING_DISABLED_SAVED_COUNT, 0);
			var hintPlayModeWarningPrefsCount = EditorPrefs.GetInt(PLAY_MODE_WARNING_DISABLED_SAVED_COUNT, 0);
			_perHintPlayModeWarningStates = new Dictionary<BeamHintHeader, PlayModeWarningState>(sessionPlayModeWarningPrefsCount + hintPlayModeWarningPrefsCount);

			_sessionPlayModeWarningDisabledHints = new List<BeamHintHeader>(sessionPlayModeWarningPrefsCount);
			_permanentlyPlayModeWarningDisabledHints = new List<BeamHintHeader>(hintPlayModeWarningPrefsCount);
			
			
			var alwaysNotifyCount = EditorPrefs.GetInt(NOTIFICATION_ALWAYS_SAVED_COUNT, 0);
			var neverNotifyCount = EditorPrefs.GetInt(NOTIFICATION_NEVER_SAVED_COUNT, 0);
			_perHintNotificationStates = new Dictionary<BeamHintHeader, BeamHintNotificationState>(alwaysNotifyCount + neverNotifyCount);

			_alwaysNotifyHints = new List<BeamHintHeader>(alwaysNotifyCount);
			_neverNotifyHints = new List<BeamHintHeader>(neverNotifyCount);

			_hintsToPlayModeWarningByDefault = new List<string>();
			_hintsToPlayModeWarningByDefault.AddRange(playModeWarningByDefaultHints ?? new List<string>());
		}

		/// <summary>
		/// Restores the current in-memory state of the <see cref="BeamHintPreferencesManager"/> to match what is stored in its persistent storages.
		/// </summary>
		public void RebuildPerHintPreferences()
		{
			// Rebuild Visibility preferences
			_perHintVisibilityStates.Clear();
			_sessionVisibilityIgnoredHints.Clear();
			_permanentlyVisibilityIgnoredHints.Clear();

			// Go through editor prefs to get all permanently silenced hints
			var permanentSilencedHints = EditorPrefs.GetString(VISIBILITY_HIDDEN_SAVED, "");
			ApplyStoredHintPreferences(permanentSilencedHints, VisibilityState.Hidden, _perHintVisibilityStates, _permanentlyVisibilityIgnoredHints);

			// Go through session state to get all silenced hints for this session
			var sessionSilencedHints = SessionState.GetString(VISIBILITY_HIDDEN_SAVED, "");
			ApplyStoredHintPreferences(sessionSilencedHints, VisibilityState.Hidden, _perHintVisibilityStates, _sessionVisibilityIgnoredHints);
			
			
			// Rebuild Play-Mode-Warning preferences
			_perHintPlayModeWarningStates.Clear();
			_sessionPlayModeWarningDisabledHints.Clear();
			_permanentlyPlayModeWarningDisabledHints.Clear();

			// Go through editor prefs to get all permanently play-mode-warning disabled hints
			var permanentDisabledPlayModeWarningHints = EditorPrefs.GetString(PLAY_MODE_WARNING_DISABLED_SAVED, "");
			ApplyStoredHintPreferences(permanentDisabledPlayModeWarningHints, PlayModeWarningState.Disabled, _perHintPlayModeWarningStates, _permanentlyPlayModeWarningDisabledHints);

			// Go through session state to get all play-mode-warning disabled hints for this session
			var sessionDisabledPlayModeWarningHints = SessionState.GetString(PLAY_MODE_WARNING_DISABLED_SAVED, "");
			ApplyStoredHintPreferences(sessionDisabledPlayModeWarningHints, PlayModeWarningState.Disabled, _perHintPlayModeWarningStates, _sessionPlayModeWarningDisabledHints);
			
			// Rebuild Notification preferences
			_perHintNotificationStates.Clear();
			_alwaysNotifyHints.Clear();
			_neverNotifyHints.Clear();
			
			// Go through stored notification preferences set as NotifyAlways. 
			var alwaysNotificationHints = EditorPrefs.GetString(NOTIFICATION_ALWAYS_SAVED, "");
			ApplyStoredHintPreferences(alwaysNotificationHints, BeamHintNotificationState.NotifyAlways, _perHintNotificationStates, _alwaysNotifyHints);
			
			// Go through stored notification preferences set as NotifyNever. 
			var neverNotificationHints = EditorPrefs.GetString(NOTIFICATION_NEVER_SAVED, "");
			ApplyStoredHintPreferences(neverNotificationHints, BeamHintNotificationState.NotifyNever, _perHintNotificationStates, _neverNotifyHints);
			
			
		}
		
		/// <summary>
		/// /// <summary>
		/// Deserializes and stores <see cref="BeamHintHeader"/> and <typeparamref name="T"/> from a serialized string of <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated
		/// <see cref="BeamHintHeader"/>s and the given state for it. Stores the results both in <paramref name="outHintStateStore"/> and <paramref name="outPerStateList"/>.
		/// </summary>
		///
		/// </summary>
		/// <param name="savedSerializedHeaders">
		/// The <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list of
		/// <see cref="BeamHintHeader"/>s (via <see cref="BeamHintHeader.AsKey"/>).
		/// </param>
		/// 
		/// <param name="stateToRestore">
		/// The state to apply to all deserialized <paramref name="savedSerializedHeaders"/>.
		/// </param>
		///
		/// <param name="outHintStateStore">
		/// A dictionary to store the combination of deserialized <paramref name="savedSerializedHeaders"/> and <paramref name="stateToRestore"/>. 
		/// </param>
		/// <param name="outPerStateList">
		/// A list to add to the deserialized <paramref name="savedSerializedHeaders"/> into. 
		/// </param>
		/// <typeparam name="T">An enum defining the state of preferences for a given hint.</typeparam>
		private void ApplyStoredHintPreferences<T>(string savedSerializedHeaders,
		                                        T stateToRestore,
		                                        Dictionary<BeamHintHeader, T> outHintStateStore,
		                                        List<BeamHintHeader> outPerStateList) where T : Enum
		{
			var savedSerializedHeadersArray = savedSerializedHeaders.Split(new[] {BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string serializedHeader in savedSerializedHeadersArray)
			{
				var header = BeamHintHeader.DeserializeBeamHintHeader(serializedHeader);
				outHintStateStore.Add(header, stateToRestore);
				outPerStateList.Add(header);
			}
		}


		/// <summary>
		/// Splits all given hints by their <see cref="VisibilityState"/>s.
		/// </summary>
		/// <param name="hints">The hints to split by.</param>
		/// <param name="outToDisplayHints">The resulting list of <see cref="BeamHint"/>s that should be displayed.</param>
		/// <param name="outToIgnoreHints">The resulting list of <see cref="BeamHint"/>s that should be ignored.</param>
		public void SplitHintsByVisibilityPreferences(IEnumerable<BeamHint> hints, out IEnumerable<BeamHint> outToDisplayHints, out IEnumerable<BeamHint> outToIgnoreHints)
		{
			var groups = hints.GroupBy(h =>
			{
				if (!_perHintVisibilityStates.TryGetValue(h.Header, out var state))
					state = VisibilityState.Display;

				return state;
			}).ToList();

			outToDisplayHints = groups.Where(h => h.Key == VisibilityState.Display)
			                          .SelectMany(h => h);
			
			outToIgnoreHints = groups.Where(h => h.Key == VisibilityState.Hidden)
			                          .SelectMany(h => h);
		}
		
		/// <summary>
		/// Splits all given hints by their <see cref="PlayModeWarningState"/>s.
		/// </summary>
		/// <param name="hints">The hints to split by.</param>
		/// <param name="outToWarnHints">The resulting list of <see cref="BeamHint"/>s that should cause a play-mode-warning.</param>
		/// <param name="outToIgnoreHints">The resulting list of <see cref="BeamHint"/>s that should cause a play-mode-warning.</param>
		public void SplitHintsByPlayModeWarningPreferences(IEnumerable<BeamHint> hints, out IEnumerable<BeamHint> outToWarnHints, out IEnumerable<BeamHint> outToIgnoreHints)
		{
			var groups = hints.GroupBy(h => {

				if (!_perHintPlayModeWarningStates.TryGetValue(h.Header, out var state))
				{
					state = _hintsToPlayModeWarningByDefault.Contains(h.Header.Id) ? PlayModeWarningState.Enabled : PlayModeWarningState.Disabled;
				}

				return state;
			}).ToList();

			outToWarnHints = groups.Where(h => h.Key == PlayModeWarningState.Enabled)
			                          .SelectMany(h => h);
			
			outToIgnoreHints = groups.Where(h => h.Key == PlayModeWarningState.Disabled)
			                         .SelectMany(h => h);
		}

		/// <summary>
		/// Splits all given hints by their <see cref="PlayModeWarningState"/>s.
		/// </summary>
		/// <param name="hints">The hints to split by.</param>
		/// <param name="outToNotifyAlways">The resulting list of <see cref="BeamHint"/>s that should always notify.</param>
		/// <param name="outToNotifyNever">The resulting list of <see cref="BeamHint"/>s that should never notify.</param>
		/// <param name="outToNotifyOncePerSession">The resulting list of <see cref="BeamHint"/>s that should notify only once per session.</param>
		/// <param name="outToNotifyOnContextObjectChange">The resulting list of <see cref="BeamHint"/>s that should notify whenever the context object changed.</param>
		public void SplitHintsByNotificationPreferences(IEnumerable<BeamHint> hints,
		                                                out IEnumerable<BeamHint> outToNotifyAlways,
		                                                out IEnumerable<BeamHint> outToNotifyNever,
		                                                out IEnumerable<BeamHint> outToNotifyOncePerSession,
		                                                out IEnumerable<BeamHint> outToNotifyOnContextObjectChange)
		{
			var groups = hints.GroupBy(h => {

				if (!_perHintNotificationStates.TryGetValue(h.Header, out var state))
				{
					switch (h.Header.Type)
					{
						case BeamHintType.Hint:
						{
							state = BeamHintNotificationState.NotifyOncePerSession;
							break;
						}
						case BeamHintType.Validation:
						{
							state = BeamHintNotificationState.NotifyOnContextObjectChanged;
							break;
						}
						default:
							throw new ArgumentOutOfRangeException();
					}
				}

				return state;
			}).ToList();

			outToNotifyAlways = groups.Where(h => h.Key == BeamHintNotificationState.NotifyAlways)
			                          .SelectMany(h => h);
			
			outToNotifyNever = groups.Where(h => h.Key == BeamHintNotificationState.NotifyNever)
			                         .SelectMany(h => h);
			
			outToNotifyOncePerSession = groups.Where(h => h.Key == BeamHintNotificationState.NotifyOncePerSession)
			                                  .SelectMany(h => h);
			
			outToNotifyOnContextObjectChange = groups.Where(h => h.Key == BeamHintNotificationState.NotifyOnContextObjectChanged)
			                                         .SelectMany(h => h);
		}

		/// <summary>
		/// Sets, for the given <paramref name="hint"/>, the given <paramref name="newVisibilityState"/> at the specified <paramref name="persistenceLevel"/>.
		/// Persistence levels are 100% independent, it is up to the caller to add/remove from both independently.
		/// </summary>
		public void SetHintVisibilityPreferences(BeamHint hint, VisibilityState newVisibilityState, PersistenceLevel persistenceLevel)
		{
			if (!_perHintVisibilityStates.TryGetValue(hint.Header, out var currState))
			{
				_perHintVisibilityStates.Add(hint.Header, newVisibilityState);
				currState = newVisibilityState;
			}

			if (persistenceLevel == PersistenceLevel.Instance)
				return;

			List<BeamHintHeader> persistenceHintHelperList;
			switch (persistenceLevel)
			{
				case PersistenceLevel.Session:
					persistenceHintHelperList = _sessionVisibilityIgnoredHints;
					break;
				case PersistenceLevel.Permanent:
					persistenceHintHelperList = _permanentlyVisibilityIgnoredHints;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(persistenceLevel));
			}
			
			if (currState == VisibilityState.Hidden)
				SetSerializedHintPreference(hint,
				                            VISIBILITY_HIDDEN_SAVED,
				                            VISIBILITY_HIDDEN_SAVED_COUNT,
				                            persistenceLevel,
				                            persistenceHintHelperList);

			if (currState == VisibilityState.Display)
				RemoveHintPreferenceState(hint,
				                            VISIBILITY_HIDDEN_SAVED,
				                            VISIBILITY_HIDDEN_SAVED_COUNT,
				                            persistenceLevel,
				                            persistenceHintHelperList);
		}


		/// <summary>
		/// Sets, for the given <paramref name="hint"/>, the given <paramref name="newPlayModeWarningState"/> at the specified <paramref name="persistenceLevel"/>.
		/// Persistence levels are 100% independent, it is up to the caller to add/remove from both independently.
		/// </summary>
		public void SetHintPlayModeWarningPreferences(BeamHint hint, PlayModeWarningState newPlayModeWarningState, PersistenceLevel persistenceLevel)
		{
			if (!_perHintPlayModeWarningStates.TryGetValue(hint.Header, out var currState))
			{
				_perHintPlayModeWarningStates.Add(hint.Header, newPlayModeWarningState);
				currState = newPlayModeWarningState;
			}

			if (persistenceLevel == PersistenceLevel.Instance)
				return;

			List<BeamHintHeader> persistenceHintHelperList;
			switch (persistenceLevel)
			{
				case PersistenceLevel.Session:
					persistenceHintHelperList = _sessionPlayModeWarningDisabledHints;
					break;
				case PersistenceLevel.Permanent:
					persistenceHintHelperList = _permanentlyPlayModeWarningDisabledHints;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(persistenceLevel));
			}
			
			if (currState == PlayModeWarningState.Disabled)
				SetSerializedHintPreference(hint,
				                            PLAY_MODE_WARNING_DISABLED_SAVED,
				                            PLAY_MODE_WARNING_DISABLED_SAVED_COUNT,
				                            persistenceLevel, 
				                            persistenceHintHelperList);

			if (currState == PlayModeWarningState.Enabled)
				RemoveHintPreferenceState(hint,
				                          PLAY_MODE_WARNING_DISABLED_SAVED,
				                          PLAY_MODE_WARNING_DISABLED_SAVED_COUNT,
				                          persistenceLevel, 
				                          persistenceHintHelperList);
		}

		/// <summary>
		/// Update the <see cref="BeamHintNotificationState"/> for a given hint. These are always set permanently (you can't disable them for just this session). 
		/// </summary>
		/// <param name="hint">The hint whose <see cref="BeamHintNotificationState"/> you want to set.</param>
		/// <param name="newNotificationState">The <see cref="BeamHintNotificationState"/> to set.</param>
		public void SetHintNotificationPreferences(BeamHint hint, BeamHintNotificationState newNotificationState)
		{
			if (!_perHintNotificationStates.TryGetValue(hint.Header, out var currState))
			{
				_perHintNotificationStates.Add(hint.Header, newNotificationState);
				currState = newNotificationState;
			}

			switch (currState)
			{
				case BeamHintNotificationState.NotifyOncePerSession:
				case BeamHintNotificationState.NotifyOnContextObjectChanged:
				{
					RemoveHintPreferenceState(hint, NOTIFICATION_ALWAYS_SAVED, NOTIFICATION_ALWAYS_SAVED_COUNT, PersistenceLevel.Permanent, _alwaysNotifyHints);
					RemoveHintPreferenceState(hint, NOTIFICATION_NEVER_SAVED, NOTIFICATION_NEVER_SAVED_COUNT, PersistenceLevel.Permanent, _neverNotifyHints);
					break;
				}
				case BeamHintNotificationState.NotifyAlways:
				{
					RemoveHintPreferenceState(hint, NOTIFICATION_NEVER_SAVED, NOTIFICATION_NEVER_SAVED_COUNT, PersistenceLevel.Permanent, _neverNotifyHints);
					SetSerializedHintPreference(hint, NOTIFICATION_ALWAYS_SAVED, NOTIFICATION_ALWAYS_SAVED_COUNT, PersistenceLevel.Permanent, _alwaysNotifyHints);
					break;
				}
				case BeamHintNotificationState.NotifyNever:
				{
					RemoveHintPreferenceState(hint, NOTIFICATION_ALWAYS_SAVED, NOTIFICATION_ALWAYS_SAVED_COUNT, PersistenceLevel.Permanent, _alwaysNotifyHints);
					SetSerializedHintPreference(hint, NOTIFICATION_NEVER_SAVED, NOTIFICATION_NEVER_SAVED_COUNT, PersistenceLevel.Permanent, _neverNotifyHints);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		
		
		/// <summary>
		/// Removes the given <paramref name="hint"/> from it's given <paramref name="persistenceLevel"/> while updating a helper per-state lists to make it easier to manage the
		/// string-based <see cref="EditorPrefs"/> and <see cref="SessionState"/>.
		/// <param name="hint">The hint to remove the preference for.</param>
		/// <param name="preferencesKey">The key to store the preferences state in.</param>
		/// <param name="preferencesCountKey">The key to store the preferences state count in.</param>
		/// <param name="persistenceLevel">Whether to save the updated preferences using <see cref="SessionState"/> or <see cref="EditorPrefs"/>.</param>
		/// <param name="outHints">
		/// Helper list of headers for the preference you are persisting.
		/// Caller should pass correct list based on <paramref name="persistenceLevel"/>.
		/// </param>
		private void RemoveHintPreferenceState(BeamHint hint, 
		                                       string preferencesKey,
		                                       string preferencesCountKey, 
		                                       PersistenceLevel persistenceLevel, 
		                                       List<BeamHintHeader> outHints)
		{
			outHints.Remove(hint.Header);
			var serializedPreferences = string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, outHints);
			
			switch (persistenceLevel)
			{
				case PersistenceLevel.Permanent:
				{
					EditorPrefs.SetString(preferencesKey, serializedPreferences);
					EditorPrefs.SetInt(preferencesCountKey, outHints.Count);
					break;
				}
				case PersistenceLevel.Session:
				{
					SessionState.SetString(preferencesKey, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, outHints));
					SessionState.SetInt(preferencesCountKey, outHints.Count);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(persistenceLevel), persistenceLevel, null);
			}
		}
		
		
		/// <summary>
		/// Adds the given <paramref name="hint"/> to it's given <paramref name="persistenceLevel"/> while updating the helper per-state list to make it easier to manage the
		/// string-based <see cref="EditorPrefs"/> and <see cref="SessionState"/>.
		/// </summary>
		/// <param name="hint">The hint to serialize the preferences for.</param>
		/// <param name="preferencesKey">The key to store the preferences state in.</param>
		/// <param name="preferencesCountKey">The key to store the preferences state count in.</param>
		/// <param name="persistenceLevel">Whether to save the preferences using <see cref="SessionState"/> or <see cref="EditorPrefs"/>.</param>
		/// <param name="outHints">
		/// Helper list of headers for the preference you are persisting.
		/// Caller should pass correct list based on <paramref name="persistenceLevel"/>.
		/// </param>
		private void SetSerializedHintPreference(BeamHint hint,
		                                          string preferencesKey,
		                                          string preferencesCountKey,
		                                          PersistenceLevel persistenceLevel, List<BeamHintHeader> outHints)
		{
			outHints.Add(hint.Header);
			outHints = outHints.Distinct().ToList();
			var keys = outHints.Select(header => header.AsKey()).ToList();
			
			switch (persistenceLevel)
			{
				case PersistenceLevel.Permanent:
				{
					EditorPrefs.SetString(preferencesKey, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, keys));
					EditorPrefs.SetInt(preferencesCountKey, keys.Count);
					break;
				}
				case PersistenceLevel.Session:
				{
					SessionState.SetString(preferencesKey, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, keys));
					SessionState.SetInt(preferencesCountKey, keys.Count);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(persistenceLevel), persistenceLevel, null);
			}
		}
		

		/// <summary>
		/// Discards all persisted <see cref="VisibilityState"/>s and <see cref="PlayModeWarningState"/>s of all hints.
		/// </summary>
		public void ClearAllPreferences()
		{
			EditorPrefs.SetString(VISIBILITY_HIDDEN_SAVED, "");
			SessionState.SetString(VISIBILITY_HIDDEN_SAVED, "");

			EditorPrefs.SetInt(VISIBILITY_HIDDEN_SAVED_COUNT, 0);
			SessionState.SetInt(VISIBILITY_HIDDEN_SAVED_COUNT, 0);
			
			EditorPrefs.SetString(PLAY_MODE_WARNING_DISABLED_SAVED, "");
			SessionState.SetString(PLAY_MODE_WARNING_DISABLED_SAVED, "");

			EditorPrefs.SetInt(PLAY_MODE_WARNING_DISABLED_SAVED_COUNT, 0);
			SessionState.SetInt(PLAY_MODE_WARNING_DISABLED_SAVED_COUNT, 0);
			
			EditorPrefs.SetInt(NOTIFICATION_ALWAYS_SAVED_COUNT, 0);
			EditorPrefs.SetString(NOTIFICATION_ALWAYS_SAVED, "");

			EditorPrefs.SetInt(NOTIFICATION_NEVER_SAVED_COUNT, 0);
			EditorPrefs.SetString(NOTIFICATION_NEVER_SAVED, "");

			RebuildPerHintPreferences();
		}
	}
}
