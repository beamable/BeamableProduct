using Beamable.Common.Assistant;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Beamable.Editor.Assistant
{
	/// <summary>
	/// Holds a mapping of UXML/USS files to ids used by <see cref="BeamHintDetailConverterProvider"/> and <see cref="BeamHintHeaderVisualElement"/> to render out
	/// hint details. 
	/// </summary>
	[CreateAssetMenu(fileName = "BeamHintDetailsConfig", menuName = "Beamable/Assistant/Hints/Hint Details Configuration", order = 0)]
	public class BeamHintDetailsConfig : ScriptableObject
	{
		[Tooltip("The id you want to reference in your "+nameof(BeamHintDetailConverterAttribute)+"s in order to map these UXML/USS files to specific set of hint(s).")]
		public string Id;

		[Tooltip("The path to a UXML file that'll be added to the BeamHintHeaderVisualElement element when rendering details of the hint.")]
		public string UxmlFile;
		[Tooltip("The paths to USS files that'll be added to the BeamHintHeaderVisualElement element when rendering details of the hint.")]
		public List<string> StylesheetsToAdd;

		private void OnValidate()
		{
			if (string.IsNullOrEmpty(Id))
				Id = name;
		}
	}
}
