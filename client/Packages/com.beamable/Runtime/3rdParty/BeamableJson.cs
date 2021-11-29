using JetBrains.Annotations;
using Utf8Json;
using Utf8Json.Formatters;
using Utf8Json.Unity;

namespace Beamable.Serialization
{
	public static class BeamableJson
	{
		static bool initialized = false;

		private static void Initialize()
		{
			if (!initialized)
			{
				Utf8Json.Resolvers.CompositeResolver.RegisterAndSetAsDefault(
					new IJsonFormatter[] {PrimitiveObjectFormatter.Default},
					new[]
					{
						Utf8Json.Resolvers.BuiltinResolver.Instance, Utf8Json.Resolvers.CompositeResolver.Instance,
						Utf8Json.Resolvers.DynamicGenericResolver.Instance,
						Utf8Json.Resolvers.AttributeFormatterResolver.Instance, UnityResolver.Instance
					}
				);
				initialized = true;
			}
		}

		public static string Serialize<T>(T objectToSerialize)
		{
			Initialize();
			return JsonSerializer.ToJsonString(objectToSerialize);
		}

		public static T Deserialize<T>(string json)
		{
			Initialize();
			return JsonSerializer.Deserialize<T>(json);
		}
	}
}
