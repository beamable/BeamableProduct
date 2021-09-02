using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Tests.UI.Buss.StyleBehaviourTests
{
   public class ComputeStyleObjectTests : BUSSTest
   {

      /// <summary>
      /// (div #a)
      ///
      /// #a { background-color: red; }
      /// div { color: green; }
      ///
      /// Should result in the div#a element having the red background and green foreground. The style rules should combine to produce one style object.
      /// </summary>
      [Test]
      public void CombineBackgroundStyles()
      {
         var sheet = new StyleSheetObject();
         sheet.Rules = new List<SelectorWithStyle>
         {
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("#a"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Enabled = true,
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            },
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("img"),
               Style = new StyleObject
               {
                  Color = new ColorBussProperty()
                  {
                     Enabled = true,
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.green)
                  }
               }
            }
         };
         SetMockFallback(sheet);

         var parent = CreateElement<ImageStyleBehaviour>("a");

         var styles = parent.ComputeStyleObject();
         Assert.IsTrue(styles.AnyDefinition);
         Assert.IsTrue(styles.Background.HasAnyStyles);
         Assert.IsTrue(styles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, styles.Background.Color.Value);

         Assert.IsTrue(styles.Color.HasAnyStyles);
         Assert.IsTrue(styles.Color.Color.HasValue);
         Assert.AreEqual(Color.green, styles.Color.Color.Value);
      }

      /// <summary>
      /// (div #a)
      ///
      /// #a { background-color: red; }
      /// div { background-color: green; }
      ///
      /// Should result in the div#a element having the red background. The #a selector is more specific, and should be used over the div selector.
      /// </summary>
      [Test]
      public void SelectorWeight()
      {
         var sheet = new StyleSheetObject();
         sheet.Rules = new List<SelectorWithStyle>
         {
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("#a"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Enabled = true,
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            },
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("img"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Enabled = true,
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.green)
                  }
               }
            }
         };
         SetMockFallback(sheet);

         var parent = CreateElement<ImageStyleBehaviour>("a");

         var styles = parent.ComputeStyleObject();
         Assert.IsTrue(styles.AnyDefinition);
         Assert.IsTrue(styles.Background.HasAnyStyles);
         Assert.IsTrue(styles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, styles.Background.Color.Value);
      }

      /// <summary>
      /// (div #a)
      ///    (text #b)
      ///
      /// img#a { color: red; background-color: red }
      /// text { color: green }
      ///
      /// The child element should prefer the green color, because it comes from a direct match style. The inherited styles can apply, but should be secondary to targeted styles
      /// </summary>
      [Test]
      public void TargetedStylesAlwaysOutweighInherited()
      {
         var sheet = new StyleSheetObject();
         sheet.Rules = new List<SelectorWithStyle>
         {
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("img#a"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Enabled = true,
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  },
                  Color = new ColorBussProperty()
                  {
                     Enabled = true,
                     Color =OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            },
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("text"),
               Style = new StyleObject
               {
                  Color = new ColorBussProperty()
                  {
                     Enabled = true,
                     Color =OptionalColor.CreateInstance<OptionalColor>(Color.green)
                  }
               }
            }
         };
         SetMockFallback(sheet);

         var parent = CreateElement<ImageStyleBehaviour>("a");
         var child = parent.CreateElement<TextStyleBehaviour>("b");

         var styles = child.ComputeStyleObject();
         var parentStyles = parent.ComputeStyleObject();
         Assert.IsTrue(parentStyles.AnyDefinition);
         Assert.IsTrue(parentStyles.Background.HasAnyStyles);
         Assert.IsTrue(parentStyles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, parentStyles.Background.Color.Value);
         Assert.IsTrue(parentStyles.Color.HasAnyStyles);
         Assert.IsTrue(parentStyles.Color.Color.HasValue);
         Assert.AreEqual(Color.red, parentStyles.Color.Color.Value);

         Assert.IsTrue(styles.AnyDefinition);
         Assert.IsTrue(styles.Background.HasAnyStyles);
         Assert.IsTrue(styles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, styles.Background.Color.Value);
         Assert.IsTrue(styles.Color.HasAnyStyles);
         Assert.IsTrue(styles.Color.Color.HasValue);
         Assert.AreEqual(Color.green, styles.Color.Color.Value);
      }

      /// <summary>
      /// (div #a)
      ///    (div #b)
      ///
      /// #a { background-color: red; }
      ///
      /// Should result in the div#b element having a red background color. The parent style should propagate to children.
      /// </summary>
      [Test]
      public void InheritedStyles()
      {
         var sheet = new StyleSheetObject();
         sheet.Rules = new List<SelectorWithStyle>
         {
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("#a"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Enabled = true,
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            }
         };
         SetMockFallback(sheet);

         var parent = CreateElement<ImageStyleBehaviour>("a");
         var child = parent.CreateElement<ImageStyleBehaviour>("b");

         var styles = child.ComputeStyleObject();
         Assert.IsTrue(styles.AnyDefinition);
         Assert.IsTrue(styles.Background.HasAnyStyles);
         Assert.IsTrue(styles.Background.Color.HasValue);
         Assert.AreEqual(Color.red, styles.Background.Color.Value);
      }
   }
}