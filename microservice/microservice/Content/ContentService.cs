using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Reflection;
using Beamable.Server.Api.Content;
using microservice.Common;
using Serilog;
using System.Diagnostics;
using static Beamable.Common.Constants.Features.Content;

namespace Beamable.Server.Content
{
   public class ContentManifest
   {
      public string id;
      public long created;
      public List<ContentReference> references;
   }


   public class ContentReference
   {

      public string id;
      public string version;
      public string uri;
      public string checksum;
      public string type;
      public string visibility;
      public string[] tags;

      public bool IsPublic => string.Equals(PUBLIC, visibility);
      public bool IsPrivate => string.Equals(PRIVATE, visibility);
   }

   public struct ContentKey
   {
      public string Id, Version;

      public override bool Equals(object obj)
      {
         if (obj is ContentKey other)
         {
            return string.Equals(other.Id, Id) && string.Equals(other.Version, Version);
         }
         else
         {
            return false;
         }
      }

      public override int GetHashCode()
      {
         return 17 + 31 * Id.GetHashCode() + 31 * Version.GetHashCode();
      }
   }

   public class ContentCacheKey
   {
      public readonly string Uri;
      public readonly string Id;
      public ContentCacheKey(string id, string uri)
      {
         Id = id;
         Uri = uri;
      }

      private bool Equals(ContentCacheKey other)
      {
         return Uri == other.Uri && Equals(Id, other.Id);
      }

      public override bool Equals(object obj)
      {
         if (ReferenceEquals(null, obj)) return false;
         if (ReferenceEquals(this, obj)) return true;
         if (obj.GetType() != this.GetType()) return false;
         return Equals((ContentCacheKey) obj);
      }

      public override int GetHashCode()
      {
         return HashCode.Combine(Uri, Id);
      }
   }

   public class UnreliableContentService : ContentService
   {
	   public UnreliableContentService(MicroserviceRequester requester, SocketRequesterContext socket, IContentResolver contentResolver, ReflectionCache reflectionCache) : base(requester, socket, contentResolver, reflectionCache)
	   {
	   }

