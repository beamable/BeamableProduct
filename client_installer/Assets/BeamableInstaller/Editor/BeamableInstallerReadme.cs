using System;
using UnityEngine;

namespace Beamable.Installer.Editor
{
   public class BeamableInstallerReadme : ScriptableObject
   {
      public string semver;
      public Texture2D icon;
      public string title;
      public Section[] sections;
      public bool loadedLayout;

      public Material headerMaterial;
      public Texture2D headerBackground;
      
      [Serializable]
      public class Section
      {
         public string heading, text, linkText, url;
         public InstallerActionType Action;
      }
   }

   public enum InstallerActionType
   {
      None, Install, Remove, OpenToolbox
   }

   public static class SectionHelper
   {
      public static bool ShouldShow(this BeamableInstallerReadme.Section section)
      {
         var packageVersionInstalled = BeamableInstaller.InstalledVersion;
         switch (section.Action)
         {
            case InstallerActionType.Install:
               return string.IsNullOrEmpty(packageVersionInstalled);
            case InstallerActionType.Remove:
            case InstallerActionType.OpenToolbox:
               return !string.IsNullOrEmpty(packageVersionInstalled);
            default:
               return true;
         }
      }

      public static string GetActionText(this BeamableInstallerReadme.Section section)
      {
         switch (section.Action)
         {
            case InstallerActionType.Install:
               return "Install Beamable SDK";
            case InstallerActionType.Remove:
               return "Remove Installer";
            case InstallerActionType.OpenToolbox:
               return "Open Login Window";
            default:
               return string.Empty;
         }
      }
   }
}