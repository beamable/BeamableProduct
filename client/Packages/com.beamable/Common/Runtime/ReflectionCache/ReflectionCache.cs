using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beamable.Common.Content;

namespace Beamable.Common
{
    /// <summary>
    /// TODO: Implement on systems that require access to cached type information.
    /// </summary>
    public interface IReflectionCacheUserSystem
    {
        List<Type> TypesOfInterest { get; }

        void OnTypeOfInterestCacheLoaded(Type typeOfInterest, List<Type> typeOfInterestSubTypes);
        void OnTypeCacheLoaded(Dictionary<Type, List<Type>> typeCache);
    }

    public interface IReflectionCachingAttribute<T> where T : Attribute, IReflectionCachingAttribute<T>
    {
        ReflectionCache.ValidationResult IsAllowedOnType(Type type, out string warningMessage, out string errorMessage);
    }
    
    public interface IUniqueNamingAttribute<T> : IReflectionCachingAttribute<T> where T : Attribute, IUniqueNamingAttribute<T>
    {
        string Name { get; }

        ReflectionCache.ValidationResult IsValidNameForType(string potentialName, out string warningMessage, out string errorMessage);
    }

    /// <summary>
    /// Cached List of Types stored by a specific type. Currently only supports Cache-ing by BaseType/Interface.
    ///
    /// Used to initialize all reflection based systems with consistent validation and to ensure we are only doing the parsing once.
    /// We can also use this to setup up compile-time validation of our Attribute-based systems such as Content and Microservices.
    /// </summary>
    public static class ReflectionCache
    {
        public enum ValidationResult { Valid, Warning, Error }
        
        public readonly static Dictionary<Type, List<Type>> perBaseTypeLists = new Dictionary<Type, List<Type>>();


