using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Dependencies;
using Beamable.Server.Api.Inventory;
using System;
using System.Threading.Tasks;

namespace Beamable.Server
{
	public delegate TMicroService MicroserviceFactory<out TMicroService>() where TMicroService : Microservice;
	public delegate IBeamableRequester RequesterFactory(RequestContext ctx);
	public delegate IBeamableServices ServicesFactory(IBeamableRequester requester, RequestContext ctx);

	public struct RequestHandlerData : IUserScope
	{
		public RequestContext Context;
		public IBeamableRequester Requester;
		public IBeamableServices Services;
		public IDependencyProvider Provider;
		public ISignedRequester SignedRequester => Provider.GetService<ISignedRequester>();

		RequestContext IUserScope.Context => Context;

		IBeamableRequester IUserScope.Request => Requester;

		IBeamableServices IUserScope.Services => Services;

		IDependencyProvider IUserScope.Provider => Provider;

		public ValueTask DisposeAsync()
		{
			// this whole type is obsolete, we don't need to do anything here. 
			return new ValueTask();
		}

		public void Dispose()
		{
			
		}
	}

	public interface IUserScope 
			: IDisposable
#if NETSTANDARD2_1_OR_GREATER
			, IAsyncDisposable
#endif
	{
		public RequestContext Context { get; }
		public IBeamableRequester Request { get; }
		public IBeamableServices Services { get; }
		public IDependencyProvider Provider { get; }
	}

	public interface IUserScopeCallbackReceiver : IUserScope
	{
		void ReceiveDefaultServices(IDependencyProviderScope scope);
	}

	public static class IUserScopeExtensions
	{
		public static IUserScope CreateUserScope(
			this IUserScope scope, 
			long userId,
			Action<IDependencyBuilder> configurator = null,
			bool requireAdmin = false
			)
		{
			if (userId <= 0)
			{
				throw new InvalidArgumentException(
					nameof(userId), $"Invalid User Id of value: {userId}. Should be a positive value.");
			}
			// require admin privs.
			if (requireAdmin)
			{
				scope.Context.AssertAdmin();
			}
			
			var newCtx = new RequestContext(
				scope.Context.Cid, scope.Context.Pid, scope.Context.Id, scope.Context.Status, userId, scope.Context.Path, scope.Context.Method, scope.Context.Body,
				scope.Context.Scopes, scope.Context.Headers);
			
			
			var provider = scope.Provider.Fork(builder =>
			{
				// each _request_ gets its own service scope, so we fork the provider again and override certain services. 
				builder.Remove<RequestContext>();
				builder.AddScoped(newCtx);
				
				configurator?.Invoke(builder);
			});

			var nextScope = new UserRequestDataHandler(provider);
			return nextScope;
		}
		
	}

