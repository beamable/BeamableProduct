
using Beamable.Tests.Runtime;
using Beamable.UI.Buss;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Tests.UI.Buss
{
   public class BUSSTest
   {
      [SetUp]
      public void RegisterTypes()
      {
         // need to make sure that all the static constructors have run within the test space.
         new TextStyleBehaviour();
         new ImageStyleBehaviour();
         new ButtonStyleBehaviour();
      }

      public static T CreateElement<T>(string id=null) where T : StyleBehaviour
      {
         var gob = new GameObject(id ?? "NewGameObject");
         return gob.AddComponent<T>();
      }

      public static void SetMockConfig(BussConfiguration config)
      {
         MockConfigurationHelper.Mock(config);
      }

      public static void SetMockFallback(StyleSheetObject fallback)
      {
         var config = new BussConfiguration();
         config.FallbackSheet = fallback;
         SetMockConfig(config);
      }
   }

   public static class BussTestExtensions
   {
      public static T CreateElement<T>(this StyleBehaviour parent, string id = null) where T : StyleBehaviour
      {
         var child = BUSSTest.CreateElement<T>(id);
         child.transform.SetParent(parent.transform);

         return child;
      }
   }
}