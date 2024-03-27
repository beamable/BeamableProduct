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
		private readonly IBeamableFilesystemAccessor _filesystemAccessor;
		private readonly CoroutineService _coroutineService;
		private readonly ContentService _contentService;

		private const float WriteToFileDelay = 5;

		public ContentCache(
			IHttpRequester requester, 
			IBeamableFilesystemAccessor filesystemAccessor, 
			ContentService contentService, 
			CoroutineService coroutineService)
		{
			_requester = requester;
			_filesystemAccessor = filesystemAccessor;
			_coroutineService = coroutineService;
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

			// Check if the data exists as baked content
			if (TryGetBaked(requestedInfo, out var bakedContent))
			{
				var promise = Promise<TContent>.Successful(bakedContent);
				SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
				return promise;
			}
			
			// Then, try the on disk cache
			PlatformLogger.Log(
				$"ContentCache: Loading content from disk for {requestedInfo.contentId}: version: {requestedInfo.version}");
			if (TryGetValueFromDisk(requestedInfo, out var diskContent, _filesystemAccessor))
			{
				var promise = Promise<TContent>.Successful(diskContent);
				SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
				return promise;
			}

			// Finally, if not found, fetch the content from the CDN
			PlatformLogger.Log(
				$"ContentCache: Fetching content from CDN for {requestedInfo.contentId}: version: {requestedInfo.version}");
			var fetchedContent = DownloadContent(requestedInfo);
			return fetchedContent;
		}

		private async Promise<TContent> DownloadContent(ClientContentInfo requestedInfo)
		{
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

			content = DeserializeContent(info, existingInfo.data);
			return true;
		}

		private bool TryGetBaked(ClientContentInfo info, out TContent content)
		{
			PlatformLogger.Log(
				$"ContentCache: Checking content from baked for {info.contentId}: version: {info.version}");
			content = default;
			var bakedContent = _contentService.BakedContentDataInfo;
			if (bakedContent == null)
			{
				return false;
			}

			return TryGetContentFromInfo(bakedContent, info, out content);
		}

		private bool TryGetValueFromDisk(ClientContentInfo info, out TContent content,
										 IBeamableFilesystemAccessor fsa)
		{
			var filePath = ContentService.ContentPath(fsa);
			content = default;
			if (!File.Exists(filePath)) // if there is no cache content file, then this info doesn't exist.
			{
				return false;
			}
			
			return TryGetContentFromInfo(_contentService.CachedContentDataInfo, info, out content);
		}

		private Coroutine _updateDiskFileCoroutine;
		private IEnumerator WriteToDisk()
		{
			yield return new WaitForSeconds(WriteToFileDelay);
			var filePath = ContentService.ContentPath(_filesystemAccessor);

			// Ensure the directory is created
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			File.WriteAllText(filePath, JsonUtility.ToJson(_contentService.CachedContentDataInfo));
		}

		private void UpdateDiskFile(ClientContentInfo info, string raw)
		{
			var needsUpdating = _contentService.CachedContentDataInfo.TryUpdateContent(info, raw);
			if (!needsUpdating)
			{
				return;
			}
			try
			{
				if (_updateDiskFileCoroutine != null)
				{
					_coroutineService.StopCoroutine(_updateDiskFileCoroutine);
				}
				_updateDiskFileCoroutine = _coroutineService.StartNew("ContentFileUpdate", WriteToDisk());
			}
			catch (Exception e)
			{
				PlatformLogger.Log($"ContentCache: Error saving content to disk: {e}");
			}
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
