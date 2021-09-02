
using Beamable.UI.Buss;
using NUnit.Framework;

namespace Beamable.Tests.UI.Buss.StyleBehaviourTests
{
   public class IsElementTypeName : BUSSTest
   {
      [Test]
      public void IsTextTypeAvailable()
      {
         var expectedType = "text";
         var isAvailable = StyleBehaviour.IsElementTypeName(expectedType, out var typeName);

         Assert.IsTrue(isAvailable);
         Assert.AreEqual(expectedType, typeName);
      }

   }
}