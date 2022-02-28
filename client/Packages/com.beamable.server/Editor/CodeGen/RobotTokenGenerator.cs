using System;
using System.IO;
using UnityEditor;

namespace Beamable.Server.Editor.CodeGen
{
	public class RobotTokenGenerator
	{
		public MicroserviceDescriptor Descriptor { get; }

		public RobotTokenGenerator(MicroserviceDescriptor descriptor)
		{
			Descriptor = descriptor;
		}

		public void GenerateFile(string filePath)
		{
			var entry = MicroserviceConfiguration.Instance.GetEntry(Descriptor.Name);
			var guid = GUID.Generate().ToString();
			entry.RobotId = guid;
			var text = $@"
namespace Beamable.Microservices
{{
	public class Beamable__Change_Token_Class
	{{
		public static string GetToken() {{ return ""{entry.RobotId}""; }}
	}}
}}
";
			File.WriteAllText(filePath, text);
		}
	}
}
