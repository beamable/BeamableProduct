using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using UnityEngine;

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
   public class ContentTypePair
   {
      public Type Type;
      public string Name;
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
   public static class ContentRegistry
   {
      public class ReflectionRegistry : IReflectionCacheUserSystem
      {
         internal static readonly List<Type> InterestTypes = new List<Type> {typeof(IContentObject), typeof(ContentObject)};
         
         internal Dictionary<string, Type> ContentTypeToClass = new Dictionary<string, Type>();
         internal  Dictionary<Type, string> ClassToContentType = new Dictionary<Type, string>();
         
         public List<Type> TypesOfInterest => InterestTypes;
         public List<Type> AttributesOfInterest => new List<Type>();

         public void OnTypeOfInterestCacheLoaded(Type typeOfInterest, List<Type> typeOfInterestSubTypes)
         {
            // Gather all Content Type Attributes (builds lists of errors/warnings so we can inform game makers of these).
            ReflectionCache.GatherReflectionCacheAttributesFromTypes<ContentTypeAttribute>(typeOfInterestSubTypes, 
               out var currentMappings,
               out var missingContentTypeAttrs,
               out var contentTypeAttrsThatAreNotAllowedOnTheirTypes);

            // Error validation for missing attributes --- simple explanation that the type will not be serializable or creatable via the Content Manager.
            if (missingContentTypeAttrs.Count > 0)
            { 
               var warningStringBuilder = new StringBuilder();
               warningStringBuilder.AppendLine($"The attribute [{nameof(ContentTypeAttribute)}] was not found on the following types:");
               foreach (var missingAttributesType in missingContentTypeAttrs)
               {
                  warningStringBuilder.AppendLine($"\t{missingAttributesType.FullName}");
               }

               warningStringBuilder.AppendLine();
               warningStringBuilder.AppendLine("These types are not deserializable by Beamable and you will not be able to create content of this type directly via the Content Manager!");
               
               BeamableLogger.Log(warningStringBuilder.ToString());
            }

            // Error validation for attributes placed on invalid classes/types over which they have no effect.
            if (contentTypeAttrsThatAreNotAllowedOnTheirTypes.Count > 0)
            {
               var validationResultsMessage = new StringBuilder();
               validationResultsMessage.AppendLine($"The following [{nameof(ContentTypeAttribute)}] are not valid. Please remove them in order to use them with Beamable's Content System.");
               
               foreach (var failedAllowedValidationType in contentTypeAttrsThatAreNotAllowedOnTheirTypes)
               {
                  var (type, validationResult, message) = failedAllowedValidationType;
                  validationResultsMessage.AppendLine($"{type.Name} | {validationResult} => {message}");
               }
               
               BeamableLogger.Log(validationResultsMessage.ToString());
            }
            
            
            ReflectionCache.GatherReflectionCacheAttributesFromTypes<ContentFormerlySerializedAsAttribute>(typeOfInterestSubTypes, 
               out var formerMappings, 
               out _, // Discard list of missing attributes since this is a fully optional attribute
               out var failedAllowedValidationTypes);
            
            if (failedAllowedValidationTypes.Count > 0)
            {
               var validationResultsMessage = new StringBuilder();
               validationResultsMessage.AppendLine($"The following [{nameof(ContentFormerlySerializedAsAttribute)}] are not valid. Please correct them in order to use them with Beamable's Content System.");
               
               foreach (var failedAllowedValidationType in failedAllowedValidationTypes)
               {
                  var (type, validationResult, message) = failedAllowedValidationType;
                  validationResultsMessage.AppendLine($"{type.Name} | {validationResult} => {message}");
               }
               
               BeamableLogger.Log(validationResultsMessage.ToString());
            }
            
            // Quick hack to "fake" the ContentFormerlySerializedAttribute is a ContentType Attribute --- easy and simple way to verify name collisions is to pretend they are of the same type.
            var typeConversionOfAttributes = formerMappings.ToList().ConvertAll(input => (input.gameMakerType, new ContentTypeAttribute(input.attribute.Name)));
            ReflectionCache.BakeUniqueNameCollisionOverTypes(ref ClassToContentType , ref ContentTypeToClass, 
               out var collidedTypes,
               currentMappings,
               typeConversionOfAttributes);

            var mappings = currentMappings.Union(typeConversionOfAttributes).ToList();
            // If collisions happened, inform the game maker of exactly the types in which the attributes collided. 
            if (collidedTypes.Count > 0)
            {
               var collisionErrorMessage = new StringBuilder();
               foreach (var collidedNameTypesPair in collidedTypes)
               {
                  var uniqueName = collidedNameTypesPair.Key;
                  var collidingTypes = collidedNameTypesPair.Value;
                  collidingTypes.Add(mappings.First(map => map.Item2.Name == uniqueName).Item1);
                  
                  collisionErrorMessage.AppendLine($"The name [{uniqueName}] is being used in multiple different [{nameof(ContentTypeAttribute)}] or [{typeof(ContentFormerlySerializedAsAttribute).FullName}].");
                  collisionErrorMessage.AppendLine($"Please change the names of the ones declared on these types so that they are unique across your project:");
                  foreach (var collidedType in collidingTypes)
                  {
                     collisionErrorMessage.AppendLine($"{collidedType}");
                  }
                  collisionErrorMessage.AppendLine();
               }
               throw new ArgumentException(collisionErrorMessage.ToString());
            }
            
            
         }

         public void OnAttributeOfInterestCacheLoaded(Type attributeOfInterestType, List<(Type gameMakerType, Attribute attribute)> typesWithAttributeOfInterest)
         {
            // Do nothing with attributes of interest since we look for content objects via interfaces and inheritances
         }

         public void OnTypeCachesLoaded(Dictionary<Type, List<Type>> perBaseTypeCache, Dictionary<Type, List<(Type gameMakerType, Attribute attribute)>> perAttributeTypeCache)
         {
            // Do nothing with the Entire Cache
         }

      }

      public static readonly ReflectionRegistry ContentTypeCaches = new ReflectionRegistry();
      
      private static readonly Dictionary<string, Type> contentTypeToClass = new Dictionary<string, Type>();
      private static readonly Dictionary<Type, string> classToContentType = new Dictionary<Type, string>();
      
      static ContentRegistry()
      {
         //LoadRuntimeTypeData();
      }

      public static void LoadRuntimeTypeData(HashSet<Type> contentTypes=null)
      {
         contentTypeToClass.Clear();
         classToContentType.Clear();

         contentTypes = contentTypes ?? GetTypesFromAssemblies();
         foreach (var type in contentTypes)
         {
            string typeName = GetContentTypeName(type); // XXX Do I need this!??? Maybe the order they come back in is sneaky for the type->name

            var formerlySerializedTypeNames = GetAllValidContentTypeNames(type, true).ToList();
            foreach (var possibleTypeName in formerlySerializedTypeNames)
            {
               if (possibleTypeName == null) continue;
               contentTypeToClass[possibleTypeName] = type;
            }
            contentTypeToClass[typeName] = type;
            classToContentType[type] = typeName;
         }
      }
      public static HashSet<Type> GetTypesFromAssemblies()
      {
         var types = new HashSet<Type>();
         var contentTypeNames = new HashSet<string>();
         var nameCollisionDetector = new HashSet<(string, Type)>();
         var assemblies = AppDomain.CurrentDomain.GetAssemblies();
         foreach (var assembly in assemblies)
         {
            var asmName = assembly.GetName().Name;
            if ("Tests".Equals(asmName)) continue; // TODO think harder about this.
            try
            {
               foreach (var type in assembly.GetTypes())
               {
                  var contentTypeAttribute = type.GetCustomAttribute<ContentTypeAttribute>(false);
                  bool hasContentAttribute = contentTypeAttribute != null;
                  bool isAssignableFromIContentObject = typeof(IContentObject).IsAssignableFrom(type);

#if !DB_MICROSERVICE
                  bool isAssignableFromScriptableObject = typeof(ScriptableObject).IsAssignableFrom(type);
#else
                  bool isAssignableFromScriptableObject = true;
#endif

                  if (hasContentAttribute && isAssignableFromIContentObject && isAssignableFromScriptableObject)
                  {
                     var contentTypeName = contentTypeAttribute.TypeName;
                     types.Add(type);
                     
                     var didCollideWithOtherTypeNames = !contentTypeNames.Add(contentTypeName);
                     if (didCollideWithOtherTypeNames)
                     {
                        var collidingTypes = nameCollisionDetector.Where(asd => asd.Item1 == contentTypeName).Select(asd => asd.Item2);
                        collidingTypes = collidingTypes.Append(type);
                        
                        var errorMessageBuilder = new StringBuilder();
                        errorMessageBuilder.AppendLine($"The name [{contentTypeName}] in a ContentTypeAttribute is being used in multiple different classes.");
                        errorMessageBuilder.AppendLine($"Please rename these so that they are unique across your project [");
                        foreach (var collidedType in collidingTypes)
                        {
                           errorMessageBuilder.AppendLine($"{collidedType}, ");
                        }
                        errorMessageBuilder.AppendLine("]");

                        throw new ArgumentException(errorMessageBuilder.ToString());
                     }
 
                     nameCollisionDetector.Add((contentTypeName, type));
                  }
               }
            }
            catch (Exception ex)
            {
               BeamableLogger.LogError(ex);
            }
         }

         return types;
      }
      
      private static IEnumerable<string> GetAllValidContentTypeNames(Type contentType, bool includeFormerlySerialized)
      {
         if (contentType == null)
         {
            yield return null;
            yield break;
         }

#if !DB_MICROSERVICE
         if (contentType == typeof(ScriptableObject))
         {
            yield return null;
            yield break;
         }


#endif
         var contentTypeAttribute = contentType.GetCustomAttribute<ContentTypeAttribute>(false);

         if (contentTypeAttribute == null)
         {
            /*
             * [ContentType("x")]
             * class X : ContentObject
             *
             * class Y : X
             *
             * [ContentType("z")]
             * class Z : Y
             *
             * x.z.foo
             */
            //
            var baseNames = GetAllValidContentTypeNames(contentType.BaseType, includeFormerlySerialized);
            foreach (var baseName in baseNames)
            {
               yield return baseName;
            }
            yield break;
         }

         var startType = contentTypeAttribute.TypeName;

         var possibleNames = new HashSet<string> {startType};

         if (includeFormerlySerialized)
         {
            var formerlySerializedAsAttributes =
               contentType.GetCustomAttributes<ContentFormerlySerializedAsAttribute>(false);
            foreach (var formerlySerializedAsAttribute in formerlySerializedAsAttributes)
            {
               if (string.IsNullOrEmpty(formerlySerializedAsAttribute?.OldTypeName)) continue;
               possibleNames.Add(formerlySerializedAsAttribute.OldTypeName);
            }
         }

         var possibleEndNames = GetAllValidContentTypeNames(contentType.BaseType, includeFormerlySerialized);

         foreach (var possibleEnd in possibleEndNames)
         {
            foreach (var possibleStart in possibleNames)
            {
               if (possibleStart != null && possibleEnd != null)
               {
                  yield return string.Join(".", possibleEnd, possibleStart);
               }
               else
               {
                  yield return possibleEnd ?? possibleStart;
               }
            }
         }
      }

      public static string GetContentTypeName(Type contentType)
      {
         return GetAllValidContentTypeNames(contentType, false).First();
      }

      public static IEnumerable<ContentTypePair> GetAll()
      {
         foreach (var kvp in ContentTypeCaches.ClassToContentType)
         {
            yield return new ContentTypePair
            {
               Type = kvp.Key,
               Name = kvp.Value
            };
         }
      }

      public static IEnumerable<Type> GetContentTypes()
      {
         return ContentTypeCaches.ClassToContentType.Keys;
      }

      public static IEnumerable<string> GetContentClassIds()
      {
         return ContentTypeCaches.ContentTypeToClass.Keys;
      }

      public static string GetTypeNameFromId(string id)
      {
         return id.Substring(0, id.LastIndexOf("."));
      }

      public static string GetContentNameFromId(string id)
      {
         return id.Substring(id.LastIndexOf(".") + 1);
      }

      public static Type GetTypeFromId(string id)
      {
         var typeName = GetTypeNameFromId(id);

         if (!ContentTypeCaches.ContentTypeToClass.TryGetValue(typeName, out var type))
         {
            // the type doesn't exist, but maybe we can try again?

            var hasAnotherDot = typeName.IndexOf('.') > -1;
            if (hasAnotherDot)
            {
               return GetTypeFromId(typeName);
            }
            else
            {
               return typeof(ContentObject);
            }
         }

         return type;
      }

      public static bool TryGetType(string typeName, out Type type)
      {
         return ContentTypeCaches.ContentTypeToClass.TryGetValue(typeName, out type);
      }

      public static bool TryGetName(Type type, out string name)
      {
         return ContentTypeCaches.ClassToContentType.TryGetValue(type, out name);
      }

      public static Type NameToType(string name)
      {
         if (ContentTypeCaches.ContentTypeToClass.TryGetValue(name, out var type))
         {
            return type;
         }
         return typeof(ContentObject);
      }

      public static string TypeToName(Type type)
      {
         if (ContentTypeCaches.ClassToContentType.TryGetValue(type, out var name))
         {
            return name;
         }

         throw new Exception($"No content name found for type=[{type.Name}]. Did you forget to add a ContentTypeAttribute with a unique name to it?");
      }

      
   }
}