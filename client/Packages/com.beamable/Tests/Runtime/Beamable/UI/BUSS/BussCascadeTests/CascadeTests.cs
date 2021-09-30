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