using Beamable.Api.CloudData;
using Beamable.Common.Api;
using Beamable.Common.Api.CloudData;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Editor.UI.CloudDataWindow.Models
{
	public class CloudDataViewService
	{
		public event Action<Dictionary<CloudMetaData,string>> OnAvailableMetaDataDownloaded;
		private List<CloudMetaData> _cloudMeta;
		private Dictionary<CloudMetaData,string> _cloudMetaContent;
		public void Init()
		{
			var api = BeamEditorContext.Default;
			Debug.Log($"<b>[CloudDataViewService]</b> Init");
			api.Requester.Request<GetCloudDataManifestResponse>(
				Method.GET,
				"/basic/cloud/meta"
			).Then(response =>
			{
				_cloudMeta = response.meta;
				_cloudMetaContent = new Dictionary<CloudMetaData, string>();
				Debug.Log($"<b>[CloudDataViewService]</b> {response.result}: {response.meta?.Count}");
				foreach (CloudMetaData metaData in _cloudMeta)
				{
					api.Requester.Request(Method.GET,
					                      $"https://{metaData.uri}", parser: s => s).Then(s =>
					{
						_cloudMetaContent.Add(metaData, s);
						if(_cloudMetaContent.Count == _cloudMeta.Count)
						{
							OnAvailableMetaDataDownloaded?.Invoke(_cloudMetaContent);
						}
							
					});
				}
			});
		}
	}
}
