using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Beamable.Editor
{
	public static class BeamableLinker
	{
		private static string[] THIRD_PARTY_ASSEMBLIES = new[]
		{
			"PubNub",
			"VirtualList",
			"UnityUIExtensions"
		};

		[MenuItem(BeamableConstants.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Generate Link File")]
		public static void GenerateLinkFile()
		{
			var linkPath = "Assets/Beamable/Resources/link.xml";

			var assemblies = new HashSet<string>();

			foreach (var asm in THIRD_PARTY_ASSEMBLIES)
			{
				assemblies.Add(asm);
			}

			var otherAssemblies = CoreConfiguration.Instance.AssembliesToSweep.Where(a => a.Contains("Beamable") && !a.Contains("Editor"));
			foreach (var asm in otherAssemblies)
			{
				assemblies.Add(asm);
			}

			var sb = new StringBuilder();
			sb.AppendLine("<linker>");
			foreach (var asm in assemblies)
			{
				sb.AppendLine($"<assembly fullname=\"{asm}\" preserve=\"all\"/>");
			}
			sb.AppendLine("</linker>");
			var xml = sb.ToString();

			var alreadyExists = (File.Exists(linkPath) && File.ReadAllText(linkPath) == xml);
			if (alreadyExists) return;
			File.WriteAllText(linkPath, xml);
		}
	}
}
