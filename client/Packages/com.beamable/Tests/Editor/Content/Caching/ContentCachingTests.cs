using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Coroutines;
using Beamable.Service;
using Beamable.Tests.Runtime;
using NUnit.Framework;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.TestTools;

namespace Beamable.Editor.Tests.Content.Caching
{
	public class ContentCachingTests : BeamableTest
	{
		public class TestFilesystemAccessor : IBeamableFilesystemAccessor
		{
			private const string tempPath = "Packages/com.beamable/Tests/Editor/Content/Caching/TEMP";

			public string GetPersistentDataPathWithoutTrailingSlash()
			{
				return tempPath;
			}

			public void ClearPersistentDataFolder()
			{
				var fullPath = Path.GetFullPath(tempPath);

				if (Directory.Exists(tempPath))
				{
					Debug.Log($"TestFilesystemAccessor: Clearing temp dir fullPath={fullPath}");
					Directory.Delete(fullPath, true);
				}
			}
		}

		[UnityTest]
		public IEnumerator SubscribingToContentManifestChanges_PublishesChanges_WithCorrectContent_AtLeastOnceAfterSubscribing()
		{
			yield break; // TODO: REPLACE THIS SOMEDAY.
			SetupEditModeContentCachingTests();

			var testManifestPath = "Packages/com.beamable/Tests/Editor/Content/Caching/FixedTestData/TestClientManifest.txt";
			var testManifest = System.IO.File.ReadAllText(testManifestPath);
			var testContentId = "ContentCachingTestContent.test_content";

			MockRequester.MockRequest<ClientManifest>(Common.Api.Method.GET, "/basic/content/manifest/public").WithResponse(ClientManifest.ParseCSV(testManifest));

			var testContentJsonPath = "Packages/com.beamable/Tests/Editor/Content/Caching/FixedTestData/ContentCachingTest_SimpleContentObject_WithOneInteger.json";
			var testContentJson = System.IO.File.ReadAllText(testContentJsonPath);
			const int expectedTestDataIntegerValue = 23;

			MockRequester.MockRequest<string>(Common.Api.Method.GET,
				"https://dev-content.beamable.com/1385004947324928/DE_1385004947324931/public/ContentCachingTestContent.test_content/f3148cf498ca6370e2ab048de83c7c4625df4f99.json")
				.WithNoAuthHeader()
				.WithResponse(testContentJson)
				;

			var callbackHappenedWithCorrectContent = false;
			MockApi.ContentService.Subscribable.Subscribe(async manifest =>
			{
				var contentDoesExist = manifest.entries.Exists(content => content.contentId == testContentId);
				if (!contentDoesExist)
					return;

				var clientContentManifest = manifest.entries.Find(content => content.contentId == testContentId);
				if (clientContentManifest == null)
					return;

				var contentRef = new ContentRef(typeof(ContentCachingTestContent), testContentId);
				var fetchedContent = await MockApi.ContentService.GetContent<ContentCachingTestContent>(contentRef);

				Debug.Log($"fetchedContent testInteger={fetchedContent.testInteger}");

				if (fetchedContent.testInteger != expectedTestDataIntegerValue)
					return;

				callbackHappenedWithCorrectContent = true;
			});

			const int maxFramesToWaitForCallback = 100;
			for (int i = 0; i < maxFramesToWaitForCallback; i++)
			{
				if (callbackHappenedWithCorrectContent)
					break;

				yield return null;
			}

			Assert.IsTrue(callbackHappenedWithCorrectContent);
		}

		private void SetupEditModeContentCachingTests()
		{
#if UNITY_EDITOR
			Debug.Log("EDITOR");
#else
			Debug.Log("NOT_IN_EDITOR");
#endif
			GameObject coroutineServiceGo = new GameObject();
			var coroutineService = MonoBehaviourServiceContainer<CoroutineService>.CreateComponent(coroutineServiceGo);
			ServiceManager.Provide(coroutineService);
			ServiceManager.ProvideWithDefaultContainer(new ContentParameterProvider
			{
				manifestID = "global"
			});
			ServiceManager.AllowInTests();

			ContentService.AllowInTests();

			var fsa = new TestFilesystemAccessor();
			fsa.ClearPersistentDataFolder();
			var cs = new ContentService(MockPlatform, MockRequester, fsa);
			MockApi.ContentService = cs;
		}
	}
}