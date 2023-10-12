using Beamable.Api.CloudSaving;
using Beamable.BSAT.Attributes;
using Beamable.BSAT.Core;
using Beamable.Common;
using System;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

/// <summary>
/// TestResult.Passed - Marks automatically a test as passed.
/// TestResult.Failed - Marks automatically a test as failed.
/// TestResult.NotSet - Requires user manual check if a test is passed or failed using buttons in scene UI (Passed/Failed). Used especially for UI tests.
/// </summary>

namespace Beamable.BSAT.Test.CloudSaving
{
	[Serializable]
	public class MyCustomData
	{
		public float Volume = 100;
		public bool IsMuted;

		public override string ToString()
		{
			return $"[MyCustomData (" +
			       $"Volume = {Volume}, " +
			       $"IsMuted = {IsMuted})]";
		}
	}
	
	public class CloudSavingScript : Testable
	{
		const string FILE_NAME = "myCustomData.json";
		private string LocalCloudDataPath => $"{_cloudSavingService.LocalCloudDataFullPath}{Path.DirectorySeparatorChar}";
		private string FilePath => $"{LocalCloudDataPath}{FILE_NAME}";
	
		private BeamContext _ctx;
		private CloudSavingService _cloudSavingService;
		private MyCustomData _customData = new MyCustomData { Volume = 1, IsMuted = false };

		[SerializeField] private TextMeshProUGUI _text;
		
		[TestRule(0)]
		public async Promise<TestResult> InitializeBeamContext()
		{
			try
			{
				_ctx = BeamContext.Default;
				await _ctx.OnReady;
				return TestResult.Passed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
		[TestRule(1)]
		public async Promise<TestResult> InitializeCloudSavingService()
		{
			try
			{
				_cloudSavingService = _ctx.Api.CloudSavingService;
				if (!_cloudSavingService.IsInitializing)
					await _cloudSavingService.Init();
				else
				{
					TestableDebug.LogError($"Cannot call Init() when isInitializing = {_cloudSavingService.IsInitializing}");
					return TestResult.Failed;
				}
				return TestResult.Passed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
		[TestRule(2)]
		public TestResult SaveData()
		{
			try
			{
				SaveDataUtil();
				return TestResult.Passed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
		[TestRule(3)]
		public TestResult LoadDataExists()
		{
			try
			{
				var data = LoadDataUtil(FilePath);
				return data != null ? TestResult.Passed : TestResult.Failed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
		[TestRule(4)]
		public TestResult LoadDataNotExists()
		{
			try
			{
				var fileName = "NotExistingFile.json";
				var data = LoadDataUtil($"{LocalCloudDataPath}{fileName}");
				return data == null ? TestResult.Passed : TestResult.Failed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
		[TestRule(5)]
		public TestResult EraseData()
		{
			if (!File.Exists(FilePath))
				return TestResult.Failed;
			
			File.Delete(FilePath);
			var data = LoadDataUtil(FilePath);
			return data == null ? TestResult.Passed : TestResult.Failed;
		}
		[TestRule(6)]
		public async Promise<TestResult> RecoverData()
		{
			try
			{
				var checkDelayInMS = 100;
				var maxChecks = 50;
				var checkCount = 0;
				var updateReceived = false;

				_cloudSavingService.UpdateReceived += _ => updateReceived = true;
				await _cloudSavingService.ReinitializeUserData();

				// Test will wait for the response for [checkDelayInMS * maxChecks] time
				while (!updateReceived || checkCount < maxChecks)
				{
					await Task.Delay(checkDelayInMS);
					checkCount++;
				}

				var data = LoadDataUtil(FilePath);
				return data != null ? TestResult.Passed : TestResult.Failed;
			}
			catch (Exception e)
			{
				TestableDebug.LogError(e);
				return TestResult.Failed;
			}
		}
		[TestRule(7)]
		public TestResult OverrideDataInPortal()
		{
			void LoadDataAndUpdateText()
			{
				var data = LoadDataUtil(FilePath);
				_text.SetText(data.ToString());
			}

			LoadDataAndUpdateText();
			_cloudSavingService.UpdateReceived += _ => LoadDataAndUpdateText();
			return TestResult.NotSet;
		}
		
		private MyCustomData LoadDataUtil(string filePath)
		{
			MyCustomData data = null;
			if (File.Exists(filePath))
			{
				var json = File.ReadAllText(filePath);
				data = JsonUtility.FromJson<MyCustomData>(json);
			}
			return data;
		}
		private void SaveDataUtil()
		{
			var json = JsonUtility.ToJson(_customData);
			if (!Directory.Exists(FilePath))
				Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
			File.WriteAllText(FilePath, json);
		}
	}
}
