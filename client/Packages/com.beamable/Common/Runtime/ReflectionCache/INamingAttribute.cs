using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Beamable.Common.Reflection
{
    /// <summary>
    /// Implement this interface over any <see cref="Attribute"/> to be able to use the existing <see cref="ReflectionCache"/> utilities to validate things with respect to attributes that name members.
    /// </summary>
    public interface INamingAttribute : IReflectionCachingAttribute
    {
        /// <summary>
        /// A list of names that must be unique between all uses of this attribute.
        /// </summary>
        string[] Names { get; }

        /// <summary>
        /// A function that validates the given list of names when being over the given member. <paramref name=""/>
        /// </summary>
        /// <returns>An <see cref="AttributeValidationResult{T}"/> with a clear message and <see cref="ReflectionCache.ValidationResultType"/>.</returns>
        AttributeValidationResult AreValidNameForType(MemberInfo member, string[] potentialNames);
    }


    /// <summary>
    /// Results of validation of a <see cref="INamingAttribute{T}"/> implementation.
    /// </summary>
    public readonly struct UniqueNameValidationResults
    {
        /// <summary>
        /// List of results from each individual check of <see cref="INamingAttribute{T}.AreValidNameForType"/>. 
        /// </summary>
        public readonly List<AttributeValidationResult> PerAttributeNameValidations;
        
        /// <summary>
        /// List of name collisions identified when running a validation sweep over a list of <see cref="MemberAttributePair"/> containing attributes of type: <see cref="INamingAttribute{T}"/>.
        /// </summary>
        public readonly List<UniqueNameCollisionData> PerNameCollisions;
        
        /// <summary>
        /// Creates a <see cref="UniqueNameValidationResults{T}"/> with the given data.
        /// </summary>
        /// <param name="perAttributeNameValidations">See <see cref="PerAttributeNameValidations"/>.</param>
        /// <param name="perNameCollisions">See <see cref="PerNameCollisions"/>.</param>
        public UniqueNameValidationResults(List<AttributeValidationResult> perAttributeNameValidations, List<UniqueNameCollisionData> perNameCollisions)
        {
            PerAttributeNameValidations = perAttributeNameValidations;
            PerNameCollisions = perNameCollisions;
        } 
    }

    /// <summary>
    /// Data struct holding information regarding a name collision.
    /// </summary>
    public readonly struct UniqueNameCollisionData
    {
        /// <summary>
        /// The collided name.
        /// </summary>
        public readonly string Name;
        
        /// <summary>
        /// The list of <see cref="MemberAttributePair"/> that collided.
        /// </summary>
        public readonly MemberAttributePair[] CollidedAttributes;

        /// <summary>
        /// Initializes the unique name collision data structure with its relevant information.
        /// </summary>
        /// <param name="name">The collided name.</param>
        /// <param name="collidedAttributes">A list of <see cref="MemberAttributePair"/> that contain the collided name.</param>
        public UniqueNameCollisionData(string name, MemberAttributePair[] collidedAttributes)
        {
            Name = name;
            CollidedAttributes = collidedAttributes;
        }
    }



    public static partial class ReflectionCacheExtensions
    {
        /// <summary>
        /// Gets and validates attributes the must enforce a unique name.
        /// Expects <paramref name="memberAttributePairs"/> to contain the entire selection of attributes whose names can't collide.
        /// </summary>
        /// <param name="memberAttributePairs">
        /// All <see cref="MemberAttributePair"/> should contain attributes implementing <see cref="INamingAttribute{T}. 
        /// </param>
        /// <typeparam name="TNamingAttr">Any type implementing <see cref="INamingAttribute"/>.</typeparam>
        /// <returns>A <see cref="UniqueNameValidationResults"/> data structure with the validation results that you can use to display errors and warnings or parse valid pairs.</returns>
        public static UniqueNameValidationResults GetAndValidateUniqueNamingAttributes<TNamingAttr>(this IReadOnlyList<MemberAttributePair> memberAttributePairs)
         where TNamingAttr : Attribute, INamingAttribute, IReflectionCachingAttribute
        {
            // Allocates lists (assumes one name per-attribute, will re-allocate list if there's two attributes)
            var namesList = new List<(string name, MemberAttributePair pair)>(memberAttributePairs.Count);
            var attributeNameStringValidations = new List<AttributeValidationResult>(memberAttributePairs.Count);           
            
            // Iterate all MemberAttributePairs validating if their names are valid while also storing them in the name's list for name-collision detection.
            foreach (var memberAttributePair in memberAttributePairs)
            {
                var info = memberAttributePair.Info;
                var attr = memberAttributePair.AttrAs<TNamingAttr>();

                var result = attr.AreValidNameForType(info, attr.Names);
                
                namesList.AddRange(attr.Names.Select(name => (name, memberAttributePair)));
                attributeNameStringValidations.Add(result);
            }

            // Get the duplicate names and bake them into a proper data structure for consumption by other systems.
            var duplicateNames = namesList
                .GroupBy(tuple => tuple.name)
                .Where(group => group.Count() > 1)
                .Select(group => new UniqueNameCollisionData(@group.Key, @group.Select(tuple=>tuple.pair).ToArray()))
                .ToList();

            
            return new UniqueNameValidationResults(attributeNameStringValidations, duplicateNames);
        }

        public static string GetOptionalNameOrMemberName<TNamingAttribute>(this MemberAttributePair attributePair) 
	        where TNamingAttribute : Attribute, INamingAttribute, IReflectionCachingAttribute
        {
	        var attr = attributePair.AttrAs<TNamingAttribute>();
	        var type = attributePair.Info;

	        var firstNonNullName = attr.Names.FirstOrDefault(s => !string.IsNullOrEmpty(s));

	        return string.IsNullOrEmpty(firstNonNullName) ? type.Name : firstNonNullName;
        }

    }
    
    
}
