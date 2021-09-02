using System;
using UnityEngine;

namespace Beamable.Installer.Editor
{
   public class BeamableInstallerReadme : ScriptableObject
   {
      public Texture2D icon;
      public string title;
      public Section[] sections;
      public bool loadedLayout;

      [Serializable]
      public class Section
      {
         public bool onlyShowWhenInstalled, onlyShowWhenNotInstalled;
         public string heading, text, linkText, url;
         public InstallerActionType Action;
         public string ActionText;
         public bool IncludeRightClickOptions;
      }
   }

   public enum InstallerActionType
   {
      None, Install, Remove, OpenToolbox
   }
}