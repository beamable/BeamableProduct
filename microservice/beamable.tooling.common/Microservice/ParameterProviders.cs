using System.Text.Json;

namespace Beamable.Server
{
	/// <summary>
	/// Represents a provider for method parameters.
	/// </summary>
   public interface IParameterProvider
   {
	   /// <summary>
	   /// Gets the parameters for a specified service method.
	   /// </summary>
	   /// <param name="method">The service method for which to retrieve parameters.</param>
	   /// <returns>An array of method parameters.</returns>
      object[] GetParameters(ServiceMethod method);
   }

	/// <summary>
	/// Provides adaptive method parameters based on the microservice request context.
	/// </summary>
   public class AdaptiveParameterProvider : IParameterProvider
   {
	   private MicroserviceRequestContext _ctx;

	   /// <summary>
	   /// Initializes a new instance of the <see cref="AdaptiveParameterProvider"/> class.
	   /// </summary>
	   /// <param name="ctx">The microservice request context used for parameter adaptation.</param>
	   public AdaptiveParameterProvider(MicroserviceRequestContext ctx)
	   {
		   _ctx = ctx;
	   }

	   /// <inheritdoc />
	   public object[] GetParameters(ServiceMethod method)
	   {
		   var hasPayloadProperty = _ctx.BodyElement.TryGetProperty("payload", out var payloadElem);

		   var isPayloadArray = hasPayloadProperty && payloadElem.ValueKind == JsonValueKind.Array;

		   var methodHasPayloadField = method.ParameterNames.Contains("payload");
		   IParameterProvider internalProvider;
		   if (hasPayloadProperty && methodHasPayloadField)
		   {
			   if (!isPayloadArray)
			   {
				   internalProvider = new NamedParameterProvider(_ctx.BodyElement);
			   }
			   else
			   {
				   // TODO: This is really stupid. But I don't know how to figure out which serialization to use; so just don't play the game, and let this fail early in testing.
				   throw new ParameterLegacyException();
			   }
		   }
		   else if (hasPayloadProperty)
		   {
			   internalProvider = new PayloadArrayParameterProvider(_ctx.BodyElement);
		   }
		   else
		   {
			   internalProvider = new NamedParameterProvider(_ctx.BodyElement);
		   }

		   return internalProvider.GetParameters(method);
	   }
   }

	/// <summary>
	/// Provides method parameters based on a JSON payload array.
	/// </summary>
   public class PayloadArrayParameterProvider : IParameterProvider
   {
	   private readonly JsonElement _bodyElement;
	   private List<string> jsonArgs;

	   /// <summary>
	   /// Initializes a new instance of the <see cref="PayloadArrayParameterProvider"/> class.
	   /// </summary>
	   /// <param name="bodyElement">The JSON element representing the payload array.</param>
      public PayloadArrayParameterProvider(JsonElement bodyElement)
      {
	      _bodyElement = bodyElement;
	      jsonArgs = new List<string>();
      }

      /// <inheritdoc />
      public object[] GetParameters(ServiceMethod method)
      {
	      if (_bodyElement.TryGetProperty("payload", out var payloadString))
	      {
		      if (payloadString.ValueKind == JsonValueKind.Null)
		      {
			      jsonArgs.Add("null");
		      }
		      else
		      {
			      using (var payloadDoc = JsonDocument.Parse(payloadString.ToString()))
			      {
				      foreach (var argJson in payloadDoc.RootElement.EnumerateArray())
				      {
					      jsonArgs.Add(argJson.GetRawText());
				      }
			      }
		      }
	      }
	      
         var args = new object[method.Deserializers.Count];
         if (jsonArgs == null)
         {
            throw new ParameterNullException();
         }

         if (jsonArgs.Count != args.Length)
         {
            throw new ParameterCardinalityException(args.Length, jsonArgs.Count);
         }

         for (var i = 0; i < args.Length; i++)
         {
            args[i] = method.Deserializers[i](jsonArgs[i]);
         }

         return args;
      }
   }


	/// <summary>
	/// Provides method parameters based on named parameters in a JSON payload.
	/// </summary>
   public class NamedParameterProvider : IParameterProvider
   {
	   private readonly JsonElement _bodyElement;

      // public NamedParameterProvider(MicroserviceRequestContext ctx)
      // {
      //    _bodyDoc = JsonDocument.Parse(ctx.Body);
      // }

      /// <summary>
      /// Initializes a new instance of the <see cref="NamedParameterProvider"/> class.
      /// </summary>
      /// <param name="bodyElement">The JSON element representing the payload with named parameters.</param>
      public NamedParameterProvider(JsonElement bodyElement)
      {
	      _bodyElement = bodyElement;
      }

      /// <inheritdoc />
      public object[] GetParameters(ServiceMethod method)
      {
         var args = new object[method.Deserializers.Count];

         for (var i = 0; i < method.ParameterNames.Count; i++)
         {
            var parameterName = method.ParameterNames[i];

            if (!_bodyElement.TryGetProperty(parameterName, out var jsonElement))
            {
               throw new ParameterMissingRequiredException(parameterName);
            }

            args[i] = method.ParameterDeserializers[parameterName](jsonElement.GetRawText());
         }

         return args;
      }
   }
}