	/// <summary>
	/// This type defines the %Microservice main entry point for the %Microservice feature.
	///
	/// A microservice architecture, or "microservice", is a solution of developing software
	/// systems that focuses on building single-function modules with well-defined interfaces
	/// and operations.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See the <a target="_blank" href="https://help.beamable.com/Unity-Latest/unity/user-reference/cloud-services/microservices/microservice-framework/">Microservice</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public abstract class Microservice : IUserScopeCallbackReceiver
	{
		/// <summary>
		/// This type defines the %Microservice %RequestContext.
		/// </summary>
		protected RequestContext Context;

		/// <summary>
		/// This type defines the %IBeamableRequester.
		/// </summary>
		protected IBeamableRequester Requester;

		/// <summary>
		/// The <see cref="ISignedRequester"/> is a requesting abstraction that uses Beamable Signed Requests instead of
		/// the secure websocket protocol
		/// </summary>
		protected ISignedRequester SignedRequester => _serviceProvider.GetService<ISignedRequester>();
		/// <summary>
		/// This type defines the %Microservice main entry point for %Beamable %Microservice features.
		///
		/// #### Related Links
		/// - See Beamable.Server.IBeamableServices script reference
		///
		/// </summary>
		protected IBeamableServices Services;

		protected IStorageObjectConnectionProvider Storage;

		RequestContext IUserScope.Context => Context;

		IBeamableRequester IUserScope.Request => Requester;

		IBeamableServices IUserScope.Services => Services;

		IDependencyProvider IUserScope.Provider => Provider;

		/// <summary>
		/// <para>
		/// The <see cref="IDependencyProvider"/> gives access to the dependency scope for this request.
		/// You can configure custom services by using the see <see cref="ConfigureServicesAttribute"/>.
		/// </para>
		///
		/// <para>
		/// This <see cref="IDependencyProvider"/> references a service scope that is created for every request.
		/// Anytime a <see cref="ClientCallableAttribute"/> is executed, a new service scope is created to handle
		/// the request. The scope is forked from a scope per service connection, which itself is forked from 1 root
		/// scope for the entire service. The <see cref="ConfigureServicesAttribute"/> is used to configure the root
		/// <see cref="IDependencyBuilder"/>. This provider is forked from that builder. 
		/// </para>
		///
		/// <para>
		/// The <see cref="InitializeServicesAttribute"/> can be used to run custom logic after the root
		/// scope is created, and before any traffic is accepted.
		/// </para>
		///
		/// </summary>
		protected IDependencyProvider Provider => _serviceProvider;

		[Obsolete("Use " + nameof(Provider) + " instead.")]
		protected IDependencyProvider ServiceProvider => _serviceProvider;

		private RequesterFactory _requesterFactory;
		private ServicesFactory _servicesFactory;
		private IDependencyProviderScope _serviceProvider;
		private UserGenerator _scopeGenerator;

		[Obsolete]
		public void ProvideContext(RequestContext ctx)
		{
			Context = ctx;
		}

		[Obsolete]
		public void ProvideRequester(RequesterFactory requesterFactory)
		{
			_requesterFactory = requesterFactory;
			Requester = _requesterFactory(Context);
		}

		[Obsolete]
		public void ProvideServices(ServicesFactory servicesFactory)
		{
			_servicesFactory = servicesFactory;
			Services = _servicesFactory(Requester, Context);
		}

		[Obsolete]
		public void ProvideDefaultServices(IDependencyProviderScope provider, UserGenerator scopeGenerator)
		{
			Context = provider.GetService<RequestContext>();
			Requester = provider.GetService<IBeamableRequester>();
			Services = provider.GetService<IBeamableServices>();
			Storage = provider.GetService<IStorageObjectConnectionProvider>();
			_serviceProvider = provider;
			_scopeGenerator = scopeGenerator;
		}
		
		public void ReceiveDefaultServices(IDependencyProviderScope scope)
		{
			Context = scope.GetService<RequestContext>();
			Requester = scope.GetService<IBeamableRequester>();
			Services = scope.GetService<IBeamableServices>();
			Storage = scope.GetService<IStorageObjectConnectionProvider>();
			_serviceProvider = scope;
		}

		/// <summary>
		/// Build a request context and collection of services that represents another player.
		/// <para>
		/// This can be used to take API actions on behalf of another player. For example, if
		/// you needed to modify another player's currency, you could use this method's return object
		/// to access an <see cref="IMicroserviceInventoryApi"/> and make a call.
		/// </para>
		/// </summary>
		/// <param name="userId">The user id of the player for whom you'd like to make actions on behalf of</param>
		/// <param name="requireAdminUser">
		/// By default, this method can only be called by a user with admin access token.
		/// <para> If you pass in false for this parameter, then any user's request can assume another user.
		/// <b> This can be dangerous, and you should be careful that the code you write cannot be exploited. </b>
		/// </para>
		/// </param>
		/// <returns>
		/// A <see cref="RequestHandlerData"/> object that contains a request context, and a collection of services to execute SDK calls against.
		/// </returns>
		[Obsolete("Please use the " + nameof(AssumeNewUser) + " instead")]
		protected RequestHandlerData AssumeUser(long userId, bool requireAdminUser = true)
		{
			var scope = (IUserScope)this;
			var nextScope = scope.CreateUserScope(userId, requireAdmin: requireAdminUser);
			return new RequestHandlerData
			{
				Context = nextScope.Context,
				Provider = nextScope.Provider,
				Requester = nextScope.Request,
				Services = nextScope.Services
			};
		}

		/// <summary>
		/// Build a request context and collection of services that represents another player.
		/// <para>
		/// This can be used to take API actions on behalf of another player. For example, if
		/// you needed to modify another player's currency, you could use this method's return object
		/// to access an <see cref="IMicroserviceInventoryApi"/> and make a call.
		/// </para>
		/// </summary>
		/// <param name="userId">The user id of the player for whom you'd like to make actions on behalf of</param>
		/// <param name="configurator">
		/// A callback that receives the <see cref="IDependencyBuilder"/> of the scope being created. 
		/// </param>
		/// <param name="requireAdminUser">
		/// By default, this method can only be called by a user with admin access token.
		/// <para> If you pass in false for this parameter, then any user's request can assume another user.
		/// <b> This can be dangerous, and you should be careful that the code you write cannot be exploited. </b>
		/// </para>
		/// </param>
		/// <returns>A <see cref="UserRequestDataHandler"/> object that contains a request context, and a collection of services to execute SDK calls against.</returns>
		protected UserRequestDataHandler AssumeNewUser(long userId, Action<IDependencyBuilder> configurator = null, bool requireAdminUser = true)
		{
			return (UserRequestDataHandler)this.CreateUserScope(userId, configurator, requireAdminUser);
		}

		public async Promise DisposeMicroservice()
		{
			await _serviceProvider.Dispose();
			_serviceProvider = null;
			Context = null;
			Requester = null;
			Services = null;
			_requesterFactory = null;
			_servicesFactory = null;
			_scopeGenerator = null;
		}

		public ValueTask DisposeAsync()
		{
			// TODO: think about doing actual clean up here?
			return new ValueTask();
		}

		public void Dispose()
		{
			
		}
	}

	public delegate IDependencyProviderScope UserGenerator(RequestContext requestContext, Action<IDependencyBuilder> configurator);
	
	public class UserRequestDataHandler : IUserScope
	{
		public RequestContext Context;
		public IBeamableRequester Requester;
		public IBeamableServices Services;
		public IDependencyProvider Provider => _serviceProvider;

		private IDependencyProviderScope _serviceProvider;
		
		public UserRequestDataHandler(IDependencyProviderScope serviceProvider)
		{
			_serviceProvider = serviceProvider;
			Context = _serviceProvider.GetService<RequestContext>();
			Requester = _serviceProvider.GetService<IBeamableRequester>();
			Services = _serviceProvider.GetService<IBeamableServices>();
		}

		public void Dispose()
		{
			_serviceProvider.Dispose();
			Context = null;
			Requester = null;
			Services = null;
			_serviceProvider = null;
		}

#if NETSTANDARD2_1_OR_GREATER
		public async ValueTask DisposeAsync()
		{
			await _serviceProvider.Dispose();
			Context = null;
			Requester = null;
			Services = null;
			_serviceProvider = null;
		}
#endif
		RequestContext IUserScope.Context => Context;
		
		IBeamableRequester IUserScope.Request => Requester;

		IBeamableServices IUserScope.Services => Services;

		IDependencyProvider IUserScope.Provider => Provider;
	}
}
