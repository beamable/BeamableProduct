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

	[System.Serializable]
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
			System.Diagnostics.Debug.Assert(
				!(id.Contains(AS_KEY_SEPARATOR) || id.Contains(BeamHintDomains.SUB_DOMAIN_SEPARATOR) || id.Contains(BeamHintSharedConstants.BEAM_HINT_PREFERENCES_SEPARATOR)),
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

		void SplitHintsByVisibilityState(IEnumerable<BeamHint> hints, out IEnumerable<BeamHint> outToDisplayHints, out IEnumerable<BeamHint> outToIgnoreHints);

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
