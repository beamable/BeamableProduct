using System;
using System.Linq;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Modules.Content;
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
		public static Texture iconFolder;
		public static Texture iconMoreOptions;
		public static Texture iconPlay;
		public static Texture iconHelp;
		public static Texture iconRefresh;
		public static Texture iconBeamableSmall;
		public static Texture iconBeamableSmallColor;
		public static Texture iconLogoHeader;
		public static Texture iconShadowSoftA;
		public static Texture iconLocked;
		public static Texture iconPlus;
		public static Texture iconCheck;
		public static Texture iconUpload;
		public static Texture iconDownload;
		public static Texture iconMenuOptions;
		public static Texture iconTag;
		public static Texture iconType;
		public static Texture iconStatus;
		public static Texture iconDelete;
		public static Texture iconStatusModified;
		public static Texture iconStatusAdded;
		public static Texture iconStatusDeleted;
		public static Texture iconStatusConflicted;
		public static Texture iconRotate;
		public static Texture iconSync;
		public static Texture iconPublish;
		public static Texture iconRevertAction;
		public static Texture iconStatusInvalid;
		public static Texture iconContentEditorIcon;
		public static Texture iconContentSnapshotColor;
		public static Texture iconContentSnapshotWhite;

		public static Texture artGameServers;
		public static Texture artLiveOps;
		public static Texture artContent;
		public static Texture artServerless;

		public static Texture[] loginArts;


		public static Texture[] unitySpinnerTextures;

		public static Texture GetSpinner(int offset=0)
		{
			var spinnerIndex = (int)( ((Time.realtimeSinceStartup*12f)+offset) % BeamGUI.unitySpinnerTextures.Length);
			return unitySpinnerTextures[spinnerIndex];
		}

		public static void LoadUnityIcons()
		{
			if (iconLocked == null)
			{
				iconLocked = EditorGUIUtility.IconContent("Locked").image;
			}
			
			if (iconHelp == null)
			{
				iconHelp = EditorGUIUtility.IconContent("_Help").image;
			}

			if (iconRefresh == null)
			{
				iconRefresh = EditorGUIUtility.IconContent("Refresh").image;
			}

			if (iconSettings == null)
			{
				iconSettings = EditorGUIUtility.IconContent("Settings").image;
			}

			if (iconFolder == null)
			{
				iconFolder = EditorGUIUtility.IconContent("Folder Icon").image;
			}


			if (iconOpenApi == null)
			{
				iconOpenApi = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
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

			if (iconPlus == null)
			{
				iconPlus = EditorGUIUtility.IconContent("d_Toolbar Plus@2x").image;
			}

			if (iconCheck == null)
			{
				// iconCheck = EditorGUIUtility.IconContent("d_FilterSelectedOnly@2x").image;
				iconCheck = EditorGUIUtility.IconContent("Toggle Icon").image;
			}

			if (iconUpload == null)
			{
				iconUpload = EditorGUIUtility.IconContent("Update-Available@2x").image;
			}

			if (iconDownload == null)
			{
				iconDownload = EditorGUIUtility.IconContent("Download-Available@2x").image;
			}

			if (iconMenuOptions == null)
			{
				iconMenuOptions = EditorGUIUtility.IconContent("d__Menu@2x").image;
			}

			if (iconRotate == null)
			{
				iconRotate = EditorGUIUtility.IconContent("RotateTool On@2x").image;
			}

		}
		
		public static void LoadNonConfigurableIcons(bool silentError=false)
		{
			try
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
				
				// do not attempt to load the icons if the beamable editor is not initialized. 
				//  it is a good proxy for, "is the asset database ready", 
				//  and if it is not, then trying to import these assets will surely fail. 
				if (!BeamEditor.IsInitialized) return;
				


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
					loginArts = new Texture[] {artLiveOps, artGameServers, artContent, artServerless};
				}


				if (iconShadowSoftA == null)
				{
					iconShadowSoftA =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/softShadow.png", true);
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

				if (iconOpenMongoExpress == null)
				{
					iconOpenMongoExpress =
						EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Database_light.png",
						                              true);
				}

				if (iconTag == null)
				{
					iconTag = EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Tag.png");
				}

				if (iconType == null)
				{
					iconType = EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Type.png");
				}

				if (iconStatus == null)
				{
					iconStatus =
						EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Statuses.png");
				}

				if (iconDelete == null)
				{
					iconDelete =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Delete.png");
				}

				if (iconStatusModified == null)
				{
					iconStatusModified =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Modified.png");
				}

				if (iconStatusAdded == null)
				{
					iconStatusAdded =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Added.png");
				}

				if (iconStatusDeleted == null)
				{
					iconStatusDeleted =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Deleted.png");
				}

				if (iconStatusConflicted == null)
				{
					iconStatusConflicted =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconLogs_WarningMsg.png");
				}

				if (iconStatusInvalid == null)
				{
					iconStatusInvalid =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Invalid.png");
				}

				if (iconSync == null)
				{
					iconSync = EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_Sync.png");
				}

				if (iconPublish == null)
				{
					iconPublish =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_Publish.png");
				}

				if (iconRevertAction == null)
				{
					iconRevertAction =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconAction_Revert.png");
				}

				if (iconContentEditorIcon == null)
				{
					iconContentEditorIcon =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_ItemFallback.png");
				}

				if (iconContentSnapshotWhite == null)
				{
					iconContentSnapshotWhite =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconsWhite_ContentSnapshot.png");
				}

				if (iconContentSnapshotColor == null)
				{
					iconContentSnapshotColor =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_ContentSnapshot.png");
				}
			}
			catch (Exception) when (silentError)
			{
				// let it go.
			}
		}

		public static void LoadConfigurableIcons()
		{
			var reflectionCache = Beam.GetReflectionSystem<ContentTypeReflectionCache>();
			var allTypes = reflectionCache.GetAll().ToList();
			if (ContentConfiguration.Instance.ContentTextureConfiguration == null || ContentConfiguration.Instance.ContentTextureConfiguration?.TextureConfigurations?.Count != allTypes.Count)
			{
				ContentConfiguration.Instance.ContentTextureConfiguration = new  ContentTextureConfiguration(allTypes);
			}
		}
		
		public static void LoadAllIcons()
		{
			LoadUnityIcons();
			LoadConfigurableIcons();
			LoadNonConfigurableIcons();
		}
	}
}
