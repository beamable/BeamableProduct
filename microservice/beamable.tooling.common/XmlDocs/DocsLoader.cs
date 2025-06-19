using LoxSmoke.DocXml;
using System.Reflection;
using beamable.tooling.common.Microservice;
using ZLogger;

namespace Beamable.Server.Common.XmlDocs;

/// <summary>
/// Provides utility methods for loading documentation comments.
/// </summary>
public static class DocsLoader
{
	private static DocXmlReader _reader = new DocXmlReader(asm =>
	{
		var xmlPath = asm.Location.Replace(".dll", ".xml");
		if (!File.Exists(xmlPath))
		{
			BeamableZLoggerProvider.LogContext.Value.ZLogTrace($"Unable to find xml docs for asm=[{asm.Location}].");
			return null;
		}

		return xmlPath;
	});

	/// <summary>
	/// Retrieves the comments associated with a specified runtime type.
	/// </summary>
	/// <param name="runtimeType">The type for which to retrieve comments.</param>
	/// <returns>Comments associated with the specified type.</returns>
	public static TypeComments GetTypeComments(Type runtimeType)
	{
		var comments = _reader.GetTypeComments(runtimeType);
		return comments;
	}

	/// <summary>
	/// Retrieves the comments associated with a specified member information.
	/// </summary>
	/// <param name="memberInfo">The member information for which to retrieve comments.</param>
	/// <returns>Comments associated with the specified member information.</returns>
	public static CommonComments GetMemberComments(MemberInfo memberInfo)
	{
		return _reader.GetMemberComments(memberInfo);
	}

	/// <summary>
	/// Retrieves the comments associated with a specified method information.
	/// </summary>
	/// <param name="methodInfo">The method information for which to retrieve comments.</param>
	/// <returns>Comments associated with the specified method information.</returns>
	public static MethodComments GetMethodComments(MethodInfo methodInfo)
	{
		return _reader.GetMethodComments(methodInfo);
	}
}
