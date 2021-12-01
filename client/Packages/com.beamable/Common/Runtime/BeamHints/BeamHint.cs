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

	public static class BeamHintSharedConstants
	{
		/// <summary> 
		/// Separator used to split the stored string of serialized <see cref="BeamHintHeader"/>s (via <see cref="BeamHintHeader.AsKey"/>).
		/// Used by <see cref="BeamHintPreferencesManager"/>.
		/// </summary>
		public const string BEAM_HINT_PREFERENCES_SEPARATOR = "₢"; 
	}

	/// <summary>
	/// <see cref="BeamHint"/> domains are a large-scale contextual grouping for hints. We use these to organize and display them in a logical and easy to navigate way.
	/// <para/>
	/// Domains cannot have "¬" or "₢" they are reserved characters (see <see cref="SUB_DOMAIN_SEPARATOR"/>). 
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
		/// Sub-domains are simply domain strings separated by <see cref="SUB_DOMAIN_SEPARATOR"/>.
		/// </summary>
		/// <param name="ownerDomain">A string generated via one of these <see cref="GenerateBeamableDomain"/>, <see cref="GenerateUserDomain"/> or <see cref="GenerateSubDomain"/>.</param>
		/// <param name="subDomainName">The name of the sub-domain to append.</param>
		/// <returns>A string defining the path to the sub-domain.</returns>
		public static string GenerateSubDomain(string ownerDomain, string subDomainName) => $"{ownerDomain}{SUB_DOMAIN_SEPARATOR}{subDomainName}";
		public const string SUB_DOMAIN_SEPARATOR = "¬";

		
		public static readonly string BEAM_REFLECTION_CACHE = GenerateBeamableDomain("REFLECTION_CACHE");
		public static bool IsReflectionCacheDomain(string domain) => IsBeamableDomain(domain) && domain.Contains(BEAM_REFLECTION_CACHE);

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
		public const string AS_KEY_SEPARATOR = "¬¬";
		
		public readonly BeamHintType Type;
		
		/// <summary>
		/// Domain this hint belongs to. See <see cref="BeamHintDomains"/> for more details.
		/// </summary>
		public readonly string Domain;
		
		/// <summary>
		/// Unique Id, within <see cref="Domain"/> and <see cref="Type"/>, that represents these hints.
		/// Cannot have "₢" character as it is reserved by the system. 
		/// </summary>
		public readonly string Id;

		public BeamHintHeader(BeamHintType type, string domain, string id = "")
		{
			System.Diagnostics.Debug.Assert(!(domain.Contains(AS_KEY_SEPARATOR) || domain.Contains(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR)),
			                                $"Domain [{domain}] cannot contain: '{AS_KEY_SEPARATOR}' or '{BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR}'");
			System.Diagnostics.Debug.Assert(!(id.Contains(AS_KEY_SEPARATOR) || id.Contains(BeamHintDomains.SUB_DOMAIN_SEPARATOR) || id.Contains(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR)),
			                                $"Id [{id}] cannot contain: '{AS_KEY_SEPARATOR}', '{BeamHintDomains.SUB_DOMAIN_SEPARATOR}' or '{BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR}'");
			
			Type = type;
			Domain = domain;
			Id = id;
		}

		public bool Equals(BeamHintHeader other) => Type == other.Type && Domain == other.Domain && Id == other.Id;

		public override bool Equals(object obj) => obj is BeamHintHeader other && Equals(other);

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

		public string AsKey() => $"{Type}{AS_KEY_SEPARATOR}{Domain}{AS_KEY_SEPARATOR}{Id}";

		public override string ToString() => $"{nameof(Type)}: {Type}, {nameof(Domain)}: {Domain}, {nameof(Id)}: {Id}";
	}

	public readonly struct BeamHint : IEquatable<BeamHint>, IEquatable<BeamHintHeader>
	{
		public readonly BeamHintHeader Header;
		public readonly object ContextObject;

		public BeamHint(BeamHintHeader header, object contextObject)
		{
			this.Header = header;
			ContextObject = contextObject;
		}

		public bool Equals(BeamHint other) => other.Header.Equals(Header);
		public bool Equals(BeamHintHeader other) => other.Equals(Header);
		public override string ToString() => $"{nameof(Header)}: {Header}, {nameof(ContextObject)}: {ContextObject}";
	}
	
	public interface IBeamHintProvider
	{
		void SetStorage(IBeamHintGlobalStorage hintGlobalStorage);
	}

	/// <summary>
	/// Current State of display tied to any specific <see cref="BeamHintHeader"/>.
	/// </summary>
	public enum VisibilityState
	{
		Display,
		Ignore,
	}

	/// <summary>
	/// Different levels of persistence of any single <see cref="BeamHint"/>'s <see cref="VisibilityState"/> that the <see cref="BeamHintPreferencesManager"/> supports.
	/// </summary>
	public enum PersistenceLevel
	{
		Instance,
		Session,
		Permanent
	}
	
	public interface IBeamHintPreferencesManager
	{
		void RebuildPerHintPreferences();

		void SetHintPreferences(BeamHint hint, VisibilityState newVisibilityState, PersistenceLevel persistenceLevel);
		
		void ClearAllPreferences();
	}
	
	public interface IBeamHintManager
	{
		void SetPreferencesManager(IBeamHintPreferencesManager preferencesManager);
		void SetStorage(IBeamHintGlobalStorage hintGlobalStorage);
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
