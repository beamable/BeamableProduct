using Beamable.Common;
using Beamable.UI.Scripts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Beamable
{
	public static class Extensions
	{
		public static async Promise<RawImage> SetTexture(this RawImage rawImage, AssetReferenceSprite assetReferenceSprite)
			=> rawImage.SetTexture(await assetReferenceSprite.LoadTexture());
		public static RawImage SetTexture(this RawImage rawImage, Texture2D texture)
		{
			if (texture != null)
				rawImage.texture = texture;
			return rawImage;
		}

		public static void ReplaceOrAddListener(this UnityEvent eventBase, UnityAction action)
		{
			eventBase.RemoveListener(action);
			eventBase.AddListener(action);
		}
		
		public static void ReplaceOrAddListener<T0>(this UnityEvent<T0> eventBase, UnityAction<T0> action)
		{
			eventBase.RemoveListener(action);
			eventBase.AddListener(action);
		}
	}
}

