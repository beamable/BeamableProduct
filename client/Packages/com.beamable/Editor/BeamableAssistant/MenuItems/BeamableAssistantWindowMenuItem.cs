using Beamable.Editor;
using Beamable.Editor.BeamableAssistant;
using System;
using UnityEngine;

namespace Editor.Beamable.ToolbarExtender
{
	[CreateAssetMenu(fileName = "AssistantMenuItem", menuName = "Beamable/Assistant/Assistant Window", order = 0)]

	public class BeamableAssistantWindowMenuItem : BeamableAssistantMenuItem
	{
		public override void OnItemClicked(EditorAPI beamableApi) => BeamableAssistantWindow.ShowWindow();
	}
}
