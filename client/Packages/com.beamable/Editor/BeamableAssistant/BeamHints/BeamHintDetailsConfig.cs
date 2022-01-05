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
		public string Id;
		
		public string UxmlFile;
		public List<string> StylesheetsToAdd;

		private void OnValidate()
		{
			if (string.IsNullOrEmpty(Id))
				Id = name;
		}
	}
}
