using Beamable.Editor;
using System;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	public abstract class BeamableAssistantMenuItem : ScriptableObject
	{
		public int Order;
		public string Text;

		public virtual GUIContent RenderLabel(EditorAPI beamableApi) => new GUIContent(Text);
		public abstract void OnItemClicked(EditorAPI beamableApi);
	}
}
