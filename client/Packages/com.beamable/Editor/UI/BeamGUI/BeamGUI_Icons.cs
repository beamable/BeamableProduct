using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Beamable.Editor.Util
{
	public partial class BeamGUI
	{

		public static Texture iconService;
		public static Texture iconStorage;
		public static Texture iconOpenApi;
		public static Texture iconOpenMongoExpress;
		public static Texture iconSettings;
		public static Texture iconOpenProject;
		public static Texture iconMoreOptions;
		public static Texture iconPlay;
		public static Texture iconHelp;
		public static Texture iconRefresh;
		public static Texture iconBeamableSmall;
		public static Texture iconBeamableSmallColor;
		public static Texture iconLogoHeader;
		public static Texture iconShadowSoftA;
		public static Texture iconLocked;

		public static Texture artGameServers;
		public static Texture artLiveOps;
		public static Texture artContent;
		public static Texture artServerless;

		public static Texture[] loginArts;


		public static Texture[] unitySpinnerTextures;

		public static Texture GetSpinner(int offset = 0)
		{
			var spinnerIndex = (int)(((Time.realtimeSinceStartup * 12f) + offset) % BeamGUI.unitySpinnerTextures.Length);
			return unitySpinnerTextures[spinnerIndex];
		}

		public static void LoadAllIcons()
		{
			if (unitySpinnerTextures == null)
			{
				unitySpinnerTextures = new Texture[]
				{
					EditorGUIUtility.IconContent("WaitSpin00").image,
					EditorGUIUtility.IconContent("WaitSpin01").image,
					EditorGUIUtility.IconContent("WaitSpin02").image,
					EditorGUIUtility.IconContent("WaitSpin03").image,
					EditorGUIUtility.IconContent("WaitSpin04").image,
					EditorGUIUtility.IconContent("WaitSpin05").image,
					EditorGUIUtility.IconContent("WaitSpin06").image,
					EditorGUIUtility.IconContent("WaitSpin07").image,
					EditorGUIUtility.IconContent("WaitSpin08").image,
					EditorGUIUtility.IconContent("WaitSpin09").image,
					EditorGUIUtility.IconContent("WaitSpin10").image,
					EditorGUIUtility.IconContent("WaitSpin11").image,
				};
			}


			if (artGameServers == null)
			{
				artGameServers =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_gameservers.png", true);
			}
			if (artLiveOps == null)
			{
				artLiveOps =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_liveops.png", true);
			}
			if (artContent == null)
			{
				artContent =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_game_content.png", true);
			}
			if (artServerless == null)
			{
				artServerless =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_serverless.png", true);
			}

			if (loginArts == null)
			{
				loginArts = new Texture[] { artLiveOps, artGameServers, artContent, artServerless };
			}


			if (iconShadowSoftA == null)
			{
				iconShadowSoftA =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/softShadow.png", true);
			}


			if (iconLocked == null)
			{
				iconLocked = EditorGUIUtility.IconContent("Locked").image;
			}


			if (iconLogoHeader == null)
			{
				iconLogoHeader =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Login/UI/icon/logo2 L white.png", true);
			}
			if (iconBeamableSmall == null)
			{
				iconBeamableSmall =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_icon_small.png", true);
			}
			if (iconBeamableSmallColor == null)
			{
				iconBeamableSmallColor =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_icon_small_color.png"
					  , true);
			}


			if (iconService == null)
			{
				iconService =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/microservice.png", true);
			}

			if (iconStorage == null)
			{
				iconStorage =
					EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/storage.png", true);
			}

			if (iconHelp == null)
			{
				iconHelp = EditorResources.Load<Texture>(
					"Packages/com.beamable/Editor/UI/Toolbox/Icons/Info_Light.png");
			}

			if (iconRefresh == null)
			{
				iconRefresh = EditorResources.Load<Texture>(
					"Packages/com.beamable/Editor/UI/Content/Icons/Refresh.png");
			}

			if (iconSettings == null)
			{
				iconSettings = EditorGUIUtility.IconContent("Settings").image;
			}


			if (iconOpenApi == null)
			{
				iconOpenApi = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
			}

			if (iconOpenMongoExpress == null)
			{
				iconOpenMongoExpress =
					EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Database_light.png", true);
			}

			if (iconOpenProject == null)
			{
				iconOpenProject = EditorGUIUtility.FindTexture("cs Script Icon");
			}
			if (iconMoreOptions == null)
			{
				iconMoreOptions = EditorGUIUtility.IconContent("pane options@2x").image;
			}
			if (iconPlay == null)
			{
				iconPlay = EditorGUIUtility.FindTexture("PlayButton");
			}
		}
	}
}
