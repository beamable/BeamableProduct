using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.Content;
using Beamable.Server.Editor.CodeGen;
using dninosores.UnityEditorAttributes;
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

         public Type Type => instance?.GetType();
         public Type ParameterType => Type.BaseType.GetGenericArguments()[0];
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
            .Select(p => EditorGUI.GetPropertyHeight(p.property) + 2).Sum();
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

            SerializedRouteParameterInfo info = new SerializedRouteParameterInfo
            {
               Name = parameter.Name,
               property = property,
               rawProperty = rawProperty
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

         position.height = EditorGUIUtility.singleLineHeight;
         EditorGUI.LabelField(position, "Route Parameters", new GUIStyle(EditorStyles.label){font = EditorStyles.boldFont});
         position.y += EditorGUIUtility.singleLineHeight + 2;
         EditorGUI.indentLevel += 1;

         var descriptor = Microservices.Descriptors.FirstOrDefault(d => d.Name.Equals(serviceNameProperty.stringValue));
         var method = descriptor.Methods.FirstOrDefault(m => m.Path.Equals(endpointProperty.stringValue));

         foreach (var info in GetRouteProperties(descriptor, method, property))
         {
            EditorGUI.BeginChangeCheck();

            var height = EditorGUIUtility.singleLineHeight;

            var infoLabel = new GUIContent(info.Name);
            var drawer = PropertyDrawerFinder.FindDrawerForProperty(info.property);
            if (drawer == null)
            {
               height = EditorGUI.GetPropertyHeight(info.property);
               position.height = height;
               EditorGUI.PropertyField(position, info.property, infoLabel, true);
            }
            else
            {
               height = drawer.GetPropertyHeight(info.property, infoLabel);
               position.height = height;
               drawer.OnGUI(position, info.property, infoLabel);
            }

            position.y += height + 2;

            var hasModifiedProperties = info.property.serializedObject.hasModifiedProperties;

            if (EditorGUI.EndChangeCheck() || hasModifiedProperties)
            {
               // TODO:debounce this...
               info.property.serializedObject.ApplyModifiedProperties();

               var nextValue = info.Type.GetField("Data", BindingFlags.Instance | BindingFlags.Public).GetValue(info.instance);

               var json = (string) typeof(MicroserviceClientHelper)
                  .GetMethod("SerializeArgument", BindingFlags.Static | BindingFlags.Public)
                  .MakeGenericMethod(info.ParameterType).Invoke(null, new object[] {nextValue});

               info.rawProperty.stringValue = json;
            }
         }

         EditorGUI.indentLevel -= 1;

         // var parametersProperty = property.FindPropertyRelative(nameof(RouteParameters.Parameters));
         //
         // parametersProperty.arraySize = method.Parameters.Length;
         //
         // for (var i = 0; i < method.Parameters.Length; i ++)
         // {
         //    var parameter = method.Parameters[i];
         //    var parameterProperty = parametersProperty.GetArrayElementAtIndex(i);
         //
         //
         //
         //    var parameterNameProperty = parameterProperty.FindPropertyRelative(nameof(RouteParameter.Name));
         //    parameterNameProperty.stringValue = parameter.Name;
         //
         //    position = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);
         //
         //    var rawProperty = parameterProperty.FindPropertyRelative(nameof(RouteParameter.Data))
         //       .FindPropertyRelative(nameof(OptionalString.Value));
         //
         //
         //    try
         //    {
         //       var type = ClientCodeGenerator.GetDataWrapperTypeForParameter(descriptor, parameter.Type);
         //
         //       // TODO: somehow cache this???
         //       var instance = ScriptableObject.CreateInstance(type);
         //
         //       try
         //       {
         //          var value = typeof(MicroserviceClientHelper)
         //             .GetMethod("DeserializeResult", BindingFlags.Static | BindingFlags.Public)
         //             .MakeGenericMethod(parameter.Type).Invoke(null, new[] {rawProperty.stringValue});
         //          type.GetField("Data", BindingFlags.Instance | BindingFlags.Public).SetValue(instance, value);
         //
         //       }
         //       catch
         //       {
         //          // we couldn't the value yet?
         //       }
         //       // deserialize the raw data string and set it.
         //
         //       var serialized = new SerializedObject(instance);
         //
         //       var subProperty = serialized.FindProperty("Data");
         //
         //       EditorGUI.BeginChangeCheck();
         //       EditorGUI.PropertyField(position, subProperty, new GUIContent(parameter.Name), true);
         //
         //       if (EditorGUI.EndChangeCheck())
         //       {
         //          serialized.ApplyModifiedProperties();
         //          var nextValue = type.GetField("Data", BindingFlags.Instance | BindingFlags.Public).GetValue(instance);
         //          rawProperty.stringValue = (string) typeof(MicroserviceClientHelper)
         //             .GetMethod("SerializeArgument", BindingFlags.Static | BindingFlags.Public)
         //             .MakeGenericMethod(parameter.Type).Invoke(null, new object[] {nextValue});
         //       }
         //    }
         //    catch (Exception ex)
         //    {
         //       EditorGUI.LabelField(position, ex.Message);
         //    }
         //
         // }
      }

   }
}