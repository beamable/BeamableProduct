using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class SolutionPostProcessor : AssetPostprocessor
	{
		private const string PROJECT_TYPE_CSHARP = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";

		private const string KEY_TYPE_GUID = "[TYPE_GUID]";
		private const string KEY_INSTANCE_GUID = "[INSTANCE_GUID]";
		private const string KEY_NAME = "[NAME]";
		private const string KEY_PROJECT_PATH = "[PROJ_PATH]";

		private const string GLOBAL_HOOK = "GlobalSection(ProjectConfigurationPlatforms) = postSolution";
		private const string PROJECT_HOOK = "# Visual Studio 2010";

		private static readonly string PROJECT_TEMPLATE = $@"
Project(""{{{KEY_TYPE_GUID}}}"") = ""{KEY_NAME}"", ""{KEY_PROJECT_PATH}"", ""{{{KEY_INSTANCE_GUID}}}""
EndProject";

		private static readonly string PROJECT_GLOBAL_TEMPLATE = $@"
		{{{KEY_INSTANCE_GUID}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{{KEY_INSTANCE_GUID}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
";



		public static bool OnPreGeneratingCSProjectFiles()
		{
			AssemblyUtil.Reload();
			CsharpProjectUtil.GenerateAllReferencedAssemblies();
			return false; // if we don't return false, then this methods PREVENTS Unity from generating csproj files what-so-ever.
		}

		public static string OnGeneratedSlnSolution(string path, string content)
		{
			if (BeamEditorContext.EditorContexts.Count == 0) // If there is no context yet, then we can't do anything
			{
				return content;
			}

			var codeService = BeamEditorContext.Default.ServiceScope.GetService<CodeService>();
			codeService.OnReady.Then((u) =>
			{
				var definitions =
					codeService.ServiceDefinitions.Where(sd => !string.IsNullOrEmpty(sd.ServiceInfo.projectPath));
				foreach (var definition in definitions)
				{
					var csprojPath = Path.Combine(definition.ServiceInfo.projectPath, $"{definition.BeamoId}.csproj");
					content = InjectProject(content, definition.BeamoId, csprojPath);
				}

				foreach (var lib in codeService.Libraries)
				{
					content = InjectProject(content, lib.name, lib.projPath);
				}

				foreach (var reference in AssemblyUtil.ReferencedAssemblies)
				{
					var referenceName = reference.name;
					content = InjectProject(content, referenceName, CsharpProjectUtil.GenerateCsharpProjectFilename(reference));
				}
				File.WriteAllText(path, content);
			});

			var oldContent = File.ReadAllText(path);

			return oldContent; //return old content and let the above promise write the new content later on, so we don't flick the sln
		}


		private static string InjectProject(string content, string name, string projectPath)
		{
			GetSnippets(name, projectPath, out var projectSnippet, out var globalSnippet);

			// only inject if the sln file does not contain this project
			if (content.IndexOf(globalSnippet, StringComparison.Ordinal) == -1)
				content = content.Replace(GLOBAL_HOOK, $@"{GLOBAL_HOOK}
{globalSnippet}
");

			if (content.IndexOf(projectSnippet, StringComparison.Ordinal) == -1)
				content = content.Replace(PROJECT_HOOK, $@"{PROJECT_HOOK}
{projectSnippet}
");
			return content;
		}

		private static void GetSnippets(string name, string projectPath, out string projectSection, out string globalSection)
		{
			using MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name + projectPath));
			var instanceGuid = new Guid(hash).ToString();

			// TODO: What happens if the developer moves their project to another folder?

			projectSection = PROJECT_TEMPLATE.Replace(KEY_TYPE_GUID, PROJECT_TYPE_CSHARP)
											 .Replace(KEY_INSTANCE_GUID, instanceGuid)
											 .Replace(KEY_NAME, name)
											 .Replace(KEY_PROJECT_PATH, projectPath);
			globalSection = PROJECT_GLOBAL_TEMPLATE.Replace(KEY_TYPE_GUID, PROJECT_TYPE_CSHARP)
											 .Replace(KEY_INSTANCE_GUID, instanceGuid)
											 .Replace(KEY_NAME, name)
											 .Replace(KEY_PROJECT_PATH, projectPath);
		}
	}
}
