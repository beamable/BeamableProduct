using Beamable.Common;
using Beamable.UI.Scripts;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace Beamable
{
	public static class Extensions
	{
		public static async Promise<RawImage> SetTexture(this RawImage rawImage,
		                                                 AssetReferenceSprite assetReferenceSprite) =>
			rawImage.SetTexture(await assetReferenceSprite.LoadTexture());

		public static RawImage SetTexture(this RawImage rawImage, Texture2D texture)
		{
			if (texture != null)
				rawImage.texture = texture;
			return rawImage;
		}
	}
}
