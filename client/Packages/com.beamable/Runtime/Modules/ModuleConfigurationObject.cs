using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Beamable
{
   public interface IConfigurationConstants
   {

      string GetSourcePath(Type type);
   }

   public class BeamableConfigurationConstants : IConfigurationConstants
   {
      private const string PACKAGE_EDITOR_DIR = "Packages/com.beamable/Editor/Config";

      public string GetSourcePath(Type type)
      {
         var name = type.Name;
         var sourcePath = $"{PACKAGE_EDITOR_DIR}/{name}.asset";
         return sourcePath;
      }
   }

   public abstract class BaseModuleConfigurationObject : ScriptableObject
   {
      /// <summary>
      /// Called by the Configuration.Instance spawn function, the FIRST time the configuration is copied from the Beamable package into the /Assets
      /// </summary>
      public virtual void OnFreshCopy()
      {

      }
   }

   public abstract class AbsModuleConfigurationObject<TConstants> : BaseModuleConfigurationObject
      where TConstants : IConfigurationConstants, new()
   {
      private const string CONFIG_RESOURCES_DIR = "Assets/Beamable/Resources";
      private static Dictionary<Type, BaseModuleConfigurationObject> _typeToConfig = new Dictionary<Type, BaseModuleConfigurationObject>();

      public static bool Exists<TConfig>() where TConfig : BaseModuleConfigurationObject
      {
         var type = typeof(TConfig);
         if (_typeToConfig.TryGetValue(type, out var existingData) && existingData && existingData != null)
         {
            return true;
         }

         var name = type.Name;
         var data = Resources.Load<TConfig>(name);
         return data != null;
      }

      public static TConfig Get<TConfig>() where TConfig : BaseModuleConfigurationObject
      {
         var type = typeof(TConfig);
         if (_typeToConfig.TryGetValue(type, out var existingData) && existingData && existingData != null)
         {
            return existingData as TConfig;
         }

         var constants = new TConstants();
         var name = type.Name;

         var data = Resources.Load<TConfig>(name);
#if UNITY_EDITOR
         if (data == null)
         {

            var sourcePath = constants.GetSourcePath(type);

            if (!File.Exists(sourcePath))
            {
               throw new Exception($"No module configuration exists at {sourcePath}. Please create it.");
            }

            Directory.CreateDirectory(CONFIG_RESOURCES_DIR);
            var sourceData = File.ReadAllText(sourcePath);
            var assetPath = $"{CONFIG_RESOURCES_DIR}/{name}.asset";
            File.WriteAllText(assetPath, sourceData);
            UnityEditor.AssetDatabase.Refresh();
            data = Resources.Load<TConfig>(name) ?? AssetDatabase.LoadAssetAtPath<TConfig>(assetPath);
            if (data == null)
            {
               throw new ModuleConfigurationNotReadyException(typeof(TConfig));
            }
            data.OnFreshCopy();

            EditorUtility.SetDirty(data);
            SettingsService.NotifySettingsProviderChanged();
         }
#endif
         _typeToConfig[type] = data;
         return data;
      }

   }

   public class ModuleConfigurationNotReadyException : Exception
   {
      public ModuleConfigurationNotReadyException(Type type) : base($"Configuration of type=[{type.Name}] is not available yet.")
      {

      }

   }
   public class ModuleConfigurationObject : AbsModuleConfigurationObject<BeamableConfigurationConstants>
   {

   }

}