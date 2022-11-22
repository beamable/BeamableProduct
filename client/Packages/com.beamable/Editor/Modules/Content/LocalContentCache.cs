using Beamable.Common;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Coroutines;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Content
{
	public class LocalContentCache : ContentCache
	{
		private readonly Type _contentType;
		private readonly CoroutineService _coroutineService;
		private readonly ContentConfiguration _config;

		public LocalContentCache(Type contentType, CoroutineService coroutineService, ContentConfiguration config)
		{
			_contentType = contentType;
			_coroutineService = coroutineService;
			_config = config;
		}

		public override Promise<IContentObject> GetContentObject(ClientContentInfo requestedInfo)
		{
			var content = (ContentObject) AssetDatabase.LoadAssetAtPath(requestedInfo.uri, _contentType);

			var delayPromise = new Promise();
			IEnumerator Delay()
			{
				var delayTime = _config.LocalContentReferenceDelaySeconds.GetOrElse(.15f);
				yield return new WaitForSecondsRealtime(delayTime);
				delayPromise.CompleteSuccess();
			}

			_coroutineService.StartNew("content-ref-delay", Delay());
			content.SetManifestID(requestedInfo.manifestID);
			return delayPromise.Map(_ => (IContentObject)content);
		}
	}
}
