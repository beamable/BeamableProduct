using Beamable.Editor.Util;
using Beamable.Server.Editor.Usam;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Beamable.Editor.Microservice.UI2
{
	public partial class UsamWindow
	{
		public Vector2 upgradeScrollPosition;
		void DrawUpgrades()
		{
			EditorGUILayout.BeginVertical();
			upgradeScrollPosition = EditorGUILayout.BeginScrollView(upgradeScrollPosition);

			{
				// draw explanation text
				EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
				{
					margin = new RectOffset(4, 4, 4, 12),
					padding = new RectOffset(6, 6, 6, 6)
				});

				{
					// draw title
					const string notice =
						"Some Microservice Upgrades are Required";
					EditorGUILayout.LabelField(notice, new GUIStyle(EditorStyles.largeLabel)
					{
						fontSize = 20,
						wordWrap = true,
					});
				}
				
				{
					// draw text
					const string notice =
						"The Beamable SDK has found upgrades that need to be made to your services. " +
						"If you make changes to the Microservice files, please refresh this window. " +
						"Before making any changes, please make sure you <b>have a backup.</b>";
					EditorGUILayout.LabelField(notice, new GUIStyle(EditorStyles.label)
					{
						fixedHeight = 0, margin = new RectOffset(2, 2, 8, 8), wordWrap = true, richText = true,
					});
				}

				
				EditorGUILayout.EndVertical();
			}


			{ // render the actual upgrades...
				for (var i = 0; i < usam._requiredUpgrades.Count; i++)
				{
					var upgrades = usam._requiredUpgrades[i];

					EditorGUILayout.BeginVertical(new GUIStyle(EditorStyles.helpBox)
					{
						margin = new RectOffset(8, 8, 4, 12),
						padding = new RectOffset(6, 6, 6, 6)
					});
					
					// render the beamoId
					EditorGUILayout.LabelField(upgrades.beamoId, new GUIStyle(EditorStyles.largeLabel));


					// render the description for all the upgrades.
					for (var j = 0; j < upgrades.fileEdits.Count; j++)
					{
						var upgrade = upgrades.fileEdits[j];
						
						EditorGUILayout.LabelField(upgrade.description, new GUIStyle(EditorStyles.label)
						{
							fixedHeight = 0, margin = new RectOffset(2, 2, 8, 8), wordWrap = true, richText = true,
						});
					}
					
					EditorGUILayout.EndVertical();

				}
			}
			
			
			{ // draw buttons
				EditorGUILayout.BeginHorizontal(new GUIStyle()
				{
					margin = new RectOffset(0, 6, 20, 10)
				});
				EditorGUILayout.Space(1, true);
				EditorGUILayout.Space(1, true);
				EditorGUILayout.Space(1, true);


				bool clickedMigrate = false;
				if (usam._requiredUpgrades.Count > 0)
				{
					clickedMigrate = BeamGUI.PrimaryButton(new GUIContent("Fix All"));
				} else
				{
					GUI.enabled = false;
					BeamGUI.PrimaryButton(new GUIContent("Fixing..."));
					GUI.enabled = true;
				}
				
				if (clickedMigrate)
				{
					AddDelayedAction(() =>
					{
						usam.DoUpgrade("*");
					});
				}
				EditorGUILayout.EndHorizontal();
			}
			
			EditorGUILayout.EndScrollView();
			EditorGUILayout.EndVertical();

		}
	}
}
