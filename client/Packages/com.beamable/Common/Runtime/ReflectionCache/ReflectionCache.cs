using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Beamable.Common.Content;
using UnityEngine;

namespace Beamable.Common
{
    /// <summary>
    /// TODO: Implement on systems that require access to cached type information.
    /// </summary>
    public interface IReflectionCacheUserSystem
    {
        List<Type> TypesOfInterest { get; }

        List<Type> AttributesOfInterest { get; }

        /// <summary>
        /// Gets called once <see cref="TypesOfInterest"/> with a list of all Types that return true for <see cref="Type.IsAssignableFrom"/>.
        /// </summary>
        void OnTypeOfInterestCacheLoaded(Type typeOfInterest, List<Type> typeOfInterestSubTypes);
        
        /// <summary>
        /// Gets called once per <see cref="AttributesOfInterest"/> with a list of all Types and Attribute that return the <see cref="AttributesOfInterest"/> for <see cref="MemberInfo.GetCustomAttributes()"/>.
        /// </summary>
        void OnAttributeOfInterestCacheLoaded(Type attributeOfInterestType, List<(Type gameMakerType, Attribute attribute)> typesWithAttributeOfInterest);
        
        /// <summary>
        /// Gets called once the entire cache and all Error data that was gathered during the sweep.
        /// Can and should be used to display error messages and/or hints of how to organize a project with Beamable.
        /// </summary>
        void OnTypeCachesLoaded(Dictionary<Type, List<Type>> perBaseTypeCache, 
            Dictionary<Type, List<(Type gameMakerType, Attribute attribute)>> perAttributeTypeCache,
            List<(Assembly, Type, BeamableReflectionSystems)> invalidTypesInAssembliesErrorData);
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
        public enum ValidationResult
        {
            Valid,
            Warning,
            Error
        }

