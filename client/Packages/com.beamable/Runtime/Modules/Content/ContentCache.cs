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
	public class ContentCache
	{
		private static readonly ClientContentSerializer _serializer = new ClientContentSerializer();
		
		private readonly Dictionary<int, Promise<ContentObject>> _cache =
			new Dictionary<int, Promise<ContentObject>>();

		private readonly IHttpRequester _requester;
		private readonly ContentService _contentService;
		private readonly ContentTypeReflectionCache _reflectionCache;

		public ContentCache(
			IHttpRequester requester, 
			IBeamableFilesystemAccessor filesystemAccessor, 
			ContentService contentService, 
			CoroutineService coroutineService) // not removing unused field so as to not cause a breaking change.
		{
			_reflectionCache = Beam.GetReflectionSystem<ContentTypeReflectionCache>();
			_requester = requester;
			_contentService = contentService;
		}

		public Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo)
		{
			return GetContent(requestedInfo).Map(content => (IContentObject)content);
		}

		public Promise<ContentObject> GetContent(ClientContentInfo requestedInfo)
		{
			var cacheId = requestedInfo.CalculateHashCode();

			// First, try the in memory cache
			PlatformLogger.Log(
				$"ContentCache: Fetching content from cache for {requestedInfo.contentId}: version: {requestedInfo.version}");
			if (_cache.TryGetValue(cacheId, out var cacheEntry)) return cacheEntry;
			
			// Then, try the on disk cache
			// Then check if the data exists as baked content
			if (TryGetValueFromDisk(requestedInfo, out var cachedContent)|| TryGetBaked(requestedInfo, out cachedContent))
			{
				var promise = Promise<ContentObject>.Successful(cachedContent);
				_cache.Add(cacheId, promise);
				return promise;
			}
			
			// Finally, if not found, fetch the content from the CDN
			var fetchedContent = DownloadContent(requestedInfo);
			return fetchedContent;
		}

		private async Promise<ContentObject> DownloadContent(ClientContentInfo requestedInfo)
		{
			PlatformLogger.Log(
				$"ContentCache: Fetching content from CDN for {requestedInfo.contentId}: version: {requestedInfo.version}");
			var cacheId = requestedInfo.CalculateHashCode();

			async Promise<ContentObject> Download()
			{
				var raw = await FetchContentFromCDN(requestedInfo);
				UpdateDiskFile(requestedInfo, raw);
				return DeserializeContent(requestedInfo, raw);
			}
			
			try
			{
				var promise = Download();
				_cache.Add(cacheId, promise);
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

		private bool TryGetContentFromInfo(ContentDataInfoWrapper wrapper, ClientContentInfo info, out ContentObject content)
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

		private bool TryGetBaked(ClientContentInfo info, out ContentObject content)
		{
			PlatformLogger.Log(
				$"ContentCache: Checking content from baked for {info.contentId}: version: {info.version}");
			return TryGetContentFromInfo(_contentService.BakedContentDataInfo, info, out content);
		}

		private bool TryGetValueFromDisk(ClientContentInfo info, out ContentObject content)
		{
			PlatformLogger.Log(
				$"ContentCache: Loading content from disk for {info.contentId}: version: {info.version}");
			return TryGetContentFromInfo(_contentService.CachedContentDataInfo, info, out content);
		}

		private void UpdateDiskFile(ClientContentInfo info, string raw)
		{
			_contentService.CachedContentDataInfo.TryUpdateContent(info, raw);
		}

		private static ContentObject DeserializeContent(ClientContentInfo info, string raw)
		{
			var content = _serializer.DeserializeByType(raw, info.ContentType);
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
