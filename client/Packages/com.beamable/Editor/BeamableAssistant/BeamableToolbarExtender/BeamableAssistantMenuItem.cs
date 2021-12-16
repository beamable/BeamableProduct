using Beamable.Editor;
using System;
using UnityEngine;

namespace Editor.Beamable.ToolbarExtender
{
	public abstract class BeamableAssistantMenuItem : ScriptableObject
	{
		public int Order;
		public string Text;

		public virtual GUIContent RenderLabel(EditorAPI beamableApi) => new GUIContent(Text);
		public abstract void OnItemClicked(EditorAPI beamableApi);
	}
}