        /// <summary>
        /// This gets called during Beamable's initialization with all system instances that care about reflection types.
        /// The list of specific sub-classes that we care about are declared inside each system that implements the <see cref="IReflectionCacheUserSystem"/>.
        /// </summary>
        public static void InitializeReflectionBasedSystemsCache(List<IReflectionCacheUserSystem> systems)
        {
            var baseTypes = systems.SelectMany(sys => sys.TypesOfInterest).ToList();

            // Parse Types
            BuildTypeCaches(baseTypes, in perBaseTypeLists);

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

        /// <summary>
        /// Helper function to be used primarily inside of <see cref="IReflectionCacheUserSystem.OnTypeOfInterestCacheLoaded"/> that gathers types we care about and
        /// throw error messages formatted by our callers.
        /// </summary>
        /// <param name="types">List of types to look for the attribute over.</param>
        /// <param name="mappings">List of Type to Attribute mappings we are going to generate</param>
        /// <param name="missingAttributeException">If NOT NULL, it means the attribute MUST EXIST over these types at all times. If null, it means the attribute is optional.</param>
        /// <param name="missingAttributeWarning">IF NOT NULL, it means that, while the attribute is optional, it comes with restrictions the game maker must know about. We use this to inform them.</param>
        public static void GatherReflectionCacheAttributesFromTypes<TAttribute>(IReadOnlyCollection<Type> types, 
            out HashSet<(Type, TAttribute)> mappings,
            Func<Type, string> missingAttributeException = null,
            Func<Type, string> missingAttributeWarning = null) where TAttribute : Attribute, IReflectionCachingAttribute<TAttribute>
        {
            mappings = new HashSet<(Type, TAttribute)>();

            // Iterate types and validate...
            foreach (var type in types)
            {
                var reflectionCachingAttribute = type.GetCustomAttribute<TAttribute>(false);

                // Check attribute existence, forceExistence can be used to make an attribute optional over the given type list. 
                var hasAttribute = reflectionCachingAttribute != null;
                if (!hasAttribute)
                {
                    if (missingAttributeException != null)
                        throw new Exception(missingAttributeException(type));
                    //$"Type [{type.FullName}] must have an attribute of type [{typeof(TAttribute).Name}]."

                    if (missingAttributeWarning != null)
                        BeamableLogger.LogWarning(missingAttributeWarning(type));
                    continue;
                }

                // Check if the attribute has further restrictions to apply on each individual type
                var isAllowedOnType = reflectionCachingAttribute.IsAllowedOnType(type, out var warningMessage, out var errorMessage);
                if (isAllowedOnType == ValidationResult.Error) throw new Exception(errorMessage);
                if (isAllowedOnType == ValidationResult.Warning) BeamableLogger.LogWarning(warningMessage);
                if (isAllowedOnType == ValidationResult.Valid) mappings.Add((type, reflectionCachingAttribute));
            }
        }

        /// <summary>
        /// Helper function to be used primarily in <see cref="IReflectionCacheUserSystem.OnTypeOfInterestCacheLoaded"/> after gathering all <see cref="IReflectionCachingAttribute{T}"/> that
        /// we care about. This call will discard existing dictionaries "Type <-> UniqueName" dictionaries.
        /// </summary>
        /// <param name="typeToUniqueName">Mapping of Type to the <see cref="IUniqueNamingAttribute{T}.Name"/> that the game maker provided.</param>
        /// <param name="uniqueNameToType">Mapping of <see cref="IUniqueNamingAttribute{T}.Name"/> that the game maker provided to the Type.</param>
        /// <param name="collidedTypes"><see cref="IUniqueNamingAttribute{T}"/> enforces that each name must be unique. This list has all types whose name failed this criteria.</param>
        /// <param name="listOfTypeMappings">The list of Type and Attribute mappings we should bake.</param>
        public static void RebakeUniqueNameCollisionOverTypes<TAttribute>(out Dictionary<Type, string> typeToUniqueName, 
            out Dictionary<string, Type> uniqueNameToType,
            out  Dictionary<string, HashSet<Type>> collidedTypes,
            params HashSet<(Type, TAttribute)>[] listOfTypeMappings) 
            where TAttribute : Attribute, IUniqueNamingAttribute<TAttribute>
        {
            typeToUniqueName = new Dictionary<Type, string>();
            uniqueNameToType = new Dictionary<string, Type>();
            BakeUniqueNameCollisionOverTypes(ref typeToUniqueName, ref uniqueNameToType, out collidedTypes, listOfTypeMappings);
        }
        
        /// <summary>
        /// Helper function to be used primarily in <see cref="IReflectionCacheUserSystem.OnTypeOfInterestCacheLoaded"/> after gathering all <see cref="IReflectionCachingAttribute{T}"/> that
        /// we care about. This call will attempt to add to the existing dictionaries.
        /// </summary>
        /// <param name="typeToUniqueName">Mapping of Type to the <see cref="IUniqueNamingAttribute{T}.Name"/> that the game maker provided.</param>
        /// <param name="uniqueNameToType">Mapping of <see cref="IUniqueNamingAttribute{T}.Name"/> that the game maker provided to the Type.</param>
        /// <param name="collidedTypes"><see cref="IUniqueNamingAttribute{T}"/> enforces that each name must be unique. This list has all types whose name failed this criteria.</param>
        /// <param name="listOfTypeMappings">The list of Type and Attribute mappings we should bake.</param>
        public static void BakeUniqueNameCollisionOverTypes<TAttribute>(ref Dictionary<Type, string> typeToUniqueName, 
            ref Dictionary<string, Type> uniqueNameToType,
            out Dictionary<string, HashSet<Type>> collidedTypes,
            params HashSet<(Type, TAttribute)>[] listOfTypeMappings) 
            where TAttribute : Attribute, IUniqueNamingAttribute<TAttribute>
        {
            // Declare helper lists to identify collisions and build comprehensive error message
            collidedTypes = new Dictionary<string, HashSet<Type>>();
            
            foreach (var listOfTypeMapping in listOfTypeMappings)
            {
                foreach (var typeAndUniqueName in listOfTypeMapping)
                {
                    var type = typeAndUniqueName.Item1;
                    var uniqueNamingAttribute = typeAndUniqueName.Item2;
                    
                    
                    // Get unique name and apply any validation we need based on TAttribute.IsValidNameForType.
                    var uniqueName = uniqueNamingAttribute.Name;
                    var isValidNameForType = uniqueNamingAttribute.IsValidNameForType(uniqueName, out var validationWarning, out var validationError);
                    switch (isValidNameForType)
                    {
                        case ValidationResult.Valid:
                        {
                            // Check if the unique name we are trying to add is already in the dictionary
                            var collidedWithExistingType = uniqueNameToType.ContainsKey(uniqueName);
                            
                            // If not, we add it to both dictionaries.
                            if (!collidedWithExistingType)
                            {
                                typeToUniqueName.Add(type, uniqueName);
                                uniqueNameToType.Add(uniqueName, type);
                            }
                            // Otherwise, we add it to a collision set for that particular name.
                            // This is returned to the caller so they can decide how to error this out. 
                            else
                            {
                                if (!collidedTypes.TryGetValue(uniqueName, out var collidedTypeSet))
                                {
                                    collidedTypeSet = new HashSet<Type>();
                                    collidedTypes.Add(uniqueName, collidedTypeSet);
                                }

                                collidedTypeSet.Add(type);
                            }
                            break;
                        }
                        case ValidationResult.Warning:
                        {
                            BeamableLogger.LogWarning(validationWarning);
                            break;
                        }
                        case ValidationResult.Error:
                        {
                            throw new Exception(validationError);
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        /// <summary>
        /// Internal method that generates, given a list of base types, a dictionary of each type that <see cref="Type.IsAssignableFrom"/> to each base type. 
        /// </summary>
        private static void BuildTypeCaches(IReadOnlyList<Type> baseTypes, in Dictionary<Type, List<Type>> perBaseTypeLists)
        {
            perBaseTypeLists.Clear();
            
            // TODO: Use TypeCache in editor and Unity 2019 and above
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
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
}