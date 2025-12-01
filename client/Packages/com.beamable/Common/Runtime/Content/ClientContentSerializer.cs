// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System;
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

		public static TContent DeserializeContent<TContent>(string json, bool disableExceptions = false) where TContent : ContentObject, IContentObject, new() =>
		   Instance.Deserialize<TContent>(json, disableExceptions);

		public static IContentObject DeserializeContentFromCli(string json, IContentObject instanceToDeserialize, string contentId, bool disableExceptions = false)
		{
			return Instance.DeserializeFromCli(json, instanceToDeserialize, contentId, disableExceptions);
		}
	}
}
