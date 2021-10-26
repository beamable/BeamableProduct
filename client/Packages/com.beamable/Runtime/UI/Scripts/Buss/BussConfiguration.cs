using System;
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
      public event Action OnUpdate;
      
      public static BussConfiguration Instance => Get<BussConfiguration>();
      
      [SerializeField] private SDFStyleConfig _globalStyleConfig;

      public SDFStyle GetStyle(string id, SDFStyleConfig canvasConfig, SDFStyleConfig objectConfig)
      {
         SDFStyle newStyle = new SDFStyle();
         
         SingleStyleObject _globalContext = Instance.GetStyleById(id, _globalStyleConfig);
         foreach (KeyWithProperty property in _globalContext.Properties)
         {
            newStyle[property.key] = property.property.Get<ISDFProperty>();
         }
         
         SingleStyleObject _canvasContext = Instance.GetStyleById(id, canvasConfig);
         foreach (KeyWithProperty property in _canvasContext.Properties)
         {
            newStyle[property.key] = property.property.Get<ISDFProperty>();
         }
         
         SingleStyleObject _objectContext = Instance.GetStyleById(id, objectConfig);
         foreach (KeyWithProperty property in _objectContext.Properties)
         {
            newStyle[property.key] = property.property.Get<ISDFProperty>();
         }

         return newStyle;
      }
      
      private SingleStyleObject GetStyleById(string id, SDFStyleConfig config)
      {
         if (config == null)
         {
            return new SingleStyleObject();
         }
         
         SingleStyleObject styleObject = config.Styles.Find(style => style.Name == id);
         return styleObject ?? new SingleStyleObject();
      }

      public void InformAboutChange()
      {
         OnUpdate?.Invoke();
      }
   }
}