using Beamable.Server;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Beamable.Microservice.SourceGen.Analyzers;

/// <summary>
/// Checks calls to Beamable's logging helpers that take a message plus log arguments.
/// <para/>
/// Those helpers treat the message as a structured template: every <c>{name}</c> in it is a placeholder that
/// consumes one of the arguments. A message that was assembled at runtime therefore cannot be trusted as a
/// template - text interpolated into it brings its own braces along, and the mismatch fails while the log is
/// being written, taking with it whatever the caller was trying to report. See
/// https://github.com/beamable/BeamableProduct/issues/4566.
/// <para/>
/// The framework's own analyzers (CA2017, CA2023, CA2254) only recognise <c>ILogger</c> calls, so they never
/// see these helpers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class LogTemplateAnalyzer : DiagnosticAnalyzer
{
	private const string BEAMABLE_LOGGER_FULL_NAME = "Beamable.Common.BeamableLogger";
	private const string SERVER_LOG_FULL_NAME = "Beamable.Server.Log";

	/// <summary>
	/// Helpers whose name ends in this hand the message straight to <see cref="string.Format(string,object[])"/>,
	/// so their placeholders are already positional and must not be rewritten.
	/// </summary>
	private const string COMPOSITE_FORMAT_METHOD_SUFFIX = "Format";

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(Diagnostics.BeamExceptionDescriptor,
			Diagnostics.Logs.NonConstantLogTemplate,
			Diagnostics.Logs.UnrenderableLogTemplate);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.RegisterOperationAction(AnalyzeLogInvocation, OperationKind.Invocation);
		context.EnableConcurrentExecution();
	}

	private void AnalyzeLogInvocation(OperationAnalysisContext context)
	{
		try
		{
			var invocation = (IInvocationOperation)context.Operation;
			if (!IsBeamableLogMethod(invocation.TargetMethod))
			{
				return;
			}

			var messageArgument = invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.Ordinal == 0);
			var logArguments = invocation.Arguments.FirstOrDefault(argument => argument.Parameter?.IsParams ?? false);
			if (messageArgument == null || logArguments == null)
			{
				return;
			}

			var logArgumentCount = GetLogArgumentCount(logArguments);
			if (logArgumentCount == 0)
			{
				// With no log arguments the message is never parsed, so its braces are already literal.
				return;
			}

			var methodName = $"{invocation.TargetMethod.ContainingType.Name}.{invocation.TargetMethod.Name}";
			var location = messageArgument.Syntax.GetLocation();
			var message = messageArgument.Value.ConstantValue;

			if (!message.HasValue)
			{
				// Judged without needing the argument count: a template assembled at runtime is a problem whether
				// or not we can see how many arguments came with it.
				if (!IsForwardedTemplate(messageArgument, logArguments))
				{
					context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Logs.NonConstantLogTemplate, location, methodName));
				}

				return;
			}

			if (!(message.Value is string template))
			{
				// A null message; the logger handles that on its own.
				return;
			}

			if (logArgumentCount < 0)
			{
				// The template is constant, but the arguments arrived as an array of unknowable length, so there is
				// nothing to compare its holes against.
				return;
			}

			var positionalFormat = IsCompositeFormatMethod(invocation.TargetMethod)
				? template
				: SafeLogTemplate.ToPositionalFormat(template);

			if (!CanRender(positionalFormat, logArgumentCount))
			{
				context.ReportDiagnostic(Diagnostic.Create(Diagnostics.Logs.UnrenderableLogTemplate, location,
					methodName, logArgumentCount));
			}
		}
		catch (Exception e)
		{
			context.ReportDiagnostic(Diagnostics.GetException(e, null, context.Compilation));
			throw;
		}
	}

	/// <summary>
	/// Matches the <c>(string message, params object[] args)</c> overloads on Beamable's logging helpers.
	/// </summary>
	private static bool IsBeamableLogMethod(IMethodSymbol method)
	{
		var containingType = method?.ContainingType?.ToDisplayString();
		if (containingType != BEAMABLE_LOGGER_FULL_NAME && containingType != SERVER_LOG_FULL_NAME)
		{
			return false;
		}

		var parameters = method.Parameters;
		return parameters.Length == 2
		       && parameters[0].Type.SpecialType == SpecialType.System_String
		       && parameters[1].IsParams;
	}

	private static bool IsCompositeFormatMethod(IMethodSymbol method)
	{
		return method.Name.EndsWith(COMPOSITE_FORMAT_METHOD_SUFFIX, StringComparison.Ordinal);
	}

	/// <summary>
	/// Counts the values passed to a <c>params</c> parameter, or returns -1 when the count genuinely cannot be
	/// known here.
	/// <para/>
	/// The count is read off the array operation rather than off <see cref="ArgumentKind"/>. Roslyn reports
	/// <see cref="ArgumentKind.ParamArray"/> only for the array it synthesizes for an <i>expanded</i> call, so a
	/// caller who writes the array out - <c>new object[] { x }</c> - arrives as
	/// <see cref="ArgumentKind.Explicit"/> and was previously skipped even though its length is plain to see.
	/// </summary>
	private static int GetLogArgumentCount(IArgumentOperation logArguments)
	{
		var value = Unwrap(logArguments.Value);
		if (!(value is IArrayCreationOperation arrayCreation))
		{
			// a variable, a field, Array.Empty<object>() - the length belongs to run time
			return -1;
		}

		if (arrayCreation.Initializer != null)
		{
			return arrayCreation.Initializer.ElementValues.Length;
		}

		if (arrayCreation.DimensionSizes.Length == 1
		    && arrayCreation.DimensionSizes[0].ConstantValue.HasValue
		    && arrayCreation.DimensionSizes[0].ConstantValue.Value is int length)
		{
			// new object[3]: no initializer, but a constant length all the same
			return length;
		}

		return -1;
	}

	/// <summary>
	/// Recognises a logging wrapper handing its own caller's message and arguments straight through, as in
	/// <c>void MyLog(string message, params object[] args) =&gt; Log.Error(message, args);</c>. The template is not
	/// being assembled here, so there is nothing to fix at this call site; the mistake, if there is one, belongs
	/// to whoever called the wrapper. Warning here would be unfixable noise, and this analyzer ships to users
	/// through the generated service templates.
	/// </summary>
	private static bool IsForwardedTemplate(IArgumentOperation messageArgument, IArgumentOperation logArguments)
	{
		return Unwrap(messageArgument.Value) is IParameterReferenceOperation messageParameter
		       && Unwrap(logArguments.Value) is IParameterReferenceOperation argumentsParameter
		       && argumentsParameter.Parameter.IsParams
		       && SymbolEqualityComparer.Default.Equals(messageParameter.Parameter.ContainingSymbol,
			       argumentsParameter.Parameter.ContainingSymbol);
	}

	/// <summary>
	/// Strips the implicit conversions Roslyn wraps an argument in - a covariant <c>string[]</c> passed as
	/// <c>object[]</c>, for instance - so that the underlying expression can be matched.
	/// </summary>
	private static IOperation Unwrap(IOperation operation)
	{
		while (operation is IConversionOperation conversion)
		{
			operation = conversion.Operand;
		}

		return operation;
	}

	/// <summary>
	/// Reports whether the given composite format string can be rendered with this many arguments. The values
	/// do not matter, only how many there are, so nulls stand in for them.
	/// </summary>
	private static bool CanRender(string positionalFormat, int argumentCount)
	{
		try
		{
			string.Format(CultureInfo.InvariantCulture, positionalFormat, new object[argumentCount]);
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}
}
