using System.Collections.Generic;
using System.Text.Json;

namespace Beamable.Server
{
	public interface IParameterProvider
	{
		object[] GetParameters(ServiceMethod method);
	}
	public class AdaptiveParameterProvider : IParameterProvider
	{
		private IParameterProvider internalProvider;
		private bool _jsonHasPayloadProperty;
		private RequestContext _ctx;
		private bool _isPayloadArray;

		public AdaptiveParameterProvider(RequestContext ctx)
		{
			_ctx = ctx;
			using var bodyDoc = JsonDocument.Parse(ctx.Body);

			_jsonHasPayloadProperty = bodyDoc.RootElement.TryGetProperty("payload", out var payloadElem);
			_isPayloadArray = _jsonHasPayloadProperty && payloadElem.ValueKind == JsonValueKind.Array;
		}
		public object[] GetParameters(ServiceMethod method)
		{
			var methodHasPayloadField = method.ParameterNames.Contains("payload");

			if (_jsonHasPayloadProperty && methodHasPayloadField)
			{
				if (!_isPayloadArray)
				{
					internalProvider = new NamedParameterProvider(_ctx);
				}
				else
				{
					// TODO: This is really stupid. But I don't know how to figure out which serialization to use; so just don't play the game, and let this fail early in testing.
					throw new ParameterLegacyException();
				}
			}
			else if (_jsonHasPayloadProperty && !methodHasPayloadField)
			{
				internalProvider = new PayloadArrayParameterProvider(_ctx);
			}
			else
			{
				internalProvider = new NamedParameterProvider(_ctx);
			}

			return internalProvider.GetParameters(method);
		}
	}

	public class PayloadArrayParameterProvider : IParameterProvider
	{
		private List<string> jsonArgs;

		public PayloadArrayParameterProvider(RequestContext ctx)
		{
			jsonArgs = new List<string>();

			using var bodyDoc = JsonDocument.Parse(ctx.Body);
			// extract the payload object.
			if (bodyDoc.RootElement.TryGetProperty("payload", out var payloadString))
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
		}

		public object[] GetParameters(ServiceMethod method)
		{
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


	public class NamedParameterProvider : IParameterProvider
	{
		private JsonDocument _bodyDoc;

		public NamedParameterProvider(RequestContext ctx)
		{
			_bodyDoc = JsonDocument.Parse(ctx.Body);
		}

		public object[] GetParameters(ServiceMethod method)
		{
			var args = new object[method.Deserializers.Count];

			for (var i = 0; i < method.ParameterNames.Count; i++)
			{
				var parameterName = method.ParameterNames[i];

				if (!_bodyDoc.RootElement.TryGetProperty(parameterName, out var jsonElement))
				{
					throw new ParameterMissingRequiredException(parameterName);
				}

				args[i] = method.ParameterDeserializers[parameterName](jsonElement.GetRawText());
			}

			return args;
		}
	}
}
