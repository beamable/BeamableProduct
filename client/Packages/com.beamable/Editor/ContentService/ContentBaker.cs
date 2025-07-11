using Beamable;
using Beamable.Api;
using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Serialization.SmallerJSON;
using Core.Platform.SDK;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Content;

namespace Editor.ContentService
{
	public class ContentBaker
	{
		public static async Task BakeContent(bool skipCheck)
		{
			void BakeLog(string message) => Debug.Log($"[Bake Content] {message}");

			var api = BeamEditorContext.Default;
			await api.InitializePromise;

			var contentService = api.ServiceScope.GetService<CliContentService>();

			var allContent = contentService.CachedManifest.Values;

			List<LocalContentManifestEntry> contentList = allContent.ToList();

			if (contentList.Count == 0)
			{
				BakeLog("Content list is empty");
				return;
			}

			// get all valid (up-to-date) content pieces
			List<LocalContentManifestEntry> objectsToBake = new List<LocalContentManifestEntry>();
			foreach (var content in contentList)
			{
				if (content.StatusEnum is ContentStatus.UpToDate)
				{
					objectsToBake.Add(content);
				}
			}

			// check for local changes
			if (!skipCheck && objectsToBake.Count != contentList.Count)
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

			ContentDataInfo[] contentData = new ContentDataInfo[objectsToBake.Count];
			for (int i = 0; i < contentList.Count; i++)
			{
				var content = contentList[i];
				var serverReference = clientManifest.entries.Find(reference => reference.contentId == content.FullId);
				if (serverReference == null)
				{
					throw new Exception($"Content object with ID {content.FullId} is missing in a remote manifest." +
					                    "Reset your content and try again.");
				}

				var contentJson = await File.ReadAllTextAsync(content.JsonFilePath);
				var deserializedResult = Json.Deserialize(contentJson);
				var root = deserializedResult as ArrayDict;
				var contentDict = new ArrayDict
				{
					{"id", content.FullId}, {"version", content.Hash ?? ""}, {"properties", root}
				};

				var propertyDict = new PropertyValue {rawJson = contentJson};
				contentDict.Add("properties", propertyDict);

				var bakedJson = Json.Serialize(contentDict, new StringBuilder());

				contentData[i] = new ContentDataInfo
				{
					contentId = content.FullId, contentVersion = content.Hash, data = bakedJson
				};
			}

			if (Bake(contentData, clientManifest, compress, out int objectsBaked))
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

		private static bool Bake(ContentDataInfo[] contentData,
		                         ClientManifest clientManifest,
		                         bool compress,
		                         out int objectsBaked)
		{
			Directory.CreateDirectory(BEAMABLE_RESOURCES_PATH);

			objectsBaked = contentData.Length;

			ContentDataInfoWrapper fileData = new ContentDataInfoWrapper {content = contentData.ToList()};

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

		private static Promise<ClientManifest> RequestClientManifest(IBeamableRequester requester)
		{
			string url = $"/basic/content/manifest/public?id={ContentConfiguration.Instance.RuntimeManifestID}";
			return requester.Request(Method.GET, url, null, true, ClientManifest.ParseCSV, true).Recover(ex =>
			{
				if (ex is PlatformRequesterException err && err.Status == 404)
				{
					return new ClientManifest {entries = new List<ClientContentInfo>()};
				}

				throw ex;
			});
		}
	}
}
