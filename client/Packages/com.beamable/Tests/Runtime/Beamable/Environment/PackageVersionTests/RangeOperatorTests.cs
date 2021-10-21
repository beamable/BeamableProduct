using Beamable.Common;
using NUnit.Framework;

namespace Beamable.Tests.Runtime.Environment.PackageVersionTests
{
   public class RangeOperatorTests
   {
      [Test]
      public void AreEqual()
      {
         var a = PackageVersion.FromSemanticVersionString("1.3.2");
         var b = PackageVersion.FromSemanticVersionString("1.3.2");

         Assert.AreEqual(true, a == b);
      }

      [Test]
      public void AreNotEqual()
      {
         var a = PackageVersion.FromSemanticVersionString("1.0.0");
         var b = PackageVersion.FromSemanticVersionString("1.3.0");

         Assert.AreEqual(true, a != b);
      }

      [Test]
      public void MajorMoreThan()
      {
         var a = PackageVersion.FromSemanticVersionString("1.0.0");
         var b = PackageVersion.FromSemanticVersionString("2.0.0");

         Assert.AreEqual(false, a > b);
         Assert.AreEqual(true, b > a);
         Assert.AreEqual(false, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void MinorMoreThan()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.0");
         var b = PackageVersion.FromSemanticVersionString("2.18.0");

         Assert.AreEqual(false, a > b);
         Assert.AreEqual(true, b > a);
         Assert.AreEqual(false, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void PatchMoreThan()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.2");
         var b = PackageVersion.FromSemanticVersionString("2.4.53");

         Assert.AreEqual(false, a > b);
         Assert.AreEqual(true, b > a);
         Assert.AreEqual(false, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void MinorMoreThan_StoppedByMajor()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.0");
         var b = PackageVersion.FromSemanticVersionString("3.1.0");

         Assert.AreEqual(false, a > b);
         Assert.AreEqual(true, b > a);
         Assert.AreEqual(false, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void PatchMoreThan_StoppedByMajor()
      {
         var a = PackageVersion.FromSemanticVersionString("2.1.4");
         var b = PackageVersion.FromSemanticVersionString("3.1.2");
         Assert.AreEqual(false, a > b);
         Assert.AreEqual(true, b > a);
         Assert.AreEqual(false, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void PatchMoreThan_StoppedByMinor()
      {
         var a = PackageVersion.FromSemanticVersionString("2.1.4");
         var b = PackageVersion.FromSemanticVersionString("2.3.2");

         Assert.AreEqual(false, a > b);
         Assert.AreEqual(true, b > a);
         Assert.AreEqual(false, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void MajorLessThan()
      {
         var a = PackageVersion.FromSemanticVersionString("1.0.0");
         var b = PackageVersion.FromSemanticVersionString("2.0.0");

         Assert.AreEqual(true, a < b);
         Assert.AreEqual(false, b < a);
         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(false, b <= a);
      }

      [Test]
      public void MinorLessThan()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.0");
         var b = PackageVersion.FromSemanticVersionString("2.18.0");

         Assert.AreEqual(true, a < b);
         Assert.AreEqual(false, b < a);
         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(false, b <= a);
      }

      [Test]
      public void PatchLessThan()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.2");
         var b = PackageVersion.FromSemanticVersionString("2.4.53");

         Assert.AreEqual(true, a < b);
         Assert.AreEqual(false, b < a);
         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(false, b <= a);
      }

      [Test]
      public void MinorLessThan_StoppedByMajor()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.0");
         var b = PackageVersion.FromSemanticVersionString("3.1.0");

         Assert.AreEqual(true, a < b);
         Assert.AreEqual(false, b < a);
         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(false, b <= a);
      }

      [Test]
      public void PatchLessThan_StoppedByMajor()
      {
         var a = PackageVersion.FromSemanticVersionString("2.1.4");
         var b = PackageVersion.FromSemanticVersionString("3.1.2");

         Assert.AreEqual(true, a < b);
         Assert.AreEqual(false, b < a);
         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(false, b <= a);
      }

      [Test]
      public void PatchLessThan_StoppedByMinor()
      {
         var a = PackageVersion.FromSemanticVersionString("2.1.4");
         var b = PackageVersion.FromSemanticVersionString("2.3.2");

         Assert.AreEqual(true, a < b);
         Assert.AreEqual(false, b < a);
         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(false, b <= a);
      }

      [Test]
      public void GreaterThanOrEqualTo_Equal()
      {
         var a = PackageVersion.FromSemanticVersionString("2.1.4");
         var b = PackageVersion.FromSemanticVersionString("2.1.4");

         Assert.AreEqual(true, a >= b);
         Assert.AreEqual(true, b >= a);
      }

      [Test]
      public void LessThanOrEqualTo_Equal()
      {
         var a = PackageVersion.FromSemanticVersionString("2.1.4");
         var b = PackageVersion.FromSemanticVersionString("2.1.4");

         Assert.AreEqual(true, a <= b);
         Assert.AreEqual(true, b <= a);
      }

      [Test]
      public void Same_AreNotGreater()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.1");
         var b = PackageVersion.FromSemanticVersionString("2.4.1");

         Assert.AreEqual(false, a > b);
         Assert.AreEqual(false, b > a);
      }

      [Test]
      public void Same_AreNotLesser()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.1");
         var b = PackageVersion.FromSemanticVersionString("2.4.1");

         Assert.AreEqual(false, a < b);
         Assert.AreEqual(false, b < a);
      }

      [Test]
      public void PatchWildCard()
      {
         var a = PackageVersion.FromSemanticVersionString("2.4.x");
         var mid = PackageVersion.FromSemanticVersionString("2.4.5");
         var min = PackageVersion.FromSemanticVersionString("2.4.1");
         var max = PackageVersion.FromSemanticVersionString("2.4.9");
         var inclusiveMin = PackageVersion.FromSemanticVersionString("2.4.0");
         var inclusiveMax = PackageVersion.FromSemanticVersionString("2.5.0");

         Assert.AreEqual(true, a < max);
         Assert.AreEqual(true, a <= max);
         Assert.AreEqual(true, a < mid);
         Assert.AreEqual(true, a <= mid);
         Assert.AreEqual(true, a < inclusiveMax);
         Assert.AreEqual(true, a <= inclusiveMax);

         Assert.AreEqual(false, max < a);
         Assert.AreEqual(true, max <= a);
         Assert.AreEqual(true, mid < a);
         Assert.AreEqual(true, mid <= a);
         Assert.AreEqual(true, inclusiveMax < a);
         Assert.AreEqual(true, inclusiveMax <= a);

         Assert.AreEqual(false, a > max);
         Assert.AreEqual(false, a >= max);
         Assert.AreEqual(false, a > mid);
         Assert.AreEqual(false, a >= mid);
         Assert.AreEqual(false, a > inclusiveMax);
         Assert.AreEqual(false, a >= inclusiveMax);

      }
   }
}