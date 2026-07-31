using System;

namespace Beamable.Server
{
	/// <summary>
	/// Declares the <see cref="BeamServiceScope"/> in which a type or member is valid to use.
	/// A symbol with no <see cref="ServiceScopeUsageAttribute"/> is considered scope-neutral and may be
	/// used in any service scope.
	///
	/// <para>
	/// Prefer the concrete <see cref="RealmScopedAttribute"/> and <see cref="ZoneScopedAttribute"/> over
	/// referencing this base directly.
	/// </para>
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct |
		AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = false,
		Inherited = true)]
	public abstract class ServiceScopeUsageAttribute : Attribute
	{
		/// <summary>
		/// The scope in which the annotated symbol is valid.
		/// </summary>
		public BeamServiceScope Scope { get; }

		protected ServiceScopeUsageAttribute(BeamServiceScope scope)
		{
			Scope = scope;
		}
	}

	/// <summary>
	/// Marks a type or member as valid only in a realm (<c>cid.pid</c>) scoped service. Using it from a
	/// zone-scoped service is an error.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct |
		AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = false,
		Inherited = true)]
	public sealed class RealmScopedAttribute : ServiceScopeUsageAttribute
	{
		public RealmScopedAttribute() : base(BeamServiceScope.Realm) { }
	}

	/// <summary>
	/// Marks a type or member as valid only in a zone (<c>cid.zid</c>) scoped service. Using it from a
	/// realm-scoped service is an error.
	/// </summary>
	[AttributeUsage(
		AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct |
		AttributeTargets.Method | AttributeTargets.Property,
		AllowMultiple = false,
		Inherited = true)]
	public sealed class ZoneScopedAttribute : ServiceScopeUsageAttribute
	{
		public ZoneScopedAttribute() : base(BeamServiceScope.Zone) { }
	}
}
