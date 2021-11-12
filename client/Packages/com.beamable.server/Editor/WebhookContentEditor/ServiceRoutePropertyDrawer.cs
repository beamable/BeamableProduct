using System.Linq;
using Beamable.Common.Content;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
   [CustomPropertyDrawer(typeof(ServiceRoute))]
   public class ServiceRoutePropertyDrawer : PropertyDrawer
   {
      private const int PADDING = 2;

      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         return EditorGUIUtility.singleLineHeight * 3 + PADDING * 2;
      }

      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         // need to show a dropdown for the available services...
         var descriptors = Microservices.Descriptors;
         //
         if (descriptors.Count == 0)
         {
            position = EditorGUI.PrefixLabel(position, label);
            EditorGUI.SelectableLabel(position, "You must create a Microservice to configure a Webhook Content", EditorStyles.wordWrappedLabel);
            return;
         }

         var routeInfoPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
         EditorGUI.LabelField(routeInfoPosition, "Route Information", new GUIStyle(EditorStyles.label){font = EditorStyles.boldFont});
         EditorGUI.indentLevel += 1;

         var serviceGuiContents = descriptors
            .Select(d => new GUIContent(d.Name))
            .ToArray();

         var nextRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + PADDING, position.width, EditorGUIUtility.singleLineHeight);

         var serviceProperty = property.FindPropertyRelative(nameof(ServiceRoute.Service));
         var originalServiceIndex = descriptors.FindIndex(d => d.Name.Equals(serviceProperty.stringValue));
         originalServiceIndex = originalServiceIndex == -1 ? 0 : originalServiceIndex;
         var serviceIndex = EditorGUI.Popup(nextRect, new GUIContent("Microservice"), originalServiceIndex, serviceGuiContents, EditorStyles.popup);
         serviceProperty.stringValue = descriptors[serviceIndex].Name;

         nextRect = new Rect(nextRect.x, nextRect.y + EditorGUIUtility.singleLineHeight + PADDING, nextRect.width, EditorGUIUtility.singleLineHeight);
         var service = descriptors.FirstOrDefault(d => d.Name.Equals(serviceProperty.stringValue));
         if (service == null)
         {
            nextRect = EditorGUI.PrefixLabel(nextRect, new GUIContent("Client Callable"));
            EditorGUI.SelectableLabel(nextRect, "You must select a valid service first", EditorStyles.wordWrappedLabel);
            return;
         }

         var clientCallableGuis = service.Methods
            .Select(m => new GUIContent(m.Path))
            .ToArray();

         var routeProperty = property.FindPropertyRelative(nameof(ServiceRoute.Endpoint));
         var originalRouteIndex = service.Methods.FindIndex(d => d.Path.Equals(routeProperty.stringValue));
         originalRouteIndex = originalRouteIndex == -1 ? 0 : originalRouteIndex;
         var routeIndex = EditorGUI.Popup(nextRect, new GUIContent("Method"), originalRouteIndex, clientCallableGuis, EditorStyles.popup);
         routeProperty.stringValue = service.Methods[routeIndex].Path;

         EditorGUI.indentLevel -= 1;

      }
   }
}