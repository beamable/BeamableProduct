using Common.Runtime.BeamHints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Editor.BeamableAssistant
{
	/// <summary>
	/// Manages and persists <see cref="BeamHint"/> preferences. Can decide to display/ignore hints and persist this configuration this session or permanenty. 
	/// </summary>
	public class BeamHintPreferencesManager : IBeamHintPreferencesManager
	{
		/// <summary>
		/// Key into both <see cref="SessionState"/> and <see cref="EditorPrefs"/> to store number of <see cref="BeamHint"/> states persisted in each of these.
		/// Used to allocate once at startup and avoid continuous resize allocations mid-loop.
		/// </summary>
		private const string BEAM_HINT_PREFERENCES_SAVED_COUNT = "BEAM_HINT_PREFERENCES_SAVED_COUNT";

		/// <summary>
		/// Key into both <see cref="SessionState"/> and <see cref="EditorPrefs"/> to store the <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list
		/// of <see cref="BeamHintHeaders"/> states persisted in each prefs.
		/// </summary>
		private const string BEAM_HINT_PREFERENCES_IGNORED_HINTS = "BEAM_HINT_PREFERENCES_IGNORED_HINTS";

		/// <summary>
		/// Current state of each <see cref="BeamHint"/>. Mapped by the <see cref="BeamHintHeader"/>.
		/// Any not found <see cref="BeamHintHeader"/> is presumed to be with the following state: <see cref="VisibilityState.Display"/>.
		/// </summary>
		private readonly Dictionary<BeamHintHeader, VisibilityState> _perHintRelevance;

		/// <summary>
		/// List of all header's currently ignored in this <see cref="SessionState"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _sessionIgnoredHints;

		/// <summary>
		/// List of all header's currently ignored in this <see cref="EditorPrefs"/>. Helper list to make the code for managing this easier.
		/// </summary>
		private readonly List<BeamHintHeader> _permanentlyIgnoredHints;

		/// <summary>
		/// Creates a new <see cref="BeamHintPreferencesManager"/> instance you can use to manage <see cref="BeamHint"/> display/ignore preferences.
		/// </summary>
		public BeamHintPreferencesManager()
		{
			var sessionPrefsCount = SessionState.GetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, 0);
			var hintPrefsCount = EditorPrefs.GetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, 0);
			_perHintRelevance = new Dictionary<BeamHintHeader, VisibilityState>(sessionPrefsCount + hintPrefsCount);

			_sessionIgnoredHints = new List<BeamHintHeader>(sessionPrefsCount);
			_permanentlyIgnoredHints = new List<BeamHintHeader>(hintPrefsCount);
		}

		/// <summary>
		/// Restores the current in-memory state of the <see cref="BeamHintPreferencesManager"/> to match what is stored in its persistent storages.
		/// </summary>
		public void RebuildPerHintPreferences()
		{
			_perHintRelevance.Clear();
			_sessionIgnoredHints.Clear();
			_permanentlyIgnoredHints.Clear();

			// Go through editor prefs to get all permanently silenced hints
			var permanentSilencedHints = EditorPrefs.GetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, "");
			ApplyStoredHintPreferences(permanentSilencedHints, VisibilityState.Ignore, _perHintRelevance, _permanentlyIgnoredHints);

			// Go through session state to get all silenced hints for this session
			var sessionSilencedHints = SessionState.GetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, "");
			ApplyStoredHintPreferences(sessionSilencedHints, VisibilityState.Ignore, _perHintRelevance, _sessionIgnoredHints);
		}

		/// <summary>
		/// Deserializes and stores <see cref="BeamHintHeader"/> and <see cref="VisibilityState"/> from a serialized string of <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated
		/// <see cref="BeamHintHeader"/>s and the given state for it. Stores the results both in <paramref name="outHintStateStore"/> and <paramref name="outPerStateList"/>.
		/// </summary>
		///
		/// <param name="savedSerializedHeaders">
		/// The <see cref="BEAM_HINT_PREFERENCES_SEPARATOR"/>-separated list of
		/// <see cref="BeamHintHeader"/>s (via <see cref="BeamHintHeader.AsKey"/>).
		/// </param>
		/// 
		/// <param name="visibilityStateToRestore">
		/// The state to apply to all deserialized <paramref name="savedSerializedHeaders"/>.
		/// </param>
		///
		/// <param name="outHintStateStore">
		/// A dictionary to store the combination of deserialized <paramref name="savedSerializedHeaders"/> and <paramref name="visibilityStateToRestore"/>. 
		/// </param>
		/// <param name="outPerStateList">
		/// A list to add to the deserialized <paramref name="savedSerializedHeaders"/> into. 
		/// </param>
		private void ApplyStoredHintPreferences(string savedSerializedHeaders,
		                                        VisibilityState visibilityStateToRestore,
		                                        Dictionary<BeamHintHeader, VisibilityState> outHintStateStore,
		                                        List<BeamHintHeader> outPerStateList)
		{
			var savedSerializedHeadersArray = savedSerializedHeaders.Split(new[] {BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string serializedHeader in savedSerializedHeadersArray)
			{
				var header = DeserializeBeamHintHeader(serializedHeader);
				outHintStateStore.Add(header, visibilityStateToRestore);
				outPerStateList.Add(header);
			}
		}

		/// <summary>
		/// Deserializes a single <see cref="BeamHintHeader"/> in the format provided by <see cref="BeamHintHeader.AsKey"/>.
		/// </summary>
		private BeamHintHeader DeserializeBeamHintHeader(string serializedHint)
		{
			var typeDomainId = serializedHint.Split(new[] {BeamHintHeader.AS_KEY_SEPARATOR}, StringSplitOptions.None);
			var type = (BeamHintType)Enum.Parse(typeof(BeamHintType), typeDomainId[0]);
			var domain = typeDomainId[1];
			var id = typeDomainId[2];

			return new BeamHintHeader(type, domain, id);
		}

		/// <summary>
		/// Splits all given hints by their <see cref="VisibilityState"/>s.
		/// </summary>
		/// <param name="hints">The hints to split by.</param>
		/// <param name="outToDisplayHints">The resulting list of <see cref="BeamHint"/>s that should be displayed.</param>
		/// <param name="outToIgnoreHints">The resulting list of <see cref="BeamHint"/>s that should be ignored.</param>
		public void SplitHintsByVisibilityState(IEnumerable<BeamHint> hints, out IEnumerable<BeamHint> outToDisplayHints, out IEnumerable<BeamHint> outToIgnoreHints)
		{
			var groups = hints.GroupBy(h =>
			{
				if (!_perHintRelevance.TryGetValue(h.Header, out var state))
					state = VisibilityState.Display;

				return state;
			}).ToList();

			outToDisplayHints = groups.Where(h => h.Key == VisibilityState.Display)
			                          .SelectMany(h => h);
			
			outToIgnoreHints = groups.Where(h => h.Key == VisibilityState.Ignore)
			                          .SelectMany(h => h);
		}
		
		
		/// <summary>
		/// Sets, for the given <paramref name="hint"/>, the given <paramref name="newVisibilityState"/> at the specified <paramref name="persistenceLevel"/>.
		/// Persistence levels are 100% independent, it is up to the caller to add/remove from both independently.
		/// </summary>
		public void SetHintPreferences(BeamHint hint, VisibilityState newVisibilityState, PersistenceLevel persistenceLevel)
		{
			if (!_perHintRelevance.TryGetValue(hint.Header, out var currState))
			{
				_perHintRelevance.Add(hint.Header, newVisibilityState);
				currState = newVisibilityState;
			}

			if (persistenceLevel == PersistenceLevel.Instance)
				return;

			if (currState == VisibilityState.Ignore)
				SerializeHintVisibilityState(hint, persistenceLevel, _permanentlyIgnoredHints, _sessionIgnoredHints);

			if (currState == VisibilityState.Display)
				RemoveHintSaveState(hint, persistenceLevel, _permanentlyIgnoredHints, _sessionIgnoredHints);
		}

		/// <summary>
		/// Removes the given <paramref name="hint"/> from it's given <paramref name="persistenceLevel"/> while updating per-state lists to make it easier to manage the
		/// string-based <see cref="EditorPrefs"/> and <see cref="SessionState"/>.
		/// </summary>
		/// <param name="outPermanentHints">Helper list (for <see cref="PersistenceLevel.Permanent"/> state) of the <see cref="VisibilityState"/> you are persisting.</param>
		/// <param name="outSessionHints">Helper list (for <see cref="PersistenceLevel.Session"/> state) of the <see cref="VisibilityState"/> you are persisting.</param>
		private void RemoveHintSaveState(BeamHint hint, PersistenceLevel persistenceLevel, List<BeamHintHeader> outPermanentHints, List<BeamHintHeader> outSessionHints)
		{
			if (persistenceLevel == PersistenceLevel.Permanent)
			{
				outPermanentHints.Remove(hint.Header);
				EditorPrefs.SetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, outPermanentHints));
				EditorPrefs.SetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, outPermanentHints.Count);
			}

			if (persistenceLevel == PersistenceLevel.Session)
			{
				outSessionHints.Remove(hint.Header);
				SessionState.SetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, outSessionHints));
				SessionState.SetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, outSessionHints.Count);
			}
		}

		/// <summary>
		/// Adds the given <paramref name="hint"/> to it's given <paramref name="persistenceLevel"/> while updating per-state lists to make it easier to manage the
		/// string-based <see cref="EditorPrefs"/> and <see cref="SessionState"/>.
		/// </summary>
		/// <param name="outPermanentHints">Helper list (for <see cref="PersistenceLevel.Permanent"/> state) of the <see cref="VisibilityState"/> you are persisting.</param>
		/// <param name="outSessionHints">Helper list (for <see cref="PersistenceLevel.Session"/> state) of the <see cref="VisibilityState"/> you are persisting.</param>
		private void SerializeHintVisibilityState(BeamHint hint, PersistenceLevel persistenceLevel, List<BeamHintHeader> outPermanentHints, List<BeamHintHeader> outSessionHints)
		{
			if (persistenceLevel == PersistenceLevel.Permanent)
			{
				outPermanentHints.Add(hint.Header);
				outPermanentHints = outPermanentHints.Distinct().ToList();
				var keys = outPermanentHints.Select(header => header.AsKey()).ToList();
				
				EditorPrefs.SetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, keys));
				EditorPrefs.SetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, keys.Count);
			}

			if (persistenceLevel == PersistenceLevel.Session)
			{
				outSessionHints.Add(hint.Header);
				outSessionHints = outSessionHints.Distinct().ToList();
				
				var keys = outSessionHints.Select(header => header.AsKey()).ToList();
				SessionState.SetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, keys));
				SessionState.SetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, keys.Count);
			}
		}

		/// <summary>
		/// Discards all persisted <see cref="VisibilityState"/>s of all hints.
		/// </summary>
		public void ClearAllPreferences()
		{
			EditorPrefs.SetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, "");
			SessionState.SetString(BEAM_HINT_PREFERENCES_IGNORED_HINTS, "");

			EditorPrefs.SetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, 0);
			SessionState.SetInt(BEAM_HINT_PREFERENCES_SAVED_COUNT, 0);

			RebuildPerHintPreferences();
		}
	}
}
