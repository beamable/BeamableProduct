using PubNubMessaging.Core;
using System;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable
{
	public class CustomSlnThingy : AssetPostprocessor
	{
		private const string PROJECT_TYPE_CSHARP = "FAE04EC0-301F-11D3-BF4B-00C04F79EFBC";
		
		private const string KEY_TYPE_GUID = "[TYPE_GUID]";
		private const string KEY_INSTANCE_GUID = "[INSTANCE_GUID]";
		private const string KEY_NAME = "[NAME]";
		private const string KEY_PROJECT_PATH = "[PROJ_PATH]";

		private const string GLOBAL_HOOK = "GlobalSection(ProjectConfigurationPlatforms) = postSolution";
		private const string PROJECT_HOOK = "# Visual Studio 2010";
		
		private static readonly string Template = $@"
Project(""{{{KEY_TYPE_GUID}}}"") = ""{KEY_NAME}"", ""{KEY_PROJECT_PATH}"", ""{{{KEY_INSTANCE_GUID}}}""
EndProject
Global
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{{{KEY_INSTANCE_GUID}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{{KEY_INSTANCE_GUID}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
	EndGlobalSection
EndGlobal";
		
		private static readonly string PROJECT_TEMPLATE = $@"
Project(""{{{KEY_TYPE_GUID}}}"") = ""{KEY_NAME}"", ""{KEY_PROJECT_PATH}"", ""{{{KEY_INSTANCE_GUID}}}""
EndProject";
		
		private static readonly string PROJECT_GLOBAL_CONTENT = $@"
		{{{KEY_INSTANCE_GUID}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{{{KEY_INSTANCE_GUID}}}.Debug|Any CPU.Build.0 = Debug|Any CPU
";

		[MenuItem("SLN/Load")]
		public static void TriggerSlnLoad()
		{
			
			// TODO: this doesn't actually work. To trigger a C# sln reload, you need to right-click and do "Open C# Project", or add/remove a file.
			CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
		}

		private void OnPreprocessAssembly(string pathName)
		{
			Debug.Log("On preprocess assembly, " + pathName);
		}

		private static string OnGeneratedCSProject(string path, string content)
		{
			return content + "\n" + $"<!-- Tuna Town {path} !-->";
		}

		private static string OnGeneratedSlnSolution(string path, string content)
		{
			// ideally, we could use dotnet for this, but since we aren't in a normal life cycle flow, we cannot. 
			// which mean}
			Debug.Log("GENERATING SLN");
			
			
			content  = InjectProject(content, "TimeoutServiceExample", "Assets/Beamable/SAMS~/Timeout/Timeout.csproj");
			content = InjectProject(content, "TimeoutServiceCommon", "Assets/Beamable/SAMS~/TimeoutCommon/TimeoutCommon.csproj");
			content = InjectProject(content, "CustomerCommonExample", "Assets/Beamable/Common/CustomerCommon.csproj");
			return content;
		}

		public static string InjectProject(string content, string name, string projectPath)
		{
			GetSnippets(name, projectPath, out var projectSnippet, out var globalSnippet);

			content = content.Replace(GLOBAL_HOOK, $@"{GLOBAL_HOOK}
{globalSnippet}
");
			content = content.Replace(PROJECT_HOOK, $@"{PROJECT_HOOK}
{projectSnippet}
");
			return content;
		}
		
		public static void GetSnippets(string name, string projectPath, out string projectSection, out string globalSection)
		{
			using MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name + projectPath));
			var instanceGuid = new Guid(hash).ToString();
			
			

			projectSection = PROJECT_TEMPLATE.Replace(KEY_TYPE_GUID, PROJECT_TYPE_CSHARP)
			                                 .Replace(KEY_INSTANCE_GUID, instanceGuid)
			                                 .Replace(KEY_NAME, name)
			                                 .Replace(KEY_PROJECT_PATH, projectPath);
			globalSection = PROJECT_GLOBAL_CONTENT.Replace(KEY_TYPE_GUID, PROJECT_TYPE_CSHARP)
			                                 .Replace(KEY_INSTANCE_GUID, instanceGuid)
			                                 .Replace(KEY_NAME, name)
			                                 .Replace(KEY_PROJECT_PATH, projectPath);
		}

		// private static string GetProjectSnippet(string name, string path)
		// {
		// 	using MD5 md5 = MD5.Create();
		// 	byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(name + path));
		// 	var result = new Guid(hash).ToString();
		//
		// 	return GetProjectSnippet(PROJECT_TYPE_CSHARP, result, name, path);
		// }
		//
		// private static string GetProjectSnippet(string typeGuid, string instanceGuid, string name, string projectPath)
		// {
		// 	return Template.Replace(KEY_TYPE_GUID, typeGuid)
		// 	               .Replace(KEY_INSTANCE_GUID, instanceGuid)
		// 	               .Replace(KEY_NAME, name)
		// 	               .Replace(KEY_PROJECT_PATH, projectPath)
		// 	               ;
		// } 
	}
}
