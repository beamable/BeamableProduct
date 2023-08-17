using Beamable.Server;
using Beamable.Tooling.Common.OpenAPI;
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
	   public Type MicroserviceType { get; set; }

	   /// <summary>
	   /// MicroserviceAttribute associated with the administrative routes.
	   /// </summary>
	   public MicroserviceAttribute MicroserviceAttribute { get; set; }

	   /// <summary>
	   /// Public host for the administrative routes.
	   /// </summary>
	   public string PublicHost { get; set; }
	   
	   
      /// <summary>
      /// A simple method to check if the microservice can send and receive network traffic.
      /// </summary>
      /// <returns>The word "responsive" if all is well.</returns>
      [Callable]
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
      [Callable]
      [CustomResponseSerializationAttribute]
      public string Docs()
      {
	      var docs = new ServiceDocGenerator();
	      var doc = docs.Generate(MicroserviceType, MicroserviceAttribute, this);

	      if (!string.IsNullOrEmpty(PublicHost))
	      {
		      doc.Servers.Add(new OpenApiServer { Url = PublicHost });

	      }
	      
	      var outputString = doc.Serialize(OpenApiSpecVersion.OpenApi3_0, OpenApiFormat.Json);

	      return outputString;
      }
   }
}
