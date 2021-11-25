using System;
using System.Collections.Generic;

namespace Common.Runtime.BeamHints
{
	[Flags]
	public enum BeamHintType
	{
		Invalid = 0,
		Validation = 1 << 0,
		Hint = 1 << 1,

		All = Validation | Hint
	}

	/// <summary>
	/// <see cref="BeamHint"/> domains are a large-scale contextual grouping for hints. We use these to organize and display them in a logical and easy to navigate way. 
	/// <see cref="BeamHint"/>s can have multiple domains --- this will cause hints to be displayed under both domains' hierarchies.
	/// </summary>
	public static class BeamHintDomains
	{
		/// <summary>
		/// Call this method to generate user domain names.
		/// We use these so we can make optimizations when querying hints and storages that are created by users versus create by Beamable.
		/// </summary>
		/// <param name="userDomainName">The "_"-separated all-caps name of your <see cref="BeamHint"/> domain.</param>
		/// <returns>A prefixed <see cref="userDomainName"/> that you can use to declare your own <see cref="IBeamHintStorage"/>s and generate your own <see cref="BeamHint"/>s.</returns>
		public static string GenerateUserDomain(string userDomainName) => $"{USER_DOMAIN_PREFIX}_{userDomainName}";
		/// <summary>
		/// Checks if a domain is a User-created domain.
		/// </summary>
		public static bool IsUserDomain(string domain) => domain.StartsWith(USER_DOMAIN_PREFIX);
		public const string USER_DOMAIN_PREFIX = "USER";
		
		/// <summary>
		/// Generates a Beamable-owned domain name.
		/// We use these so we can make optimizations when querying hints and storages that are created by users versus create by Beamable. 
		/// </summary>
		/// <param name="domainName">The "_"-separated all-caps name of your <see cref="BeamHint"/> domain.</param>
		/// <returns>A prefixed <see cref="domainName"/> that we use to declare our <see cref="IBeamHintStorage"/>s and generate our <see cref="BeamHint"/>s.</returns>
		internal static string GenerateBeamableDomain(string domainName) => $"{BEAM_DOMAIN_PREFIX}_{domainName}";
		/// <summary>
		/// Checks if a domain is a Beamable-created domain.
		/// </summary>
		public static bool IsBeamableDomain(string domain) => domain.StartsWith(BEAM_DOMAIN_PREFIX);
		public const string BEAM_DOMAIN_PREFIX = "BEAM";
		
		/// <summary>
		/// Generate a sub-domain. These are used by the UI to group <see cref="BeamHint"/>s hierarchically and display them in a more organized way. 
		/// </summary>
		/// <param name="ownerDomain">A string generated via one of these <see cref="GenerateBeamableDomain"/>, <see cref="GenerateUserDomain"/> or <see cref="GenerateSubDomain"/>.</param>
		/// <param name="subDomainName">The name of the sub-domain to append.</param>
		/// <returns>A string defining the path to the sub-domain.</returns>
		public static string GenerateSubDomain(string ownerDomain, string subDomainName) => $"{ownerDomain}{SUB_DOMAIN_PREFIX}{subDomainName}";
		public const string SUB_DOMAIN_PREFIX = "¬";

		public static readonly string BEAM_CSHARP_MICROSERVICES = GenerateBeamableDomain("C#MS");
		public static readonly string BEAM_CSHARP_MICROSERVICES_CODE_MISUSE = GenerateSubDomain(BEAM_CSHARP_MICROSERVICES, "CODE_MISUSE");
		public static readonly string BEAM_CSHARP_MICROSERVICES_DOCKER = GenerateSubDomain(BEAM_CSHARP_MICROSERVICES, "DOCKER");
		public static bool IsCSharpMSDomain(string domain) => IsBeamableDomain(domain) && domain.Contains(BEAM_CSHARP_MICROSERVICES);


		public static readonly string BEAM_CONTENT = GenerateBeamableDomain("Content");
		public static readonly string BEAM_CONTENT_CODE_MISUSE = GenerateSubDomain(BEAM_CONTENT, "CODE_MISUSE");
		public static bool IsContentDomain(string domain) => IsBeamableDomain(domain) && domain.Contains(BEAM_CONTENT);

		
	}

	public readonly struct BeamHintHeader : IEquatable<BeamHintHeader>
	{
		public readonly BeamHintType Type;
		public readonly string Domain;
		public readonly string Id;

		public BeamHintHeader(BeamHintType type, string domain, string id = "")
		{
			Type = type;
			Domain = domain;
			Id = id;
		}

		public bool Equals(BeamHintHeader other)
		{
			return Type == other.Type && Domain == other.Domain && Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is BeamHintHeader other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = (int)Type;
				hashCode = (hashCode * 397) ^ (Domain != null ? Domain.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
				return hashCode;
			}
		}
	}

