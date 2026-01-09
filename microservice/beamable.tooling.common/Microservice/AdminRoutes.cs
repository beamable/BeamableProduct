using Beamable.Common;
using Beamable.Common.Dependencies;
using Beamable.Common.Util;
using beamable.server;
using Beamable.Server;
using Beamable.Server.Api.Usage;
using Beamable.Server.Editor;
using Beamable.Tooling.Common.OpenAPI;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;

namespace microservice.Common
{
	/// <summary>
	/// Represents default administrative routes associated with a microservice.
	/// </summary>
   public class AdminRoutes
   {
	   /// <summary>
	   /// Microservice associated with the administrative routes.
	   /// </summary>
	   [Obsolete("This type is no longer bound")]
	   public Type MicroserviceType { get; set; }

	   /// <summary>
	   /// MicroserviceAttribute associated with the administrative routes.
	   /// </summary>
	   public IMicroserviceAttributes MicroserviceAttribute { get; set; }
	   
	   /// <summary>
	   /// List of pre-generated federation component data
	   /// </summary>
	   public List<FederationComponent> FederationComponents { get; set; }

	   /// <summary>
	   /// Public host for the administrative routes.
	   /// </summary>
	   public string PublicHost { get; set; }
	   
	   /// <summary>
	   /// The dependency provider for the entire service
	   /// </summary>
	   public IDependencyProvider GlobalProvider { get; set; }
	   
	   public string sdkVersionBaseBuild { get; set; }
	   public string sdkVersionExecution { get; set; }
	   
	   /// <summary>
	   /// sometimes called the namePrefix, or localPrefix, the routingKey is the
	   /// unique name for the service to group traffic separately away from the deployed service.
	   ///
	   /// In a deloyed context, the routingKey should always be empty.
	   /// </summary>
	   public string routingKey { get; set; }
	   
	   /// <summary>
	   /// The PlayerId for the player that started this microservice instance.
	   /// In a deployed environment, this value should be empty (0)
	   /// </summary>
	   public long executorPlayerId { get; set; }
	   
      /// <summary>
      /// A simple method to check if the microservice can send and receive network traffic.
      /// </summary>
      /// <returns>The word "responsive" if all is well.</returns>
      [Callable(flags:CallableFlags.SkipGenerateClientFiles)]
      public string HealthCheck()
      {
         return "responsive";
      }

      /// <summary>
      /// Generates an OpenAPI/Swagger 3.0 document that describes the available service endpoints.
      /// </summary>
      /// <remarks>
      /// Any [ClientCallable] methods on the service will be included in the generated OpenAPI document.
      /// Any doc-comments on the methods will be included in the generated document.
      /// The summary, remarks, returns, and parameter tags are supported.
      /// </remarks>
      /// <returns>A json OpenAPI document</returns>
      [Callable(flags:CallableFlags.SkipGenerateClientFiles)]
      [CustomResponseSerializationAttribute]
      public string Docs()
      {
	      var docs = new ServiceDocGenerator();
	      var ctx = GlobalProvider.GetService<StartupContext>();
	      var doc = docs.Generate(ctx, GlobalProvider);
	     
	      if (!string.IsNullOrEmpty(PublicHost))
	      {
		      doc.Servers.Add(new OpenApiServer { Url = PublicHost });
	      }

	      var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);
	      
	      
	      return outputString;
      }

      /// <summary>
      /// Fetch various Beamable SDK metadata for the Microservice
      /// </summary>
      /// <remarks>
      /// <para>
      /// The sdkVersion is the Beamable SDK version used for the Microservice.
      /// </para>
      /// <para>
      /// the instanceId is the AWS ARN when deployed, the local docker imageId when running locally in docker.
      /// </para>
      /// </remarks>
      /// <returns>Metadata for the service</returns>
      [Callable(flags:CallableFlags.SkipGenerateClientFiles)]
      public MicroserviceRuntimeMetadata Metadata()
      {
	      var version = BeamAssemblyVersionUtil.GetVersion<AdminRoutes>();
	      var usageApi = GlobalProvider.GetService<IUsageApi>();
	      var metadata = usageApi.GetMetadata();
	      return new MicroserviceRuntimeMetadata
	      {
		      instanceId = metadata.instanceId,
		      sdkVersion = version,
		      sdkExecutionVersion = sdkVersionExecution,
		      sdkBaseBuildVersion = sdkVersionBaseBuild,
		      serviceName = MicroserviceAttribute.MicroserviceName,
#pragma warning disable CS0618 // Type or member is obsolete
		      useLegacySerialization = MicroserviceAttribute.UseLegacySerialization,
#pragma warning restore CS0618 // Type or member is obsolete
		      disableAllBeamableEvents = MicroserviceAttribute.DisableAllBeamableEvents,
		      enableEagerContentLoading = MicroserviceAttribute.EnableEagerContentLoading,
		      routingKey = routingKey,
		      federatedComponents = FederationComponents?.Select(x => new FederationComponentMetadata
		      {
			      federationNamespace = x.identity.GetUniqueName(),
			      federationType = x.typeName
		      }).ToList()
	      };
      }
   }
}
