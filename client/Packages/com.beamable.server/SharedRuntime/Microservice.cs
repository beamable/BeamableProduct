using Beamable.Common.Api;
using Beamable.Server.Api.Inventory;
using Beamable.Server.Editor;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Server
{
	public delegate TMicroService MicroserviceFactory<out TMicroService>() where TMicroService : Microservice;
	public delegate IBeamableRequester RequesterFactory(RequestContext ctx);
	public delegate IBeamableServices ServicesFactory(IBeamableRequester requester, RequestContext ctx);

	public struct RequestHandlerData
	{
		public RequestContext Context;
		public IBeamableRequester Requester;
		public IBeamableServices Services;
	}

	public abstract class MicroView
	{
		protected const string BASE_IMAGE = "node:12";

		public virtual string GenerateDockerBuildEnv(MicroserviceDescriptor service, ViewDescriptor view)
		{
			var srcPath = view.SourceDirectory.Substring(service.SourcePath.Length + 1);
			srcPath = Path.Combine(service.Type.Assembly.GetName().Name , srcPath);

			return $@"
FROM {BASE_IMAGE} AS {view.BuildEnvName}

# copy the various config files
COPY ./{srcPath} {view.WorkingDir}

# build the distro
WORKDIR {view.WorkingDir}/app~
RUN npm install
RUN npm run build
";
		}

		public virtual string GenerateDockerCopy(MicroserviceDescriptor service, ViewDescriptor view)
		{
			return $@"
# copy the the front end distro from the build env
COPY --from={view.BuildEnvName} {view.WorkingDir}/dist {view.WorkingDir}
";
		}

		public virtual string GetHtml(string imageName, Type serviceTpe, ViewDescriptor view)
		{
			var basePath = $"{imageName}/{serviceTpe.Assembly.GetName().Name}";
			var path = Path.Combine(basePath, $"{view.WorkingDir}/bundle.js");
			var javascript = File.ReadAllText(path);
			return "<script>" + javascript + "</script>";
		}
	}

	public class SvelteView : MicroView
	{
		// TODO: Create other standard views, like React, Vue, Solid, Angular, etc...
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
	/// - See the <a target="_blank" href="https://docs.beamable.com/docs/microservices-feature">Microservice</a> feature documentation
	/// - See Beamable.Server.IBeamableServices script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public abstract class Microservice
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
		/// This type defines the %Microservice main entry point for %Beamable %Microservice features.
		///
		/// #### Related Links
		/// - See Beamable.Server.IBeamableServices script reference
		///
		/// </summary>
		protected IBeamableServices Services;

		protected IStorageObjectConnectionProvider Storage;

		private RequesterFactory _requesterFactory;
		private ServicesFactory _servicesFactory;
		private IServiceProvider _serviceProvider;
		private Func<RequestContext, IServiceProvider> _scopeGenerator;

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

		public void ProvideDefaultServices(IServiceProvider provider, Func<RequestContext, IServiceProvider> scopeGenerator)
		{
			Context = provider.GetService<RequestContext>();
			Requester = provider.GetService<IBeamableRequester>();
			Services = provider.GetService<IBeamableServices>();
			Storage = provider.GetService<IStorageObjectConnectionProvider>();
			_serviceProvider = provider;
			_scopeGenerator = scopeGenerator;
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
		protected RequestHandlerData AssumeUser(long userId, bool requireAdminUser = true)
		{
			// require admin privs.
			if (requireAdminUser)
			{
				Context.CheckAdmin();
			}

			var newCtx = new RequestContext(
			   Context.Cid, Context.Pid, Context.Id, Context.Status, userId, Context.Path, Context.Method, Context.Body,
			   Context.Scopes);
			var provider = _scopeGenerator(newCtx);

			var requester = provider.GetService<IBeamableRequester>();
			var services = provider.GetService<IBeamableServices>();
			return new RequestHandlerData
			{
				Context = newCtx,
				Requester = requester,
				Services = services
			};
		}
	}
}
