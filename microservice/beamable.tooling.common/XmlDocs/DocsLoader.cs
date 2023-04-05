using Beamable.Common;
using LoxSmoke.DocXml;
using Serilog;
using System.Reflection;

namespace Beamable.Server.Common.XmlDocs;

public static class DocsLoader
{
	private static DocXmlReader _reader = new DocXmlReader(asm =>
	{
		var xmlPath = asm.Location.Replace(".dll", ".xml");
		if (!File.Exists(xmlPath))
		{
			Log.Verbose($"Unable to find xml docs for asm=[{asm.Location}].");
			return null;
		}

		return xmlPath;
	});

	public static TypeComments GetTypeComments(Type runtimeType)
	{
		var comments = _reader.GetTypeComments(runtimeType);
		return comments;
	}

	public static CommonComments GetMemberComments(MemberInfo memberInfo)
	{
		return _reader.GetMemberComments(memberInfo);
	}

	public static MethodComments GetMethodComments(MethodInfo methodInfo)
	{
		return _reader.GetMethodComments(methodInfo);
	}
}
