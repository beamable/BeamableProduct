using Beamable.Common.Assistant;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Beamable.Editor.Assistant
{
	[CreateAssetMenu(fileName = "BeamHintDetailsConfig", menuName = "Beamable/Assistant/Hints/Hint Details Configuration", order = 0)]
	public class BeamHintDetailsConfig : ScriptableObject
	{
		[Serializable]
		public struct HeaderMatcher
		{
			public BeamHintType MatchType;
			public string Domain;
			public string IdRegex;

			private Regex _regex;

			public HeaderMatcher(BeamHintType matchType, string domain, string idRegex) : this()
			{
				MatchType = matchType;
				Domain = domain;
				IdRegex = idRegex;
				_regex = new Regex(idRegex);
			}

			public bool MatchAgainstHeader([NotNull] BeamHintHeader other)
			{
				var matchType = MatchType.HasFlag(other.Type);
				var matchDomain = string.IsNullOrEmpty(Domain) || other.Domain.Contains(Domain);
				var idMatch = string.IsNullOrEmpty(IdRegex) || _regex.IsMatch(other.Id);

				return matchType && matchDomain && idMatch;
			}
		}

		public HeaderMatcher Matcher;

		public string UxmlFile;
		public List<string> StylesheetsToAdd;

		public bool MatchesHint(BeamHintHeader hint)
		{
			return Matcher.MatchAgainstHeader(hint);
		}
	}
}
