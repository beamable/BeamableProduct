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
   public class ContentTypeAttribute : UnityEngine.Scripting.PreserveAttribute, IHasSourcePath, ContentRegistry.IUniqueNamingAttribute<ContentTypeAttribute>
   {
      public string TypeName { get; }
      public string SourcePath { get; }

      public ContentTypeAttribute(string typeName, [CallerFilePath] string sourcePath = "")
      {
         TypeName = typeName;
         SourcePath = sourcePath;
      }

      public string Name => TypeName;

      public bool IsAllowedOnType(Type type, out string errorMessage)
      {
         bool isAssignableFromIContentObject = typeof(IContentObject).IsAssignableFrom(type);

#if !DB_MICROSERVICE
         bool isAssignableFromScriptableObject = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
#else
         bool isAssignableFromScriptableObject = true;
#endif
         if (!(isAssignableFromIContentObject && isAssignableFromScriptableObject))
            errorMessage =
               $"Type [{type}] must not have a [{typeof(ContentTypeAttribute).FullName}]." +
               $"\nThis attribute should only be used on ScriptableObjects that implement the [{typeof(IContentObject).FullName}] interface.";

         errorMessage = "";
         return isAssignableFromIContentObject && isAssignableFromScriptableObject;
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
   public class ContentFormerlySerializedAsAttribute : Attribute, ContentRegistry.IUniqueNamingAttribute<ContentTypeAttribute>
   {
      public string OldTypeName { get; }
      public ContentFormerlySerializedAsAttribute(string oldTypeName)
      {
         OldTypeName = oldTypeName;
      }
      
      public string Name => OldTypeName;

      public bool IsAllowedOnType(Type type, out string errorMessage)
      {
         bool isAssignableFromIContentObject = typeof(IContentObject).IsAssignableFrom(type);

#if !DB_MICROSERVICE
         bool isAssignableFromScriptableObject = typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
#else
         bool isAssignableFromScriptableObject = true;
#endif
         if (!(isAssignableFromIContentObject && isAssignableFromScriptableObject))
            errorMessage =
               $"Type [{type}] must not have a [{typeof(ContentTypeAttribute).FullName}]." +
               $"\nThis attribute should only be used on ScriptableObjects that implement the [{typeof(IContentObject).FullName}] interface.";

         errorMessage = "";
         return isAssignableFromIContentObject && isAssignableFromScriptableObject;
      }
   }
}
