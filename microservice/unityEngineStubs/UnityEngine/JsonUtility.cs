namespace UnityEngine
{
	public static class JsonUtility
	{
		public interface IConverter
		{
			string ToJson(object data);
			T FromJson<T>(string json);
		}

		public static IConverter Converter { get; set; }

		public static string ToJson(object data) => Converter.ToJson(data);

		public static T FromJson<T>(string json) => Converter.FromJson<T>(json);
	}
}
