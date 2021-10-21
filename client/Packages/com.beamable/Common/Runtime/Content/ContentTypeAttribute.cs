using System;
using System.Runtime.CompilerServices;
namespace Beamable.Common.Content
{
   /// <summary>
   /// This type defines part of the %Beamable %ContentObject system.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See Beamable.Common.Content.ContentObject script reference
   /// 
   /// ![img beamable-logo]
   ///
   /// </summary>
   [AttributeUsage(AttributeTargets.Class)]
   public class ContentTypeAttribute : UnityEngine.Scripting.PreserveAttribute, IHasSourcePath, IUniqueNamingAttribute<ContentTypeAttribute>
   {
      public string TypeName { get; }
      public string SourcePath { get; }

      public ContentTypeAttribute(string typeName, [CallerFilePath] string sourcePath = "")
      {
         TypeName = typeName;
         SourcePath = sourcePath;
      }

      public string Name => TypeName;
      public ReflectionCache.ValidationResult IsValidNameForType(string potentialName, out string warningMessage, out string errorMessage)
      {
         if (potentialName.Contains("."))
         {
            warningMessage = "";
            errorMessage = $"We do not support...";
            return ReflectionCache.ValidationResult.Error;
         }

         warningMessage = "";
         errorMessage = "";
         return ReflectionCache.ValidationResult.Valid;
      }

      public ReflectionCache.ValidationResult IsAllowedOnType(Type type, out string warningMessage, out string errorMessage)
      {
         bool isAssignableFromIContentObject = typeof(IContentObject).IsAssignableFrom(type);

#if !DB_MICROSERVICE
         bool isAssignableFromScriptableObject = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
#else
         bool isAssignableFromScriptableObject = true;
#endif
         if (isAssignableFromIContentObject && isAssignableFromScriptableObject)
         {
            errorMessage = "";
            warningMessage = "";
            return ReflectionCache.ValidationResult.Valid;
         }
         else
         {
            warningMessage = "";

            errorMessage = $"This attribute should only be used on ScriptableObjects that implement the [{nameof(IContentObject)}] interface.";
            return ReflectionCache.ValidationResult.Error;
         }
      }
   }

   /// <summary>
   /// This type defines part of the %Beamable %ContentObject system.
   ///
   /// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
   ///
   /// #### Related Links
   /// - See Beamable.Common.Content.ContentObject script reference
   /// 
   /// ![img beamable-logo]
   ///
   /// </summary>
   [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
   public class ContentFormerlySerializedAsAttribute : Attribute, IUniqueNamingAttribute<ContentFormerlySerializedAsAttribute>
   {
      public string OldTypeName { get; }
      public ContentFormerlySerializedAsAttribute(string oldTypeName)
      {
         OldTypeName = oldTypeName;
      }
      
      public string Name => OldTypeName;
      public ReflectionCache.ValidationResult IsValidNameForType(string potentialName, out string warningMessage, out string errorMessage)
      {
         if (potentialName.Contains("."))
         {
            warningMessage = "";
            errorMessage = $"We do not support...";
            return ReflectionCache.ValidationResult.Error;
         }

         warningMessage = "";
         errorMessage = "";
         return ReflectionCache.ValidationResult.Valid;
      }

      public ReflectionCache.ValidationResult IsAllowedOnType(Type type, out string warningMessage, out string errorMessage)
      {
         bool isAssignableFromIContentObject = typeof(IContentObject).IsAssignableFrom(type);

         // TODO: Check with CHRIS what validation cases does this cover and why, maybe we don't need to enforce this... (especially in TestCases)
         // TODO: Can easily ignore this from test Assemblies via type.Assembly.Name.Contains("Test") but its not a good long term solution... Or maybe it is?
#if !DB_MICROSERVICE
         bool isAssignableFromScriptableObject = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
#else
         bool isAssignableFromScriptableObject = true;
#endif
         if (!(isAssignableFromIContentObject && isAssignableFromScriptableObject))
         {
            warningMessage = "";
            errorMessage = $"This attribute should only be used on ScriptableObjects that implement the [{nameof(IContentObject)}] interface.";
            return ReflectionCache.ValidationResult.Error;
         }

         warningMessage = "";
         errorMessage = "";
         return ReflectionCache.ValidationResult.Valid;
      }
   }
}
