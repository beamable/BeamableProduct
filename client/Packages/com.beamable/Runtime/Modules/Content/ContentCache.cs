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
	public struct ContentCacheEntry<TContent> where TContent : ContentObject
	{
		public readonly string Version;
		public readonly Promise<TContent> Content;

		public ContentCacheEntry(string version, Promise<TContent> content)
		{
			Version = version;
			Content = content;
		}
	}

	public abstract class ContentCache
	{
		public abstract Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo);
	}

	public class ContentCache<TContent> : ContentCache where TContent : ContentObject, new()
	{
		private static readonly ClientContentSerializer _serializer = new ClientContentSerializer();
		
		private readonly Dictionary<string, ContentCacheEntry<TContent>> _cache =
			new Dictionary<string, ContentCacheEntry<TContent>>();

		private readonly IHttpRequester _requester;
		private readonly ContentService _contentService;


		public ContentCache(
			IHttpRequester requester, 
			IBeamableFilesystemAccessor filesystemAccessor, 
			ContentService contentService, 
			CoroutineService coroutineService) // not removing unused field so as to not cause a breaking change.
		{
			_requester = requester;
			_contentService = contentService;
		}

		public override Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo)
		{
			return GetContent(requestedInfo).Map(content => (IContentObject)content);
		}

		private static string GetCacheKey(ClientContentInfo requestedInfo)
		{
			return $"{requestedInfo.contentId}:{requestedInfo.manifestID}:{requestedInfo.version}";
		}

		private void SetCacheEntry(string cacheId, ContentCacheEntry<TContent> entry)
		{
			_cache[cacheId] = entry;
		}

		public Promise<TContent> GetContent(ClientContentInfo requestedInfo)
		{
			var cacheId = GetCacheKey(requestedInfo);

			// First, try the in memory cache
			PlatformLogger.Log(
				$"ContentCache: Fetching content from cache for {requestedInfo.contentId}: version: {requestedInfo.version}");
			if (_cache.TryGetValue(cacheId, out var cacheEntry)) return cacheEntry.Content;
			
			// Then, try the on disk cache
			if (TryGetValueFromDisk(requestedInfo, out var diskContent))
			{
				var promise = Promise<TContent>.Successful(diskContent);
				SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
				return promise;
			}
			
			// Check if the data exists as baked content
			if (TryGetBaked(requestedInfo, out var bakedContent))
			{
				var promise = Promise<TContent>.Successful(bakedContent);
				SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
				return promise;
			}
			
			// Finally, if not found, fetch the content from the CDN
			var fetchedContent = DownloadContent(requestedInfo);
			return fetchedContent;
		}

		private async Promise<TContent> DownloadContent(ClientContentInfo requestedInfo)
		{
			PlatformLogger.Log(
				$"ContentCache: Fetching content from CDN for {requestedInfo.contentId}: version: {requestedInfo.version}");
			var cacheId = GetCacheKey(requestedInfo);

			async Promise<TContent> Download()
			{
				var raw = await FetchContentFromCDN(requestedInfo);
				UpdateDiskFile(requestedInfo, raw);
				return DeserializeContent(requestedInfo, raw);
			}
			
			try
			{
				var promise = Download();
				SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
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

		private bool TryGetContentFromInfo(ContentDataInfoWrapper wrapper, ClientContentInfo info, out TContent content)
		{
			content = default;
			if (!wrapper.TryGetContent(info.contentId, info.version, out var existingInfo))
			{
				return false;
			}

			try
			{
				content = DeserializeContent(info, existingInfo.data);
			}
			catch (Exception ex)
			{
				PlatformLogger.Log(
					$"ContentCache: Unable to deserialize content type correctly {info.contentId}: version: {info.version} message: {ex.Message}");
				return false;
			}

			return true;
		}

		private bool TryGetBaked(ClientContentInfo info, out TContent content)
		{
			PlatformLogger.Log(
				$"ContentCache: Checking content from baked for {info.contentId}: version: {info.version}");
			return TryGetContentFromInfo(_contentService.BakedContentDataInfo, info, out content);
		}

		private bool TryGetValueFromDisk(ClientContentInfo info, out TContent content)
		{
			PlatformLogger.Log(
				$"ContentCache: Loading content from disk for {info.contentId}: version: {info.version}");
			return TryGetContentFromInfo(_contentService.CachedContentDataInfo, info, out content);
		}

		private void UpdateDiskFile(ClientContentInfo info, string raw)
		{
			_contentService.CachedContentDataInfo.TryUpdateContent(info, raw);
		}

		private static TContent DeserializeContent(ClientContentInfo info, string raw)
		{
			var content = _serializer.Deserialize<TContent>(raw);
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
