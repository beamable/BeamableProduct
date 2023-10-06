using Beamable.Common.Player;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Player
{
	[CustomPropertyDrawer(typeof(AbsRefreshableObservable), true)]
	public class ObservableReadonlyListPropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var dataProperty = property.FindPropertyRelative("_data");
			if (dataProperty == null ) return base.GetPropertyHeight(property, label);
			
			return EditorGUI.GetPropertyHeight(dataProperty, label, true);
		}
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var dataProperty = property.FindPropertyRelative("_data");
			if (dataProperty == null)
			{
				base.OnGUI(position, property, label);
				return;
			}

			GUI.enabled = false;
			EditorGUI.PropertyField(position, dataProperty, label, true);
			GUI.enabled = true;
		}
	}
}
