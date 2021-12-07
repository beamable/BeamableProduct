using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Beamable.Common
{
    /// <summary>
    /// Implement this interface over any <see cref="Attribute"/> to be able to use the existing <see cref="ReflectionCache"/> utilities to validate things with respect to attributes that name members.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Attribute"/> implementing this interface.</typeparam>
    public interface INamingAttribute<T> : IReflectionCachingAttribute<T> where T : Attribute, INamingAttribute<T>
    {
        /// <summary>
        /// A list of names that must be unique between all uses of this attribute.
        /// </summary>
        string[] Names { get; }

        /// <summary>
        /// A function that validates the given list of names when being over the given member. <paramref name=""/>
        /// </summary>
        /// <returns>An <see cref="AttributeValidationResult{T}"/> with a clear message and <see cref="ReflectionCache.ValidationResultType"/>.</returns>
        AttributeValidationResult<T> AreValidNameForType(MemberInfo member, string[] potentialNames);
    }


    /// <summary>
    /// Results of validation of a <see cref="INamingAttribute{T}"/> implementation.
    /// </summary>
    /// <typeparam name="T">The attribute implementing <see cref="INamingAttribute{T}"/>.</typeparam>
    public readonly struct UniqueNameValidationResults<T> where T : Attribute, INamingAttribute<T>
    {
        /// <summary>
        /// List of results from each individual check of <see cref="INamingAttribute{T}.AreValidNameForType"/>. 
        /// </summary>
        public readonly List<AttributeValidationResult<T>> PerAttributeNameValidations;
        
        /// <summary>
        /// List of name collisions identified when running a validation sweep over a list of <see cref="MemberAttributePair"/> containing attributes of type: <see cref="INamingAttribute{T}"/>.
        /// </summary>
        public readonly List<UniqueNameCollisionData> PerNameCollisions;
        
        /// <summary>
        /// Creates a <see cref="UniqueNameValidationResults{T}"/> with the given data.
        /// </summary>
        /// <param name="perAttributeNameValidations">See <see cref="PerAttributeNameValidations"/>.</param>
        /// <param name="perNameCollisions">See <see cref="PerNameCollisions"/>.</param>
        public UniqueNameValidationResults(List<AttributeValidationResult<T>> perAttributeNameValidations, List<UniqueNameCollisionData> perNameCollisions)
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
        /// <typeparam name="T">Any type implementing <see cref="INamingAttribute{T}"/>.</typeparam>
        /// <returns>A <see cref="UniqueNameValidationResults{T}"/> data structure with the validation results that you can use to display errors and warnings or parse valid pairs.</returns>
        public static UniqueNameValidationResults<T> GetAndValidateUniqueNamingAttributes<T>(this IReadOnlyList<MemberAttributePair> memberAttributePairs) where T : Attribute, INamingAttribute<T>
        {
            // Allocates lists (assumes one name per-attribute, will re-allocate list if there's two attributes)
            var namesList = new List<(string name, MemberAttributePair pair)>(memberAttributePairs.Count);
            var attributeNameStringValidations = new List<AttributeValidationResult<T>>(memberAttributePairs.Count);           
            
            // Iterate all MemberAttributePairs validating if their names are valid while also storing them in the name's list for name-collision detection.
            foreach (var memberAttributePair in memberAttributePairs)
            {
                var info = memberAttributePair.Info;
                var attr = memberAttributePair.AttrAs<T>();

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

            
            return new UniqueNameValidationResults<T>(attributeNameStringValidations, duplicateNames);
        }

        /// <summary>
        /// Generates a consistent error message from a list of <see cref="UniqueNameCollisionData"/>.
        /// </summary>
        /// <param name="collisionData">The list of <see cref="UniqueNameCollisionData"/> to generate from.</param>
        /// <param name="msgIntro">A context-specific message to display before the collision list.</param>
        /// <param name="msgOutro">A context-specific message to display after the collision list.</param>
        /// <returns></returns>
        public static string GenerateNameCollisionMessage(this IReadOnlyList<UniqueNameCollisionData> collisionData,  string msgIntro = "", string msgOutro = "")
        {
            var msgBuilder = new StringBuilder();

            msgBuilder.AppendLine(msgIntro);

            msgBuilder.AppendLine("Found name collisions: ");
            foreach (var uniqueNameCollisionData in collisionData)
            {
                var name = uniqueNameCollisionData.Name;
                var collidedAttributesOwners = uniqueNameCollisionData.CollidedAttributes.Select(pair => pair.Info.Name);
                var nameCollisionsMsg = $"{name} => [{string.Join(", ", collidedAttributesOwners)}]";
                msgBuilder.AppendLine(nameCollisionsMsg);
            }

            msgBuilder.AppendLine();
            msgBuilder.AppendLine(msgOutro);

            return msgBuilder.ToString();
        }

        public static string GetOptionalNameOrMemberName<TNamingAttribute>(this MemberAttributePair attributePair) 
	        where TNamingAttribute : Attribute, INamingAttribute<TNamingAttribute>
        {
	        var attr = attributePair.AttrAs<TNamingAttribute>();
	        var type = attributePair.Info;

	        var firstNonNullName = attr.Names.FirstOrDefault(s => !string.IsNullOrEmpty(s));

	        return string.IsNullOrEmpty(firstNonNullName) ? type.Name : firstNonNullName;
        }

    }
    
    
}
