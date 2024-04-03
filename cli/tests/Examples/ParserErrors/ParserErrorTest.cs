using cli;
using cli.Services;
using Moq;
using NUnit.Framework;
using System;

namespace tests.Examples.ParserErrors;

public class ParserErrorTest : CLITest
{
	[Test]
	public void ReportsParseErrorOnRaw()
	{
		Mock<IDataReporterService>(mock =>
		{
			// the data reporter service needs to get called
			mock.Setup(x => x.Exception(
				It.Is<Exception>(ex => ex.Message == "Unrecognized command or argument 's'."), 
				1, 
				It.IsAny<string>()));
		});
		var exitCode = RunFull(new string[]{"me", "s", "--raw"});
		Assert.That(exitCode, Is.EqualTo(1), "exit code should indicate failure");
	}

	[Test]
	public void DoesNotReportErrorIfNotOnRaw()
	{
		ResetConfigurator();
		var exitCode = RunFull(new string[]{"me", "s"}, configurator: builder =>
		{
			var mock = new Mock<IDataReporterService>();
			mock.Setup(x => x.Exception(It.IsAny<Exception>(), It.IsAny<int>(), It.IsAny<string>()))
				.Callback<Exception, int, string>((ex, code, invocation) => Assert.Fail($"No error should be reported! message=[{ex.Message}]"));
			builder.ReplaceSingleton<IDataReporterService>(mock.Object);

			var mockApp = new Mock<IAppContext>();
			mockApp.SetupGet(x => x.UsePipeOutput).Returns(false);
			builder.ReplaceSingleton(mockApp.Object);
		});
		Assert.That(exitCode, Is.EqualTo(1), "exit code should indicate failure");
	}

}
