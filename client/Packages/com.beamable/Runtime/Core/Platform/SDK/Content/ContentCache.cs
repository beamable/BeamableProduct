using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Content;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Spew;
using Core.Platform.SDK;
using UnityEngine;
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
        private readonly IContentApi _contentService;

        public ContentCache(IHttpRequester requester, IBeamableFilesystemAccessor filesystemAccessor, IContentApi contentService)
        {
            _requester = requester;
            _filesystemAccessor = filesystemAccessor;
            _contentService = contentService;
        }

        public override Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo)
        {
            return GetContent(requestedInfo).Map(content => (IContentObject) content);
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
            PlatformLogger.Log(
                $"ContentCache: Loading content from disk for {requestedInfo.contentId}: version: {requestedInfo.version}");
            if (TryGetValueFromDisk(requestedInfo, out var diskContent, _filesystemAccessor))
            {
                var promise = Promise<TContent>.Successful(diskContent);
                SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
                return promise;
            }
            
            // Check baked file for requested content
            PlatformLogger.Log(
                $"ContentCache: Loading content from baked file for {requestedInfo.contentId}: version: {requestedInfo.version}");
            if (TryGetValueFromBaked(requestedInfo, out var bakedContent))
            {
                var promise = Promise<TContent>.Successful(bakedContent);
                SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, promise));
                return promise;
            }
            
            // Finally, if not found, fetch the content from the CDN
            PlatformLogger.Log(
                $"ContentCache: Fetching content from CDN for {requestedInfo.contentId}: version: {requestedInfo.version}");
            var fetchedContent = FetchContentFromCDN(requestedInfo)
                .Map(raw =>
                {
                    // Write the content to disk
                    SaveToDisk(requestedInfo, raw, _filesystemAccessor);
                    return DeserializeContent(requestedInfo, raw);
                })
                .Error(err =>
                {
                    _cache.Remove(cacheId);
                    PlatformLogger.Log(
                        $"ContentCache: Failed to resolve {requestedInfo.contentId} {requestedInfo.version} {requestedInfo.uri} ; ERR={err}");
                });
            SetCacheEntry(cacheId, new ContentCacheEntry<TContent>(requestedInfo.version, fetchedContent));
            return fetchedContent;
        }


        private static bool TryGetValueFromDisk(ClientContentInfo info, out TContent content,
            IBeamableFilesystemAccessor fsa)
        {
            var filePath = ContentPath(info, fsa);

            // Ensure the directory is created
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            try
            {
                var raw = File.ReadAllText(filePath);
                var deserialized = DeserializeContent(info, raw);
                if (deserialized.Version == info.version)
                {
                    content = deserialized;
                    return true;
                }

                content = null;
                return false;
            }
            catch (Exception e)
            {
                PlatformLogger.Log($"ContentCache: Error fetching content from disk: {e}");
                content = null;
                return false;
            }
        }

        private bool TryGetValueFromBaked(ClientContentInfo info, out TContent contentObject)
        {
            contentObject = null;
            
            // extract content archive if available
            string contentPath = Path.Combine(Application.streamingAssetsPath, "bakedContent.zip");
            string extractPath = Application.streamingAssetsPath + "/Baked/Content";
            if (File.Exists(contentPath) && !Directory.Exists(extractPath))
            {
                ZipFile.ExtractToDirectory(contentPath, extractPath);
#if !UNITY_EDITOR
                File.Delete(contentPath);
#endif
            }
            
            string resourcePath = Path.Combine(extractPath, info.contentId);
            if (File.Exists(resourcePath))
            {
                var json = File.ReadAllText(resourcePath);
                contentObject = _serializer.Deserialize<TContent>(json);
                if (contentObject == null || contentObject.Version != info.version)
                {
                    return false;
                }

                return true;
            }
            
            return false;
        }

        private static void SaveToDisk(ClientContentInfo info, string raw, IBeamableFilesystemAccessor fsa)
        {
            try
            {
                var filePath = ContentPath(info, fsa);
                // Ensure the directory is created
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, raw);
            }
            catch (Exception e)
            {
                PlatformLogger.Log($"ContentCache: Error saving content to disk: {e}");
            }
        }

        private static string ContentPath(ClientContentInfo info, IBeamableFilesystemAccessor fsa)
        {
            return fsa.GetPersistentDataPathWithoutTrailingSlash() + $"/content/{info.contentId}.json";
        }

        private static TContent DeserializeContent(ClientContentInfo info, string raw)
        {
            var content = _serializer.Deserialize<TContent>(raw);
            content.SetManifestID(info.manifestID);
            return content;
        }


        private Promise<string> FetchContentFromCDN(ClientContentInfo info)
        {
            return _requester.ManualRequest(Method.GET, info.uri, parser: s => s);
        }
    }
}
