using Beamable.Common;
using Moq;
using Moq.Language;
using Moq.Language.Flow;
using System;

namespace tests.MoqExtensions;

public static class PromiseHelperExtensions
{
	public static IReturnsResult<TMock> ReturnsPromise<TMock, TResult>(this IReturns<TMock, Promise<TResult>> self, TResult result)
		where TMock : class
	{
		return self.Returns(Promise<TResult>.Successful(result));
	}

	public static IReturnsResult<TMock> ReturnsPromise<TMock, TResult>(this IReturns<TMock, Promise<TResult>> self, Func<TResult> resultGenerator)
		where TMock : class
	{
		return self.Returns(() => Promise<TResult>.Successful(resultGenerator()));
	}
}


public static class MockUtil
{
	public static Mock<T> Create<T>()
		where T : class
	{
		return new Mock<T>();
	}
}
