using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Semantics;
using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.BeamCli.UI.LogHelpers;
using Beamable.Editor.Microservice.UI2.PublishWindow;
using Beamable.Editor.ThirdParty.Splitter;
using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow2
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
				case WindowState.NORMAL:
					DrawContent();
					break;
			}
			

			// {
			// 	EditorGUILayout.BeginHorizontal();
			// 	serverSplitter.BeginSplitView();
			//
			// 	DrawCards();
			// 	
			// 	EditorGUILayout.Space(15, false);
			// 	serverSplitter.Split(this);
			// 	EditorGUILayout.Space(5, false);
			// 	
			// 	
			// 	serverSplitter.EndSplitView();
			// 	EditorGUILayout.EndHorizontal();
			// }
		}



		void DrawContent()
		{
			
			{
				EditorGUILayout.BeginVertical();



				var selectedCard = cards.FirstOrDefault(x => x.name == selectedBeamoId);
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
				}

				EditorGUILayout.EndVertical();
			} 
			// depending on the card, rendering a storage object vs a service object is different. 
		}



		public enum WindowState
		{
			CREATE_SERVICE,
			CREATE_STORAGE,
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
			var clickedRefresh = false;
			var clickedHelp = false;
			var clickedConfig = false;
			var clickedPublish = false;
			
			{ // draw button strip
				EditorGUILayout.BeginHorizontal(new GUIStyle(), GUILayout.ExpandWidth(true), GUILayout.MinHeight(35));


				if (state != WindowState.MIGRATE)
				{ // draw the left buttons
					if (state == WindowState.SETTINGS)
					{
						clickedConfig = BeamGUI.HeaderButton("Services", iconService);
					}
					else
					{
						clickedConfig = BeamGUI.HeaderButton("Config", EditorGUIUtility.FindTexture("Settings"));
					}

					clickedPublish =
						BeamGUI.HeaderButton("Publish", EditorGUIUtility.FindTexture("Profiler.GlobalIllumination"));


					GUI.enabled = state != WindowState.CREATE_SERVICE && state != WindowState.CREATE_STORAGE;
					clickedCreate = BeamGUI.HeaderButton("Create", EditorGUIUtility.FindTexture("Toolbar Plus"));
					GUI.enabled = true;
				}

				EditorGUILayout.Space(1, true);

				{ // draw the right buttons
					clickedRefresh = BeamGUI.HeaderButton(null, EditorGUIUtility.FindTexture("Refresh"),
					                                      width: 30,
					                                      padding: 4,
					                                      drawBorder: false);

					clickedHelp = BeamGUI.HeaderButton(null, EditorGUIUtility.FindTexture("d__Help@2x"),
					                                   width: 30,
					                                   padding: 4,
					                                   drawBorder: false);
				}

				EditorGUILayout.Space(12, false);


				EditorGUILayout.EndHorizontal();
			}

			{ 
				var rect = new Rect(0, GUILayoutUtility.GetLastRect().yMax, position.width, 30);
				EditorGUILayout.BeginHorizontal(new GUIStyle()
				                                {
					                               
				                                }, GUILayout.ExpandWidth(true),
				                                GUILayout.Height(30));
				EditorGUI.DrawRect(rect, new Color(0, 0, 0, .6f));
				
				EditorGUILayout.Space(1, true);

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
					}

					EditorGUILayout.Space(4, false);

					BeamGUI.LayoutDropDown(this, new GUIContent("realm"), GUILayout.MaxWidth(80),
					                       ScriptableObject.CreateInstance<BeamGuiPopup>);
					EditorGUILayout.Space(4, false);
				}
				
				EditorGUILayout.EndHorizontal();
			}

			if (clickedRefresh)
			{
				activeMigration = null;
				usam.Reload();
			}

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

			if (clickedHelp)
			{
				throw new NotImplementedException("No help yet"); // TODO: 
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
				
				
				menu.ShowAsContext();
			}
		}
	}
}
