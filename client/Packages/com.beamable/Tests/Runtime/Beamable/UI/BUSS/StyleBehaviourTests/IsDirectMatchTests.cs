using Beamable.UI.Buss;
using NUnit.Framework;

namespace Beamable.Tests.UI.Buss.StyleBehaviourTests
{
	public class IsDirectMatchTests : BUSSTest
	{
		[Test]
		public void IsDirectMatch_Simple()
		{
			var parent = CreateElement<ImageStyleBehaviour>("a");
			var selector = SelectorParser.Parse("#a");
			var match = parent.IsDirectMatch(selector);
			Assert.IsTrue(match);
		}

		[Test]
		public void IsDirectMatch_HasParentRequirement()
		{
			var parent = CreateElement<ImageStyleBehaviour>("a");
			var child = parent.CreateElement<ImageStyleBehaviour>("b");
			var selector = SelectorParser.Parse("#a #b");
			var match = child.IsDirectMatch(selector);
			Assert.IsTrue(match);
		}

		[Test]
		public void IsDirectMatch_HasParentRequirement_WithGap()
		{
			var parent = CreateElement<ImageStyleBehaviour>("a");
			var child = parent.CreateElement<ImageStyleBehaviour>("b");
			var grandChild = child.CreateElement<ImageStyleBehaviour>("c");
			var selector = SelectorParser.Parse("#a #c");
			var match = grandChild.IsDirectMatch(selector);
			Assert.IsTrue(match);
		}

		[Test]
		public void IsNotDirectMatch_Simple()
		{
			var parent = CreateElement<ImageStyleBehaviour>("a");
			var selector = SelectorParser.Parse("#b");
			var match = parent.IsDirectMatch(selector);
			Assert.IsFalse(match);
		}

		[Test]
		public void IsNotDirectMatch_DueToParentRequirement()
		{
			var parent = CreateElement<ImageStyleBehaviour>("a");
			var child = parent.CreateElement<ImageStyleBehaviour>("b");
			var selector = SelectorParser.Parse("#b #b");
			var match = child.IsDirectMatch(selector);
			Assert.IsFalse(match);
		}

		[Test]
		public void IsNotDirectMatch_DueToParentRequirement_WithGap()
		{
			var parent = CreateElement<ImageStyleBehaviour>("a");
			var child = parent.CreateElement<ImageStyleBehaviour>("b");
			var grandChild = child.CreateElement<ImageStyleBehaviour>("c");
			var selector = SelectorParser.Parse("#b #a #c");
			var match = grandChild.IsDirectMatch(selector);
			Assert.IsFalse(match);
		}
	}
}
