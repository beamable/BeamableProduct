using Beamable.Server;

namespace microservice.Common
{
   public class AdminRoutes
   {
      public BeamableMicroService Microservice { get; set; }

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
         var doc = SwaggerGenerator.GenerateDocument(Microservice);
         return SwaggerGenerator.GetDocJson(doc);
      }
   }
}