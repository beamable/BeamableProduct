using Beamable.Common.Api;
using Beamable.Server;

namespace microserviceTests.microservice
{
   [Microservice("serviceWithDeps", EnableEagerContentLoading = false)]
   public class ServiceWithDependency : Microservice
   {
      public IBeamableServices Services { get; }
      public IBeamableRequester Requester { get; }
      public RequestContext Ctx { get; }
      public CustomDependency Dep { get; }

      public ServiceWithDependency(IBeamableServices services, IBeamableRequester requester, RequestContext ctx, CustomDependency dep)
      {
         Services = services;
         Requester = requester;
         Ctx = ctx;
         Dep = dep;
      }

      [ClientCallable]
      public bool NoNulls()
      {
         return Services != null && Requester != null && Ctx != null && Dep.NoNulls();
      }

      [ConfigureServices]
      public static void Configure(IServiceBuilder builder)
      {
         builder.AddTransient<CustomDependency>();
      }
   }

   public class CustomDependency
   {
      public IBeamableServices Services { get; }
      public IBeamableRequester Requester { get; }
      public RequestContext Ctx { get; }

      public CustomDependency(IBeamableServices services, IBeamableRequester requester, RequestContext ctx)
      {
         Services = services;
         Requester = requester;
         Ctx = ctx;
      }

      public bool NoNulls()
      {
         return Services != null && Requester != null && Ctx != null;
      }
   }
}
