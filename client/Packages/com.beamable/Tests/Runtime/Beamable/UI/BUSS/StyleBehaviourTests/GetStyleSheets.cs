using System.Linq;
using Beamable.UI.Buss;
using NUnit.Framework;

namespace Beamable.Tests.UI.Buss.StyleBehaviourTests
{
   public class GetStyleSheets : BUSSTest
   {
      [Test]
      public void CanMockConfiguration_ZeroSheets()
      {
         SetMockConfig(new BussConfiguration());

         var elem = CreateElement<ImageStyleBehaviour>();
         var sheets = elem.GetStyleSheets().ToList();

         Assert.AreEqual(0, sheets.Count());
      }

      [Test]
      public void CanMockConfiguration_Fallback()
      {
         var config = new BussConfiguration();
         config.FallbackSheet = new StyleSheetObject();

         SetMockConfig(config);

         var elem = CreateElement<ImageStyleBehaviour>();
         var sheets = elem.GetStyleSheets().ToList();

         Assert.AreEqual(1, sheets.Count());
         Assert.AreEqual(config.FallbackSheet, sheets[0]);
      }
   }
}