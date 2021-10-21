using System;
using Beamable.Common;
using UnityEngine.Scripting;

namespace Beamable.ConsoleCommands
{
    /// <summary>
    /// Used on a class to annotate the class as having console commands.
    /// The class must have an empty constructor, or no console commands will be loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BeamableConsoleCommandProviderAttribute : PreserveAttribute, IReflectionCachingAttribute<BeamableConsoleCommandProviderAttribute>
    {
        public readonly static Type[] EmptyTypeArray = new Type[] { };
        public readonly static object[] EmptyObjectArray = new object[] { };

        public ReflectionCache.ValidationResult IsAllowedOnType(Type type, out string warningMessage, out string errorMessage)
        {
            var emptyConstructor = type.GetConstructor(EmptyTypeArray);
            if (emptyConstructor == null)
            {
                warningMessage = "";
                errorMessage = $"Console Command Provider [{type.Name}] must have an empty constructor.";
                return ReflectionCache.ValidationResult.Error;
            }
            warningMessage = "";
            errorMessage = "";
            return ReflectionCache.ValidationResult.Valid;
            
        }
    }
}
