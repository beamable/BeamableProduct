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


		public static void LoadAllIcons()
		{

			if (iconBeamableSmall == null)
			{
				iconBeamableSmall =
					EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/beam_icon_small.png", true);
			}


			if (iconService == null)
			{
				iconService =
					EditorResources.Load<Texture>(
						"Packages/com.beamable.server/Editor/UI/Icons/MS not running without wifi.png", true);
			}

			if (iconStorage == null)
			{
				iconStorage =
					EditorResources.Load<Texture>("Packages/com.beamable.server/Editor/UI/Icons/SO running.png", true);
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
					EditorResources.Load<Texture>("Packages/com.beamable.server/Editor/UI/Icons/Database_light.png", true);
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
