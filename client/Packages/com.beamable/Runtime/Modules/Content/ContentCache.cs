using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Spew;
using Beamable.Coroutines;
using Core.Platform.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using static Beamable.Common.Constants.Features.Content;
using Debug = UnityEngine.Debug;

namespace Beamable.Content
{
	public struct ContentCacheEntry
	{
		public readonly string Version;
		public readonly Promise<ContentObject> Content;

		public ContentCacheEntry(string version, Promise<ContentObject> content)
		{
			Version = version;
			Content = content;
		}
	}

	public interface IContentCache
	{
		Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo, Type contentType);
	}

	public class ContentCache : IContentCache 
	{
		private static readonly ClientContentSerializer _serializer = new ClientContentSerializer();
		
		private readonly Dictionary<string, ContentCacheEntry> _cache =
			new Dictionary<string, ContentCacheEntry>();

		private readonly IHttpRequester _requester;
		private readonly ContentService _contentService;
		private readonly ContentTypeReflectionCache _reflectionCache;

		public ContentCache(IHttpRequester requester,
		                    ContentService contentService) // not removing unused field so as to not cause a breaking change.
		{
			_requester = requester;
			_contentService = contentService;
			_reflectionCache = Beam.GetReflectionSystem<ContentTypeReflectionCache>();
		}

		public Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo, Type contentType)
		{
			return GetContent(requestedInfo, contentType).Map(content => (IContentObject)content);
		}

		private static string GetCacheKey(ClientContentInfo requestedInfo)
		{
			return $"{requestedInfo.contentId}:{requestedInfo.manifestID}:{requestedInfo.version}";
		}

		private void SetCacheEntry(string cacheId, ContentCacheEntry entry)
		{
			_cache[cacheId] = entry;
		}

		public Promise<ContentObject> GetContent(ClientContentInfo requestedInfo, Type contentType)
		{
			var cacheId = GetCacheKey(requestedInfo);

			// First, try the in memory cache
			PlatformLogger.Log(
				$"ContentCache: Fetching content from cache for {requestedInfo.contentId}: version: {requestedInfo.version}");
			if (_cache.TryGetValue(cacheId, out var cacheEntry)) return cacheEntry.Content;
			
			// Then, try the on disk cache
			if (TryGetValueFromDisk(requestedInfo, contentType, out var diskContent))
			{
				var promise = Promise<ContentObject>.Successful(diskContent);
				SetCacheEntry(cacheId, new ContentCacheEntry(requestedInfo.version, promise));
				return promise;
			}
			
			// Check if the data exists as baked content
			if (TryGetBaked(requestedInfo, contentType, out var bakedContent))
			{
				var promise = Promise<ContentObject>.Successful(bakedContent);
				SetCacheEntry(cacheId, new ContentCacheEntry(requestedInfo.version, promise));
				return promise;
			}
			
			// Finally, if not found, fetch the content from the CDN
			var fetchedContent = DownloadContent(requestedInfo, contentType);
			return fetchedContent;
		}

		private async Promise<ContentObject> DownloadContent(ClientContentInfo requestedInfo, Type contentType)
		{
			PlatformLogger.Log(
				$"ContentCache: Fetching content from CDN for {requestedInfo.contentId}: version: {requestedInfo.version}");
			var cacheId = GetCacheKey(requestedInfo);

			async Promise<ContentObject> Download()
			{
				var raw = await FetchContentFromCDN(requestedInfo);
				UpdateDiskFile(requestedInfo, raw);
				return DeserializeContent(requestedInfo, raw, contentType);
			}
			
			try
			{
				var promise = Download();
				SetCacheEntry(cacheId, new ContentCacheEntry(requestedInfo.version, promise));
				return await promise;
			}
			catch (Exception err)
			{
				_cache.Remove(cacheId);
				PlatformLogger.Log(
					$"ContentCache: Failed to resolve {requestedInfo.contentId} {requestedInfo.version} {requestedInfo.uri} ; ERR={err}");
				throw;
			}
		}

		private bool TryGetContentFromInfo(ContentDataInfoWrapper wrapper, ClientContentInfo info, Type contentType, out ContentObject content)
		{
			content = null;
			if (!wrapper.TryGetContent(info.contentId, info.version, out var existingInfo))
			{
				return false;
			}

			try
			{
				content = DeserializeContent(info, existingInfo.data, contentType);
			}
			catch (Exception ex)
			{
				PlatformLogger.Log(
					$"ContentCache: Unable to deserialize content type correctly {info.contentId}: version: {info.version} message: {ex.Message}");
				return false;
			}

			return true;
		}

		private bool TryGetBaked(ClientContentInfo info, Type contentType, out ContentObject content)
		{
			PlatformLogger.Log(
				$"ContentCache: Checking content from baked for {info.contentId}: version: {info.version}");
			return TryGetContentFromInfo(_contentService.BakedContentDataInfo, info, contentType, out content);
		}

		private bool TryGetValueFromDisk(ClientContentInfo info, Type contentType, out ContentObject content)
		{
			PlatformLogger.Log(
				$"ContentCache: Loading content from disk for {info.contentId}: version: {info.version}");
			return TryGetContentFromInfo(_contentService.CachedContentDataInfo, info, contentType, out content);
		}

		private void UpdateDiskFile(ClientContentInfo info, string raw)
		{
			_contentService.CachedContentDataInfo.TryUpdateContent(info, raw);
		}

		private static ContentObject DeserializeContent(ClientContentInfo info, string raw, Type contentType)
		{
			var content = _serializer.DeserializeByType(raw, contentType);
			content.SetManifestID(info.manifestID);
			content.Tags = info.tags;
			return content;
		}


		private Promise<string> FetchContentFromCDN(ClientContentInfo info)
		{
			return _requester.ManualRequest(Method.GET, info.uri, parser: s => s);
		}
	}
}
