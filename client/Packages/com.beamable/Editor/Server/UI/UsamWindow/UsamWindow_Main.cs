using Beamable.Common.Util;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Microservice.UI2.PublishWindow;
using Beamable.Editor.Util;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{
		public Color primaryColor = new Color(.25f, .5f, 1f, .8f);
		
		public List<CardButton> cards = new List<CardButton>();
		public string selectedBeamoId;
		public WindowState state = WindowState.NORMAL;

		[NonSerialized]
		private Dictionary<string, LogDataProvider> _beamoIdToLogProvider = new Dictionary<string, LogDataProvider>();

		void PrepareLogs()
		{
			foreach (var namedLog in usam._namedLogs)
			{
				if (!_beamoIdToLogProvider.TryGetValue(namedLog.beamoId, out var provider))
				{
					provider = _beamoIdToLogProvider[namedLog.beamoId] = new CliLogDataProvider(namedLog.logs);
					namedLog.logView.BuildView(provider);
				}
			}
		}
		
		void PrepareCards()
		{

			{ // maybe we need to do upgrades?
				if (usam?._requiredUpgrades?.Count > 0)
				{
					state = WindowState.REQUIRED_UGPRADES;
					return;
				} else if (state == WindowState.REQUIRED_UGPRADES)
				{
					// but there are no upgrades...
					state = WindowState.NORMAL;
				}
			}

			
			{ // refresh cards
				cards.Clear();
				
				for (var i = 0 ; i < usam?.latestManifest?.services?.Count; i ++)
				{
					var service = usam.latestManifest.services[i];
					cards.Add(new CardButton
					{
						serviceIndex = i,
						storageIndex = -1,
						name = service.beamoId,
						subText = "service",
						icon = EditorGUIUtility.FindTexture("Settings") // TODO: icon
					});
				}

				for (var i = 0; i < usam?.latestManifest?.storages?.Count; i++)
				{
					var storage = usam.latestManifest.storages[i];
					cards.Add(new CardButton
					{
						serviceIndex = -1,
						storageIndex = i,
						name = storage.beamoId,
						subText = "storage",
						icon = EditorGUIUtility.FindTexture("Profiler.GlobalIllumination") // TODO: icon
					});
				}

				if (cards.Count == 0)
				{
					selectedBeamoId = null;
					
					if (!usam.hasReceivedManifestThisDomain)
					{
						state = WindowState.NORMAL;
					} else if (state == WindowState.NORMAL)
					{
						state = WindowState.CREATE_SERVICE;
					}
				}
				else
				{
					CardButton selectedCard = cards[0];
					for (var i = 0; i < cards.Count; i++)
					{
						if (cards[i].name == selectedBeamoId)
						{
							selectedCard = cards[i];
							break;
						}
					}
					selectedBeamoId = selectedCard.name;
				}
			
			}
		}
		
		void DrawMain()
		{
			DrawHeader();
			
			switch (state)
			{
				case WindowState.SETTINGS:
					DrawSettings();
					break;
				case WindowState.CREATE_SERVICE:
					DrawNewService();
					break;
				case WindowState.CREATE_STORAGE:
					DrawNewStorage();
					break;
				case WindowState.CREATE_FEDERATION_ID:
					DrawNewFederationId();
					break;
				case WindowState.NORMAL:
					DrawContent();
					break;
				case WindowState.REQUIRED_UGPRADES:
					DrawUpgrades();
					break;
			}
		}

		void DrawContent()
		{
			EditorGUILayout.BeginVertical();
			
			if (!string.IsNullOrEmpty(selectedCard.name))
			{
				if (selectedCard.serviceIndex >= 0)
				{
					var service = usam.latestManifest.services[selectedCard.serviceIndex];
					DrawService(service);
				} else if (selectedCard.storageIndex >= 0)
				{
					var storage = usam.latestManifest.storages[selectedCard.storageIndex];
					DrawStorage(storage);
				}
				else
				{
					DrawTempLoading();
				}
			}
			else
			{
				DrawTempLoading();
			}

			EditorGUILayout.EndVertical();
		}

		void DrawTempLoading()
		{
			EditorGUILayout.Space(12, false);
			BeamGUI.LoadingSpinnerWithState("Loading...");

		}



		public enum WindowState
		{
			CREATE_SERVICE,
			CREATE_STORAGE,
			CREATE_FEDERATION_ID,
			NORMAL,
			SETTINGS,
			PUBLISH,
			
			/// <summary>
			/// these are on-going file replacement upgrades, where as <see cref="MIGRATE"/> is the 1.x to 2.x migratino flow.
			/// </summary>
			REQUIRED_UGPRADES
		}
		
		[Serializable]
		public struct CardButton
		{
			public string name;
			public string subText;
			public CardButtonRunState state;
			public Texture2D icon;
			public int serviceIndex;
			public int storageIndex;
		}

		public enum CardButtonRunState
		{
			NotRunning,
			Starting,
			Running
		}

		void DrawHeader()
		{
			var clickedCreate = false;
			var clickedConfig = false;
			var clickedPublish = false;
			BeamGUI.DrawHeaderSection(this, ActiveContext, 
				drawLowBarGui: () =>
				{
					{ // draw the dropdowns
						if (cards.Count > 0) // if there are no cards, then there is nothing to pick.
						{
							BeamGUI.LayoutDropDown(this, new GUIContent(selectedBeamoId), GUILayout.ExpandHeight(true),
							                       () =>
							                       {
								                       var popup = CreateInstance<ServicePickerWindow>();
								                       popup.usamWindow = this;
								                       return new BeamGUI.DropdownMetadata<ServicePickerWindow>
								                       {
									                       window = popup,
									                       startSize = new Vector2(300, cards.Count * ServicePickerWindow.ComputedHeight + 40)
								                       };
							                       }
							);
							EditorGUILayout.Space(4, false);

						}

					}
				},
				drawTopBarGui: () =>
				{
					{ // draw the left buttons
						if (state == WindowState.SETTINGS)
						{
							clickedConfig = BeamGUI.HeaderButton("Services", BeamGUI.iconService);
						}
						else
						{
							clickedConfig = BeamGUI.HeaderButton("Config", EditorGUIUtility.FindTexture("Settings"));
						}

						clickedPublish =
							BeamGUI.HeaderButton("Release", BeamGUI.iconPublish);


						clickedCreate = BeamGUI.ShowDisabled(state != WindowState.CREATE_SERVICE && state != WindowState.CREATE_STORAGE,
						                                     () => BeamGUI.HeaderButton(
							                                     "Create", EditorGUIUtility.FindTexture("Toolbar Plus")));

						if (BeamGUI.HeaderButton("Open", BeamGUI.iconFolder))
						{
							AddDelayedAction(() =>
							{
								usam.OpenSolution();
							});
						}
					}

				},
				onClickedHelp: () =>
				{
					Application.OpenURL(
						DocsPageHelper.GetUnityDocsPageUrl(
							"unity/user-reference/cloud-services/microservices/microservice-unity-integration/",
							EditorConstants.UNITY_CURRENT_DOCS_VERSION));
				},
				onClickedRefresh: () =>
				{
					usam.Reload();
				});
			
			
			if (clickedPublish)
			{
				CheckDocker("publish", () => UsamPublishWindow.Init(ActiveContext), out _);
			}

			if (clickedConfig)
			{
				AddDelayedAction(() =>
				{
					if (state == WindowState.SETTINGS)
					{
						state = WindowState.NORMAL;
					}
					else
					{
						ActivateSettings();
					}
				});
			}

			if (clickedCreate)
			{
				var menu = new GenericMenu();
				
				menu.AddItem(new GUIContent("Service"), false, () =>
				{
					state = WindowState.CREATE_SERVICE;
					newServiceName = "";
				});
				menu.AddItem(new GUIContent("Storage"), false, () =>
				{
					state = WindowState.CREATE_STORAGE;
					newServiceName = "";
				});
				menu.AddItem(new GUIContent("Federation Id"), false, () =>
				{
					state = WindowState.CREATE_FEDERATION_ID;
				});
				
				menu.ShowAsContext();
			}
		}
	}
}
