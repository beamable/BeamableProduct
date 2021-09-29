using System;
using System.ComponentModel.Design;
using Beamable.Common.Api;

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


      protected RequestHandlerData AssumeUser(long userId)
      {
         // require admin privs.
         Context.CheckAdmin();
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
