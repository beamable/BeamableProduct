using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Beamable.Editor.Util.PropertyDrawers
{
	[CustomPropertyDrawer(typeof(HelpBoxDecoratorAttribute))]
	public class HelpBoxDecoratorDrawer : DecoratorDrawer
	{
		HelpBoxDecoratorAttribute HelpAttr => ((HelpBoxDecoratorAttribute)attribute);

		public GUIStyle style => new GUIStyle(EditorStyles.helpBox)
		{
			wordWrap = true,
			richText = true,
			padding = new RectOffset(4, 4, 4, 4),
			margin = new RectOffset(0, 0, 4, 2)
		};

		public override float GetHeight()
		{
			return style.CalcHeight(new GUIContent(HelpAttr.Tooltip),
									EditorGUIUtility.currentViewWidth) + 8;
		}

		public override void OnGUI(Rect position)
		{
			EditorGUI.LabelField(new Rect(position.x, position.y + 6, position.width, position.height - 6), HelpAttr.Tooltip, style);
		}
	}

}
