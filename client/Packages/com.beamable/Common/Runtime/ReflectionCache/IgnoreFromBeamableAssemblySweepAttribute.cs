using System;

namespace Beamable.Common
{    
    [AttributeUsage(AttributeTargets.Assembly)]
    public class IgnoreFromBeamableAssemblySweepAttribute : Attribute
    {
        public bool IsStrict { get; }
        
        public Type[] IgnoredBaseTypes { get; }
        public Type[] IgnoredAttributes { get; }

        /// <summary>
        /// Place this in any file inside an AsmDef scoped directory to exclude all declared types of that AsmDef from the AssemblySweep of the <see cref="ReflectionCache"/>.
        /// </summary>
        /// <param name="strictMode">
        /// When <see cref="true"/>, if any of the ignored types is found inside the assembly will thrown an exception.
        /// </param>
        /// <param name="ignoredBaseTypes">
        /// List of types provided by all <see cref="IReflectionCacheTypeProvider.BaseTypesOfInterest"/> that should not be detected inside this Assembly.
        /// Leaving it as <see cref="null"/> means NO types registered by <see cref="IReflectionCacheTypeProvider.BaseTypesOfInterest"/> will be detected.
        /// In strict mode, this can be used to easily enforce in which Assemblies certain types can be declared.
        /// </param>
        /// <param name="ignoredAttributes">
        /// List of types provided by all <see cref="IReflectionCacheTypeProvider.AttributesOfInterest"/> that should not be detected inside this Assembly.
        /// Leaving it as <see cref="null"/> means NO types registered by <see cref="IReflectionCacheTypeProvider.AttributesOfInterest"/> will be detected.
        /// In strict mode, this can be used to easily enforce in which Assemblies certain types can be declared.
        /// </param>
        public IgnoreFromBeamableAssemblySweepAttribute(bool strictMode = true, Type[] ignoredBaseTypes = null, Type[] ignoredAttributes = null)
        {
            IsStrict = strictMode;

            IgnoredBaseTypes = ignoredBaseTypes;
            IgnoredAttributes = ignoredAttributes;
        }
    }
}