using Beamable.UI.Buss;
using NUnit.Framework;

namespace Beamable.Tests.UI.Buss.SelectorParserTests
{
   public class Parse : BUSSTest
   {

      [Test]
      public void CanParseType_Text()
      {
         var selector = SelectorParser.Parse("text");
         Assert.AreEqual("text", selector.ElementTypeConstraint);
      }

   }
}