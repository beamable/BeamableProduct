using Beamable.Common.Reflection;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Beamable.Server
{

	public enum MicroViewSlot
	{
		PLAYER,
		OPERATE
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class MicroViewAttribute : Attribute
	{
		public string ViewName { get; }
		public MicroViewSlot UIPath { get; }
		public string SourcePath { get; }

		public MicroViewAttribute(string viewName, MicroViewSlot uiPath,  [CallerFilePath] string sourcePath = "")
		{
			ViewName = viewName;
			UIPath = uiPath;
			SourcePath = sourcePath;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public class MicroserviceAttribute : Attribute, INamingAttribute
	{
		public string MicroserviceName { get; }
		public string SourcePath { get; }

		[Obsolete(
		   "Any new client build of your game won't require the payload string. Unless you've deployed a client build using Beamable before version 0.11.0, you shouldn't set this")]
		public bool UseLegacySerialization { get; set; } = false;

		public MicroserviceAttribute(string microserviceName, [CallerFilePath] string sourcePath = "")
		{
			MicroserviceName = microserviceName;
			SourcePath = sourcePath;
		}

		public string GetSourcePath()
		{
			return SourcePath;
		}

		public AttributeValidationResult IsAllowedOnMember(MemberInfo member)
		{
			// Guaranteed to be a type, due to AttributeUsage attribute being set to Class.

			if (!typeof(Microservice).IsAssignableFrom((Type)member))
			{
				return new AttributeValidationResult(this,
																			member,
																			ReflectionCache.ValidationResultType.Error,
																			$"Microservice Attribute [{MicroserviceName}] cannot be over type [{member.Name}] " +
																			$"since [{member.Name}] does not inherit from [{nameof(Microservice)}].");
			}

			return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Valid, "");
		}

		public string[] Names => new[] { MicroserviceName };

		public AttributeValidationResult AreValidNameForType(MemberInfo member, string[] potentialNames)
		{
			// TODO: Validate no invalid characters are in the C#MS/Storage Object name
			return new AttributeValidationResult(this, member, ReflectionCache.ValidationResultType.Valid, "");
		}
	}
}
