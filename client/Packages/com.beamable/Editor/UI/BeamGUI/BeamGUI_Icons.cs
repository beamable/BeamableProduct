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
			if (!iconLocked)
			{
				iconLocked = EditorGUIUtility.IconContent("Locked").image;
			}

			if (!iconHelp)
			{
				iconHelp = EditorGUIUtility.IconContent("_Help").image;
			}

			if (!iconRefresh)
			{
				iconRefresh = EditorGUIUtility.IconContent("Refresh").image;
			}

			if (!iconSettings)
			{
				iconSettings = EditorGUIUtility.IconContent("Settings").image;
			}

			if (!iconFolder)
			{
				iconFolder = EditorGUIUtility.IconContent("Folder Icon").image;
			}


			if (!iconOpenApi)
			{
				iconOpenApi = EditorGUIUtility.IconContent("BuildSettings.Web.Small").image;
			}

			if (!iconOpenProject)
			{
				iconOpenProject = EditorGUIUtility.FindTexture("cs Script Icon");
			}

			if (!iconMoreOptions)
			{
				iconMoreOptions = EditorGUIUtility.IconContent("pane options@2x").image;
			}

			if (!iconPlay)
			{
				iconPlay = EditorGUIUtility.FindTexture("PlayButton");
			}

			if (!iconPlus)
			{
				iconPlus = EditorGUIUtility.IconContent("d_Toolbar Plus@2x").image;
			}

			if (!iconCheck)
			{
				// iconCheck = EditorGUIUtility.IconContent("d_FilterSelectedOnly@2x").image;
				iconCheck = EditorGUIUtility.IconContent("Toggle Icon").image;
			}

			if (!iconUpload)
			{
				iconUpload = EditorGUIUtility.IconContent("Update-Available@2x").image;
			}

			if (!iconDownload)
			{
				iconDownload = EditorGUIUtility.IconContent("Download-Available@2x").image;
			}

			if (!iconMenuOptions)
			{
				iconMenuOptions = EditorGUIUtility.IconContent("d__Menu@2x").image;
			}

			if (!iconRotate)
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
				


				if (!artGameServers)
				{
					artGameServers =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_gameservers.png", true);
				}

				if (!artLiveOps)
				{
					artLiveOps =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_liveops.png", true);
				}

				if (!artContent)
				{
					artContent =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_game_content.png", true);
				}

				if (!artServerless)
				{
					artServerless =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/beam_art_serverless.png", true);
				}

				if (loginArts == null)
				{
					loginArts = new Texture[] {artLiveOps, artGameServers, artContent, artServerless};
				}


				if (!iconShadowSoftA)
				{
					iconShadowSoftA =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/softShadow.png", true);
				}


				if (!iconLogoHeader)
				{
					iconLogoHeader =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Login/UI/icon/logo2 L white.png", true);
				}

				if (!iconBeamableSmall)
				{
					iconBeamableSmall =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/beam_icon_small.png", true);
				}

				if (!iconBeamableSmallColor)
				{
					iconBeamableSmallColor =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/beam_icon_small_color.png"
						  , true);
				}


				if (!iconService)
				{
					iconService =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/microservice.png", true);
				}

				if (!iconStorage)
				{
					iconStorage =
						EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/storage.png", true);
				}

				if (!iconOpenMongoExpress)
				{
					iconOpenMongoExpress =
						EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Database_light.png",
						                              true);
				}

				if (!iconTag)
				{
					iconTag = EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Tag.png");
				}

				if (!iconType)
				{
					iconType = EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Type.png");
				}

				if (!iconStatus)
				{
					iconStatus =
						EditorResources.Load<Texture>("Packages/com.beamable/Editor/UI/Common/Icons/Statuses.png");
				}

				if (!iconDelete)
				{
					iconDelete =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Delete.png");
				}

				if (!iconStatusModified)
				{
					iconStatusModified =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Modified.png");
				}

				if (!iconStatusAdded)
				{
					iconStatusAdded =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Added.png");
				}

				if (!iconStatusDeleted)
				{
					iconStatusDeleted =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Deleted.png");
				}

				if (!iconStatusConflicted)
				{
					iconStatusConflicted =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconLogs_WarningMsg.png");
				}

				if (!iconStatusInvalid)
				{
					iconStatusInvalid =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconStatus_Invalid.png");
				}

				if (!iconSync)
				{
					iconSync = EditorResources.Load<Texture>(
						"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_Sync.png");
				}

				if (!iconPublish)
				{
					iconPublish =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_Publish.png");
				}

				if (!iconRevertAction)
				{
					iconRevertAction =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconAction_Revert.png");
				}

				if (!iconContentEditorIcon)
				{
					iconContentEditorIcon =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconBeam_ItemFallback.png");
				}

				if (!iconContentSnapshotWhite)
				{
					iconContentSnapshotWhite =
						EditorResources.Load<Texture>(
							"Packages/com.beamable/Editor/UI/Common/Icons/IconsWhite_ContentSnapshot.png");
				}

				if (!iconContentSnapshotColor)
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
