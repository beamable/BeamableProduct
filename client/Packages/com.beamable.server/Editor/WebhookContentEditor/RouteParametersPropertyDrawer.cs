using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor;
using Beamable.Editor.Content;
using Beamable.Server.Editor.CodeGen;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beamable.Server.Editor
{
   [CustomPropertyDrawer(typeof(RouteParameters))]
   public class RouteParametersPropertyDrawer : PropertyDrawer
   {
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
            info.typeProperty.stringValue = string.IsNullOrEmpty(info.typeProperty.stringValue)
               ? ApiVariable.GetTypeName(info.ParameterType)
               : info.typeProperty.stringValue ;

            EditorGUI.BeginChangeCheck();

            var height = info.IsUsingVariable
               ? EditorGUIUtility.singleLineHeight
               : EditorGUI.GetPropertyHeight(info.property);
            var infoLabel = new GUIContent(info.Name);
            var rightWidth = hasAnyVariables ? 30 : 0;
            var fieldPosition = new Rect(position.x, position.y, position.width - rightWidth, height);
            var buttonButton = new Rect(position.xMax - 20, position.y, 20, EditorGUIUtility.singleLineHeight);
            position.height = height;
            position.y += height + 2;

            GUIStyle iconButtonStyle = GUI.skin.FindStyle("IconButton") ?? EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).FindStyle("IconButton");
            GUIContent content = new GUIContent(EditorGUIUtility.Load("icons/_Popup.png") as Texture2D);

            if (hasAnyVariables && EditorGUI.DropdownButton(buttonButton, content, FocusType.Keyboard,
               iconButtonStyle))
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
               var parameterTypeValue = info.typeProperty.stringValue;

               for (var i = 0; i < options.Length; i++)
               {
                  var variableName = variablesArrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ApiVariable.Name)).stringValue;
                  var variableType = variablesArrayProperty.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(ApiVariable.TypeName)).stringValue;

                  if (!string.Equals(variableType, parameterTypeValue))
                  {
                     continue;
                  }
                  options[i] = new GUIContent(variableName);

                  if (info.variableValueProperty.stringValue.Equals(variableName))
                  {
                     selectedIndex = i;
                  }
               }

               options = options.Where(o => o != null).ToArray();
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

            info.property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            var nextValue = info.Type.GetField("Data", BindingFlags.Instance | BindingFlags.Public).GetValue(info.instance);

            EditorDebouncer.Debounce("api-content-route-parameter", () =>
            {
               var json = (string) typeof(MicroserviceClientHelper)
                  .GetMethod("SerializeArgument", BindingFlags.Static | BindingFlags.Public)
                  .MakeGenericMethod(info.ParameterType).Invoke(null, new object[] {nextValue});
               info.rawProperty.stringValue = json;
               info.rawProperty.serializedObject.ApplyModifiedProperties();
               info.rawProperty.serializedObject.Update();
               EditorUtility.SetDirty(info.rawProperty.serializedObject.targetObject);
            });
         }

         EditorGUI.indentLevel -= 1;
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
            var typeProperty = parameterProperty.FindPropertyRelative(nameof(RouteParameter.TypeName));
            var variableOptionProperty = parameterProperty.FindPropertyRelative(nameof(RouteParameter.variableReference));
            var variableHasValueProperty = variableOptionProperty.FindPropertyRelative(nameof(Optional.HasValue));
            var variableValueProperty = variableOptionProperty.FindPropertyRelative(nameof(Optional<string>.Value)).FindPropertyRelative(nameof(ApiVariableReference.Name));

            SerializedRouteParameterInfo info = new SerializedRouteParameterInfo
            {
               Name = parameter.Name,
               property = property,
               rawProperty = rawProperty,
               isUsingVariableProperty = variableHasValueProperty,
               variableValueProperty = variableValueProperty,
               typeProperty = typeProperty
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


      [Serializable]
      private class SerializedRouteParameterInfo
      {
         public string Name;
         public ScriptableObject instance;
         public SerializedProperty property;
         public SerializedProperty rawProperty;
         public SerializedProperty variableValueProperty;
         public SerializedProperty isUsingVariableProperty;
         public SerializedProperty typeProperty;

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

   }
}