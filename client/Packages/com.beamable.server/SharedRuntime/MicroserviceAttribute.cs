using Beamable.Common;
using System;
using System.Reflection;

namespace Beamable.Server
{
   [AttributeUsage(AttributeTargets.Class)]
   public class MicroserviceAttribute : Attribute, IUniqueNamingAttribute<MicroserviceAttribute>
   {
      public string MicroserviceName { get; }
      public string SourcePath { get; }

      [Obsolete(
         "Any new client build of your game won't require the payload string. Unless you've deployed a client build using Beamable before version 0.11.0, you shouldn't set this")]
      public bool UseLegacySerialization { get; set; } = false;

      public MicroserviceAttribute(string microserviceName, [System.Runtime.CompilerServices.CallerFilePath] string sourcePath="")
      {
         MicroserviceName = microserviceName;
         SourcePath = sourcePath;
      }

      public string GetSourcePath()
      {
         return SourcePath;
      }

      public AttributeValidationResult<MicroserviceAttribute> IsAllowedOnMember(MemberInfo member)
      {
	      // Guaranteed to be a type, due to AttributeUsage attribute being set to Class.

	      if (!typeof(Microservice).IsAssignableFrom((Type)member))
	      {
		      return new AttributeValidationResult<MicroserviceAttribute>(this,
		                                                                  member,
		                                                                  ReflectionCache.ValidationResultType.Error,
		                                                                  $"Microservice Attribute [{MicroserviceName}] cannot be over type [{member.Name}] " +
		                                                                  $"since [{member.Name}] does not inherit from [{nameof(Microservice)}].");
	      }

	      return new AttributeValidationResult<MicroserviceAttribute>(this, member, ReflectionCache.ValidationResultType.Valid, "");
      }

      public string[] Names => new[] {MicroserviceName};

      public AttributeValidationResult<MicroserviceAttribute> AreValidNameForType(MemberInfo member, string[] potentialNames)
      {
	      return new AttributeValidationResult<MicroserviceAttribute>(this, member, ReflectionCache.ValidationResultType.Valid, "");
      }
   }
}
