using Beamable.Microservice.SourceGen;
using Beamable.Microservice.SourceGen.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace Microservice.SourceGen.Tests;

/// <summary>
/// Covers <see cref="LogTemplateAnalyzer"/>, which catches the mistake behind
/// https://github.com/beamable/BeamableProduct/issues/4566 at compile time: a log message that was assembled at
/// runtime, or that declares more placeholders than the call passes arguments.
/// </summary>
public partial class BeamableSourceGeneratorTests
{
	private const string LogPreamble = @"
using Beamable.Common;
using Beamable.Server;

namespace TestNamespace;

public static class LogCallSites
{
";

	private const string LogEpilogue = @"
}
";

	private static CSharpAnalyzerTest<LogTemplateAnalyzer, DefaultVerifier> LogTest(string body)
	{
		var ctx = new CSharpAnalyzerTest<LogTemplateAnalyzer, DefaultVerifier>();
		PrepareForRun(ctx, LogPreamble + body + LogEpilogue);
		return ctx;
	}

	[Fact]
	public async Task Test_Diagnostic_Log_ExpandedArguments_DeclaresTooManyHoles()
	{
		// The shape that already worked before the explicit-array fix; kept as a regression guard.
		var ctx = LogTest(@"
	public static void Broken()
	{
		BeamableLogger.LogError({|#0:""two {a} {b}""|}, 1);
	}");

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Logs.UnrenderableLogTemplate)
			.WithLocation(0)
			.WithArguments("BeamableLogger.LogError", 1));

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Log_ExplicitArray_DeclaresTooManyHoles()
	{
		// Roslyn marks a hand-written array as ArgumentKind.Explicit rather than ParamArray, which used to
		// make the analyzer skip the call even though the length is right there.
		var ctx = LogTest(@"
	public static void Broken()
	{
		BeamableLogger.LogError({|#0:""two {a} {b}""|}, new object[] { 1 });
	}");

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Logs.UnrenderableLogTemplate)
			.WithLocation(0)
			.WithArguments("BeamableLogger.LogError", 1));

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Log_SizedArrayWithoutInitializer_DeclaresTooManyHoles()
	{
		var ctx = LogTest(@"
	public static void Broken()
	{
		BeamableLogger.LogError({|#0:""four {a} {b} {c} {d}""|}, new object[2]);
	}");

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Logs.UnrenderableLogTemplate)
			.WithLocation(0)
			.WithArguments("BeamableLogger.LogError", 2));

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Log_ServerLogHelper_DeclaresTooManyHoles()
	{
		// Beamable.Server.Log is the other helper the analyzer watches.
		var ctx = LogTest(@"
	public static void Broken()
	{
		Log.Error({|#0:""two {a} {b}""|}, new object[] { 1 });
	}");

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Logs.UnrenderableLogTemplate)
			.WithLocation(0)
			.WithArguments("Log.Error", 1));

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_Diagnostic_Log_RuntimeTemplateWithForwardedArray_IsNotConstant()
	{
		// The most dangerous shape, and the one the old ordering could never reach: the array's length is
		// unknowable, but the message being built at runtime is decidable on its own.
		var ctx = LogTest(@"
	public static void Broken(string id)
	{
		var message = ""failed for "" + id;
		var args = new object[] { id };
		BeamableLogger.LogError({|#0:message|}, args);
	}");

		ctx.ExpectedDiagnostics.Add(new DiagnosticResult(Diagnostics.Logs.NonConstantLogTemplate)
			.WithLocation(0)
			.WithArguments("BeamableLogger.LogError"));

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_NoDiagnostic_Log_PassThroughWrapper()
	{
		// A wrapper forwarding its own caller's message and arguments is not building a template, so there is
		// nothing to fix here. Warning would be unfixable noise - and this analyzer ships to users.
		var ctx = LogTest(@"
	public static void MyLog(string message, params object[] args)
	{
		BeamableLogger.LogError(message, args);
	}

	public static void MyServerLog(string message, params object[] args)
	{
		Log.Error(message, args);
	}");

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_NoDiagnostic_Log_WithoutArgumentsBracesAreLiteral()
	{
		var ctx = LogTest(@"
	public static void Fine()
	{
		BeamableLogger.LogError(""braces {a} are literal here"");
		BeamableLogger.LogError(""braces {a} are literal here too"", new object[0]);
	}");

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_NoDiagnostic_Log_WellFormedTemplates()
	{
		var ctx = LogTest(@"
	public static void Fine()
	{
		BeamableLogger.LogError(""one {a}"", 1);
		BeamableLogger.LogError(""one {a}"", new object[] { 1 });
		BeamableLogger.LogError(""escaped {{a}} braces"", new object[] { 1 });
		Log.Error(""two {a} {b}"", 1, 2);
	}");

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_NoDiagnostic_Log_CompositeFormatMethodsUsePositionalHoles()
	{
		// The *Format helpers hand the message to string.Format themselves, so their holes are already
		// positional and must not be rewritten.
		var ctx = LogTest(@"
	public static void Fine()
	{
		BeamableLogger.LogErrorFormat(""positional {0}"", new object[] { 1 });
		BeamableLogger.LogWarningFormat(""positional {0} {1}"", new object[] { 1, 2 });
	}");

		await ctx.RunAsync();
	}

	[Fact]
	public async Task Test_NoDiagnostic_Log_ExceptionOverloadIsNotATemplate()
	{
		// Log.Error(Exception, string) also takes two parameters, but the first is not the message.
		var ctx = LogTest(@"
	public static void Fine()
	{
		Log.Error(new System.Exception(""boom""), ""message {a}"");
	}");

		await ctx.RunAsync();
	}
}
