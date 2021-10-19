using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beamable.Content;
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

   public static class ReflectionBasedSystemCache
   {
      public static Dictionary<Type, List<Type>> perBaseTypeLists =  new Dictionary<Type, List<Type>>();

      /// <summary>
      /// TODO: Implement on systems that require access to cached type information.
      /// </summary>
      public interface IReflectionBasedSystem
      {
         List<Type> TypesOfInterest { get; }

         void OnTypeOfInterestCacheLoaded(Type typeOfInterest, List<Type> typeOfInterestSubTypes);
         void OnTypeCacheLoaded(Dictionary<Type, List<Type>> typeCache);
      }
      
      public static void InitializeReflectionBasedSystemsCache(List<IReflectionBasedSystem> systems)
      {
         var baseTypes = systems.SelectMany(sys => sys.TypesOfInterest).ToList();
         
         // Parse Types
         BuildTypeCaches(baseTypes, out perBaseTypeLists);
         
         // Pass down to each given system only the types they are interested in
         foreach (var reflectionBasedSystem in systems)
         {
            reflectionBasedSystem.OnTypeCacheLoaded(perBaseTypeLists);
            foreach (var type in reflectionBasedSystem.TypesOfInterest)
            {
               reflectionBasedSystem.OnTypeOfInterestCacheLoaded(type, perBaseTypeLists[type]);
            }
         }
      }
      
      private static void BuildTypeCaches(IReadOnlyList<Type> baseTypes, out Dictionary<Type, List<Type>> perBaseTypeLists)
      {
         // TODO: Use TypeCache in editor and Unity 2019 and above
         var assemblies = AppDomain.CurrentDomain.GetAssemblies();
         perBaseTypeLists = new Dictionary<Type, List<Type>>();

         foreach (var assembly in assemblies)
         {
            var asmName = assembly.GetName().Name;
            if ("Tests".Equals(asmName)) continue; // TODO think harder about this.

            var types = assembly.GetTypes();
            foreach (var type in types)
            {
               var idxOfBaseType = FindBaseTypeIdx(type);
               if (idxOfBaseType == -1) continue;

               var baseType = baseTypes[idxOfBaseType];
               if (!perBaseTypeLists.TryGetValue(baseType, out var baseTypesList))
                  perBaseTypeLists[baseType] = baseTypesList = new List<Type>();
               
               baseTypesList.Add(type);
            }
         }

         int FindBaseTypeIdx(Type type)
         {
            for (var i = 0; i < baseTypes.Count; i++)
            {
               var baseType = baseTypes[i];
               if (baseType.IsAssignableFrom(type)) return i;
            }

            return -1;
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
   public static class ContentRegistry
   {
      private static readonly Dictionary<string, Type> contentTypeToClass = new Dictionary<string, Type>();
      private static readonly Dictionary<Type, string> classToContentType = new Dictionary<Type, string>();
      static ContentRegistry()
      {
         //LoadRuntimeTypeData();
         LoadRuntimeTypeData2();
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

      public static void LoadRuntimeTypeData2()
      {
         contentTypeToClass.Clear();
         classToContentType.Clear();
        
         // Fetch all assemblies that are not Test Assemblies (Use Type Cache on 2019 or newer and is in UnityEditor), otherwise we have to go through each assembly...
         // Fetch all Types inheriting from IContentObject -- we can move this later on into a Reflection Cache of sorts...
         BuildTypeCaches(new []{typeof(ContentObject)}, out var perBaseTypeLists);

         // Check for containing ContentType and ContentFormerlySerializedAs  Attributes
         BakeTypeToUniqueNameMappings<ContentTypeAttribute>(perBaseTypeLists[typeof(ContentObject)], 
            out var mappingsCurrent,
            false, 
            (type) => $"Type [{type.FullName}] does not have an attribute of type [{typeof(ContentTypeAttribute).FullName}] --- it will not be deserializable.");
         
         BakeTypeToUniqueNameMappings<ContentFormerlySerializedAsAttribute>(perBaseTypeLists[typeof(ContentObject)], out var mappingsFormer, false);
         
         var currentNames = mappingsCurrent.Select(map => map.Item1);
         var formerNames = mappingsFormer.Select(map => map.Item1);

         // Check for ContentType Collisions with Former Names
         var collisionsBetweenFormerAndCurrent = currentNames.Intersect(formerNames).ToList();
         if (collisionsBetweenFormerAndCurrent.Count > 0)
         {
            var errorBuilder = new StringBuilder();
            // For each collision, let the user know which Type is colliding with each Type.
            errorBuilder.AppendLine($"The following Unique names were used in different Content classes already.");
            errorBuilder.AppendLine($"Please check that all your ContentType and ContentFormerlySerializedAs attributes are unique.");
            errorBuilder.AppendLine($"These were the ones that collided [");
            foreach (var collidedType in collisionsBetweenFormerAndCurrent)
            {
               errorBuilder.AppendLine($"{collidedType}, ");
            }
            errorBuilder.AppendLine("]");
            throw new ArgumentException(errorBuilder.ToString());
         }

         // Cache Current Type data into dictionaries here
         foreach (var mapping in mappingsCurrent)
         {
            contentTypeToClass.Add(mapping.Item1, mapping.Item2);
            classToContentType.Add(mapping.Item2, mapping.Item1);
         }
         
         // Cache Former ContentType names to classes
         foreach (var mapping in mappingsFormer)
         {
            contentTypeToClass.Add(mapping.Item1, mapping.Item2);
         }
      }

      public interface IUniqueNamingAttribute<T> where T : Attribute, IUniqueNamingAttribute<T>
      {
         string Name { get; }

         bool IsAllowedOnType(Type type, out string errorMessage);

         // Alternatively, we can have the former serialized things here --- a benefit would be that we can generalize detection of former names.
         // On the other hand, this is not really a serialization thing just an organization attribute, so maybe keep it separate...
      }

      public static void BuildTypeCaches(IReadOnlyList<Type> baseTypes, out Dictionary<Type, List<Type>> perBaseTypeLists)
      {
         // TODO: Use TypeCache in editor and Unity 2019 and above
         var assemblies = AppDomain.CurrentDomain.GetAssemblies();
         perBaseTypeLists = new Dictionary<Type, List<Type>>();

         foreach (var assembly in assemblies)
         {
            var asmName = assembly.GetName().Name;
            if ("Tests".Equals(asmName)) continue; // TODO think harder about this.

            var types = assembly.GetTypes();
            foreach (var type in types)
            {
               var idxOfBaseType = FindBaseTypeIdx(type);
               if (idxOfBaseType == -1) continue;

               var baseType = baseTypes[idxOfBaseType];
               if (!perBaseTypeLists.TryGetValue(baseType, out var baseTypesList))
                  perBaseTypeLists[baseType] = baseTypesList = new List<Type>();
               
               baseTypesList.Add(type);
            }
         }

         int FindBaseTypeIdx(Type type)
         {
            for (var i = 0; i < baseTypes.Count; i++)
            {
               var baseType = baseTypes[i];
               if (baseType.IsAssignableFrom(type)) return i;
            }

            return -1;
         }
      }
      
      public static void BakeTypeToUniqueNameMappings<TAttribute>(IReadOnlyCollection<Type> types, out HashSet<(string, Type)> mappings,
         bool forceExistence = true, Func<Type, string> missingAttributeWarning = null)
         where TAttribute  : Attribute, IUniqueNamingAttribute<TAttribute>
      {
         // Declare helper lists to identify collisions and build comprehensive error message
         var addedTypeNames = new HashSet<string>(); 
         var collidedTypes = new Dictionary<string, HashSet<Type>>();
         
         mappings = new HashSet<(string, Type)>();
         
         // Iterate types and validate...
         foreach (var type in types)
         {
            var uniqueNamingAttribute = type.GetCustomAttribute<TAttribute>(false);
            
            // Check attribute existence, forceExistence can be used to make an attribute optional over the given type list. 
            var hasNamingAttribute = uniqueNamingAttribute != null;
            if (!hasNamingAttribute)
            {
               if(forceExistence)
                  throw new Exception($"Type [{type.FullName}] must have an attribute of type [{typeof(TAttribute).Name}].");
               
               if(missingAttributeWarning != null)
                  BeamableLogger.LogWarning(missingAttributeWarning(type));
               continue;
            }

            // Check if the attribute has further restrictions to apply on each individual type
            // TODO: Allow for warning and error results of this
            var isAllowedOnType = uniqueNamingAttribute.IsAllowedOnType(type, out var errorMessage);
            if (!isAllowedOnType) throw new Exception(errorMessage);

            // Verify collision happened and store data to make a nice error message
            var uniqueName = uniqueNamingAttribute.Name;
            var didntCollideUniqueName = addedTypeNames.Add(uniqueName);
            if (didntCollideUniqueName)
            {
               mappings.Add((uniqueName, type));
            }
            else
            {
               if (!collidedTypes.TryGetValue(uniqueName, out var collidedTypeSet))
               {
                  collidedTypeSet = new HashSet<Type>();
                  collidedTypes.Add(uniqueName, collidedTypeSet);
               }
               collidedTypeSet.Add(type);
            }
         }

         // If collisions happened, inform the game maker of exactly the types in which the attributes collided. 
         if (collidedTypes.Count > 0)
         {
            var collisionErrorMessage = new StringBuilder();
            foreach (var collidedNameTypesPair in collidedTypes)
            {
               var uniqueName = collidedNameTypesPair.Key;
               var collidingTypes = collidedNameTypesPair.Value;
               collidingTypes.Add(mappings.First(map => map.Item1 == uniqueName).Item2);
                  
               collisionErrorMessage.AppendLine($"The name [{uniqueName}] is being used in multiple different [{typeof(TAttribute).FullName}].");
               collisionErrorMessage.AppendLine($"Please change the names of the ones declared on these types so that they are unique across your project [");
               foreach (var collidedType in collidingTypes)
               {
                  collisionErrorMessage.AppendLine($"    {collidedType}, ");
               }
               collisionErrorMessage.AppendLine("]");
            }
            throw new ArgumentException(collisionErrorMessage.ToString());
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
         foreach (var kvp in classToContentType)
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
         return classToContentType.Keys;
      }

      public static IEnumerable<string> GetContentClassIds()
      {
         return contentTypeToClass.Keys;
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

         if (!contentTypeToClass.TryGetValue(typeName, out var type))
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
         return contentTypeToClass.TryGetValue(typeName, out type);
      }

      public static bool TryGetName(Type type, out string name)
      {
         return classToContentType.TryGetValue(type, out name);
      }

      public static Type NameToType(string name)
      {
         if (contentTypeToClass.TryGetValue(name, out var type))
         {
            return type;
         }
         return typeof(ContentObject);
      }

      public static string TypeToName(Type type)
      {
         if (classToContentType.TryGetValue(type, out var name))
         {
            return name;
         }

         throw new Exception($"No content name found for type=[{type.Name}]. Did you forget to add a ContentTypeAttribute with a unique name to it?");
      }
   }
}