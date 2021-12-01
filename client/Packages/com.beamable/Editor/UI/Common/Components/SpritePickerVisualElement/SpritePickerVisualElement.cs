using Beamable.Editor.UI.Buss;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.UI.Components
{
	public class SpritePickerVisualElement : BeamableVisualElement
	{
		public new class UxmlFactory : UxmlFactory<SpritePickerVisualElement, UxmlTraits> { }

		public SpritePickerVisualElement() : base(
			$"{BeamableComponentsConstants.COMP_PATH}/{nameof(SpritePickerVisualElement)}/{nameof(SpritePickerVisualElement)}") { }

		public Sprite SelectedSprite { get; set; }

		public override void Refresh()
		{
			base.Refresh();

			ObjectField imageField = Root.Q<ObjectField>("imageField");
			imageField.objectType = typeof(Sprite);

			imageField.UnregisterValueChangedCallback(SpriteChanged);
			imageField.RegisterValueChangedCallback(SpriteChanged);
		}

		private void SpriteChanged(ChangeEvent<Object> evt)
		{
			Sprite sprite = evt.newValue as Sprite;
			SelectedSprite = sprite;
		}
	}
}