	public readonly struct BeamHint
	{
		public readonly BeamHintHeader Header;
		public readonly object ContextObject;

		public BeamHint(BeamHintHeader header, object contextObject)
		{
			this.Header = header;
			ContextObject = contextObject;
		}
	}

	

	public static class test
	{
		public static void HUEHUEHUE()
		{
			var beamHintStorage = (IBeamHintGlobalStorage)new BeamHintEditorStorage();

			// Adds a docker not running hint with no context object, since this hint doesn't need it
			beamHintStorage.AddOrReplaceHint(new BeamHintHeader(BeamHintType.Hint, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_DOCKER, "DOCKER_NOT_INSTALLED"));
			beamHintStorage.AddOrReplaceHint(BeamHintType.Hint, BeamHintDomains.BEAM_CSHARP_MICROSERVICES, "DOCKER_NOT_RUNNING");
			beamHintStorage.AddOrReplaceHint(BeamHintType.Hint, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_DOCKER, "ConflictingPortBindings", new List<int>() {8000});

			// Adds a validation hint with List<Type> as the context object (all the conflicting types) and the name as the Id
			beamHintStorage.AddOrReplaceHint(new BeamHintHeader(BeamHintType.Validation, BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE, "ConflictingMicroServiceName"),
			                        new List<Type> {typeof(int), typeof(Enum)});

			foreach (BeamHint beamHint in beamHintStorage)
			{
				// GOES THROUGH ALL HINTS
			}

			// Tries to add a hint of incorrect origin system here. This will throw an Assert Exception. The advantage for calling the correct one is performance related.
			// This is also useful as a declaration of intent.
			// Also, having multiple of these allow for easy iteration as shown below -- which will help BeamableAssistantControllers to be easily implementable. 
			foreach (BeamHint asdCSharpMSHint in beamHintStorage.CSharpMSHints)
			{
				// GOES THROUGH ALL C#MS HINTS, INCLUDING DOCKER SPECIFIC ONES
			}

			// Tries to add a hint of correct origin system here but incorrect class. This will throw an Assert Exception.
			// The advantage for calling the correct one is performance related. This is also useful as a declaration of intent.
			// Also, having multiple of these allow for easy iteration as shown below -- which will help BeamableAssistantControllers to be easily implementable.
			foreach (BeamHint beamHint in beamHintStorage.CSharpMSHints)
			{
				// GOES THROUGH ALL C#MS DOCKER SPECIFIC HINTS 
			}

			// Removing hints is pretty straight-forward... Clears the entire storage, each sub call clears the internal data for that specific storage.
			beamHintStorage.RemoveAllHints();

			// Removing supports filtering by class, origin system and type
			beamHintStorage.RemoveAllHints(BeamHintDomains.BEAM_CSHARP_MICROSERVICES_CODE_MISUSE); // clears all of the given domain
			beamHintStorage.RemoveAllHints(BeamHintType.Validation); // clears all validations

			// Updating is achieved by getting the hint, creating a new struct from it, removing the old one and storing the new one
			// TODO THINK: SHOULD I ADD HELPERS TO THIS INTERFACE?
		}
	}

	public class BeamableHintPreferencesManager
	{
		// Handles Editor Preferences
		// Stores key pair stuff on a per-beamable user state... 
		// Not sure what's the best way to do this now...
	}

	public class BaseBeamableAssistantController
	{
		public IBeamHintGlobalStorage GlobalHintStorage
		{
			get;
			set;
		}

		public List<BeamHintBaseVisualElement> AliveElements;

		// Updates UI on a Repeating timer of X seconds --- Polling can be our best friend here if we do it right...

		//  
	}

	public class BeamHintBaseVisualElement
	{
		public virtual void SetBeamHint(BeamHint hint)
		{
			// Tries to fill stuff out automagically
			// Handles every primitive type here and tries to inject into correct parameter
		}
	}

	public interface IBeamHintContextObject { } // Type constrain only

	public abstract class BeamHintBaseVisualElement<T> : BeamHintBaseVisualElement where T : IBeamHintContextObject
	{
		public sealed override void SetBeamHint(BeamHint hint)
		{
			base.SetBeamHint(hint);
			OnBeamHintChange(hint.Header, (T)hint.ContextObject);
		}

		public abstract void OnBeamHintChange(BeamHintHeader header, T type);
	}
}
