using Beamable.Common.Dependencies;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Beamable.Server
{
	/// <summary>
	/// A <see cref="DependencyBuilder"/> that is aware of a <see cref="BeamServiceScope"/> and refuses to
	/// build a provider containing any service whose <see cref="ServiceScopeUsageAttribute"/> declares a
	/// different scope.
	///
	/// <para>
	/// This turns the (otherwise inert) <see cref="RealmScopedAttribute"/> / <see cref="ZoneScopedAttribute"/>
	/// annotations into a fail-fast, boot-time guard: building a zone container that contains a
	/// <c>[RealmScoped]</c> service (or a realm container with a <c>[ZoneScoped]</c> service) throws
	/// immediately, rather than surfacing as a null reference deep inside a request.
	/// </para>
	///
	/// <para>
	/// Validation runs once, at <see cref="Build"/> time, over the entire registration set — so the error
	/// reports <em>every</em> offending service at once instead of breaking on the first. The base
	/// <see cref="DependencyBuilder"/> is intentionally left scope-agnostic (it is shared with Unity, where
	/// scopes do not apply); the scope concept lives only in this subclass.
	/// </para>
	/// </summary>
	public class ScopedDependencyBuilder : DependencyBuilder
	{
		/// <summary>
		/// The scope this builder enforces. Services annotated for a different scope cannot be built.
		/// </summary>
		public BeamServiceScope Scope { get; }

		public ScopedDependencyBuilder(BeamServiceScope scope)
		{
			Scope = scope;
		}

		public override IDependencyProviderScope Build(BuildOptions options = null)
		{
			ValidateAllRegistrations();
			return base.Build(options);
		}

		public override IDependencyBuilder Clone()
		{
			return new ScopedDependencyBuilder(Scope)
			{
				ScopedServices = new List<ServiceDescriptor>(ScopedServices),
				TransientServices = new List<ServiceDescriptor>(TransientServices),
				SingletonServices = new List<ServiceDescriptor>(SingletonServices)
			};
		}

		/// <summary>
		/// Scans every registered descriptor and throws a single aggregated error listing all services whose
		/// declared scope does not match <see cref="Scope"/>.
		/// </summary>
		private void ValidateAllRegistrations()
		{
			var violations = new HashSet<string>();
			CollectViolations(SingletonServices, violations);
			CollectViolations(ScopedServices, violations);
			CollectViolations(TransientServices, violations);

			if (violations.Count == 0) return;

			var sb = new StringBuilder();
			sb.AppendLine(
				$"Cannot build a {Scope}-scoped service provider; {violations.Count} registered service(s) " +
				$"are not valid in a {Scope} scope:");
			foreach (var violation in violations)
			{
				sb.AppendLine($"  - {violation}");
			}

			throw new InvalidOperationException(sb.ToString());
		}

		private void CollectViolations(IEnumerable<ServiceDescriptor> descriptors, HashSet<string> violations)
		{
			foreach (var descriptor in descriptors)
			{
				if (TryGetConflict(descriptor.Interface, out var message) ||
					TryGetConflict(descriptor.Implementation, out message))
				{
					violations.Add(message);
				}
			}
		}

		private bool TryGetConflict(Type type, out string message)
		{
			message = null;
			if (!TryGetRequiredScope(type, out var required, out var declaringType)) return false;
			if (required == Scope) return false;

			message = declaringType == type
				? $"{type.Name} requires [{required}Scoped]"
				: $"{type.Name} requires [{required}Scoped] (declared on {declaringType.Name})";
			return true;
		}

		/// <summary>
		/// Looks for a <see cref="ServiceScopeUsageAttribute"/> on the type (and its base types), or on any
		/// interface it implements. Interface-to-interface attribute inheritance is not honored by the
		/// reflection <c>inherit</c> flag, so implemented interfaces are checked explicitly — this lets a
		/// scope annotation on a base contract (e.g. <c>IInventoryApi</c>) cover a derived one
		/// (e.g. <c>IMicroserviceInventoryApi</c>) and vice-versa.
		/// </summary>
		private static bool TryGetRequiredScope(Type type, out BeamServiceScope scope, out Type declaringType)
		{
			scope = default;
			declaringType = null;
			if (type == null) return false;

			var attribute = type.GetCustomAttribute<ServiceScopeUsageAttribute>(inherit: true);
			if (attribute != null)
			{
				scope = attribute.Scope;
				declaringType = type;
				return true;
			}

			foreach (var contract in type.GetInterfaces())
			{
				attribute = contract.GetCustomAttribute<ServiceScopeUsageAttribute>(inherit: false);
				if (attribute != null)
				{
					scope = attribute.Scope;
					declaringType = contract;
					return true;
				}
			}

			return false;
		}
	}
}
