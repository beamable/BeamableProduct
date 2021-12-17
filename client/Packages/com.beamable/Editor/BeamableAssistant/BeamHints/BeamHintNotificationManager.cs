using Beamable.Common;
using Beamable.Common.Assistant;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Beamable.Editor.Assistant
{
	public class BeamHintNotificationManager : IBeamHintManager
	{
		private IBeamHintGlobalStorage _hintStorage;
		private IBeamHintPreferencesManager _hintPreferences;

		private List<BeamHintHeader> _hintsDisplayedThisSession;
		private Dictionary<BeamHintHeader, object> _lastDetectedContextObjects;

		private List<BeamHintHeader> _pendingNotificationHints;
		private List<BeamHintHeader> _pendingNotificationValidations;

		private double _lastTickTime;
		public double TickRate { get; set; }

		public IEnumerable<BeamHintHeader> AllPendingNotifications => _pendingNotificationHints.Union(_pendingNotificationValidations);
		public IEnumerable<BeamHintHeader> PendingHintNotifications => _pendingNotificationHints;
		public IEnumerable<BeamHintHeader> PendingValidationNotifications => _pendingNotificationValidations;

		public BeamHintNotificationManager()
		{
			// Pick up hints that have already been notified this session.
			_hintsDisplayedThisSession = SessionState.GetString("TestNotification", "")
			                                         .Split(new[] {BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR}, StringSplitOptions.RemoveEmptyEntries)
			                                         .Select(BeamHintHeader.DeserializeBeamHintHeader)
			                                         .ToList();

			_lastDetectedContextObjects = new Dictionary<BeamHintHeader, object>();

			_pendingNotificationHints = new List<BeamHintHeader>();
			_pendingNotificationValidations = new List<BeamHintHeader>();

			TickRate = 1;
			_lastTickTime = 0;
		}

		public void SetPreferencesManager(IBeamHintPreferencesManager preferencesManager)
		{
			_hintPreferences = preferencesManager;
		}

		public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage)
		{
			_hintStorage = hintGlobalStorage;
		}

		public void ClearPendingNotifications(IEnumerable<BeamHintHeader> hintsToMarkAsSeen = null)
		{
			// Update in-memory and SessionState of notifications for hint BeamHints.
			_hintsDisplayedThisSession.AddRange(_pendingNotificationHints);
			_hintsDisplayedThisSession = _hintsDisplayedThisSession.Distinct().ToList();
			var serializedHeaders = string.Join(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR, _hintsDisplayedThisSession.Select(h => h.AsKey()));
			SessionState.SetString("TestNotification", serializedHeaders);

			// Update in-memory caches of state of notifications for validation BeamHints.
			foreach (var hint in _pendingNotificationValidations.Select(_hintStorage.GetHint))
			{
				if (!_lastDetectedContextObjects.ContainsKey(hint.Header))
					_lastDetectedContextObjects.Add(hint.Header, hint.ContextObject);
			}

			if (hintsToMarkAsSeen == null)
			{
				_pendingNotificationHints.Clear();
				_pendingNotificationValidations.Clear();
			}
			else
			{
				var toMarkAsSeen = hintsToMarkAsSeen.ToList();
				var markHintAsSeen = toMarkAsSeen.Where(h => (h.Type & BeamHintType.Hint) != 0);
				var markValidationAsSeen = toMarkAsSeen.Where(h => (h.Type & BeamHintType.Validation) != 0);

				_pendingNotificationHints = _pendingNotificationHints.Except(markHintAsSeen).ToList();
				_pendingNotificationValidations = _pendingNotificationValidations.Except(markValidationAsSeen).ToList();
			}
		}

		public void Update()
		{
			var currTickTime = EditorApplication.timeSinceStartup;

			// Do nothing if it's not time to check for notifications again.
			if (_lastTickTime != 0 && !(currTickTime - _lastTickTime >= TickRate)) return;

			// Update the last tick time and 
			_lastTickTime = currTickTime;
			CheckNotifications();
		}

		private void CheckNotifications()
		{
			if (_hintStorage == null) return;

			UpdateNotifications(_hintPreferences,
			                    _hintStorage.ReflectionCacheHints.ToList(),
			                    _hintsDisplayedThisSession,
			                    _lastDetectedContextObjects,
			                    _pendingNotificationHints,
			                    _pendingNotificationValidations);

			var reflectionCacheHints = AllPendingNotifications.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"ReflectionCache Hints -- Count: {reflectionCacheHints.Count}\n" +
			                   $"{string.Join("\n", reflectionCacheHints)}");

			UpdateNotifications(_hintPreferences,
			                    _hintStorage.CSharpMSHints.ToList(),
			                    _hintsDisplayedThisSession,
			                    _lastDetectedContextObjects,
			                    _pendingNotificationHints,
			                    _pendingNotificationValidations);

			var cSharpHints = AllPendingNotifications.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"C# Microservice Hints -- Count: {cSharpHints.Count}\n" +
			                   $"{string.Join("\n", cSharpHints)}");

			UpdateNotifications(_hintPreferences,
			                    _hintStorage.ContentHints.ToList(),
			                    _hintsDisplayedThisSession,
			                    _lastDetectedContextObjects,
			                    _pendingNotificationHints,
			                    _pendingNotificationValidations);

			var contentHints = AllPendingNotifications.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"Beamable Content Hints -- Count: {contentHints.Count}\n" +
			                   $"{string.Join("\n", contentHints)}");

			UpdateNotifications(_hintPreferences,
			                    _hintStorage.UserDefinedStorage.ToList(),
			                    _hintsDisplayedThisSession,
			                    _lastDetectedContextObjects,
			                    _pendingNotificationHints,
			                    _pendingNotificationValidations);

			var userDefinedHints = AllPendingNotifications.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"User Defined Hints -- Count: {userDefinedHints.Count}\n" +
			                   $"{string.Join("\n", userDefinedHints)}");

			UpdateNotifications(_hintPreferences,
			                    _hintStorage.AssistantHints.ToList(),
			                    _hintsDisplayedThisSession,
			                    _lastDetectedContextObjects,
			                    _pendingNotificationHints,
			                    _pendingNotificationValidations);

			var assistant = AllPendingNotifications.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"Beamable Assistant Hints -- Count: {assistant.Count}\n" +
			                   $"{string.Join("\n", assistant)}");
		}

		private static void UpdateNotifications(IBeamHintPreferencesManager beamHintPreferencesManager,
		                                        IReadOnlyCollection<BeamHint> hintsToUpdate,
		                                        List<BeamHintHeader> hintsClearedThisSession,
		                                        Dictionary<BeamHintHeader, object> lastDetectedContextObjects,
		                                        List<BeamHintHeader> outPendingNotificationHints,
		                                        List<BeamHintHeader> outPendingNotificationValidations)
		{
			beamHintPreferencesManager.SplitHintsByNotificationPreferences(hintsToUpdate,
			                                                               out var toNotifyAlways,
			                                                               out var toNotifyNever,
			                                                               out var sessionNotify,
			                                                               out var contextObjectChangeNotify);

			var notYetNotifiedThisSession = sessionNotify.Where(hint => !hintsClearedThisSession.Contains(hint.Header)).ToList();
			var notYetNotifiedWithCurrentContextObj = contextObjectChangeNotify.Where(hint => {
				if (lastDetectedContextObjects.TryGetValue(hint.Header, out var ctxObject)) return ctxObject != hint.ContextObject;
				return true;
			}).ToList();

			var toNotify = toNotifyAlways
			               .Union(notYetNotifiedThisSession)
			               .Union(notYetNotifiedWithCurrentContextObj)
			               .ToList();

			outPendingNotificationHints.AddRange(toNotify.Where(h => (h.Header.Type & BeamHintType.Hint) != 0).Select(h => h.Header));
			outPendingNotificationValidations.AddRange(toNotify.Where(h => (h.Header.Type & BeamHintType.Validation) != 0).Select(h => h.Header));
		}
	}
}
