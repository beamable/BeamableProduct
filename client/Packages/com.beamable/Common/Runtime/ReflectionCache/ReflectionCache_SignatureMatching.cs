using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Beamable.Common
{
	/// <summary>
	/// Defines a method signature of interest so that we can validate that game-makers
	/// are placing <see cref="IReflectionCachingAttribute{T}"/> that match expected signatures.
	/// </summary>
	public readonly struct SignatureOfInterest
	{
		/// <summary>
		/// Whether or not the signature is expected to be <b>static</b>. Assumes it is an <b>instanced</b> call if <b>false</b>.
		/// </summary>
		public readonly bool IsStatic;

		/// <summary>
		/// The return type for the method. Currently we don't support detecting for <b>ref</b> or <b>readonly</b> returns. 
		/// </summary>
		public readonly Type ReturnType;

		/// <summary>
		/// List of <see cref="ParameterOfInterest"/> that define the parameters of the signature we are matching.
		/// </summary>
		public readonly ParameterOfInterest[] Parameters;

		/// <summary>
		/// <see cref="SignatureOfInterest"/> default constructor.
		/// </summary>
		public SignatureOfInterest(bool isStatic, Type returnType, ParameterOfInterest[] parameters)
		{
			IsStatic = isStatic;
			ReturnType = returnType;
			Parameters = parameters;
		}
	}

	/// <summary>
	/// Defines a parameter signature of interest so we can guarantee parameters are being declared as we expect them to be.
	/// </summary>
	public readonly struct ParameterOfInterest
	{
		/// <summary>
		/// The type of the parameter we are expecting.
		/// </summary>
		public readonly Type ParameterType;

		/// <summary>
		/// Whether or not the parameter is an <b>in</b> parameter.
		/// </summary>
		public readonly bool IsIn;

		/// <summary>
		/// Whether or not the parameter is an <b>out</b> parameter.
		/// </summary>
		public readonly bool IsOut;

		/// <summary>
		/// Whether or not the parameter is a <b>ref</b> parameter.
		/// </summary>
		public readonly bool IsByRef;
		
		/// <summary>
		/// <see cref="ParameterOfInterest"/> default constructor. Validates in/ref/out correctness.
		/// </summary>
		public ParameterOfInterest(Type parameterType, bool isIn, bool isOut, bool isByRef)
		{
			System.Diagnostics.Debug.Assert(!(isIn && isOut && isByRef) || (isIn ^ isOut ^ isByRef), "All In/Ref/Out must be false or only one of them can be true.");

			ParameterType = parameterType;

			IsIn = isIn;
			IsOut = isOut;
			IsByRef = isIn || isOut || isByRef;
		}
	}

	public static partial class ReflectionCacheExtensions
	{
		/// <summary>
		/// Generates a human-readable string of the given <paramref name="signatureOfInterest"/> (see <see cref="SignatureOfInterest"/> for more details).
		/// </summary>
		public static string ToHumanReadableSignature(this in SignatureOfInterest signatureOfInterest)
		{
			var paramsDeclaration = string.Join(", ", signatureOfInterest.Parameters.Select(param =>
			{
				var prefix = param.IsOut ? "out " :
					param.IsIn ? "in " :
					param.ParameterType.IsByRef ? "ref " :
					"";

				return $"{prefix}{param.ParameterType.Name}";
			}));
			return $"{signatureOfInterest.ReturnType.Name}({paramsDeclaration})";
		}

		/// <summary>
		/// Iterates through a list of <paramref name="acceptedSignatures"/> and match the given <paramref name="methodInfo"/> against them.
		/// Allow classes and sublclass
		/// </summary>
		/// <param name="acceptedSignatures">A list of <see cref="SignatureOfInterest"/> that the method is allowed to have.</param>
		/// <param name="methodInfo">The <see cref="MethodInfo"/> to match against the <paramref name="acceptedSignatures"/>.</param>
		/// <returns>A list, parallel to <paramref name="acceptedSignatures"/>, containing the index or -1 for each of the given <paramref name="acceptedSignatures
		/// "/>.</returns>
		public static List<int> FindMatchingMethodSignatures(this IReadOnlyList<SignatureOfInterest> acceptedSignatures, MethodInfo methodInfo)
		{
			var parameters = methodInfo.GetParameters();
			var retValType = methodInfo.ReturnType;
			var isStatic = methodInfo.IsStatic;

			var matchedSignaturesIndices = acceptedSignatures.Select((acceptableSignature, signatureIdx) =>
			{
				if (isStatic != acceptableSignature.IsStatic) return -1;
				if (retValType != acceptableSignature.ReturnType) return -1;
				if (parameters.Length != acceptableSignature.Parameters.Length) return -1;

				for (var i = 0; i < parameters.Length; i++)
				{
					var parameter = parameters[i];
					var acceptableParameter = acceptableSignature.Parameters[i];

					// Use assignable from in case we accept interfaces.
					var matchType = acceptableParameter.ParameterType.IsAssignableFrom(parameter.ParameterType);
					var matchIn = acceptableParameter.IsIn == parameter.IsIn;
					var matchOut = acceptableParameter.IsOut == parameter.IsOut;
					var matchRef = acceptableParameter.IsByRef == parameter.ParameterType.IsByRef;

					if (!(matchType && matchIn && matchOut && matchRef))
						return -1;
				}

				return signatureIdx;
			}).ToList();

			return matchedSignaturesIndices;
		}
	}
}
