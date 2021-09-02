using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#elif UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#endif

namespace Beamable.Editor.Config
{
   public static class BeamableSettingsProvider
   {

      [MenuItem(
         BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE + "/" +
         BeamableConstants.OPEN + " " +
         BeamableConstants.CONFIG_MANAGER,
         priority = BeamableConstants.MENU_ITEM_PATH_WINDOW_PRIORITY_2
      )]
      public static void Open()
      {
         ConfigManager.Initialize(forceCreation: true);
         SettingsService.OpenProjectSettings("Project/Beamable");
      }

      [SettingsProvider]
      public static SettingsProvider CreateBeamableProjectSettings()
      {
         try
         {
            var provider = new SettingsProvider($"Project/Beamable", SettingsScope.Project)
            {
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
                        {
                           text = "Create Beamable Config Files"
                        };
                        var missingConfigs =
                           string.Join(",\n",ConfigManager.MissingConfigurations.Select(d => $" - {d.Name}"));
                        var lbl = new Label()
                        {
                           text = $"Welcome to Beamable! These configurations need to be created:\n{missingConfigs}"
                        };
                        lbl.AddTextWrapStyle();
                        rootElement.Add(lbl);
                        rootElement.Add(createButton);
                     }
                     var options = ConfigManager.GenerateOptions();

                     var scroller = new ScrollView();
                     rootElement.AddStyleSheet($"{ConfigWindowConstants.BASE_PATH}/ConfigWindow.uss");
                     rootElement.Add(scroller);

                     ConfigWindow.CreateFields(scroller, null, options, true);
                  }
                  catch (Exception)
                  {
                     // try to reset the assets.
                     AssetDatabase.Refresh();
                  }
               },
               keywords = new HashSet<string>(new[] {"Beamable"})
            };

            return provider;
         }
         catch (Exception)
         {
            return null;
         }
      }

      [SettingsProviderGroup]
      public static SettingsProvider[] CreateBeamableProjectModuleSettings()
      {
         try
         {
            ConfigManager.Initialize(); // re-initialize every time the window is activated, so that we make sure the SO's always exist.

            var providers = ConfigManager.ConfigObjects.Select(config =>
            {
               var options = ConfigManager.GenerateOptions(config);
               var provider = new SettingsProvider($"Project/Beamable/{options[0].Module}", SettingsScope.Project)
               {
                  activateHandler = (searchContext, rootElement) =>
                  {
                     options = ConfigManager.GenerateOptions(config);
                     var scroller = new ScrollView();
                     rootElement.AddStyleSheet($"{ConfigWindowConstants.BASE_PATH}/ConfigWindow.uss");
                     rootElement.Add(scroller);
                     ConfigWindow.CreateFields(scroller, null, options, false);
                  },

                  keywords = new HashSet<string>(options.Select(o => o.Name))
               };

               return provider;
            }).ToArray();

            return providers;
         }
         catch (Exception)
         {
            return null;
         }
      }
   }
}