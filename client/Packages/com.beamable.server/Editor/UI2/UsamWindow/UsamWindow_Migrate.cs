using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{
		public ActiveMigration activeMigration;

		public Vector2 migrateScrollPosition;

		private const string MIGRATION_DOCS_URL = "https://docs.beamable.com/v2.0.0/docs/migrating-from-beamable-119x";
		
		
		void DrawMigrate()
		{

			// EditorGUILayout.BeginVertical();
			
			migrateScrollPosition = EditorGUILayout.BeginScrollView(migrateScrollPosition);

			{ // draw explanation text
				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					margin = new RectOffset(4, 4, 4, 12),
					padding = new RectOffset(6, 6, 6, 6)
				});

				{
					// draw title
					const string notice =
						"Welcome to the new version of Beamable Microservices!";
					EditorGUILayout.LabelField(notice, new GUIStyle(EditorStyles.largeLabel)
					{
						fontSize = 20,
						wordWrap = true,
					});
				}

				if (activeMigration == null || !activeMigration.isComplete)
				{ // the CTA before the migration starts, and while it is going.
					{
						// draw text
						const string notice =
							"Before you can continue, you must migrate or delete your existing Microservices and " +
							"Storage Objects. Your current services are Unity Assembly Definitions and .cs class files. " +
							"However, in the newer version of Beamable, services are separate dotnet projects that live " +
							"alongside your <i>/Assets</i> folder";
						EditorGUILayout.LabelField(notice, new GUIStyle(EditorStyles.label)
						{
							fixedHeight = 0, margin = new RectOffset(2, 2, 8, 8), wordWrap = true, richText = true,
							// fontSize = 14
						});
					}

					{
						// draw cta
						const string notice =
							"Your services will automatically be migrated when you click the <i>Migrate Services</i> button. " +
							"Before you migrate, please make sure you have a backup of your services. " +
							"Learn about the migration process by checking our documentation. ";
						EditorGUILayout.LabelField(notice, new GUIStyle(EditorStyles.label)
						{
							fixedHeight = 0, margin = new RectOffset(2, 2, 8, 8), wordWrap = true, richText = true,
						});
						var docsClick = EditorGUILayout.LinkButton("Read the docs");
						if (docsClick)
						{
							AddDelayedAction(() =>
							{
								Application.OpenURL(MIGRATION_DOCS_URL);
							});
						}
						
					
					}
				} else if (activeMigration.isComplete)
				{ // CTA for post-migration
					const string notice =
						"Your services have been migrated. The service files exist in the <i>/BeamableServices</i> folder. ";
					EditorGUILayout.LabelField(notice, new GUIStyle(EditorStyles.label)
					{
						fixedHeight = 0, margin = new RectOffset(2, 2, 8, 8), wordWrap = true, richText = true,
					});
					
					
					var docsClick = EditorGUILayout.LinkButton("Read the docs");
					if (docsClick)
					{
						AddDelayedAction(() =>
						{
							Application.OpenURL(MIGRATION_DOCS_URL);
						});
					}
				}

				EditorGUILayout.EndVertical();
				
				
				// if action is required, we should call that out!
				if (usam.migrationPlan.manualSteps.Count > 0)
				{
					
					var sb = new StringBuilder();
					if (activeMigration == null || !activeMigration.isComplete)
					{
						sb.AppendLine(
							"The migration will not be able to automatically perform every step. The following list of actions you must address after the migration. ");
					}
					else {
						sb.AppendLine("The migration was not able to automatically perform every step. The following list of actions you must address. ");
					}
					foreach (var action in usam.migrationPlan.manualSteps)
					{
						sb.AppendLine("\n");
						sb.AppendLine(action);
					}
					EditorGUILayout.HelpBox(sb.ToString(), MessageType.Warning);
				}
			}

			
			{ // draw storages that will migrate
				
				for (var i = 0; i < usam.migrationPlan.storages.Count; i++)
				{
					EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
					{
						
						var service = usam.migrationPlan.storages[i];
						var migratingService = activeMigration?.services?.FirstOrDefault(x => x.name == service.beamoId);

						const int foldoutLoadingPadding = 35;
						var loadingRect = GUILayoutUtility.GetRect(GUIContent.none, new GUIStyle
						                                           {
							                                           margin = new RectOffset(0, 38, 2, 4)
						                                           },
						                                           GUILayout.ExpandWidth(true), GUILayout.Height(6));
						loadingRect = new Rect(loadingRect.x + foldoutLoadingPadding, loadingRect.y, loadingRect.width - foldoutLoadingPadding,
						                       loadingRect.height);
						
						
						BeamGUI.LoadingRect(loadingRect, migratingService?.TotalRatio ?? 0f, animate: (!(migratingService?.isComplete ?? true)));
						
						service.isFoldOut = EditorGUILayout.Foldout(service.isFoldOut, new GUIContent(service.beamoId, BeamGUI.iconStorage));
						
						if (service.isFoldOut)
						{
							// draw the steps
							EditorGUILayout.BeginVertical(new GUIStyle()
							{
								margin = new RectOffset(2, 0, 8, 0)
							});

							for (var stepIndex = 0; stepIndex < service.stepNames.Count; stepIndex++)
							{
								var value = 0f;
								if (migratingService != null)
								{
									value = migratingService.stepRatios[stepIndex];
								}
								BeamGUI.DrawLoadingBar(service.stepNames[stepIndex], value);

							}
							EditorGUILayout.EndVertical();
						}
					}
					EditorGUILayout.EndVertical();
				}
			}

			{ // draw services that will migrate
				
				for (var i = 0; i < usam.migrationPlan.services.Count; i++)
				{
					EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox));
					{
						
						var service = usam.migrationPlan.services[i];
						var migratingService = activeMigration?.services?.FirstOrDefault(x => x.name == service.beamoId);

						const int foldoutLoadingPadding = 35;
						var loadingRect = GUILayoutUtility.GetRect(GUIContent.none, new GUIStyle
						                                           {
							                                           margin = new RectOffset(0, 38, 2, 4)
						                                           },
						                                           GUILayout.ExpandWidth(true), GUILayout.Height(6));
						loadingRect = new Rect(loadingRect.x + foldoutLoadingPadding, loadingRect.y, loadingRect.width - foldoutLoadingPadding,
						                       loadingRect.height);
						
						
						BeamGUI.LoadingRect(loadingRect, migratingService?.TotalRatio ?? 0f, animate: (!(migratingService?.isComplete ?? true)));
						
						service.isFoldOut = EditorGUILayout.Foldout(service.isFoldOut, new GUIContent(service.beamoId, BeamGUI.iconService), new GUIStyle(EditorStyles.foldout)
						{
							fontSize = 12
						});
						
						if (service.isFoldOut)
						{
							// draw the steps
							EditorGUILayout.BeginVertical(new GUIStyle()
							{
								margin = new RectOffset(2, 0, 8, 0)
							});

							for (var stepIndex = 0; stepIndex < service.stepNames.Count; stepIndex++)
							{
								var value = 0f;
								if (migratingService != null)
								{
									value = migratingService.stepRatios[stepIndex];
								}
								BeamGUI.DrawLoadingBar(service.stepNames[stepIndex], value);

							}
							EditorGUILayout.EndVertical();
						}
					}
					EditorGUILayout.EndVertical();
				}
			}

			{ // draw buttons
				EditorGUILayout.BeginHorizontal(new GUIStyle()
				{
					margin = new RectOffset(0, 0, 20, 10)
				});
				EditorGUILayout.Space(1, true);
				EditorGUILayout.Space(1, true);
				EditorGUILayout.Space(1, true);


				bool clickedMigrate = false;
				bool clickedOkay = false;
				if (activeMigration == null)
				{
					clickedMigrate = BeamGUI.PrimaryButton(new GUIContent("Migrate Services"));
				} else if (!activeMigration.isComplete)
				{
					GUI.enabled = false;
					BeamGUI.PrimaryButton(new GUIContent("Migrating"));
					GUI.enabled = true;
				}
				else
				{
					if (usam.migrationPlan.manualSteps.Count > 0)
					{
						if (BeamGUI.CancelButton("Open Solution"))
						{
							AddDelayedAction(() =>
							{
								usam.OpenSolution();
							});
						}
					}
					clickedOkay = BeamGUI.PrimaryButton(new GUIContent("View Services"));
				}

				if (clickedOkay)
				{
					AddDelayedAction(() =>
					{
						usam.migrationPlan = null;
						activeMigration = null;
						state = WindowState.NORMAL;
						AssetDatabase.Refresh();
					});
				}
				if (clickedMigrate)
				{
					AddDelayedAction(() =>
					{
						activeMigration = UsamMigrator.Migrate(usam.migrationPlan, usam, usam.Cli, usam._dispatcher);
					});
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();

		}
	}
}
