using Beamable.Editor;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.ToolbarExtender
{
	public abstract class BeamableToolbarButton : ScriptableObject
	{
		public enum Side { Left, Right }
		
		[SerializeField] private Vector2 ButtonSize = Vector2.one * 30;
		[SerializeField] private Texture ButtonImage = null;
		[SerializeField] private string ButtonText = "Button";

		public abstract bool ShouldDisplayButton(EditorAPI editorAPI);
		public abstract Side GetButtonSide(EditorAPI editorAPI);
		public abstract int GetButtonOrder(EditorAPI editorAPI);
		
		public abstract void OnButtonClicked(EditorAPI editorAPI);
		

		public virtual GenericMenu GetDropdownOptions(EditorAPI editorAPI) => null;
		public virtual string GetButtonText(EditorAPI editorAPI) => ButtonText;
		public virtual Vector2 GetButtonSize(EditorAPI editorAPI) => ButtonSize;
		public virtual Texture GetButtonTexture(EditorAPI editorAPI) => ButtonImage;
	}
}
