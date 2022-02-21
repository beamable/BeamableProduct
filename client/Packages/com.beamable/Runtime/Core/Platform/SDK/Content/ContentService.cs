using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Content;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Coroutines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Content
{
	/// <summary>
	/// This type defines part of the %Beamable %ContentObject system.
	///
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	///
	/// #### Related Links
	/// - See Beamable.Common.Content.ContentObject script reference
	///
	/// ![img beamable-logo]
	///
	/// </summary>
	public class ManifestSubscription : PlatformSubscribable<ClientManifest, ClientManifest>
	{
		public string ManifestID { get; } = "global";

		private ClientManifest _latestManifiest;

		private readonly Promise<Unit> _manifestPromise = new Promise<Unit>();

		private readonly Dictionary<string, ClientContentInfo> _contentIdTable =
			new Dictionary<string, ClientContentInfo>();

		public Dictionary<Type, ContentCache> _contentCaches = new Dictionary<Type, ContentCache>();

		public ManifestSubscription(IDependencyProvider provider,
			string manifestID) : base(provider, "content")
		{
			ManifestID = manifestID;
		}

		public bool TryGetContentId(string contentId, out ClientContentInfo clientInfo)
		{
			return _contentIdTable.TryGetValue(contentId, out clientInfo);
		}

		public Promise<ClientManifest> GetManifest()
		{
			return _manifestPromise.Map(x => _latestManifiest);
		}

		public Promise<ClientManifest> GetManifest(ContentQuery query)
		{
			return _manifestPromise.Map(x => _latestManifiest.Filter(query));
		}

		public Promise<ClientManifest> GetManifest(string filter)
		{
			return _manifestPromise.Map(x => _latestManifiest.Filter(filter));
		}

		protected override string CreateRefreshUrl(string scope)
		{
			return $"/basic/content/manifest/public?id={ManifestID}";
		}

		protected override Promise<ClientManifest> ExecuteRequest(IBeamableRequester requester, string url)
		{
			return requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
			{
				// TODO: Put "global" as a constant value somewhere. Currently it lives in a different asm, and its too much trouble.
				if (ex is PlatformRequesterException err && err.Status == 404 && ManifestID.Equals("global"))
				{
					return new ClientManifest
					{
						entries = new List<ClientContentInfo>()
					};
				}

				throw ex;
			});
		}

		protected override void OnRefresh(ClientManifest data)
		{
			_latestManifiest = data;
			// TODO work on better refresh strategy. A total wipe isn't very performant.
			_contentIdTable.Clear();
			foreach (var entry in _latestManifiest.entries) _contentIdTable.Add(entry.contentId, entry);

			Notify(data);

			_manifestPromise.CompleteSuccess(new Unit());
		}
	}

	/// <summary>
	/// This type defines the %Client main entry point for the %Content feature.
	/// [img beamable-logo]: https://landen.imgix.net/7udgo2lvquge/assets/xgh89bz1.png?w=400 "Beamable Logo"
	/// #### Related Links
	///     - See the <a target="_blank" href="https://docs.beamable.com/docs/content-feature">Content</a> feature
	///     documentation
	/// - See Beamable.API script reference
	/// ![img beamable-logo]
	/// </summary>
	public class ContentService : IContentApi,
		IHasPlatformSubscriber<ManifestSubscription, ClientManifest, ClientManifest>,
		IHasPlatformSubscribers<ManifestSubscription, ClientManifest, ClientManifest>
	{
		private readonly IDependencyProvider _provider;
		public string CurrentDefaultManifestID { get; private set; } = "global";
		public IBeamableFilesystemAccessor FilesystemAccessor { get; }
		public ManifestSubscription Subscribable { get; private set; }
		public Dictionary<string, ManifestSubscription> Subscribables { get; }
		public IBeamableRequester Requester => _provider.GetService<IBeamableRequester>();

		public Action<string> OnManifestChanged;

		private IPlatformService Platform => _provider.GetService<IPlatformService>();
		private readonly Dictionary<Type, ContentCache> _contentCaches = new Dictionary<Type, ContentCache>();
		private static bool _testScopeEnabled;

		public ContentDataInfoWrapper ContentDataInfo;
		
		private ClientManifest bakedManifest;

#if UNITY_EDITOR

		public class ContentServiceTestScope : IDisposable
		{
			internal static readonly ContentServiceTestScope Instance = new ContentServiceTestScope();

			private ContentServiceTestScope()
			{
			}

			public void Dispose()
			{
				_testScopeEnabled = false;
			}
		}
#endif

		public ContentService(IDependencyProvider provider,
		                      IBeamableFilesystemAccessor filesystemAccessor, ContentParameterProvider config)
		{
			_provider = provider;
			CurrentDefaultManifestID = config.manifestID;
			FilesystemAccessor = filesystemAccessor;

			Subscribable = new ManifestSubscription(_provider, CurrentDefaultManifestID);
			Subscribable.Subscribe(cb =>
			{
				// pay attention, server...
			});

			// if (!InitializeBakedFile())
			{
				Subscribables = new Dictionary<string, ManifestSubscription>();
				AddSubscriber(CurrentDefaultManifestID);	
			}
		}
		
		private bool InitializeBakedFile()
		{
			string path = FilesystemAccessor.GetPersistentDataPathWithoutTrailingSlash() + "/content/content.json";

			if (File.Exists(path))
			{
				var json = File.ReadAllText(path);
				ContentDataInfo = JsonUtility.FromJson<ContentDataInfoWrapper>(json);
				
				// save baked data to disk
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					File.WriteAllText(path, json);
				}
				catch (Exception e)
				{
					Debug.LogError($"[EXTRACT] Failed to write baked data to disk: {e.Message}");
					return false;
				}
			}
			else
			{
				var bakedFile = Resources.Load<TextAsset>(ContentConstants.BakedFileResourcePath);

				if (bakedFile == null)
				{
					return false;
				}

				string json = bakedFile.text;
				try
				{
					ContentDataInfo = JsonUtility.FromJson<ContentDataInfoWrapper>(json);
				}
				catch
				{
					json = Gzip.Decompress(bakedFile.bytes);
					ContentDataInfo = JsonUtility.FromJson<ContentDataInfoWrapper>(json);
				}
			}
			
			if (ContentDataInfo == null)
			{
				return false;
			}
			
			var manifestData = ContentDataInfo.contentManifestData;
			if (!string.IsNullOrEmpty(manifestData))
			{
				bakedManifest = JsonUtility.FromJson<ClientManifest>(manifestData);
			}
		    
			return bakedManifest != null;
		}

