using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Editor.Environment;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
   [CustomPropertyDrawer(typeof(HideUnlessServerPackageInstalled))]
   public class HideServerPropertyDrawer : PropertyDrawer
   {
      private static Promise<BeamablePackageMeta> _serverPackagePromise;
      private static Promise<bool> _check;

      public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
      {
         if (!CanDisplay()) return 0;

         var isOptional = (typeof(Optional).IsAssignableFrom(fieldInfo.FieldType));
         if (isOptional)
         {
            var optionalDrawer = new OptionalPropertyDrawer();
            return optionalDrawer.GetPropertyHeight(property, label);
         }

         return EditorGUI.GetPropertyHeight(property);

      }

      public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
      {
         if (!CanDisplay()) return;

         var isOptional = (typeof(Optional).IsAssignableFrom(fieldInfo.FieldType));
         if (isOptional)
         {
            var optionalDrawer = new OptionalPropertyDrawer();
            optionalDrawer.OnGUI(position, property, label);
            return;
         }

         EditorGUI.PropertyField(position, property, label, true);
      }

      private bool CanDisplay()
      {
         if (_check == null)
         {
            _check = HasMicroservicePackage();
         }

         if (_check.IsCompleted)
         {
            return _check.GetResult();
         }

         return false;
      }

      private async Promise<bool> HasMicroservicePackage()
      {
         if (_serverPackagePromise == null)
         {
            _serverPackagePromise = BeamablePackages.GetServerPackage();
            return await HasMicroservicePackage();
         }

         var hasPackage = false;
         try
         {
            var result = await _serverPackagePromise;
            hasPackage = result.IsPackageAvailable;
         }
         catch
         {
            // its okay, don't do anything.
         }

         return hasPackage;
      }
   }
}