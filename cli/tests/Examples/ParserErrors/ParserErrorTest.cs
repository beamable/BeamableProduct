using Beamable.Common.BeamCli;
using cli;
using cli.Services;
using Moq;
using NUnit.Framework;
using System;

namespace tests.Examples.ParserErrors;

/// <summary>
/// A parse error is only written to the error channel when the caller is piping (`--raw`); an interactive
/// run gets System.CommandLine's own message and nothing on the channel.
///
/// Both tests set expectations on <see cref="IDataReporterService.Report{T}"/>, NOT on the
/// <c>Exception(...)</c> extension method that <c>App</c> calls: Moq can only intercept interface members,
/// and a setup on an extension method throws "Unsupported expression" while the mock is being built. That
/// throw used to happen inside the DI configurator, where it was swallowed into the exit code these tests
/// assert on — so both tests passed without ever checking anything.
/// </summary>
public class ParserErrorTest : CLITest
{
	[Test]
	public void ReportsParseErrorOnRaw()
	{
		Mock<IDataReporterService>(mock =>
		{
			// The data reporter service needs to get called. Asserted on the payload rather than the message
			// text: System.CommandLine localizes "Unrecognized command or argument 's'.", and the CLI test
			// matrix runs under pl-PL as well as en-US.
			mock.Setup(x => x.Report(
				DefaultErrorStream.CHANNEL,
				It.Is<ErrorOutput>(err => err.exitCode == 1 && !string.IsNullOrEmpty(err.message))));
		});
		var exitCode = RunFull(new string[] { "me", "s", "--raw" });
		Assert.That(exitCode, Is.EqualTo(1), "exit code should indicate failure");
	}

	[Test]
	public void DoesNotReportErrorIfNotOnRaw()
	{
		ResetConfigurator();
		var mock = new Mock<IDataReporterService>();
		var exitCode = RunFull(new string[] { "me", "s" }, configurator: builder =>
		{
			builder.ReplaceSingleton<IDataReporterService>(mock.Object);

			var mockApp = new Mock<IAppContext>();
			mockApp.SetupGet(x => x.UsePipeOutput).Returns(false);
			builder.ReplaceSingleton(mockApp.Object);
		});
		Assert.That(exitCode, Is.EqualTo(1), "exit code should indicate failure");
		// Verified after the run rather than through a failing callback: an Assert.Fail raised inside the
		// invocation would be caught by the CLI's own exception handler and turned into an exit code.
		mock.Verify(x => x.Report(It.IsAny<string>(), It.IsAny<It.IsAnyType>()), Times.Never);
	}
}