        public readonly static Dictionary<Type, List<Type>> PerBaseTypeCache = new Dictionary<Type, List<Type>>();
        public readonly static Dictionary<Type, List<(Type, Attribute)>> PerAttributeCache = new Dictionary<Type, List<(Type, Attribute)>>();

        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// This gets called during Beamable's initialization with all system instances that care about reflection types.
        /// The list of specific sub-classes that we care about are declared inside each system that implements the <see cref="IReflectionCacheUserSystem"/>.
        /// </summary>
        public static void InitializeReflectionBasedSystemsCache(List<IReflectionCacheUserSystem> systems)
        {
            if (IsInitialized) return;

            

            // Parse Types
            // As needed, add other cache-ing strategies here (per attribute type, per-assembly, etc...), just keep it inside this method
            // so we don't run over the entire code-base multiple times. If you want to split it, move each strategy to different thread (no need to lock anything)
            // since no simultaneous read/write access to the same data can happen.
            // The Invalid Types in Assemblies Error Data is here so game makers can enforce certain Beamable types to not be declared in specific assembly definitions
            //   (It, coupled with the IgnoreFromBeamableAssemblySweepAttribute, also lets us ignore test assemblies that we choose to but keep others)
            BuildTypeCaches(systems, in PerBaseTypeCache, in PerAttributeCache, out var invalidTypesInAssembliesErrorData);

            // Pass down to each given system only the types they are interested in
            foreach (var reflectionBasedSystem in systems)
            {
                reflectionBasedSystem.OnTypeCachesLoaded(PerBaseTypeCache, PerAttributeCache, invalidTypesInAssembliesErrorData);
                foreach (var type in reflectionBasedSystem.TypesOfInterest)
                {
                    reflectionBasedSystem.OnTypeOfInterestCacheLoaded(type, PerBaseTypeCache[type]);
                }

                foreach (var attributeType in reflectionBasedSystem.AttributesOfInterest)
                {
                    reflectionBasedSystem.OnAttributeOfInterestCacheLoaded(attributeType, PerAttributeCache[attributeType]);
                }
            }

            IsInitialized = true;
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
            out HashSet<(Type gameMakerType, TAttribute attribute)> mappings,
            out HashSet<Type> missingAttributesTypes,
            out HashSet<(Type notAllowedType, ValidationResult result, string notAllowedReason)> failedAllowedValidationTypes)
            where TAttribute : Attribute, IReflectionCachingAttribute<TAttribute>
        {
            mappings = new HashSet<(Type, TAttribute)>();
            missingAttributesTypes = new HashSet<Type>();
            failedAllowedValidationTypes = new HashSet<(Type notAllowedType, ValidationResult result, string notAllowedReason)>();

            // Iterate types and validate...
            foreach (var type in types)
            {
                var reflectionCachingAttribute = type.GetCustomAttributes<TAttribute>(false).ToList();

                // Check attribute existence, if none exist skip validation of type. 
                var hasAttribute = reflectionCachingAttribute.Any();
                if (!hasAttribute)
                {
                    missingAttributesTypes.Add(type);
                    continue;
                }

                foreach (var cachingAttribute in reflectionCachingAttribute)
                {
                    // Check if the attribute has further restrictions to apply on each individual type
                    var isAllowedOnType = cachingAttribute.IsAllowedOnType(type, out var warningMessage, out var errorMessage);
                    switch (isAllowedOnType)
                    {
                        // TODO: Remove error messaging from this layer instead, bubble up organized error data so we can display a single comprehensive error message of
                        // all warning and/or errors.
                        case ValidationResult.Error:
                        {
                            failedAllowedValidationTypes.Add((type, isAllowedOnType, errorMessage));
                            break;
                        }
                        case ValidationResult.Warning:
                        {
                            failedAllowedValidationTypes.Add((type, isAllowedOnType, warningMessage));
                            break;
                        }
                        case ValidationResult.Valid:
                        {
                            mappings.Add((type, cachingAttribute));
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
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
            out Dictionary<string, HashSet<Type>> collidedTypes,
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
            params IEnumerable<(Type, TAttribute)>[] listOfTypeMappings)
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

        public static List<(MethodInfo method, TAttribute attr)> GatherInstanceMethodsWithAttributes<TAttribute>(Type type,
            out List<MethodInfo> methodsMissingAttr)
            where TAttribute : Attribute
        {
            return GatherMethodsWithAttributes<TAttribute>(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, out methodsMissingAttr);
        }

        public static List<(MethodInfo method, TAttribute attr)> GatherStaticMethodsWithAttributes<TAttribute>(Type type,
            out List<MethodInfo> methodsMissingAttr)
            where TAttribute : Attribute
        {
            return GatherMethodsWithAttributes<TAttribute>(type, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, out methodsMissingAttr);
        }

        private static List<(MethodInfo method, TAttribute attr)> GatherMethodsWithAttributes<TAttribute>(Type type, BindingFlags flags,
            out List<MethodInfo> methodsMissingAttr)
            where TAttribute : Attribute
        {
            var methods = type.GetMethods(flags);

            var foundAttr = new List<(MethodInfo method, TAttribute attr)>();
            methodsMissingAttr = new List<MethodInfo>();
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<TAttribute>();
                if (attribute == null)
                    methodsMissingAttr.Add(method);
                else
                    foundAttr.Add((method, attribute));
            }

            return foundAttr;
        }


        /// <summary>
        /// Internal method that generates, given a list of base types, a dictionary of each type that <see cref="Type.IsAssignableFrom"/> to each base type. 
        /// </summary>
        private static void BuildTypeCaches(List<IReflectionCacheUserSystem> systems,
            in Dictionary<Type, List<Type>> perBaseTypeLists,
            in Dictionary<Type, List<(Type gameMakerType, Attribute attributeOfInterest)>> perAttributeLists,
            out List<ValueTuple<Assembly, Type, BeamableReflectionSystems>> invalidTypesInAssembliesErrorData)
        {
            var baseTypesOfInterest = systems.SelectMany(sys => sys.TypesOfInterest).ToList();
            var attributesOfInterest = systems.SelectMany(sys => sys.AttributesOfInterest).ToList();
            
            perBaseTypeLists.Clear();
            perAttributeLists.Clear();

            // Initialize Per-Base Cache
            {
                perBaseTypeLists.Clear();
                foreach (var baseType in baseTypesOfInterest)
                {
                    perBaseTypeLists.Add(baseType, new List<Type>());
                }
            }

            // Initialize Per-Attribute Cache
            {
                perAttributeLists.Clear();
                foreach (var attrType in attributesOfInterest)
                {
                    perAttributeLists.Add(attrType, new List<(Type gameMakerType, Attribute attributeOfInterest)>());
                }
            }

            // TODO: Use TypeCache in editor and Unity 2019 and above ---  This path should go through BEAMABLE_MICROSERVICE || DB_MICROSERVICE
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var validAssemblies = assemblies.Where(asm => !asm.GetCustomAttributes(typeof(IgnoreFromBeamableAssemblySweepAttribute)).Any());
            foreach (var assembly in validAssemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // Check for attributes of interest
                    {
                        // Get a list of all attributes of interest that were found on this type.
                        var matchingAttributes = FindAttributesOfInterest(type, attributesOfInterest);
                        // For each of those, add the attribute and its matching GameMaker-declared Type to the cache. 
                        foreach (var matchingAttribute in matchingAttributes)
                        {
                            var attrType = matchingAttribute.GetType();

                            if (perAttributeLists.TryGetValue(attrType, out var attributeOfInterestList))
                                attributeOfInterestList.Add((type, matchingAttribute));
                        }
                    }

                    // Check for base types of interest
                    {
                        var idxOfBaseType = FindBaseTypeIdx(type, baseTypesOfInterest);
                        if (idxOfBaseType != -1)
                        {
                            var baseType = baseTypesOfInterest[idxOfBaseType];
                            if (perBaseTypeLists.TryGetValue(baseType, out var baseTypesList))
                                baseTypesList.Add(type);
                        }
                    }
                }
            }

            
            // If we are in the editor, sweep the invalid assemblies to enforce that
            invalidTypesInAssembliesErrorData = new List<ValueTuple<Assembly, Type, BeamableReflectionSystems>>();
#if UNITY_EDITOR
            var invalidAssembliesData = assemblies
                .Where(asm => asm.GetCustomAttributes(typeof(IgnoreFromBeamableAssemblySweepAttribute)).Any())
                .Select(asm => new ValueTuple<Assembly, IgnoreFromBeamableAssemblySweepAttribute>()
                {
                    Item1 = asm,
                    Item2 = asm.GetCustomAttributes(typeof(IgnoreFromBeamableAssemblySweepAttribute))
                        .Cast<IgnoreFromBeamableAssemblySweepAttribute>()
                        .First()
                })
                .Where(asmData => asmData.Item2.LogComplianceFailureAsError != BeamableReflectionSystems.None);

            foreach (var (asm, assemblySweepAttribute) in invalidAssembliesData)
            {
                var types = asm.GetTypes();

                foreach (var type in types)
                {
                    var isOfBaseTypeOfInterest = FindBaseTypeIdx(type, baseTypesOfInterest) != -1;
                    var hasOfAttributeOfInterest = FindAttributesOfInterest(type, attributesOfInterest).Any();
                    if (isOfBaseTypeOfInterest || hasOfAttributeOfInterest)
                    {
                        invalidTypesInAssembliesErrorData.Add((asm, type, assemblySweepAttribute.LogComplianceFailureAsError));
                    }
                }
            }
            
#else

#endif


            int FindBaseTypeIdx(Type type, IEnumerable<Type> baseTypesToSearchIn)
            {
                var typesToSearchIn = baseTypesToSearchIn.ToList();
                for (var i = 0; i < typesToSearchIn.Count; i++)
                {
                    var baseType = typesToSearchIn[i];
                    if (baseType.IsAssignableFrom(type)) return i;
                }

                return -1;
            }

            IEnumerable<Attribute> FindAttributesOfInterest(Type type, List<Type> attributesToSearchFor)
            {
                return attributesToSearchFor.Select(attributeType => type.GetCustomAttribute(attributeType, false)).Where(attr => attr != null);
            }
        }
    }
}