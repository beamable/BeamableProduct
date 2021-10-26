using System.Collections.Generic;
using Beamable.UI.SDF;
using Beamable.UI.SDF.Styles;
using UnityEngine;

namespace Beamable.UI.Buss
{
   public class BussConfiguration : ModuleConfigurationObject
   {
      #region Old system

      public StyleSheetObject FallbackSheet;

      public List<StyleSheetObject> DefaultSheets = new List<StyleSheetObject>();

      public IEnumerable<StyleSheetObject> EnumerateSheets()
      {

         foreach (var sheet in DefaultSheets)
         {
            if (sheet != null)
            {
               yield return sheet;
            }
         }

         if (FallbackSheet != null)
         {
            yield return FallbackSheet;
         }
      }

      #endregion

      // New system
      public static BussConfiguration Instance => Get<BussConfiguration>();
      
      [SerializeField] private SDFStyleConfig _globalStyleConfig;
      [SerializeField] private List<SDFStyleProvider> _styleProviders = new List<SDFStyleProvider>();

      public void RegisterObserver(SDFStyleProvider styleProvider)
      {
         if (!_styleProviders.Contains(styleProvider))
         {
            _styleProviders.Add(styleProvider);
         }
      }

      public void UnregisterObserver(SDFStyleProvider styleProvider)
      {
         if (_styleProviders.Contains(styleProvider))
         {
            _styleProviders.Remove(styleProvider);
         }
      }

      public List<SingleStyleObject> GetGlobalStyles()
      {
         return _globalStyleConfig ? _globalStyleConfig.Styles : new List<SingleStyleObject>();
      }

      private void OnValidate()
      {
         if (_globalStyleConfig != null)
         {
            _globalStyleConfig.OnChange = OnGlobalStyleChanged;
         }
      }

      private void OnGlobalStyleChanged()
      {
         foreach (SDFStyleProvider styleProvider in _styleProviders)
         {
            styleProvider.NotifyOnStyleChanged();
         }
      }
   }
}