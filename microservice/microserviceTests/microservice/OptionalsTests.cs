using Beamable.Common.Content;
using NUnit.Framework;

namespace microserviceTests.microservice;

public class OptionalsTests
{
    private static readonly object[] ToIntTestCases =
    {
        new object[] {null, null},
        new object[] {new OptionalInt(), null},
        new object[] {new OptionalInt {Value = 7856, HasValue = false}, null},
        new object[] {new OptionalInt {Value = 0, HasValue = true}, (int?) 0},
        new object[] {new OptionalInt {Value = 6345, HasValue = true}, (int?) 6345}
    };

    [Test]
    [TestCaseSource(nameof(ToIntTestCases))]
    public void CastsOptionalIntToNullableInt(OptionalInt sourceValue, int? expectedValue)
    {
        int? casted = sourceValue;

        Assert.AreEqual(expectedValue, casted);
    }
}