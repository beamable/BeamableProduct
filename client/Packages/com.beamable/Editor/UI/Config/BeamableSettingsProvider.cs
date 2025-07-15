using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif
using static Beamable.Common.Constants;
using static Beamable.Common.Constants.Features.Config;
using Button = UnityEngine.UIElements.Button;

namespace Beamable.Editor.Config
{
	public static class BeamableSettingsProvider
	{
		[MenuItem(
			MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
			Commons.OPEN + " Project Settings"
			,
			priority = MenuItems.Windows.Orders.MENU_ITEM_PATH_WINDOW_PRIORITY_2
		)]
		public static void Open()
		{
			ConfigManager.Initialize(forceCreation: true);
			SettingsService.OpenProjectSettings("Project/Beamable");
		}
		
		static bool DoDrawDefaultInspector(SerializedObject obj)
		{
			EditorGUI.BeginChangeCheck();
			obj.UpdateIfRequiredOrScript();
			SerializedProperty iterator = obj.GetIterator();
			for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
			{
				if ("m_Script" == iterator.propertyPath) continue;
				EditorGUILayout.PropertyField(iterator, true);
			}
			obj.ApplyModifiedProperties();
			return EditorGUI.EndChangeCheck();
		}

		[SettingsProvider]
		public static SettingsProvider CreateBeamableProjectSettings()
		{
			try
			{
				var maxSize = 100f;
				List<UnityEditor.Editor> editors = new List<UnityEditor.Editor>();
				// var provider2 = new SettingsProvider("", SettingsScope.Project)
				var provider = new SettingsProvider($"Project/Beamable", SettingsScope.Project)
				{
					guiHandler = search =>
					{
						EditorGUILayout.BeginVertical(new GUIStyle
						{
							padding = new RectOffset(10, 4, 10, 4)
						});
						var oldWidth = EditorGUIUtility.labelWidth;
						try
						{
							EditorGUIUtility.labelWidth = maxSize + 18;
							foreach (var editor in editors)
							{
								EditorGUILayout.LabelField(new GUIContent(editor.target.name), EditorStyles.largeLabel);
								EditorGUI.indentLevel++;
								DoDrawDefaultInspector(editor.serializedObject);
								EditorGUI.indentLevel--;
								EditorGUILayout.Space(12, false);

							}
						}
						finally
						{
							EditorGUIUtility.labelWidth = oldWidth;
						}

						EditorGUILayout.EndVertical();
					},
					activateHandler = (searchContext, rootElement) =>
					{
						try
						{
							ConfigManager.Initialize(); // re-initialize every time the window is activated, so that we make sure the SO's always exist.


							if (ConfigManager.MissingAnyConfiguration)
							{
								var createButton = new Button(() =>
								{
									Open();
									SettingsService.NotifySettingsProviderChanged();
								})
								{ text = "Create Beamable Config Files" };
								var missingConfigs =
									string.Join(",\n", ConfigManager.MissingConfigurations.Select(d => $" - {d.Name}"));
								var lbl = new Label() { text = $"Welcome to Beamable! These configurations need to be created:\n{missingConfigs}" };
								// lbl.AddTextWrapStyle();
								rootElement.Add(lbl);
								rootElement.Add(createButton);
							}

							editors = ConfigManager.ConfigObjects.Select(UnityEditor.Editor.CreateEditor).ToList();
							var options = ConfigManager.GenerateOptions();
							maxSize = 0f;
							foreach (var option in options)
							{
								var size = EditorStyles.label.CalcSize(new GUIContent(option.Property.name));
								maxSize = Mathf.Max(maxSize, size.x);
							}

						}
						catch (Exception)
						{
							// try to reset the assets.
							AssetDatabase.Refresh();
						}
					},
					keywords = new HashSet<string>(new[] { "Beamable" })
				};

				return provider;
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
				return null;
			}
		}

		public static SettingsProvider[] provider;

		[SettingsProviderGroup]
		public static SettingsProvider[] CreateBeamableProjectModuleSettings()
		{
			DelayCall(false);

			void DelayCall(bool notifyIfFound)
			{
				if (!BeamEditor.IsInitialized)
				{
					EditorApplication.delayCall += () => DelayCall(true);
					return;
				}

				try
				{
					ConfigManager.Initialize(); // re-initialize every time the window is activated, so that we make sure the SO's always exist.

					List<SettingsProvider> providers = new List<SettingsProvider>();

					foreach (BaseModuleConfigurationObject config in ConfigManager.ConfigObjects)
					{
						var options = ConfigManager.GenerateOptions(config);

						if (options.Count == 0)
						{
							continue;
						}
						
						var editor = UnityEditor.Editor.CreateEditor(config);

						var maxSize = 0f;
						

						var settingsProvider = new SettingsProvider($"Project/Beamable/{options[0].Module}", SettingsScope.Project)
						{
							
							guiHandler = (searchContext) =>
							{
								if (maxSize < 1)
								{
									foreach (var option in options)
									{
										var size = EditorStyles.label.CalcSize(new GUIContent(option.Property.name));
										maxSize = Mathf.Max(maxSize, size.x);
									}
								}

								EditorGUILayout.BeginVertical(new GUIStyle
								{
									padding = new RectOffset(10, 4, 10, 4)
								});
								var oldWidth = EditorGUIUtility.labelWidth;
								try
								{
									EditorGUIUtility.labelWidth = maxSize + 18;
									DoDrawDefaultInspector(editor.serializedObject);
								}
								finally
								{
									EditorGUIUtility.labelWidth = oldWidth;
								}

								EditorGUILayout.EndVertical();
							},
						
							keywords = new HashSet<string>(options.Select(o => o.Name))
						};
						providers.Add(settingsProvider);
					}

					provider = providers.ToArray();
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}

				if (notifyIfFound)
					SettingsService.NotifySettingsProviderChanged();
			}

			return provider;
		}
	}
}
