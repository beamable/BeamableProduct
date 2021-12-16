using Beamable.Common;
using Common.Runtime.BeamHints;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Editor.BeamableAssistant
{
	public class BeamHintNotificationManager : IBeamHintManager
	{
		private IBeamHintGlobalStorage _hintStorage;
		private IBeamHintPreferencesManager _hintPreferences;

		public void SetPreferencesManager(IBeamHintPreferencesManager preferencesManager)
		{
			_hintPreferences = preferencesManager;
		}

		public void SetStorage(IBeamHintGlobalStorage hintGlobalStorage)
		{
			_hintStorage = hintGlobalStorage;
		}

		public void DelayedVerifyNotifications()
		{
			DumpHints();
		}
		
		private void DumpHints()
		{
			if (_hintStorage == null) return;

			var reflectionCacheHints = _hintStorage.ReflectionCacheHints.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"ReflectionCache Hints -- Count: {reflectionCacheHints.Count}\n" +
			                   $"{string.Join("\n", reflectionCacheHints)}");
			
			var cSharpHints = _hintStorage.CSharpMSHints.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"C# Microservice Hints -- Count: {cSharpHints.Count}\n" +
			                   $"{string.Join("\n", cSharpHints)}");
			
			var contentHints = _hintStorage.ContentHints.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"Beamable Content Hints -- Count: {contentHints.Count}\n" +
			                   $"{string.Join("\n", contentHints)}");
			
			var userDefinedHints = _hintStorage.UserDefinedStorage.Select(hint => hint.ToString()).ToList();
			BeamableLogger.Log($"User Defined Hints -- Count: {userDefinedHints.Count}\n" +
			                   $"{string.Join("\n", userDefinedHints)}");			
		}
	}
}
