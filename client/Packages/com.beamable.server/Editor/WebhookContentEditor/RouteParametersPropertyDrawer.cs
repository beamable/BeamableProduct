using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.Content;
using Beamable.Server.Editor.CodeGen;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor
{
   [CustomPropertyDrawer(typeof(RouteParameters))]
   public class RouteParametersPropertyDrawer : PropertyDrawer
   {
      [Serializable]
      private class SerializedRouteParameterInfo
      {
         public string Name;
         public ScriptableObject instance;
         public SerializedProperty property;
         public SerializedProperty rawProperty;
         public SerializedProperty variableValueProperty;
         public SerializedProperty isUsingVariableProperty;

         public Type Type => instance?.GetType();
         public Type ParameterType => Type.BaseType.GetGenericArguments()[0];

         public bool ToggleVariable()
         {
             isUsingVariableProperty.boolValue = !isUsingVariableProperty.boolValue;

             isUsingVariableProperty.serializedObject.ApplyModifiedProperties();
             return IsUsingVariable;
         }

         public bool IsUsingVariable => isUsingVariableProperty.boolValue;
      }

      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         var apiProperty = property.FindPropertyRelative(nameof(RouteParameters.ApiContent));

         if (apiProperty == null || apiProperty.serializedObject == null)
         {
            return EditorGUIUtility.singleLineHeight;
         }

         var serviceRouteProperty = apiProperty.serializedObject.FindProperty(nameof(ApiContent.ServiceRoute));
         var serviceNameProperty = serviceRouteProperty.FindPropertyRelative(nameof(ServiceRoute.Service));
         var endpointProperty = serviceRouteProperty.FindPropertyRelative(nameof(ServiceRoute.Endpoint));

         var descriptor = Microservices.Descriptors.FirstOrDefault(d => d.Name.Equals(serviceNameProperty.stringValue));
         var method = descriptor.Methods.FirstOrDefault(m => m.Path.Equals(endpointProperty.stringValue));

         var totalPropertyHeight = GetRouteProperties(descriptor, method, property)
            .Select(p => p.IsUsingVariable
               ? EditorGUIUtility.singleLineHeight
               : EditorGUI.GetPropertyHeight(p.property) + 2).Sum();
         return totalPropertyHeight + EditorGUIUtility.singleLineHeight;
      }

      private List<SerializedRouteParameterInfo> GetRouteProperties(MicroserviceDescriptor descriptor, ClientCallableDescriptor method, SerializedProperty property)
      {
         var output = new List<SerializedRouteParameterInfo>();

         var parametersProperty = property.FindPropertyRelative(nameof(RouteParameters.Parameters));

         parametersProperty.arraySize = method.Parameters.Length;

         for (var i = 0; i < method.Parameters.Length; i++)
         {
            var parameter = method.Parameters[i];
            var parameterProperty = parametersProperty.GetArrayElementAtIndex(i);

            var rawProperty = parameterProperty.FindPropertyRelative(nameof(RouteParameter.Data));

            var variableOptionProperty = parameterProperty.FindPropertyRelative(nameof(RouteParameter.variableReference));
            var variableHasValueProperty = variableOptionProperty.FindPropertyRelative(nameof(Optional.HasValue));
            var variableValueProperty = variableOptionProperty.FindPropertyRelative(nameof(Optional<string>.Value)).FindPropertyRelative(nameof(ApiVariableReference.Name));

            SerializedRouteParameterInfo info = new SerializedRouteParameterInfo
            {
               Name = parameter.Name,
               property = property,
               rawProperty = rawProperty,
               isUsingVariableProperty = variableHasValueProperty,
               variableValueProperty = variableValueProperty
            };
            try
            {
               var type = ClientCodeGenerator.GetDataWrapperTypeForParameter(descriptor, parameter.Type);

               // TODO: somehow cache this???
               var instance = ScriptableObject.CreateInstance(type);

               try
               {
                  var value = typeof(MicroserviceClientHelper)
                     .GetMethod("DeserializeResult", BindingFlags.Static | BindingFlags.Public)
                     .MakeGenericMethod(parameter.Type).Invoke(null, new[] {rawProperty.stringValue});
                  type.GetField("Data", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, value);
               }
               catch
               {
                  // its okay to ignore this exception and present the default view.
               }
               // deserialize the raw data string and set it.

               var serialized = new SerializedObject(instance);
               info.property = serialized.FindProperty("Data");
               info.instance = instance;
            }
            catch
            {
               info.property = property;
               info.instance = null;

            }

            output.Add(info);

         }

         return output;
      }

      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {

         var apiProperty = property.FindPropertyRelative(nameof(RouteParameters.ApiContent));

         if (apiProperty == null || apiProperty.serializedObject == null)
         {
            EditorGUI.LabelField(position, "could not find parent api content.");
            return;
         }

         var serviceRouteProperty = apiProperty.serializedObject.FindProperty(nameof(ApiContent.ServiceRoute));
         var serviceNameProperty = serviceRouteProperty.FindPropertyRelative(nameof(ServiceRoute.Service));
         var endpointProperty = serviceRouteProperty.FindPropertyRelative(nameof(ServiceRoute.Endpoint));
         var variablesProperty = apiProperty.serializedObject.FindProperty("_variables");
         var variablesArrayProperty =  variablesProperty.FindPropertyRelative(nameof(RouteVariables.Variables));

         var hasAnyVariables = variablesArrayProperty.arraySize > 0;

         position.height = EditorGUIUtility.singleLineHeight;
         EditorGUI.LabelField(position, "Route Parameters", new GUIStyle(EditorStyles.label){font = EditorStyles.boldFont});
         position.y += EditorGUIUtility.singleLineHeight + 2;
         EditorGUI.indentLevel += 1;

         var descriptor = Microservices.Descriptors.FirstOrDefault(d => d.Name.Equals(serviceNameProperty.stringValue));
         var method = descriptor.Methods.FirstOrDefault(m => m.Path.Equals(endpointProperty.stringValue));

         foreach (var info in GetRouteProperties(descriptor, method, property))
         {


            EditorGUI.BeginChangeCheck();

            var height = info.IsUsingVariable
               ? EditorGUIUtility.singleLineHeight
               : EditorGUI.GetPropertyHeight(info.property);
            var infoLabel = new GUIContent(info.Name);
            var fieldPosition = new Rect(position.x, position.y, position.width - 30, height);
            var buttonButton = new Rect(position.xMax - 30, position.y, 30, EditorGUIUtility.singleLineHeight);
            position.height = height;
            position.y += height + 2;

            if (hasAnyVariables && EditorGUI.DropdownButton(buttonButton, new GUIContent(""), FocusType.Keyboard,
               EditorStyles.toolbarDropDown))
            {
               GenericMenu menu = new GenericMenu();

               menu.AddItem(new GUIContent("Use Variable"), info.IsUsingVariable, () =>
               {
                  info.ToggleVariable();
               });
               // display the menu
               menu.ShowAsContext();
            }

            if (info.IsUsingVariable)
            {

               var options = new GUIContent[variablesArrayProperty.arraySize];
               var selectedIndex = 0;
               for (var i = 0; i < options.Length; i++)
               {
                  var variableName = variablesArrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ApiVariable.Name)).stringValue;
                  options[i] = new GUIContent(variableName);
                  if (info.variableValueProperty.stringValue.Equals(variableName))
                  {
                     selectedIndex = i;
                  }
               }

               EditorGUI.BeginChangeCheck();
               var nextSelectedIndex = EditorGUI.Popup(fieldPosition, infoLabel, selectedIndex, options);
               if (EditorGUI.EndChangeCheck())
               {
                  info.variableValueProperty.stringValue = options[nextSelectedIndex].text;
               }

               continue;
            }


            EditorGUI.PropertyField(fieldPosition, info.property, infoLabel, true);
            var hasModifiedProperties = info.property.serializedObject.hasModifiedProperties;
            if (!EditorGUI.EndChangeCheck() && !hasModifiedProperties) continue;

            // TODO:debounce this...
            info.property.serializedObject.ApplyModifiedProperties();
            var nextValue = info.Type.GetField("Data", BindingFlags.Instance | BindingFlags.Public).GetValue(info.instance);
            var json = (string) typeof(MicroserviceClientHelper)
               .GetMethod("SerializeArgument", BindingFlags.Static | BindingFlags.Public)
               .MakeGenericMethod(info.ParameterType).Invoke(null, new object[] {nextValue});

            info.rawProperty.stringValue = json;
         }

         EditorGUI.indentLevel -= 1;

      }

   }
}