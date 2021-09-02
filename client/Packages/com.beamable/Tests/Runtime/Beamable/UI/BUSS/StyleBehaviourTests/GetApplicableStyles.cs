using System.Collections.Generic;
using Beamable.UI.Buss;
using Beamable.UI.Buss.Properties;
using NUnit.Framework;
using UnityEngine;

namespace Beamable.Tests.UI.Buss.StyleBehaviourTests
{
   public class GetApplicableStyles : BUSSTest
   {
      [Test]
      public void NoStyles_FromNoSheets()
      {
         SetMockFallback(new StyleSheetObject());

         var elem = CreateElement<ImageStyleBehaviour>();
         var styles = elem.GetApplicableStyles();

         Assert.AreEqual(1, styles.Count); // the 1 inline style
         Assert.AreEqual(elem.InlineStyle, styles[0].Rule.Style);
      }

      [Test]
      public void GetOneStyle_FromDirectMatch()
      {
         var sheet = new StyleSheetObject();
         sheet.Rules = new List<SelectorWithStyle>
         {
            new SelectorWithStyle
            {
               Selector = SelectorParser.Parse("img"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            }
         };
         SetMockFallback(sheet);

         var elem = CreateElement<ImageStyleBehaviour>();
         var styles = elem.GetApplicableStyles();

         Assert.AreEqual(2, styles.Count); // 1 rule, plus the 1 inline style
      }

      [Test]
      public void GetTwoStyles_FromDirectMatch()
      {
         var sheet = new StyleSheetObject();
         sheet.Rules = new List<SelectorWithStyle>
         {
            new SelectorWithStyle
            {
               Selector = SelectorParser.Parse("img"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            },
            new SelectorWithStyle()
            {
               Selector = SelectorParser.Parse("#a"),
               Style = new StyleObject
               {
                  Background = new BackgroundBussProperty
                  {
                     Color = OptionalColor.CreateInstance<OptionalColor>(Color.red)
                  }
               }
            }
         };
         SetMockFallback(sheet);

         var elem = CreateElement<ImageStyleBehaviour>("a");
         var styles = elem.GetApplicableStyles();

         Assert.AreEqual(3, styles.Count); // 2 rules, plus the 1 inline style

         Assert.AreEqual("inline", styles[0].Selector.ToString()); // inline is always first
         Assert.AreEqual("#a", styles[1].Selector.ToString()); // id is more specific than type...
         Assert.AreEqual("img", styles[2].Selector.ToString());
      }


   }
}