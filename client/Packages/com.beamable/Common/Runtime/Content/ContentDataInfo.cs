// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using Beamable.Common.Content.Serialization;
using Beamable.Common.Spew;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Common.Content
{
	[Serializable]
	public class ContentDataInfo
	{
		private static readonly ClientContentSerializer _serializer = new ClientContentSerializer();
		
		public string contentId;
		public string data;
		
		/// <summary>
		/// Added in 1.19.17; so may be absent from earlier records.
		/// To safely access this property, please use <see cref="GetContentVersion"/>
		/// </summary>
		[SerializeField]
		public string contentVersion;

		public string GetContentVersion()
		{
			if (!string.IsNullOrEmpty(contentVersion))
			{
				// hooray, there is a content version hanging out we can use without needing to work for it.
				return contentVersion;
			}
			
			// because the contentVersion field was added late, it may be blank, so we need to retrieve it
			//  from the internal data structure.
			var proxy = JsonUtility.FromJson<VersionProxy>(data);
			contentVersion = proxy.version;

			return contentVersion;
		}

		[Serializable]
		class VersionProxy
		{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
			public string version;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
		}
	}

	[Serializable]
	public class ContentDataInfoWrapper : ISerializationCallbackReceiver
	{
		/// <summary>
		/// Member holding the serialized content. However,
		/// instead of writing/reading from this, please use
		/// <see cref="TryGetContent"/>, and <see cref="TryUpdateContent"/>
		/// </summary>
		public List<ContentDataInfo> content = new List<ContentDataInfo>();

		[NonSerialized]
		public Dictionary<string, ContentDataInfo> KeyToContentData = new Dictionary<string, ContentDataInfo>();
		
		[NonSerialized]
		public ulong cacheVersion;
		
		public static string GetCacheKey(string contentId, string contentVersion) => contentId + contentVersion;

		/// <summary>
		/// Retrieve raw content data from the collection
		/// </summary>
		/// <param name="contentId"></param>
		/// <param name="version"></param>
		/// <param name="contentInfo"></param>
		/// <returns></returns>
		public bool TryGetContent(string contentId, string version, out ContentDataInfo contentInfo)
		{
			var key = GetCacheKey(contentId, version);
			return KeyToContentData.TryGetValue(key, out contentInfo);
		} 
		
		public bool TryUpdateContent(ClientContentInfo info, string raw)
		{
			// we only need one entry per content-id, and we'll save the latest version of it. 
			var key = GetCacheKey(info.contentId, info.version);

			if (KeyToContentData.ContainsKey(key))
			{
				// the version is the same, and this content does not need to be updated.
				return false;
			}
			else
			{
				// but much more likely, this acts as more of a NEW content write,
				var newItem = KeyToContentData[key] = new ContentDataInfo
				{
					data = raw, 
					contentVersion = info.version, 
					contentId = info.contentId
				};
				
				// and we need to add it to the actual serialized data
				content.Add(newItem);
				cacheVersion++;
				return true;
			}
		}
		
		public void OnBeforeSerialize()
		{
			content ??= new List<ContentDataInfo>();

			// we need to evict old files. 
			//  since new content is always added to the end,
			//  we'll evict any content with a repeat content-id
			//  going from back to front.
			var seen = new HashSet<string>();
			var startSize = content.Count;
			for (var i = content.Count - 1; i >= 0; i--)
			{
				var info = content[i];
				if (seen.Contains(info.contentId))
				{
					// because we are going backwards through the list, we 
					//  can safely delete entries as we go and avoid concurrent modification
					content.RemoveAt(i);
				}

				seen.Add(info.contentId);
			}
			var endSize = content.Count;
			PlatformLogger.Log($"Content Data Info: Before serializing, removed {startSize - endSize} entries.");
		}

		public void OnAfterDeserialize()
		{
			KeyToContentData.Clear();
			content ??= new List<ContentDataInfo>();

			foreach (var info in content)
			{
				var key = GetCacheKey(info.contentId, info.GetContentVersion());
				KeyToContentData[key] = info;
			}
		}
	}
}
