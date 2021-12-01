using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Beamable.Common
{
    /// <summary>
    /// Struct that holds data for all Attribute-related reflection data caches.
    /// </summary>
    public readonly struct PerAttributeCache
    {
        /// <summary>
        /// List of attribute types that can only be placed in non-type-members (any <see cref="AttributeTargets"/> not in <see cref="AttributeOfInterest.INTERNAL_TYPE_SEARCH_WHEN_TARGETS"/>).
        /// </summary>
        public readonly List<AttributeOfInterest> AttributeTypes;
        
        /// <summary>
        /// List of attribute types that can only be placed in type-members (any <see cref="AttributeTargets"/> in <see cref="AttributeOfInterest.INTERNAL_TYPE_SEARCH_WHEN_TARGETS"/>).
        /// </summary>
        public readonly List<AttributeOfInterest> MemberAttributeTypes;
        
        /// <summary>
        /// Cached list of <see cref="AttributeOfInterest"/> and all <see cref="MemberAttributePair"/>s that were found in the assembly sweep.
        /// </summary>
        public readonly Dictionary<AttributeOfInterest, List<MemberAttributePair>> AttributeMappings;

        public int TotalAttributesOfInterestCount => AttributeTypes.Count + MemberAttributeTypes.Count;

        /// <summary>
        /// Constructs a new <see cref="PerAttributeCache"/>.
        /// If planning to build with <see cref="ReflectionCache.BuildTypeCaches"/>, call this with large pre-allocated lists since allocation is expensive and list's "expansion when full" re-allocates.  
        /// </summary>
        public PerAttributeCache(List<AttributeOfInterest> attributeTypes, List<AttributeOfInterest> memberAttributeTypes, Dictionary<AttributeOfInterest, List<MemberAttributePair>> attributeMappings)
        {
            AttributeTypes = attributeTypes;
            AttributeMappings = attributeMappings;
            MemberAttributeTypes = memberAttributeTypes;
        }
    }
    
    /// <summary>
    /// Struct that defines an attribute of interest and gives us information on where to look for it.
    /// </summary>
    public readonly struct AttributeOfInterest
    {
        private const AttributeTargets INTERNAL_TYPE_SEARCH_WHEN_TARGETS = AttributeTargets.Constructor |
                                                                          AttributeTargets.Event |
                                                                          AttributeTargets.Field |
                                                                          AttributeTargets.Method |
                                                                          AttributeTargets.Property;

        private const MemberTypes INTERNAL_TYPE_SEARCH_WHEN_MEMBER_TYPES = MemberTypes.Constructor | 
                                                                           MemberTypes.Event | 
                                                                           MemberTypes.Field | 
                                                                           MemberTypes.Method |
                                                                           MemberTypes.Property;
        
        /// <summary>
        /// Type of the attribute you are interested in.
        /// </summary>
        public readonly Type AttributeType;
        
        /// <summary>
        /// Over which types of language constructs the attribute can be found. To use <see cref="ReflectionCache"/> to find attributes,
        /// the attributes MUST have an <see cref="AttributeUsageAttribute"/> on them. This allows us some performance optimizations to make this interfere less with your editor experience.
        /// </summary>
        public readonly AttributeTargets Targets;

        /// <summary>
        /// List of all base types whose implementations we should look through the members to find this attribute. Only relevant if <see cref="IsDeclaredMember"/> returns true.
        /// </summary>
        public readonly List<Type> FoundInBaseTypes;
        
        /// <summary>
        /// List of all attribute types whose user types (classes/structs that have the attribute over them) we should look through the members to find this attribute. Only relevant if <see cref="IsDeclaredMember"/> returns true.
        /// </summary>
        public readonly List<Type> FoundInTypesWithAttributes;

        /// <summary>
        /// Whether or not the attribute targets a Non-Type-Member (see <see cref="INTERNAL_TYPE_SEARCH_WHEN_TARGETS"/> and <see cref="INTERNAL_TYPE_SEARCH_WHEN_MEMBER_TYPES"/>).
        /// </summary>
        public bool IsDeclaredMember => INTERNAL_TYPE_SEARCH_WHEN_TARGETS.ContainsAnyFlag(Targets);
        
        /// <summary>
        /// Tries to get an attribute from the given member info. Has a guard against passing in members whose <see cref="MemberInfo.MemberType"/> don't respect <see cref="INTERNAL_TYPE_SEARCH_WHEN_MEMBER_TYPES"/>.
        /// </summary>
        public bool TryGetFromMemberInfo(MemberInfo info, out Attribute attribute)
        {
            attribute = info.GetCustomAttribute(AttributeType, false);
            
            // Assert instead of failing silently. Failing silently here means we could fail due to the member not having the correct flag. This is a case where we should fail loudly, as it's 
            // supposed to be impossible.
            System.Diagnostics.Debug.Assert(INTERNAL_TYPE_SEARCH_WHEN_MEMBER_TYPES.ContainsAnyFlag(info.MemberType), 
                "Calling this with a member info that is not a declared member. Please ensure all MemberInfos passed to this function respect this clause.");
            return attribute != null;
        }

        /// <summary>
        /// Checks if the given type should have it's declared members searched for the given attribute. 
        /// </summary>
        public bool CanBeFoundInType(Type type)
        {
            var canHaveDeclaredMembers = FoundInBaseTypes.Any(baseType => baseType.IsAssignableFrom(type));
            canHaveDeclaredMembers |= FoundInTypesWithAttributes.Any(attrType => type.GetCustomAttribute(attrType) != null);
            return canHaveDeclaredMembers;
        }

        /// <summary>
        /// Copies this <see cref="AttributeOfInterest"/> but as a strict version.
        /// </summary>        
        public AttributeOfInterest ToStrict() => new AttributeOfInterest(AttributeType, FoundInTypesWithAttributes.ToArray(), FoundInBaseTypes.ToArray());  
        
        /// <summary>
        /// Constructs a new <see cref="AttributeOfInterest"/> with guards against incorrect usage.
        /// </summary>
        /// <param name="attributeType">The Attribute's type. Expects to have <see cref="AttributeUsageAttribute"/> with correctly declared <see cref="AttributeTargets"/>.</param>
        /// <param name="foundInTypesWithAttributes">
        /// Only relevant when <see cref="Targets"/> match <see cref="INTERNAL_TYPE_SEARCH_WHEN_TARGETS"/>. 
        /// List of attributes types whose using types (classes/structs that use the attributes) should be have their members searched for the <paramref name="attributeType"/>.
        /// </param>
        /// <param name="foundInBaseTypes">
        /// Only relevant when <see cref="Targets"/> match <see cref="INTERNAL_TYPE_SEARCH_WHEN_TARGETS"/>. 
        /// List of types whose subclasses should be have their members searched for the <paramref name="attributeType"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown if <paramref name="attributeType"/> does not have a <see cref="AttributeUsageAttribute"/> or if <see cref="IsDeclaredMember"/> and both
        /// <paramref name="foundInBaseTypes"/> and <paramref name="foundInTypesWithAttributes"/> have no types.
        /// </exception>
        public AttributeOfInterest(Type attributeType, Type[] foundInTypesWithAttributes = null, Type[] foundInBaseTypes = null)
        {
            AttributeType = attributeType;             
            Targets = AttributeType.GetCustomAttribute<AttributeUsageAttribute>()?.ValidOn ?? 
                      throw new ArgumentException($"To use Attribute Of Interest, you must declare a AttributeUsage attribute with the correct usage targets.");

            FoundInBaseTypes = new List<Type>(foundInBaseTypes ?? new Type[]{});
            FoundInTypesWithAttributes = new List<Type>(foundInTypesWithAttributes ?? new Type[]{});

            // What this does is:
            //   - If the attribute of interest Has a Method/Constructor/Property/Field/Event Target, we'll look for them into each individual type that's given in the two lists declared here., 
            //   - Will work with structs, classes both declared at root or internal as the Assembly.GetTypes() returns all of these.
            //
            // Assumption 1 ===> Does not need work for parameters or return values --- this is specific enough that each individual user system can do their own thing here.            
            if (Targets.HasFlag(INTERNAL_TYPE_SEARCH_WHEN_TARGETS))
            {
                // If you didn't tell us where to look, we'd have to look everywhere -- which is terrible for editor performance so we don't support it.
                if (FoundInBaseTypes.Count == 0 && FoundInTypesWithAttributes.Count == 0)
                {
                    throw new ArgumentException($"{nameof(AttributeOfInterest)} [{attributeType.Name}] with these {nameof(AttributeTargets)} [{INTERNAL_TYPE_SEARCH_WHEN_TARGETS.ToString()}]" +
                                                $"must have at least one entry into the {nameof(FoundInBaseTypes)} or {nameof(FoundInTypesWithAttributes)} lists.\n" +
                                                $"Without it, we would need to go into every existing type which would be bad for editor performance.");
                }
            }     
        }
    }
    
    /// <summary>
    /// Utility struct that represents a pairing of a <see cref="MemberInfo"/> with an <see cref="Attribute"/> instance.
    /// </summary>
    public readonly struct MemberAttributePair
    {
        public MemberTypes MemberType => Info.MemberType;

        public readonly MemberInfo Info;
        public readonly Attribute Attribute;

        public MemberAttributePair(MemberInfo info, Attribute attribute)
        {
            Info = info;
            Attribute = attribute;
        }

        public T InfoAs<T>() where T : MemberInfo => (T) Info;
        public T AttrAs<T>() where T : Attribute => (T) Attribute;
    }
    
    
    public partial class ReflectionCache
    {
        /// <summary>
        /// Call to see if a given type matches any attributes of interest or if their members have any attributes they care about.
        /// Fills <paramref name="foundAttributes"/> with the results
        /// (allocating in this function would be a large performance drain allocate the list once, re-use for every type you want to call this with).
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> to check against the <see cref="AttributeOfInterest"/>.</param>
        /// <param name="attributesToSearchFor">List of pre-filtered <see cref="AttributeOfInterest"/> that fails <see cref="AttributeOfInterest.IsDeclaredMember"/>.</param>
        /// <param name="declaredMemberAttributesToSearchFor">List of pre-filtered <see cref="AttributeOfInterest"/> that passes <see cref="AttributeOfInterest.IsDeclaredMember"/>.</param>
        /// <param name="foundAttributes">Dictionary with pre-allocated lists for all registered <see cref="AttributeOfInterest"/> --- is cleared when this is called.</param>
        private void GatherMemberAttributePairsFromAttributesOfInterest(MemberInfo member, 
            IReadOnlyList<AttributeOfInterest> attributesToSearchFor,
            IReadOnlyList<AttributeOfInterest> declaredMemberAttributesToSearchFor,
            Dictionary<AttributeOfInterest, List<MemberAttributePair>> foundAttributes)
        {   
            // Check for attributes over the type itself.
            foreach (var attributeOfInterest in attributesToSearchFor)
            {
                var attribute = member.GetCustomAttribute(attributeOfInterest.AttributeType, false); 
                if(attribute != null)
                    foundAttributes[attributeOfInterest].Add(new MemberAttributePair(member, attribute));
            }           
            
            // Checks for Attributes declared over types' members
            if (member.MemberType == MemberTypes.TypeInfo || member.MemberType == MemberTypes.NestedType)
            {
                var type = (Type) member;
                foreach (var attributeOfInterest in declaredMemberAttributesToSearchFor) 
                {
                    // See if this type that we are checking can actually have an attribute of this type. Skip it if we can't.  
                    var canHaveDeclaredMembers = attributeOfInterest.CanBeFoundInType(type);
                    if (!canHaveDeclaredMembers) continue;
                
                    // For each declared member, check if they have the current attribute of interest -- if they do, add them to the found attribute list.
                    // In this step we catch every member with the attribute --- individual systems are welcome to parse and yield errors at a later step. 
                    foreach (var memberInfo in type.GetMembers(BindingFlags.Public |
                                                               BindingFlags.NonPublic |
                                                               BindingFlags.Instance |
                                                               BindingFlags.Static))
                    {
                        if(attributeOfInterest.TryGetFromMemberInfo(memberInfo, out var attribute))
                            foundAttributes[attributeOfInterest].Add(new MemberAttributePair(memberInfo, attribute));                        
                    }
                }
            }                        
        }
        
        
        
    }
}
