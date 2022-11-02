using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Common.Content.Serialization;
using Beamable.Common.Content.Validation;
using Beamable.Common.Runtime;
using Beamable.Content;
using Beamable.Editor.Content.UI;
using Beamable.Serialization;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Content;
using static Beamable.Common.Constants.Features.ContentManager;

namespace Beamable.Editor.Content
{
	public class ContentIOAssetProcessor : UnityEditor.AssetModificationProcessor
	{
		private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
		{
			if (AssetDatabase.IsValidFolder(assetPath))
			{
				var subAssets = AssetDatabase.FindAssets("", new string[] { assetPath });
				foreach (var sub in subAssets)
				{
					var subPath = AssetDatabase.GUIDToAssetPath(sub);
					OnWillDeleteAsset(subPath, options);
				}

				return AssetDeleteResult.DidNotDelete;
			}

			var asset = AssetDatabase.LoadAssetAtPath<ContentObject>(assetPath);
			if (asset == null) return AssetDeleteResult.DidNotDelete;

			var method = typeof(ContentIO).GetMethod(nameof(ContentIO.NotifyDeleted),
				BindingFlags.NonPublic | BindingFlags.Static);
			var genMethod = method.MakeGenericMethod(asset.GetType());
			genMethod.Invoke(null, new object[] { asset });

			return AssetDeleteResult.DidNotDelete;
		}


		private static string[] OnWillSaveAssets(string[] paths)
		{
			foreach (var path in paths)
			{
				var asset = AssetDatabase.LoadAssetAtPath<ContentObject>(path);
				if (asset == null) continue;

				var method = typeof(ContentIO).GetMethod(nameof(ContentIO.NotifyCreated),
					BindingFlags.NonPublic | BindingFlags.Static);
				var genMethod = method.MakeGenericMethod(asset.GetType());

				var rootPath = $"{Directories.DATA_DIR}/{asset.ContentType}/";
				var subPath = path.Substring(rootPath.Length);
				var qualifiedName = Path.GetFileNameWithoutExtension(subPath.Replace(Path.DirectorySeparatorChar, '.'));

				asset.SetContentName(qualifiedName);

				genMethod.Invoke(null, new object[] { asset });
			}

			return paths;
		}
	}

	public interface IContentIO
	{
		Promise<Manifest> FetchManifest();
		Promise<Manifest> FetchManifest(string id);
		IEnumerable<ContentObject> FindAll(ContentQuery query = null);
		string Checksum(IContentObject content);
	}

