// using System.Linq;
// using Beamable.Server.Content;
// using UnityEditor;
// using UnityEngine;
//
// namespace Beamable.Server.Editor
// {
//    // [CustomEditor(typeof(WebhookContent))]
//    // public class WebhookContentInspector : UnityEditor.Editor
//    // {
//    //    public override void OnInspectorGUI()
//    //    {
//    //       var self = target as WebhookContent;
//    //       if (self == null) return;
//    //
//    //       EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(WebhookContent.Description)));
//    //    }
//    // }
//
//    [CustomPropertyDrawer(typeof(ServiceRoute))]
//    public class ServiceRoutePropertyDrawer : PropertyDrawer
//    {
//       public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//       {
//          return EditorGUIUtility.singleLineHeight * 4;
//       }
//
//       public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//       {
//          // need to show a dropdown for the available services...
//          var descriptors = Microservices.Descriptors;
//          //
//          // EditorGUI.DrawRect(position, Color.magenta);
//          // if (descriptors.Count == 0)
//          // {
//          //    position = EditorGUI.PrefixLabel(position, label);
//          //
//          //    EditorGUI.SelectableLabel(position, "You must create a Microservice to configure a Webhook Content", EditorStyles.wordWrappedLabel);
//          //    return;
//          // }
//          //
//          // property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);
//          // if (!property.isExpanded) return;
//          // // position = new Rect(position.x + 15, position.y, position.width - 15, position.height);
//          //
//          var serviceGuiContents = descriptors
//             .Select(d => new GUIContent(d.Name))
//             .ToArray();
//          // //
//          // var nextRect = new Rect(position.x, position.y + 20, position.width, 300);
//          // EditorGUI.DrawRect(nextRect, Color.red);
//
//          var serviceProperty = property.FindPropertyRelative(nameof(ServiceRoute.Service));
//          Debug.Log(serviceProperty.stringValue);
//          var originalServiceIndex = descriptors.FindIndex(d => d.Name.Equals(serviceProperty.stringValue));
//          originalServiceIndex = originalServiceIndex == -1 ? 0 : originalServiceIndex;
//          var serviceIndex = EditorGUI.Popup(position, new GUIContent("Microservice"), originalServiceIndex, serviceGuiContents, EditorStyles.popup);
//          serviceProperty.stringValue = descriptors[serviceIndex].Name;
//       }
//    }
// }