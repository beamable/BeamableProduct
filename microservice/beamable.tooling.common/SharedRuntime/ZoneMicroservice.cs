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

		/// <summary>
		/// The zone service surface — the zone analog of the realm <see cref="Microservice"/>'s <c>Services</c>.
		/// Use <c>Services.Customer</c> to query the customer's realms and zones. There is no player/realm SDK
		/// here; to act within a realm, use <see cref="AssumeRealm"/>.
		/// </summary>
		protected IZoneServices Services => _serviceProvider.GetService<IZoneServices>();

		/// <summary>
		/// Enter a realm (<c>cid.pid</c>) from this zone service and get back the full realm SDK — the same
		/// dependency scope a realm <see cref="Microservice"/> would have — scoped to <paramref name="pid"/>.
		/// <para>
		/// This is the zone→realm analog of <see cref="Microservice.AssumeNewUser"/>: the returned
		/// <see cref="UserRequestDataHandler"/> is <b>disposable</b> and owns its own child scope, so wrap it
		/// in a <c>using</c> and dispose it when done. Access the realm SDK through its <c>Services</c>,
		/// requests through its <c>Requester</c>, and DI through its <c>Provider</c>.
		/// </para>
		/// </summary>
		/// <param name="pid">The realm (project) id to act within.</param>
		/// <param name="gamerTag">
		/// Optional player on that realm to act on behalf of. When 0 (the default), the scope acts with the
		/// zone service's own (server) identity rather than a specific player.
		/// </param>
		/// <param name="useSignedRequests">
		/// When false (the default), realm requests ride this zone's websocket with an <c>X-BEAM-SCOPE: cid.pid</c>
		/// header — the fast path, valid only for a realm that belongs to this zone. Set true to route through a
		/// signed HTTP requester (authenticated with the realm's own secret) instead, which also reaches realms
		/// that are not part of this zone.
		/// </param>
		protected UserRequestDataHandler AssumeRealm(string pid, long gamerTag = 0, bool useSignedRequests = false)
		{
			if (string.IsNullOrEmpty(pid))
			{
				throw new InvalidArgumentException(nameof(pid), "A realm (pid) is required to AssumeRealm.");
			}
			return _serviceProvider.GetService<IRealmScopeFactory>().CreateRealmScope(pid, gamerTag, useSignedRequests);
		}

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
