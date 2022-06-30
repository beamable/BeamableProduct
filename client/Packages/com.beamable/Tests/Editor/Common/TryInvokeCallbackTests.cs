using Beamable.Common;
using NUnit.Framework;

namespace Beamable.Editor.Tests.Common
{
	public class TryInvokeCallbackTests
	{
		[SetUp]
		public void Setup()
		{
			BaseClass.InvokeTunaCount = 0;
			BaseClass.InvokeFishCount = 0;
		}

		[Test]
		public void CanFindPublicMethod()
		{
			var instance = new BaseClass();
			instance.TryInvokeCallback(nameof(BaseClass.Tuna));
			Assert.AreEqual(BaseClass.InvokeTunaCount, 1);
		}

		[Test]
		public void CanFindPrivateMethod()
		{
			var instance = new BaseClass();
			instance.TryInvokeCallback("Fish");
			Assert.AreEqual(BaseClass.InvokeFishCount, 1);
		}

		[Test]
		public void CanFindPublicMethodFromBase()
		{
			var instance = new SubClass();
			instance.TryInvokeCallback(nameof(BaseClass.Tuna));
			Assert.AreEqual(BaseClass.InvokeTunaCount, 1);
		}

		[Test]
		public void CanFindPrivateMethodFromBase()
		{
			var instance = new SubClass();
			instance.TryInvokeCallback("Fish");
			Assert.AreEqual(BaseClass.InvokeFishCount, 1);
		}
	}


	class BaseClass
	{
		public static long InvokeTunaCount = 0;
		public static long InvokeFishCount = 0;
		public void Tuna()
		{
			InvokeTunaCount ++;
		}

		private void Fish()
		{
			InvokeFishCount++;
		}
	}

	class SubClass : BaseClass
	{

	}
}
