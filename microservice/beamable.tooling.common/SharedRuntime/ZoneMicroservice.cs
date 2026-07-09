using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using System;
using System.Threading.Tasks;

namespace Beamable.Server
{
	/// <summary>
	/// Base class for a zone (<c>cid.zid</c>) scoped microservice, analogous to <see cref="Microservice"/>
	/// but without the realm-scoped SDK surface.
	///
	/// <para>
	/// A zone runs above realms and has no player/realm context, so the realm accessors that
	/// <see cref="Microservice"/> exposes — <c>Services</c>, <c>Context</c>, <c>Requester</c>,
	/// <c>Storage</c>, <c>SignedRequester</c>, and the on-behalf-of-a-player helpers — are intentionally
	/// absent here. Only the dependency <see cref="Provider"/> is exposed; zone-appropriate services are
	/// resolved from it.
	/// </para>
	///
	/// <para>
	/// This type is marked <see cref="ZoneScopedAttribute"/>, so a <see cref="ScopedDependencyBuilder"/>
	/// will reject it (and any subclass) if it is ever registered into a realm-scoped container.
	/// </para>
	/// </summary>
	[ZoneScoped]
	public abstract class ZoneMicroservice : IUserScopeCallbackReceiver
	{
		private IDependencyProviderScope _serviceProvider;

		/// <summary>
		/// The <see cref="IDependencyProvider"/> for this request's scope. Configure custom services with the
		/// <c>ConfigureServices</c> attribute, as in a realm <see cref="Microservice"/>.
		/// </summary>
		protected IDependencyProvider Provider => _serviceProvider;

		/// <summary>
		/// The zone (<c>cid.zid</c>) request context for the current request — the zone analog of the realm
		/// <see cref="Microservice"/>'s <c>Context</c>. It has no realm/player identity (<c>Pid</c>/<c>UserId</c>).
		/// </summary>
		protected ZonedRequestContext Context => _serviceProvider?.GetService<ZonedRequestContext>();

		public void ReceiveDefaultServices(IDependencyProviderScope scope)
		{
			// A zone service has no realm/player context, so — unlike Microservice — only the dependency
			// scope is captured. The realm-scoped services are not registered in a zone container.
			_serviceProvider = scope;
		}

		// IUserScope is realm-shaped. These members are implemented explicitly so they do not appear on the
		// ZoneMicroservice surface, and throw if something reaches for them through the interface.
		RequestContext IUserScope.Context => throw NotAvailable(nameof(IUserScope.Context));
		IBeamableRequester IUserScope.Request => throw NotAvailable(nameof(IUserScope.Request));
		IBeamableServices IUserScope.Services => throw NotAvailable(nameof(IUserScope.Services));
		IDependencyProvider IUserScope.Provider => Provider;

		private static NotSupportedException NotAvailable(string member) =>
			new NotSupportedException(
				$"{member} is realm (cid.pid) scoped and is not available in a zone-scoped service " +
				$"({nameof(ZoneMicroservice)}).");

		public async Promise DisposeMicroservice()
		{
			if (_serviceProvider != null)
			{
				await _serviceProvider.Dispose();
			}
			_serviceProvider = null;
		}

		public ValueTask DisposeAsync() => new ValueTask();

		public void Dispose() { }
	}
}
