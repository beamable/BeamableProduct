
using Beamable.UI.Buss;
using NUnit.Framework;

namespace Beamable.Tests.UI.Buss.StyleBehaviourTests
{
   public class MatchSelectorDistance : BUSSTest
   {
      [Test]
      public void MatchSelfByType_DistanceOfOne()
      {
         var imgBehaviour = CreateElement<ImageStyleBehaviour>();
         var selector = SelectorParser.Parse("img");
         var distance = imgBehaviour.MatchSelectorDistance(selector);
         Assert.AreEqual(1, distance);
      }

      [Test]
      public void MatchSelfWithEmptySelector_DistanceOfOne()
      {
         var imgBehaviour = CreateElement<ImageStyleBehaviour>();
         var selector = SelectorParser.Parse("");
         var distance = imgBehaviour.MatchSelectorDistance(selector);
         Assert.AreEqual(1, distance);
      }

      /// <summary>
      /// This test SHOULD pass. The child element SHOULD match #a, but in a limited way.
      /// Consider the html...
      /// DIV#a
      ///  SPAN#b
      ///
      /// And some css...
      /// #a { color: red; }
      ///
      /// Then it should be true that SPAN#b's color is red. For that to be true, the rule
      /// must apply to SPAN#b.
      /// </summary>
      [Test]
      public void ChildDoesntMatchButParentChainDoes_DistanceOfTwo()
      {
         var parent = CreateElement<ImageStyleBehaviour>("a");
         var child = parent.CreateElement<ImageStyleBehaviour>();
         var selector = SelectorParser.Parse("#a");
         var distance = child.MatchSelectorDistance(selector);
         Assert.AreEqual(2, distance);
      }

      [Test]
      public void DoesntMatch_DistanceZero()
      {
         var imgBehaviour = CreateElement<ImageStyleBehaviour>();
         var selector = SelectorParser.Parse("text");
         var distance = imgBehaviour.MatchSelectorDistance(selector);
         Assert.AreEqual(0, distance);
      }

      [Test]
      public void ParentChild_ExactMatch_DistanceOfOne()
      {
         var parent = CreateElement<ImageStyleBehaviour>();
         var child = parent.CreateElement<TextStyleBehaviour>();


         var selector = SelectorParser.Parse("img text");

         var distance = child.MatchSelectorDistance(selector);
         Assert.AreEqual(1, distance);
      }

      [Test]
      public void ParentChild_MatchedGrandparent_DistanceOfTwo()
      {
         var grandparent = CreateElement<ImageStyleBehaviour>("a");
         var parent = grandparent.CreateElement<ImageStyleBehaviour>();
         var child = parent.CreateElement<TextStyleBehaviour>();

         var selector = SelectorParser.Parse("img#a text");
         var distance = child.MatchSelectorDistance(selector);

         // The selector _does_ match, but the match is matching against a 3 element, where the selector only had 2 parts.
         Assert.AreEqual(2, distance);
      }
   }
}