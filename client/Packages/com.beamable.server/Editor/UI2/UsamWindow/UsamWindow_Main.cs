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

		void PrepareCards()
		{
			{ // if migrating, do that before anything else
				if (usam?.migrationPlan?.NeedsMigration ?? false)
				{
					state = WindowState.MIGRATE;
					return;
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
				case WindowState.MIGRATE:
					DrawMigrate();
					break;
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
			MIGRATE,
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
					if (state != WindowState.MIGRATE)
					{ // draw the dropdowns
						
						if (cards.Count > 0) // if there are no cards, then there is nothing to pick.
						{
							BeamGUI.LayoutDropDown(this, new GUIContent(selectedBeamoId), GUILayout.ExpandHeight(true),
							                       () =>
							                       {
								                       var popup = CreateInstance<ServicePickerWindow>();
								                       popup.usamWindow = this;
								                       return popup;
							                       }
							);
							EditorGUILayout.Space(4, false);

						}

					}
				},
				drawTopBarGui: () =>
				{
					if (state != WindowState.MIGRATE)
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
							BeamGUI.HeaderButton("Publish", EditorGUIUtility.FindTexture("Profiler.GlobalIllumination"));


						clickedCreate = BeamGUI.ShowDisabled(state != WindowState.CREATE_SERVICE && state != WindowState.CREATE_STORAGE,
						                                     () => BeamGUI.HeaderButton(
							                                     "Create", EditorGUIUtility.FindTexture("Toolbar Plus")));
					}

				},
				onClickedHelp: () =>
				{
					throw new NotImplementedException("sad no docs yet");
				},
				onClickedRefresh: () =>
				{
					activeMigration = null;
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