#if UNITY_EDITOR
		public static ContentServiceTestScope AllowInTests()
		{
			_testScopeEnabled = true;
			return ContentServiceTestScope.Instance;
		}
#endif

		private void AddSubscriber(string manifestID)
		{
			if (Subscribables.ContainsKey(manifestID))
				return;

			Subscribables.Add(manifestID, new ManifestSubscription(_provider, manifestID));
			Subscribables[manifestID].Subscribe(cb => { });
		}

		private void RemoveSubscriber(string manifestID)
		{
			if (!Subscribables.ContainsKey(manifestID))
				return;

			Subscribables[manifestID].Subscribe(_ => { }).Unsubscribe();
			Subscribables.Remove(manifestID);
		}


		public Promise<IContentObject> GetContent(string contentId, string manifestID = "")
		{
			var referencedType = ContentRegistry.GetTypeFromId(contentId);
			return GetContent(contentId, referencedType, DetermineManifestID(manifestID));
		}

		public Promise<IContentObject> GetContent(string contentId, Type contentType, string manifestID = "")
		{
			if (string.IsNullOrEmpty(contentId))
				return Promise<IContentObject>.Failed(new ContentNotFoundException(contentId));
#if UNITY_EDITOR
			if (!UnityEditor.EditorApplication.isPlaying && !_testScopeEnabled)
			{
				BeamableLogger.LogError("Cannot resolve content at edit time!");
				throw new Exception("Cannot resolve content at edit time!");
			}
#endif
			ContentCache rawCache;

			if (!_contentCaches.TryGetValue(contentType, out rawCache))
			{
				var cacheType = typeof(ContentCache<>).MakeGenericType(contentType);
				var constructor = cacheType.GetConstructor(new[]
					{typeof(IHttpRequester), typeof(IBeamableFilesystemAccessor), typeof(ContentService), typeof(CoroutineService)});
				rawCache = (ContentCache)constructor.Invoke(new[] { Requester, (object)FilesystemAccessor, this, Platform.CoroutineService });

				_contentCaches.Add(contentType, rawCache);
			}

			var determinedManifestID = DetermineManifestID(manifestID);
			return GetManifestWithID(determinedManifestID).FlatMap(manifest =>
			{
				var subscribable = GetSubscription(determinedManifestID);

				if (subscribable == null)
					AddSubscriber(determinedManifestID);

				if (!subscribable.TryGetContentId(contentId, out var info))
					return Promise<IContentObject>.Failed(new ContentNotFoundException(contentId));

				info.manifestID = determinedManifestID;
				return rawCache.GetContentObject(info);
			});
		}


		public Promise<IContentObject> GetContent(IContentRef reference, string manifestID = "")
		{
			var referencedType = ContentRegistry.GetTypeFromId(reference.GetId());
			return GetContent(reference.GetId(), referencedType, DetermineManifestID(manifestID));
		}

		public Promise<TContent> GetContent<TContent>(IContentRef reference, string manifestID = "")
			where TContent : ContentObject, new()
		{
			if (reference == null || string.IsNullOrEmpty(reference.GetId()))
				return Promise<TContent>.Failed(new ContentNotFoundException());
			var referencedType = reference.GetReferencedType();
			return GetContent(reference.GetId(), referencedType, DetermineManifestID(manifestID)).Map(c => (TContent)c);
		}

		public Promise<TContent> GetContent<TContent>(IContentRef<TContent> reference, string manifestID = "")
			where TContent : ContentObject, new()
		{
			return GetContent<TContent>(reference as IContentRef, DetermineManifestID(manifestID));
		}

		public Promise<ClientManifest> GetManifest(ContentQuery query) => GetSubscription(CurrentDefaultManifestID)?.GetManifest(query);

		public Promise<ClientManifest> GetManifestWithID(string manifestID = "")
		{
			return GetSubscription(DetermineManifestID(manifestID))?.GetManifest();
		}

		public Promise<ClientManifest> GetManifest(string filter = "", string manifestID = "")
		{
			return GetSubscription(DetermineManifestID(manifestID))?.GetManifest(filter);
		}

		public Promise<ClientManifest> GetManifest(ContentQuery query, string manifestID = "")
		{
			return GetSubscription(DetermineManifestID(manifestID))?.GetManifest(query);
		}


		public Promise<ClientManifest> GetCurrent(string scope = "")
		{
			return Subscribables[CurrentDefaultManifestID].GetCurrent(scope);
		}

		public void SwitchDefaultManifestID(string manifestID)
		{
			if (string.IsNullOrEmpty(manifestID))
			{
				Debug.LogError($"Manifest can't be null or empty.");
				return;
			}

			if (manifestID == CurrentDefaultManifestID)
			{
				Debug.LogWarning($"Manifest id: \"{manifestID}\" is already set as default.");
				return;
			}

			var subscription = GetSubscription(manifestID);

			if (subscription == null)
			{
				Debug.LogError(
					$"Can't reach manifest for given \"{manifestID}\" id. Make sure that manifest with specific id is on the server.");
				return;
			}

			CurrentDefaultManifestID = manifestID;
			Subscribable = subscription;

			OnManifestChanged?.Invoke(CurrentDefaultManifestID);
		}


		private ManifestSubscription GetSubscription(string manifestID)
		{
			if (!Subscribables.ContainsKey(manifestID))
				AddSubscriber(manifestID);
			return Subscribables.FirstOrDefault(x => x.Key == manifestID).Value;
		}

		private string DetermineManifestID(string manifestID)
		{
			return string.IsNullOrEmpty(manifestID) ? CurrentDefaultManifestID : manifestID;
		}
	}
}