	   public override Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestID = "")
	   {
		   BeamableLogger.LogWarning("The content=[{contentId}] may be unreliable. When the Microservice's {attributeName} " +
		                             "is enabled, the Microservice will never receive content updates. " +
		                             "This means that you may resolve content, but after the first time you do so, " +
		                             "the content will never ever change until the Microservice instance stops and restarts. " +
		                             "It is highly recommend that you do not use the Content service at all when the configuration is set.",
			   contentId, nameof(MicroserviceAttribute.DisableAllBeamableEvents));
		   return base.GetContent(contentId, contentType, manifestID);
	   }
   }

   public class ContentService : IMicroserviceContentApi
   {
      private readonly MicroserviceRequester _requester;
      private readonly SocketRequesterContext _socket;
      private readonly IContentResolver _contentResolver;

      private readonly ConcurrentDictionary<string, ContentReference> _idToContentReference = new ConcurrentDictionary<string, ContentReference>();

      private Promise<ClientManifest> _waitForManifest; // TODO: Turn this into a null-checking property accessor
      private readonly MicroserviceContentSerializer _serializer = new MicroserviceContentSerializer();

      private readonly Cache<ContentCacheKey, IContentObject> _contentCache;
      private readonly object _manifestLock = new object();
      private readonly ContentTypeReflectionCache _contentTypeReflectionCache;
      private bool _hasStartedInit;

      public ContentService(MicroserviceRequester requester, SocketRequesterContext socket, IContentResolver contentResolver, ReflectionCache reflectionCache)
      {
         _contentCache = new Cache<ContentCacheKey, IContentObject>(CacheResolver);
         _requester = requester;
         _socket = socket;
         _contentResolver = contentResolver;
         _contentTypeReflectionCache = reflectionCache.GetFirstSystemOfType<ContentTypeReflectionCache>();
      }

      private async Task<IContentObject> CacheResolver(ContentCacheKey key)
      {
         var json = await _contentResolver.RequestContent(key.Uri);
         var referencedType = _contentTypeReflectionCache.GetTypeFromId(key.Id);
         var content = _serializer.DeserializeByType(json, referencedType);
         return content;
      }

      private Promise<ClientManifest> WaitForManifest()
      {
         var hasNoManifest = true;
         lock (_manifestLock)
         {
            hasNoManifest = _waitForManifest == null;
            if (hasNoManifest)
            {
               RefreshManifest();
            }
         }

         return _waitForManifest;
      }

      public async Promise Init(bool preload = false)
      {
		  if (_hasStartedInit) return;
	      _hasStartedInit = true;
	      _socket.Subscribe<ContentManifestEvent>(Constants.Features.Services.CONTENT_UPDATE_EVENT, HandleContentPublish);

	      if (!preload) return;
	      await DownloadAllContent();
      }

      private async Promise DownloadAllContent()
      {
	      // cache entire content
	      Log.Verbose($"LOADING MANIFEST");
	      var sw = new Stopwatch();
	      sw.Start();
	      var manifest = await GetManifest();
	      Log.Verbose($"MANIFEST CONTAINS {manifest.entries.Count} OBJECTS time=[{sw.ElapsedMilliseconds}]");

	      var processed = 0f;
	      var granularityForLogs = 100;
	      var downloads = manifest.entries.Select(e => Task.Run(async () =>
	      {
		      try
		      {
			      await e.Resolve();
			      processed++;
			      if (processed % granularityForLogs == 0)
			      {
				      Log.Verbose(
					      $"Content Load - [{(processed / manifest.entries.Count) * 100:00}%] processed=[{processed}] total-time=[{sw.ElapsedMilliseconds}]");
			      }
		      }
		      catch (Exception ex)
		      {
			      Log.Error($"Failed to load preload content. id=[{e.contentId}] message=[{ex.Message}] type=[{ex.GetType().Name}] stack=[{ex.StackTrace}]");
		      }
	      })).ToList();
	      await Task.WhenAll(downloads);
	      Log.Debug($"Downloaded Content. items=[{downloads.Count}] time=[{sw.ElapsedMilliseconds}]");
	      sw.Stop();
      }

      void HandleContentPublish(ContentManifestEvent manifestEvent)
      {
         // the event data isn't super useful on its own, so just use it to know that our cache is invalid.
         RefreshManifest(); // TODO: Maybe this should get debounced? If a flood of events started pouring in for whatever reason, this could kill us.
      }

      void RefreshManifest()
      {
         lock (_manifestLock)
         {
            var oldPromise = _waitForManifest;
            // reset the manifest promise, so that if a content request comes while a manifest is being retrieved, we wait for the updated content to be served
            _waitForManifest = _requester.Request<ContentManifest>(Method.GET, "/basic/content/manifest")
               .Map(manifest =>
               {
                  _idToContentReference.Clear();

                  foreach (var reference in manifest.references)
                  {
                     // TODO how do we separate the private from the public?
                     if (!reference.IsPublic) continue;

                     if (!_idToContentReference.ContainsKey(reference.id))
                     {
                        if (!_idToContentReference.TryAdd(reference.id, reference))
                        {
                           BeamableLogger.LogWarning("Couldn't add content id=[{id}] because it already existed.", reference.id);
                        }
                     }
                  }

                  var keysToKeep = _idToContentReference
                     .Select(kvp => new ContentCacheKey(kvp.Key, kvp.Value.uri))
                     .ToHashSet();
                  _contentCache.PurgeAllExcept(keysToKeep);

                  return new ClientManifest
                  {
                     entries = _idToContentReference.Values.Select(r => new ClientContentInfo
                     {
                        contentId = r.id,
                        visibility = ContentVisibilityExtensions.FromString(r.visibility),
                        tags = r.tags,
                        version = r.version,
                        uri = r.uri,
                        type = r.type
                     }).ToList()
                  };
               });

            if (oldPromise != null && !oldPromise.IsCompleted)
            {
               _waitForManifest.Then(manifest => oldPromise.CompleteSuccess(manifest));
            }
         }
      }

      public Promise<TContent> Resolve<TContent>(IContentRef<TContent> reference) where TContent : IContentObject, new()
      {
         return GetContent(reference?.GetId(), typeof(TContent)).Map(content => (TContent) content);


      }

      TContent ParseContent<TContent>(string json) where TContent : IContentObject, new()
      {
         return _serializer.Deserialize<TContent>(json);
      }



      public Promise<ClientManifest> GetCurrent(string scope = "")
      {
         throw new NotImplementedException(); // TODO ?
      }

      public Promise<TContent> GetContent<TContent>(IContentRef<TContent> reference, string manifestID = "") where TContent : ContentObject, new()
      {
         return Resolve(reference);
      }

      public virtual Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestID = "")
      {
         return WaitForManifest().FlatMap(_ =>
         {
            if (!_idToContentReference.TryGetValue(contentId, out var content))
            {
               throw new ContentNotFoundException(contentId);
            }

            return _contentCache.Get(new ContentCacheKey(content.id, content.uri)).ToPromise();
         });
      }

      public Promise<IContentObject> GetContent(string contentId, string manifestID = "")
      {
         var referencedType = _contentTypeReflectionCache.GetTypeFromId(contentId);
         return GetContent(contentId, referencedType);
      }

      public Promise<IContentObject> GetContent(IContentRef reference, string manifestID = "")
      {
         var referencedType = _contentTypeReflectionCache.GetTypeFromId(reference.GetId());
         return GetContent(reference.GetId(), referencedType);
      }

      public Promise<TContent> GetContent<TContent>(IContentRef reference, string manifestID = "")
         where TContent : ContentObject, new()
      {
         if (reference == null || string.IsNullOrEmpty(reference.GetId()))
         {
            return Promise<TContent>.Failed(new ContentNotFoundException());
         }
         var referencedType = reference.GetReferencedType();
         return GetContent(reference.GetId(), referencedType).Map( c => (TContent)c);
      }


      public Promise<ClientManifest> GetManifestWithID(string manifestID = "")
      {
         return WaitForManifest();
      }

      public Promise<ClientManifest> GetManifest(string filter = "", string manifestID = "")
      {
         return WaitForManifest().Map(x => x.Filter(filter));
      }

      public Promise<ClientManifest> GetManifest(ContentQuery query, string manifestID = "")
      {
         return WaitForManifest().Map(x => x.Filter(query));
      }
   }
}
