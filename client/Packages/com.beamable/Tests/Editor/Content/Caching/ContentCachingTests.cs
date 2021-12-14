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
			var cs = new ContentService(MockPlatform, MockRequester, fsa, new ContentParameterProvider {
				manifestID = "global"
			});
			MockApi.ContentService = cs;
		}
	}
}