	[System.Serializable]
	public class AvailableManifests : JsonSerializable.ISerializable
	{
		public List<AvailableManifestModel> manifests;

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.SerializeList(nameof(manifests), ref manifests);
		}
	}

	[System.Serializable]
	public class AvailableManifestModel : JsonSerializable.ISerializable, ISearchableElement
	{
		public string id;
		public string checksum;
		public long createdAt;
		public bool archived;

		public string DisplayName
		{
			get => id;
		}

		public static AvailableManifestModel CreateId(string id)
		{
			return new AvailableManifestModel()
			{
				id = id,
				checksum = "",
				createdAt = 0
			};
		}

		public void Serialize(JsonSerializable.IStreamSerializer s)
		{
			s.Serialize(nameof(id), ref id);
			s.Serialize(nameof(checksum), ref checksum);
			s.Serialize(nameof(createdAt), ref createdAt);
		}


		public int GetOrder()
		{
			return 0;
		}

		public bool IsAvailable()
		{
			return true;
		}

		public bool IsToSkip(string filter)
		{
			return !string.IsNullOrEmpty(filter) && !id.ToLower().Contains(filter);
		}

		public string GetClassNameToAdd()
		{
			return string.Empty;
		}
	}

	public static class ManifestModelExtensions
	{
		public static bool AreManifestIdsEquals(this IEnumerable<AvailableManifestModel> self,
			IEnumerable<AvailableManifestModel> other)
		{
			var selfIds = self == null
				? new HashSet<string>()
				: new HashSet<string>(self.Select(x => x?.id?.ToLower()));
			var otherIds = other == null
				? new HashSet<string>()
				: new HashSet<string>(other.Select(x => x?.id?.ToLower()));

			return selfIds.SetEquals(otherIds);
		}
	}

	/// <summary>
	/// The purpose of this class is to
	/// 1. scrape local editor directory for content assets
	/// 2. handle the upload of the assets to Platform
	/// 3. create new editor-side-not-yet-deployed content
	/// </summary>
	public class ContentIO : IContentIO
	{
		public ContentManager ContentManager => _contentManager ?? (_contentManager = new ContentManager());
		private ContentManager _contentManager;
		private ContentTypeReflectionCache _contentTypeReflectionCache;

		private readonly IBeamableRequester _requester;
		private Promise<Manifest> _manifestPromise;
		private ContentObject _lastSelected;

		public Promise<Manifest> OnManifest => _manifestPromise ?? FetchManifest();

		public event ContentDelegate OnSelectionChanged;

		public static event IContentDelegate OnContentCreated, OnContentDeleted;
		public static event IContentRenamedDelegate OnContentRenamed;
		public static Action<string> OnManifestChanged;
		public static Action<AvailableManifests> OnManifestsListFetched;
		public static Action<IEnumerable<AvailableManifestModel>> OnArchivedManifestsFetched;

		private ValidationContext ValidationContext { get; } = new ValidationContext
		{ AllContent = new Dictionary<string, IContentObject>() };

		public ContentIO(IBeamableRequester requester)
		{
			_requester = requester;
			Selection.selectionChanged += SelectionChanged;

			OnContentCreated += Internal_HandleContentCreated;
			OnContentDeleted += Internal_HandleContentDeleted;
			_contentTypeReflectionCache = BeamEditor.GetReflectionSystem<ContentTypeReflectionCache>();
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			OnContentCreated = null;
			OnContentDeleted = null;
			OnContentRenamed = null;
		}

		private void Internal_HandleContentCreated(IContentObject content)
		{
			ValidationContext.AllContent[content.Id] = content;
		}

		private void Internal_HandleContentDeleted(IContentObject content)
		{
			ValidationContext.AllContent.Remove(content.Id);
		}


		private void SelectionChanged()
		{
			var activeContent = Selection.activeObject as ContentObject;

			if (_lastSelected != null && _lastSelected != activeContent)
			{
				// selection has been lost! Save and broadcast an update!
				_lastSelected.BroadcastUpdate();
			}

			if (_lastSelected != activeContent && activeContent != null)
			{
				OnSelectionChanged?.Invoke(activeContent);
			}

			_lastSelected = activeContent;
		}

		public IValidationContext GetValidationContext()
		{
			return ValidationContext;
		}

		public Promise<Manifest> FetchManifest()
		{
			return FetchManifest(ContentConfiguration.Instance.EditorManifestID);
		}

		public Promise<Manifest> FetchManifest(string manifestID)
		{
			if (string.IsNullOrWhiteSpace(manifestID))
			{
				manifestID = DEFAULT_MANIFEST_ID;
			}

			if (string.IsNullOrEmpty(_requester?.AccessToken?.Token))
			{
				return Promise<Manifest>.Failed(new Exception("Not logged into Beamable Editor"));
			}

			var manifestUrl = $"/basic/content/manifest?id={manifestID}";

			_manifestPromise = new Promise<Manifest>();
			var webRequest = _requester.Request<ContentManifest>(Method.GET, manifestUrl, useCache: true);
			webRequest.Error(error =>
			{
				// Nullref check added for offline stability - srivello
				if (error is PlatformRequesterException err && err?.Error?.status == 404)
				{
					// create a blank in-memory copy of the manifest for usage now. This is the same as assuming no manifest.
					_manifestPromise.CompleteSuccess(new Manifest(new List<ContentManifestReference>()));
				}
				else
				{
					_manifestPromise.CompleteError(error);
				}
			}).Then(source => { _manifestPromise.CompleteSuccess(new Manifest(source)); });

			return _manifestPromise;
		}

		public Promise<ContentStatus> GetStatus(ContentObject content)
		{
			return OnManifest.Map(manifest =>
			{
				var data = manifest.Get(content.Id);
				if (data == null)
				{
					return ContentStatus.NEW;
				}

				var checksumsMatch = data.checksum.Equals(Checksum(content));
				var distinctTagsExist = AreTagsEqual(data.tags, content.Tags);
				if (checksumsMatch && distinctTagsExist)
				{
					return ContentStatus.CURRENT;
				}

				return ContentStatus.MODIFIED;
			});
		}


		public Promise<AvailableManifests> GetAllManifestIDs()
		{
			if (string.IsNullOrEmpty(_requester?.AccessToken?.Token))
			{
				return Promise<AvailableManifests>.Failed(new Exception("Not logged into Beamable Editor"));
			}

			var manifestUrl = "/basic/content/manifest/checksums";

			var manifestIdsPromise = new Promise<AvailableManifests>();
			var webRequest = _requester.Request<AvailableManifests>(Method.GET, manifestUrl, useCache: true);
			webRequest.Error(error =>
			{
				if (error is PlatformRequesterException err && err?.Error?.status == 404)
				{
					manifestIdsPromise.CompleteSuccess(new AvailableManifests());
				}
				else
				{
					manifestIdsPromise.CompleteError(error);
				}
			}).Then(source =>
			{
				var archivedManifests = source.manifests.Where(m => m.archived).ToList();
				source.manifests.RemoveAll(m => m.archived);
				if (source.manifests.Count == 0 ||
					source.manifests.All(m => m.id != DEFAULT_MANIFEST_ID))
				{
					source.manifests.Insert(0, AvailableManifestModel.CreateId(DEFAULT_MANIFEST_ID));
				}

				OnManifestsListFetched?.Invoke(source);
				OnArchivedManifestsFetched?.Invoke(archivedManifests);
				manifestIdsPromise.CompleteSuccess(source);
			});

			return manifestIdsPromise;
		}

		public Promise<Unit> SwitchManifest(string manifestID)
		{
			if (!ContentConfiguration.IsValidManifestID(manifestID, out var msg))
			{
				return Promise<Unit>.Failed(new Exception("Invalid manifest name:\n" + msg));
			}

			if (ContentConfiguration.Instance.EditorManifestID == manifestID)
			{
				return Promise<Unit>.Failed(new Exception("You have already this manifest"));
			}

			if (!ContentConfiguration.Instance.EnableMultipleContentNamespaces &&
				manifestID != DEFAULT_MANIFEST_ID)
			{
				Debug.LogWarning("You are switching manifest while manifest namespaces feature is disabled!");
			}

			ContentConfiguration.Instance.EditorManifestID = manifestID;

			return FetchManifest(manifestID).Then(manifest =>
			{
				ContentManager.Model.SetServerContent(manifest);
				OnManifestChanged?.Invoke(manifestID);
			}).ToUnit();
		}

		public Promise<Unit> ArchiveManifests(params string[] ids)
		{
			if (string.IsNullOrEmpty(_requester?.AccessToken?.Token))
			{
				return Promise<Unit>.Failed(new Exception("Not logged into Beamable Editor"));
			}

			var arg = new ManifestsToArchiveArg(ids);
			var manifestDeleteUrl = "/basic/content/manifests/archive";

			var manifestIdsPromise = new Promise<Unit>();
			var webRequest = _requester.Request<Unit>(Method.POST, manifestDeleteUrl, arg);
			webRequest.Error(error => { manifestIdsPromise.CompleteError(error); }).Then(source =>
			{
				GetAllManifestIDs();
				manifestIdsPromise.CompleteSuccess(source);
			});

			return manifestIdsPromise;
		}

		public Promise<Unit> UnarchiveManifest(params string[] ids)
		{
			if (string.IsNullOrEmpty(_requester?.AccessToken?.Token))
			{
				return Promise<Unit>.Failed(new Exception("Not logged into Beamable Editor"));
			}

			var url = "/basic/content/manifests/unarchive";

			var arg = new ManifestsToArchiveArg(ids);
			var manifestIdsPromise = new Promise<Unit>();
			var webRequest = _requester.Request<Unit>(Method.POST, url, arg);
			webRequest.Error(error => { manifestIdsPromise.CompleteError(error); }).Then(source =>
			{
				manifestIdsPromise.CompleteSuccess(source);
			});

			return manifestIdsPromise;
		}

		[Serializable]
		private class ManifestsToArchiveArg
		{
			public string[] manifestIds;

			public ManifestsToArchiveArg(string[] ids)
			{
				manifestIds = ids;
			}
		}

		public void Select(ContentObject content)
		{
			var actual = Find(content);
			Selection.SetActiveObjectWithContext(actual, actual);
		}

		private ContentObject Find(ContentObject content)
		{
			var assetGuids = BeamableAssetDatabase.FindAssets(
				content.GetType(),
				new[] { Directories.DATA_DIR }
			);
			foreach (var guid in assetGuids)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ContentObject asset = (ContentObject)AssetDatabase.LoadAssetAtPath(assetPath, content.GetType());
				if (asset.Id == content.Id)
					return asset;
			}

			throw new Exception("No matching content.");
		}

		public IEnumerable<TContent> FindAllContent<TContent>(ContentQuery query = null, bool inherit = true)
			where TContent : ContentObject, new()
		{
			if (query == null) query = ContentQuery.Unit;
			if (!AssetDatabase.IsValidFolder(Directories.DATA_DIR))
			{
				Directory.CreateDirectory(Directories.DATA_DIR);
				yield break; // there is no folder, therefore, no content. Nothing to search for.
			}

			var assetGuids = BeamableAssetDatabase.FindAssets<TContent>(
				new[] { Directories.DATA_DIR }
			);
			var contentType = _contentTypeReflectionCache.TypeToName(typeof(TContent));

			foreach (var guid in assetGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<TContent>(path);

				if (asset == null || !query.Accept(asset))
					continue;
				if (asset == null || (!inherit && asset.ContentType != contentType))
					continue;


				var name = Path.GetFileNameWithoutExtension(path);
				asset.SetContentName(name);
				yield return asset;
			}
		}

		public string[] FindAllDirectoriesForSubtypes<TContent>() where TContent : ContentObject, new()
		{
			return FindAllSubtypes<TContent>().Select(contentType =>
			{
				string contentTypeName = ContentObject.GetContentTypeName(contentType);
				var dir = $"{Directories.DATA_DIR}/{contentTypeName}";
				return dir;
			}).ToArray();
		}

		public Type[] FindAllSubtypes<TContent>() where TContent : ContentObject, new()
		{
			return GetContentTypes().Where(contentType => { return typeof(TContent).IsAssignableFrom(contentType); })
				.ToArray();
		}

		public IEnumerable<ContentObject> FindAll(ContentQuery query = null)
		{
			var contentArray = new List<ContentObject>();
			var contentIdHashSet = new HashSet<string>();
			foreach (var contentType in GetContentTypes())
			{
				foreach (var content in FindAllContentByType(contentType, query))
				{
					if (!contentIdHashSet.Contains(content.Id))
					{
						contentIdHashSet.Add(content.Id);
						contentArray.Add(content);
					}
				}
			}

			return contentArray;
		}

		public LocalContentManifest BuildLocalManifest()
		{
			var localManifest = new LocalContentManifest();
			ValidationContext.AllContent.Clear();

			if (!Directory.Exists(Directories.DATA_DIR))
			{
				// If the directory is not here, there is no local content and that is fine.
				return localManifest;
			}

			foreach (var contentType in _contentTypeReflectionCache.GetContentTypes()) // TODO check hierarchy types.
			{
				var assetGuids = BeamableAssetDatabase.FindAssets(
					contentType,
					new[] { Directories.DATA_DIR }
				);
				var typeName = _contentTypeReflectionCache.TypeToName(contentType);

				foreach (var assetGuid in assetGuids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
					var rawAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(IContentObject));
					var content = rawAsset as IContentObject;

					var fileName = Path.GetFileNameWithoutExtension(assetPath);

					if (content == null || rawAsset.GetType() != contentType) continue;
					var contentId = $"{typeName}.{fileName}";
					content.SetIdAndVersion(contentId, "");

					var entry = new LocalContentManifestEntry
					{
						ContentType = contentType,
						Content = content,
						AssetPath = assetPath
					};
					ValidationContext.AllContent[content.Id] = content;
					if (!localManifest.Content.ContainsKey(content.Id))
					{
						localManifest.Content.Add(content.Id, entry);
					}
				}
			}

			ContentObject.ValidationContext = ValidationContext;
			return localManifest;
		}

		public IEnumerable<Type> GetContentTypes()
		{
			return _contentTypeReflectionCache.GetContentTypes();
		}

		public IEnumerable<string> GetContentClassIds()
		{
			return _contentTypeReflectionCache.GetContentClassIds();
		}

		public ContentObject LoadContent(LocalContentManifestEntry manifestEntry)
		{
			return AssetDatabase.LoadAssetAtPath<ContentObject>(manifestEntry.AssetPath);
		}

		public ContentObject LoadContent(string assetPath)
		{
			return AssetDatabase.LoadAssetAtPath<ContentObject>(assetPath);
		}


		public IEnumerable<ContentObject> FindAllContentByType(Type type, ContentQuery query = null,
			bool inherit = true)
		{
			var methodName = nameof(FindAllContent);
			var method = typeof(ContentIO).GetMethod(methodName).MakeGenericMethod(type);
			var content = method.Invoke(this, new object[] { query, inherit }) as IEnumerable<ContentObject>;
			return content;
		}

		public void EnsureAllDefaultContent()
		{
			foreach (var contentType in GetContentTypes())
			{
				EnsureDefaultContentByType(contentType);
			}
		}

		public void EnsureDefaultContentByType(Type type)
		{
			var methodName = nameof(EnsureDefaultContent);
			var method = typeof(ContentIO).GetMethod(methodName).MakeGenericMethod(type);
			method.Invoke(this, null);
		}

		public void EnsureDefaultContent<TContent>() where TContent : ContentObject
		{
			string typeName = ContentObject.GetContentType<TContent>();
			var dir = $"{Directories.DATA_DIR}/{typeName}";
			EnsureDefaultAssets<TContent>();
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
				var defaultDir = $"{Directories.DEFAULT_DATA_DIR}/{typeName}";
				if (Directory.Exists(defaultDir))
				{
					string[] files = Directory.GetFiles(defaultDir);
					foreach (var src in files)
					{
						if (!Path.GetExtension(src).Equals(".asset"))
							continue;

						var filename = Path.GetFileName(src);
						var dest = Path.Combine(dir, filename);
						File.Copy(src, dest, true);
					}

					AssetDatabase.ImportAsset(dir, ImportAssetOptions.ImportRecursive);
				}
			}
		}

		public string GetAssetPathByType(Type contentType, IContentObject content)
		{
			foreach (var nextContentType in _contentTypeReflectionCache.GetContentTypes()) // TODO check heirarchy types.
			{
				var assetGuids = BeamableAssetDatabase.FindAssets(
					nextContentType,
					new[] { Directories.DATA_DIR }
				);

				foreach (var assetGuid in assetGuids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
					var rawAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(IContentObject));
					var nextContent = rawAsset as IContentObject;

					if (nextContent == null || rawAsset.GetType() != nextContentType) continue;

					if (nextContentType == contentType &&
						nextContent == content)
					{
						return assetPath;
					}
				}
			}

			return "";
		}

		public void EnsureDefaultAssetsByType(Type type)
		{
			var methodName = nameof(EnsureDefaultAssets);
			var method = typeof(ContentIO).GetMethod(methodName).MakeGenericMethod(type);
			method.Invoke(this, null);
		}

		public void EnsureDefaultAssets<TContent>() where TContent : ContentObject
		{
			string contentType = ContentObject.GetContentType<TContent>();
			var assetDir = $"{Directories.ASSET_DIR}/{contentType}";
			var defaultDir = $"{Directories.DEFAULT_ASSET_DIR}/{contentType}";
			if (Directory.Exists(assetDir) || !Directory.Exists(defaultDir))
			{
				return;
			}

			Directory.CreateDirectory(assetDir);
			string[] files = Directory.GetFiles(defaultDir);
			var addedEntries = new List<AddressableAssetEntry>();

			var addressableAssetSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
			if (addressableAssetSettings == null)
			{
				throw new Exception("Addressables was not configured");
			}

			AssetDatabase.Refresh();

			var filesToMarkAddressable = new List<string>();

			foreach (var src in files)
			{
				var filename = Path.GetFileName(src);
				var dest = Path.Combine(assetDir, filename);
				File.Copy(src, dest, true);

				if (src.EndsWith("meta"))
					// we don't need to mark a meta file as addressable... silly...
					continue;
				filesToMarkAddressable.Add(dest);
			}

			// mark all files as addressable after copy completes...
			CommitAssetDatabase();

			var addressablesGroup = addressableAssetSettings.FindGroup(BEAMABLE_ASSET_GROUP);
			if (addressablesGroup == null)
			{
				addressablesGroup = addressableAssetSettings.CreateGroup(
					BEAMABLE_ASSET_GROUP,
					setAsDefaultGroup: false,
					readOnly: false,
					postEvent: true,
					schemasToCopy: new List<AddressableAssetGroupSchema>(),
					types: new Type[] { typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema) }
				);
			}

			foreach (var file in filesToMarkAddressable)
			{
				var guid = AssetDatabase.AssetPathToGUID(file);
				var entry = addressableAssetSettings.CreateOrMoveEntry(guid, addressablesGroup);
				addedEntries.Add(entry);
			}

			addressableAssetSettings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, addedEntries,
				true);
			CommitAssetDatabase();
		}

		public string GetAvailableFileName(string assetPath, string id, LocalContentManifest localContentManifest)
		{
			//Exit early if the file doesn't exist and don't create a numbered copy.
			var assetPathExists = File.Exists(assetPath);

			if (!assetPathExists)
			{
				if (localContentManifest != null && !localContentManifest.Content.ContainsKey(id))
					return assetPath;
			}

			var testName = Path.GetFileNameWithoutExtension(assetPath);
			var result = string.Concat(testName.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse());
			var i = 0;
			int.TryParse(result, out i);
			while (true)
			{
				i++;
				var newName = testName.Substring(0, testName.Length - result.Length) + i;
				var newID = id.Substring(0, id.Length - result.Length) + i;
				var directoryName = Path.GetDirectoryName(assetPath);
				var extension = Path.GetExtension(assetPath);
				var newPath = Path.Combine(directoryName, newName) + extension;
				var exists = File.Exists(newPath);
				if (!exists && (localContentManifest != null && !localContentManifest.Content.ContainsKey(newID)))
				{
					return newPath;
				}
			}
		}

		public void Create<TContent>(TContent content, string assetPath = null, bool replace = true, LocalContentManifest localContentManifest = null)
			where TContent : ContentObject, new()
		{
			if (string.IsNullOrEmpty(assetPath))
			{
				var newNameAsPath = content.Id.Replace('.', Path.DirectorySeparatorChar);
				assetPath = $"{Directories.DATA_DIR}/{newNameAsPath}.asset";
			}
			//Check if we are replacing the local content, or if we should create a numbered copy
			var modifiedAssetPath = replace ? assetPath : GetAvailableFileName(assetPath, content.Id, localContentManifest);

			content.name = ""; // force the SO name to be empty. Maintaining two names is too hard.
			var directory = Path.GetDirectoryName(modifiedAssetPath);
			Directory.CreateDirectory(directory);
			AssetDatabase.CreateAsset(content, modifiedAssetPath);

			var modifiedName = Path.GetFileNameWithoutExtension(modifiedAssetPath);
			content.SetContentName(modifiedName);
			NotifyCreated(content);
		}

		/// <summary>
		/// Delete a <see cref="TContent"/> item of type <see cref="Type"/>
		/// </summary>
		/// <param name="type"></param>
		/// <param name="content"></param>
		public void DeleteByType(Type type, IContentObject content)
		{
			var methodName = nameof(Delete);
			var method = typeof(ContentIO).GetMethod(methodName).MakeGenericMethod(type);
			method.Invoke(this, new object[] { content });
		}

		/// <summary>
		/// Delete a <see cref="TContent"/> item of type <see cref="TContent"/>
		/// </summary>
		/// <typeparam name="TContent"></typeparam>
		/// <param name="content"></param>
		public void Delete<TContent>(TContent content)
			where TContent : ContentObject, new()
		{
			var contentName = content.ContentName;
			var contentSystemType = content.GetType();

			var path = AssetDatabase.GetAssetPath(content);
			if (string.IsNullOrEmpty(path))
			{
				var newNameAsPath = contentName.Replace('.', Path.DirectorySeparatorChar);
				path = $"{Directories.DATA_DIR}/{content.ContentType}/{newNameAsPath}.asset";
			}

			NotifyDeleted(content);

			AssetDatabase.DeleteAsset(path);
			File.Delete(path);
		}

		public static bool AreTagsEqual(string[] firstContentTags, string[] secondContentTags)
		{
			var firstContentDistinctTags = firstContentTags.Distinct();
			var secondContentDistinctTags = secondContentTags.Distinct();

			var firstContentTagCount = firstContentDistinctTags.Count();
			var secondContentTagCount = secondContentDistinctTags.Count();

			var countOfSharedTags = firstContentDistinctTags.Intersect(secondContentDistinctTags).Count();

			var areTagsEqual = firstContentTagCount == countOfSharedTags && secondContentTagCount == countOfSharedTags;

			return areTagsEqual;
		}


		struct ValidationChecksum
		{
			public Guid ValidationId;
			public string Checksum;
		}

		private static Dictionary<string, ValidationChecksum> _checksumTable =
			new Dictionary<string, ValidationChecksum>();

		private static Dictionary<Guid, string> _validationIdToChecksumTable = new Dictionary<Guid, string>();

		public static string ComputeChecksum(IContentObject content)
		{
			// TODO: Un-confuse this if statement.
			if (content is ContentObject contentObj && contentObj &&
				_checksumTable.TryGetValue(contentObj.Id, out var existing) &&
				existing.ValidationId == contentObj.ValidationGuid)
			{
				return existing.Checksum;
			}

			using (var md5 = MD5.Create())
			{
				var json = ClientContentSerializer.SerializeProperties(content);
				var bytes = Encoding.ASCII.GetBytes(json);
				var hash = md5.ComputeHash(bytes);
				var checksum = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

				if (content is ContentObject contentObj2 && contentObj2)
				{
					_checksumTable[contentObj2.Id] = new ValidationChecksum
					{
						ValidationId = contentObj2.ValidationGuid,
						Checksum = checksum
					};
				}

				return checksum;
			}
		}

		public string Checksum(IContentObject content)
		{
			return ComputeChecksum(content);
		}

		public string Serialize<TContent>(TContent content)
			where TContent : ContentObject, new()
		{
			return ClientContentSerializer.SerializeContent(content);
		}


		public void Rename(string existingAssetPath, string nextAssetpath, ContentObject content)
		{
			var newDirectory = Path.GetDirectoryName(existingAssetPath);
			Directory.CreateDirectory(newDirectory);
			var nextName = Path.GetFileNameWithoutExtension(nextAssetpath);

			var oldId = content.Id;
			//         content.name = nextName; // don't touch the SO name field.
			content.SetContentName(nextName);
			NotifyRenamed(oldId, content, nextAssetpath);
			content.BroadcastUpdate();
			var result = AssetDatabase.MoveAsset(existingAssetPath, nextAssetpath);
			if (!string.IsNullOrEmpty(result))
			{
				throw new Exception(result);
			}

			EditorUtility.SetDirty(content);
			AssetDatabase.ForceReserializeAssets(new[] { nextAssetpath },
				ForceReserializeAssetsOptions.ReserializeAssetsAndMetadata);
		}

		private void CommitAssetDatabase()
		{
			// TODO: Revisit this code. It was written this way, because that was the way I could make sure the assets always received the changes
			AssetDatabase.Refresh();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		internal static void NotifyRenamed<TContent>(string oldId, TContent content, string nextAssetPath)
			where TContent : ContentObject, new()
		{
			OnContentRenamed?.Invoke(oldId, content, nextAssetPath);
		}

		internal static void NotifyCreated<TContent>(TContent content)
			where TContent : ContentObject, new()
		{
			OnContentCreated?.Invoke(content);
		}

		internal static void NotifyDeleted<TContent>(TContent content)
			where TContent : ContentObject, new()
		{
			OnContentDeleted?.Invoke(content);
		}

		private static Promise<ClientManifest> RequestClientManifest(IBeamableRequester requester)
		{
			string url = $"/basic/content/manifest/public?id={ContentConfiguration.Instance.RuntimeManifestID}";
			return requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
			{
				if (ex is PlatformRequesterException err && err.Status == 404)
				{
					return new ClientManifest { entries = new List<ClientContentInfo>() };
				}

				throw ex;
			});
		}

		/// <summary>
		/// Checks if local content has changes. If no changes then it proceeds to baking.
		/// If there are local changes then displays a warning.
		/// Writes all content objects to streaming assets in either compressed or uncompressed form
		/// based on setting in Content Configuration.
		/// </summary>
		[MenuItem(MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Bake Content")]
		public static async Task BakeContent()
		{
			void BakeLog(string message) => Debug.Log($"[Bake Content] {message}");

			var api = BeamEditorContext.Default;
			await api.InitializePromise;

			var allContent = api.ContentIO.FindAll();

			List<ContentObject> contentList = null;
			if (allContent != null)
			{
				contentList = allContent.ToList();
			}

			if (contentList == null || contentList.Count == 0)
			{
				BakeLog("Content list is empty");
				return;
			}

			// get all valid (up-to-date) content pieces
			List<ContentObject> objectsToBake = new List<ContentObject>();
			foreach (var content in contentList)
			{
				var status = await api.ContentIO.GetStatus(content);
				if (status == ContentStatus.CURRENT)
				{
					objectsToBake.Add(content);
				}
			}

			// check for local changes
			if (objectsToBake.Count != contentList.Count)
			{
				bool continueBaking = EditorUtility.DisplayDialog("Local changes",
																  "You have local changes in your content. " +
																  "Do you want to proceed with baking using only the unchanged data?",
																  "Yes", "No");
				if (!continueBaking)
				{
					return;
				}
			}

			BakeLog($"Baking {objectsToBake.Count} items");

			var clientManifest = await RequestClientManifest(api.Requester);

			bool compress = ContentConfiguration.Instance.EnableBakedContentCompression;

			if (Bake(objectsToBake, clientManifest, compress, out int objectsBaked))
			{
				BakeLog(
					$"Baked {objectsBaked} content objects to '{BAKED_CONTENT_FILE_PATH + ".bytes"}'");
				AssetDatabase.Refresh();
			}
			else
			{
				Debug.LogError($"Baking failed");
			}
		}

		private static bool Bake(List<ContentObject> contentList,
								 ClientManifest clientManifest,
								 bool compress,
								 out int objectsBaked)
		{
			Directory.CreateDirectory(BEAMABLE_RESOURCES_PATH);

			objectsBaked = contentList.Count;
			ContentDataInfo[] contentData = new ContentDataInfo[contentList.Count];
			for (int i = 0; i < contentList.Count; i++)
			{
				var content = contentList[i];
				var serverReference = clientManifest.entries.Find(reference => reference.contentId == content.Id);
				if (serverReference == null)
				{
					throw new Exception($"Content object with ID {content.Id} is missing in a remote manifest." +
										"Reset your content and try again.");
				}

				var version = serverReference.version;
				content.SetIdAndVersion(content.Id, version);
				contentData[i] = new ContentDataInfo { contentId = content.Id, data = content.ToJson() };
			}

			ContentDataInfoWrapper fileData = new ContentDataInfoWrapper { content = contentData.ToList() };

			try
			{
				string contentJson = JsonUtility.ToJson(fileData);
				string contentPath = BAKED_CONTENT_FILE_PATH + ".bytes";
				string manifestJson = JsonUtility.ToJson(clientManifest);
				string manifestPath = BAKED_MANIFEST_FILE_PATH + ".bytes";
				if (compress)
				{
					File.WriteAllBytes(contentPath, Gzip.Compress(contentJson));
					File.WriteAllBytes(manifestPath, Gzip.Compress(manifestJson));
				}
				else
				{
					File.WriteAllText(contentPath, contentJson);
					File.WriteAllText(manifestPath, manifestJson);
				}
			}
			catch (Exception e)
			{
				Debug.LogError(
					$"Failed to write baked file to '{BAKED_CONTENT_FILE_PATH}': {e.Message}");
				return false;
			}

			return true;
		}
	}
}
