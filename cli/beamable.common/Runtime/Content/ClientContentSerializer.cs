using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Beamable.Common.Content.Serialization
{
	public class ClientContentSerializer : ContentSerializer<ContentObject>
	{
		private static ClientContentSerializer _instance;

		private static ContentSerializer<ContentObject> Instance => _instance ?? (_instance = new ClientContentSerializer());
		

		public static string SerializeContent<TContent>(TContent content) where TContent : IContentObject, new() =>
		   Instance.Serialize(content);

		[Obsolete("content serializer options are no longer supported.")]
		public new static string SerializeProperties<TContent>(TContent content, ContentSerializerOptions options = null) where TContent : IContentObject =>
		   Instance.SerializeProperties(content, options);

		public new static string SerializeProperties<TContent>(TContent content) where TContent : IContentObject =>
			Instance.SerializeProperties(content);


		protected override TContent CreateInstance<TContent>()
		{
			return ScriptableObject.CreateInstance<TContent>();
		}

		protected override ContentObject CreateInstanceWithType(Type type)
		{
			return (ContentObject)ScriptableObject.CreateInstance(type);
		}

		public static TContent DeserializeContent<TContent>(string json, bool disableExceptions = false) where TContent : ContentObject, IContentObject, new() =>
		   Instance.Deserialize<TContent>(json, disableExceptions);

		public static IContentObject DeserializeContentFromCli(string json, IContentObject instanceToDeserialize, string contentId, out SchemaDifference schemaIsDifferent, bool disableExceptions = false)
		{
			return Instance.DeserializeFromCli(json, instanceToDeserialize, contentId, out schemaIsDifferent, disableExceptions);
		}
		
		public static string CalculateChecksum(string propertiesJsonWithoutIndent)
		{
			var bytes = Encoding.UTF8.GetBytes(propertiesJsonWithoutIndent);
			using var sha1 = SHA1.Create();
			var hash = sha1.ComputeHash(bytes);
			var checksum = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
			return checksum;
		}
	}
}
