using Beamable.Common.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Modules.Content
{
	[Serializable]
	public class ContentTextureConfiguration
	{
#if UNITY_EDITOR
		public List<ContentTexturePair> TextureConfigurations;

		public static Dictionary<string, Texture> OriginalTypeIcons = new()
		{
			["announcements"] = GetTextureFromBeamAssets("IconBeam_Announcements.png"),
			["api"] = GetTextureFromBeamAssets("IconBeam_API.png"),
			["api.announcementApi"] = GetTextureFromBeamAssets("IconBeam_AnnouncementsAPI.png"),
			["game_types"] = GetTextureFromBeamAssets("IconBeam_GameType.png"),
			["currency"] =  GetTextureFromBeamAssets("IconBeam_Currency.png"),
			["items"] =  GetTextureFromBeamAssets("IconBeam_Item.png"),
			["listings"] =  GetTextureFromBeamAssets("IconBeam_Listing.png"),
			["tournaments"] =  GetTextureFromBeamAssets("IconBeam_Tournaments.png"),
			["leaderboards"] =  GetTextureFromBeamAssets("IconBeam_LeaderBoards.png"),
			["events"] =  GetTextureFromBeamAssets("IconBeam_Tournaments.png"),
			["calendars"] =  GetTextureFromBeamAssets("IconBeam_Calendar.png"),
			["stores"] =  GetTextureFromBeamAssets("IconBeam_ItemStoreContent.png"),
			["skus"] =  GetTextureFromBeamAssets("IconBeam_SKUs.png"),
			["emails"] =  GetTextureFromBeamAssets("IconBeam_Emails.png"),
			["donations"] =  GetTextureFromBeamAssets("IconBeam_Donation.png"),
			["vip"] =  GetTextureFromBeamAssets("IconBeam_VIP.png"),
		};

		public static Texture FallbackTexture => GetTextureFromBeamAssets("IconBeam_ItemFallback.png");

		public ContentTextureConfiguration(IEnumerable<ContentTypePair> contentTypes)
		{
			TextureConfigurations = new List<ContentTexturePair>();
			foreach (ContentTypePair contentTypePair in contentTypes)
			{
				OriginalTypeIcons.TryGetValue(contentTypePair.Name, out Texture icon);
				
				TextureConfigurations.Add(new ContentTexturePair()
				{
					TypeName = contentTypePair.Name,
					Texture = icon
				});
			}
		}

		public Texture GetTextureForType(string typeName)
		{
			var item = TextureConfigurations?.FirstOrDefault(item => item.TypeName == typeName);
			return item == null || !item.Texture ? FallbackTexture : item.Texture;
		}

		
		private static Texture GetTextureFromBeamAssets(string fileName)
		{
			return EditorResources.Load<Texture>(
				$"Packages/com.beamable/Editor/UI/Common/Icons/{fileName}");
		}
		#endif
	}

	[Serializable]
	public class ContentTexturePair
	{
		public string TypeName;
		public Texture Texture;
	}
}
