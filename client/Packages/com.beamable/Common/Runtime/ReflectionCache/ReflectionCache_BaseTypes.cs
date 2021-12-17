using System;
using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Reflection
{
    /// <summary>
    /// Struct that holds data for all BaseType-related reflection data caches.
    /// </summary>
    public readonly struct PerBaseTypeCache
    {
        /// <summary>
        /// List of all classes and/or interfaces that we care about when processing.
        /// </summary>
        public readonly List<BaseTypeOfInterest> BaseTypes;

        /// <summary>
        /// Cached list of <see cref="BaseTypeOfInterest"/> and all <see cref="Type"/>s that were found in the assembly sweep that matched.
        /// </summary>
        public readonly Dictionary<BaseTypeOfInterest, List<Type>> MappedSubtypes;

        /// <summary>
        /// Constructs a new <see cref="PerBaseTypeCache"/>.
        /// If planning to build with <see cref="ReflectionCache.BuildTypeCaches"/>, call this with large pre-allocated lists since allocation is expensive and list's "expansion when full" re-allocates.  
        /// </summary>
        public PerBaseTypeCache(List<BaseTypeOfInterest> baseTypes, Dictionary<BaseTypeOfInterest, List<Type>> mappedSubtypes)
        {
            BaseTypes = baseTypes;
            MappedSubtypes = mappedSubtypes;
        }
    }

    /// <summary>
    /// Struct that defines an base type (class or interface) of interest and gives us information on where to look for it or what to cache in relation to it.
    /// </summary>
    public readonly struct BaseTypeOfInterest : IEquatable<Type>
    {
        /// <summary>
        /// The base type whose subclasses and implementations we will look for in the sweep.
        /// </summary>
        public readonly Type BaseType;
        
        /// <summary>
        /// Whether or not we should include <see cref="BaseType"/> itself in the mapping for this <see cref="BaseTypeOfInterest"/>.
        /// </summary>
        public readonly bool IncludesItself;

        /// <summary>
        /// Constructs a new <see cref="BaseTypeOfInterest"/>. 
        /// </summary>
        /// <param name="baseType">The base type whose subclasses and implementations we will look for in the sweep.</param>
        /// <param name="includesItself">Whether or not we should include <see cref="BaseType"/> itself in the mapping for this <see cref="BaseTypeOfInterest"/>.</param>
        public BaseTypeOfInterest(Type baseType, bool includesItself = false)
        {
            BaseType = baseType;
            IncludesItself = includesItself;
        }

        public bool Equals(Type other) => BaseType == other;
    }


    public partial class ReflectionCache
    {
        /// <summary>
        /// Checks <paramref name="type"/> to see if it matches any of the <see cref="BaseTypeOfInterest"/> that were registered with the reflection cache.
        /// </summary>
        /// <param name="type">The current type being evaluated.</param>
        /// <param name="baseTypesToSearchIn">The <see cref="BaseTypeOfInterest"/> to check <paramref name="type"/> against.</param>
        /// <param name="foundType">The first <see cref="BaseTypeOfInterest"/> that matches.</param>
        /// <returns><see cref="true"/>, if a type was found. <see cref="false"/> otherwise.</returns>
        public bool TryFindBaseTypesOfInterest(Type type, IEnumerable<BaseTypeOfInterest> baseTypesToSearchIn, out BaseTypeOfInterest foundType)
        {
            var typesToSearchIn = baseTypesToSearchIn.ToList();
            for (var i = 0; i < typesToSearchIn.Count; i++)
            {
                // TODO: Can probably write this in a faster way... but don't really want to think about this right now --- will add profiler markers to system wrapped
                // in BEAMABLE_DIAGNOSTICS directives for us to see if we need to do better.
                var baseType = typesToSearchIn[i];
                if (baseType.IncludesItself || baseType.BaseType.IsInterface)
                {
                    if (baseType.BaseType.IsAssignableFrom(type))
                    {
                        foundType = baseType;
                        return true;
                    }
                }
                else
                {
                    if (type.IsSubclassOf(baseType.BaseType))
                    {
                        foundType = baseType;
                        return true;
                    }
                }
            }

            foundType = default;
            return false;
        }
    }
}
