using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Tests.UI.Buss.BussCascadeTests
{
   public class CascadeTests : BUSSTest
   {
      [Test]
      public void CheckThatChildrenInheritStyles()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = parent.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            })
         };
         SetMockFallback(sheet);

         BussCascade.Cascade(parent);

         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNotNull(b.InheritedStyles);

         Assert.IsNotNull(parent.DirectStyles);
         Assert.IsNull(a.DirectStyles);
         Assert.IsNull(b.DirectStyles);

         Assert.IsNotNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNotNull(b.ComputedStyles);

         Assert.IsTrue(parent.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, parent.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, a.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(b.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, b.ComputedStyles.Background.Color.Value);
      }

      [Test]
      public void CanApplyAtEndOfOfLineage()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = parent.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            })
         };
         SetMockFallback(sheet);

         BussCascade.Cascade(a);

         // nothing happens to parent, or b, because they aren't in the child-path of a.
         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNull(b.InheritedStyles);

         Assert.IsNull(parent.DirectStyles);
         Assert.IsNull(a.DirectStyles);
         Assert.IsNull(b.DirectStyles);

         Assert.IsNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNull(b.ComputedStyles);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, a.ComputedStyles.Background.Color.Value);

      }

      [Test]
      public void CanApplyInMiddleOfOfLineage()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = a.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            })
         };
         SetMockFallback(sheet);

         BussCascade.Cascade(a);

         // nothing happens to parent, because it isn't in the child-path of a.
         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNotNull(b.InheritedStyles);

         Assert.IsNull(parent.DirectStyles);
         Assert.IsNull(a.DirectStyles);
         Assert.IsNull(b.DirectStyles);

         Assert.IsNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNotNull(b.ComputedStyles);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, a.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(b.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, b.ComputedStyles.Background.Color.Value);
      }

      /// <summary>
      /// The tree should be
      /// Root
      /// + a
      /// + b
      ///
      /// There is a global style sheet that defines a property on #root, which would cascade to #a and #b.
      /// However, there is also a style sheet on #a that defines a rule on #root.
      ///   There are now two rules for #root in #a's presence, and it should use the one defined by its own style sheet.
      /// </summary>
      [Test]
      public void ChildElementCanHaveItsOwnStyleSheet()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = parent.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            })
         };
         SetMockFallback(sheet);

         var aSheet1 = ScriptableObject.CreateInstance<StyleSheetObject>();
         aSheet1.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.blue)
               }
            })
         };
         a.StyleSheets.Add(aSheet1);

         BussCascade.Cascade(parent);

         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNotNull(b.InheritedStyles);

         Assert.IsNotNull(parent.DirectStyles);
         Assert.IsNull(a.DirectStyles);
         Assert.IsNull(b.DirectStyles);

         Assert.IsNotNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNotNull(b.ComputedStyles);

         Assert.IsTrue(parent.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, parent.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.blue, a.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(b.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, b.ComputedStyles.Background.Color.Value);
      }

      [Test]
      public void GrandChildCanInheritFromParentSheet()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = a.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            })
         };
         SetMockFallback(sheet);

         var aSheet1 = ScriptableObject.CreateInstance<StyleSheetObject>();
         aSheet1.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#a", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.blue)
               }
            })
         };
         a.StyleSheets.Add(aSheet1);

         BussCascade.Cascade(parent);

         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNotNull(b.InheritedStyles);

         Assert.IsNotNull(parent.DirectStyles);
         Assert.IsNotNull(a.DirectStyles);
         Assert.IsNull(b.DirectStyles);

         Assert.IsNotNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNotNull(b.ComputedStyles);

         Assert.IsTrue(parent.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, parent.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.blue, a.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(b.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.blue, b.ComputedStyles.Background.Color.Value);
      }

      [Test]
      public void OneChildCanOverrideProperty()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = parent.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            }),
            SelectorWithStyle.Create("#a", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.blue)
               }
            })
         };
         SetMockFallback(sheet);

         BussCascade.Cascade(parent);

         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNotNull(b.InheritedStyles);

         Assert.IsNotNull(parent.DirectStyles);
         Assert.IsNotNull(a.DirectStyles);
         Assert.IsNull(b.DirectStyles);

         Assert.IsNotNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNotNull(b.ComputedStyles);

         Assert.IsTrue(parent.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, parent.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.blue, a.ComputedStyles.Background.Color.Value);
         Assert.IsTrue(a.InheritedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, a.InheritedStyles.Background.Color.Value);

         Assert.IsTrue(b.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, b.ComputedStyles.Background.Color.Value);
      }

      [Test]
      public void CheckThatGrandChildCanOverrideWithGap()
      {
         var parent = CreateElement<ImageStyleBehaviour>("root");
         var a = parent.CreateElement<ImageStyleBehaviour>("a");
         var b = a.CreateElement<ImageStyleBehaviour>("b");

         var sheet = ScriptableObject.CreateInstance<StyleSheetObject>();
         sheet.Rules = new List<SelectorWithStyle>
         {
            SelectorWithStyle.Create("#root", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
               }
            }),
            SelectorWithStyle.Create("#root #b", new StyleObject
            {
               Background = new BackgroundBussProperty
               {
                  Enabled = true, Color = OptionalColor.CreateInstance<OptionalColor>(Color.blue)
               }
            })
         };
         SetMockFallback(sheet);

         BussCascade.Cascade(parent);

         Assert.IsNull(parent.InheritedStyles);
         Assert.IsNotNull(a.InheritedStyles);
         Assert.IsNotNull(b.InheritedStyles);

         Assert.IsNotNull(parent.DirectStyles);
         Assert.IsNull(a.DirectStyles);
         Assert.IsNotNull(b.DirectStyles);

         Assert.IsNotNull(parent.ComputedStyles);
         Assert.IsNotNull(a.ComputedStyles);
         Assert.IsNotNull(b.ComputedStyles);

         Assert.IsTrue(parent.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, parent.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(a.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, a.ComputedStyles.Background.Color.Value);

         Assert.IsTrue(b.InheritedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, b.InheritedStyles.Background.Color.Value);
         Assert.IsTrue(b.ComputedStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.blue, b.ComputedStyles.Background.Color.Value);
      }
   }
}