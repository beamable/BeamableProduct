using Beamable.Editor.BeamCli.Commands;
using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.WSA;

namespace Beamable.Server.Editor.Usam
{
	public static class CsharpProjectUtil
	{

		private const string KEY_COMPILED_FILES = "[COMPILED_FILES]";
		private const string KEY_OTHER_PROJECTS = "[PROJECT_REFS]";
		private const string KEY_DLL_REFERENCES = "[DLL_REFS]";
		private const string KEY_INCLUDE = "[INCLUDE]";
		private const string KEY_HINT = "[HINT_PATH]";
		private const string KEY_FOLDER = "FOLDER";
		private const string KEY_BEAMABLE_VERSION = "[BEAM_VERSION]";

		private static readonly string PROJECT_NAME_TAG = "[ASSEMBLY_NAME]";
		private static readonly string PROJECT_NAME = $"{PROJECT_NAME_TAG}.csproj";
		private static readonly string TEMPLATE_OUTPUT_DIR =
			Path.Combine("Library", "BeamableEditor", "GeneratedProjects", KEY_FOLDER);
		private static readonly string SOURCE_TEMPLATE = $"<Compile Include=\"{KEY_INCLUDE}\" Condition=\"Exists('{KEY_INCLUDE}')\" />";
		private static readonly string PROJECT_TEMPLATE = $"<ProjectReference Include=\"{KEY_INCLUDE}\" />";
		private static readonly string DLL_TEMPLATE = $"<Reference Include=\"{KEY_INCLUDE}\">\n\t\t\t<HintPath>{KEY_HINT}</HintPath>\n\t\t</Reference>";
		private static readonly string TEMPLATE = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <!-- Provide compiler symbol -->
    <PropertyGroup>
        <DefineConstants>$(DefineConstants);BEAMABLE_MICROSERVICE</DefineConstants>
    </PropertyGroup>

	<PropertyGroup Label=""Beamable Settings"">
        <!-- All Unity Assembly References must have the value: ""unity"" -->
        <BeamProjectType>unity</BeamProjectType>
		<LangVersion>8.0</LangVersion>
		<GenerateClientCode>false</GenerateClientCode>
    </PropertyGroup>

  <PropertyGroup Label=""Beamable Version"" Condition=""$(DOTNET_RUNNING_IN_CONTAINER)!=true"">
    <DotNetConfigPath Condition=""'$(DotNetConfigPath)' == ''"">$([MSBuild]::GetDirectoryNameOfFileAbove(""$(MSBuildProjectDirectory)/.."", "".config/dotnet-tools.json""))</DotNetConfigPath>
    <DotNetConfig Condition=""'$(DotNetConfig)' == ''"">$([System.IO.File]::ReadAllText(""$(DotNetConfigPath)/.config/dotnet-tools.json""))</DotNetConfig>
    <!-- Extracts the version number from the first tool defined in 'dotnet-tools.json' that starts with ""beamable"". -->
    <BeamableVersion Condition=""'$(BeamableVersion)' == ''"">$([System.Text.RegularExpressions.Regex]::Match(""$(DotNetConfig)"", ""beamable.*?\""([0-9]+\.[0-9]+\.[0-9]+.*?)\"","", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace).Groups[1].Value)</BeamableVersion>
    <!-- When running from inside docker, this gets injected via the Dockerfile at build-time. -->
  </PropertyGroup>

    <!-- Project settings -->
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>
        <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>
        <EnableDefaultItems>false</EnableDefaultItems>
    </PropertyGroup>

    <!-- Nuget references -->
    <ItemGroup>
        <PackageReference Include=""Beamable.UnityEngine"" Version=""$(BeamableVersion)""/>
        <PackageReference Include=""Beamable.Common"" Version=""$(BeamableVersion)""/>
        <PackageReference Include=""Beamable.Microservice.Runtime"" Version=""$(BeamableVersion)""/>
    </ItemGroup>

    <!-- Source files -->
    <ItemGroup>
		{KEY_COMPILED_FILES}
	</ItemGroup>

    <!-- Other projects -->
    <ItemGroup>
		{KEY_OTHER_PROJECTS}
	</ItemGroup>

	<!-- Dll references -->
	<ItemGroup>
		{KEY_DLL_REFERENCES}
	</ItemGroup>

</Project>
";

		private const string invalidAssembliesFilePath =
			"Packages/com.beamable.server/Editor/invalid-assemblies.txt";

		private const string customInvalidAssembliesPath = "Assets/Beamable/Resources/custom-invalid-assemblies.txt";

		/// <summary>
		/// AssemblyUtil.Reload(); must be called before this
		/// </summary>
		/// <param name="beamCommands"></param>
		public static void GenerateAllReferencedAssemblies(UsamService usam)
		{
			var asmUtil = usam.AssemblyService;
			var cli = usam.Cli;
			foreach (var assembly in asmUtil.ReferencedAssemblies)
			{
				var path = GenerateCsharpProjectPath(assembly);
				var content = GenerateCsharpProject(assembly, path);
				var fileName = GenerateCsharpProjectFilename(assembly);

				var needsFileWrite = true;
				if (File.Exists(fileName))
				{
					var oldContent = File.ReadAllText(fileName);
					if (oldContent.Equals(content))
					{
						needsFileWrite = false;
					}
				}

				if (needsFileWrite)
				{
					Debug.Log(
						$"Writing generated project for assembly definition {assembly.name} in the file: {fileName}");
					Directory.CreateDirectory(path);
					File.WriteAllText(fileName, content);
				}

				var _ = cli.UnityRestore(new UnityRestoreArgs {csproj = fileName}).Run();
			}
		}
		
		public static string GenerateCsharpProjectPath(Assembly assembly)
		{
			return TEMPLATE_OUTPUT_DIR.Replace(KEY_FOLDER, assembly.name);
		}
		
		public static string GenerateCsharpProjectPath(string assembly)
		{
			return TEMPLATE_OUTPUT_DIR.Replace(KEY_FOLDER, assembly);
		}

		public static string GenerateCsharpProjectFilename(Assembly assembly)
		{
			return Path.Combine(GenerateCsharpProjectPath(assembly), PROJECT_NAME.Replace(PROJECT_NAME_TAG, assembly.name));
		}

		public static string GenerateCsharpProjectFilename(string assembly)
		{
			return Path.Combine(GenerateCsharpProjectPath(assembly), PROJECT_NAME.Replace(PROJECT_NAME_TAG, assembly));
		}

		public static string GenerateCsharpProject(Assembly assembly, string csProjDir)
		{
			var sourceFiles = string.Join(Environment.NewLine + "\t\t",
						assembly.sourceFiles.Select(x => GenerateCompileSourceEntry(x, csProjDir)));

			var assemblyReferences = GetValidAssemblyReferences(assembly);
			var projectReferences = string.Join(Environment.NewLine + "\t\t",
												assemblyReferences.Select(
													x => GenerateProjectReferenceEntry(x, csProjDir)));

			var sdkVersion = BeamableEnvironment.NugetPackageVersion;



			var dlls = GetValidDllReferences(assembly);
			var dllReferences = string.Join(Environment.NewLine + "\t\t",
											dlls.Select(x => GenerateDllReferenceEntry(x, csProjDir)));

			var file = TEMPLATE
					.Replace(KEY_COMPILED_FILES, sourceFiles)
					.Replace(KEY_OTHER_PROJECTS, projectReferences)
					.Replace(KEY_DLL_REFERENCES, dllReferences)
					.Replace(KEY_BEAMABLE_VERSION, sdkVersion.ToString())
				;

			return file;
		}

		public static IEnumerable<string> GetValidDllReferences(Assembly assembly)
		{
			var projectRoot = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.Length - "/Assets".Length);

			foreach (var dll in assembly.compiledAssemblyReferences)
			{
				if (!dll.StartsWith(projectRoot)) continue;
				var dllPath = dll.Substring(projectRoot.Length + 1);
				var dllName = Path.GetFileName(dllPath);
				if (!IsValidReference(dllName.Replace(".dll", ""))) continue;
				yield return dllPath;
			}

			yield break;
		}

		static IEnumerable<Assembly> GetValidAssemblyReferences(Assembly assembly)
		{
			foreach (var reference in assembly.assemblyReferences)
			{
				if (IsValidReference(reference.name))
				{
					yield return reference;
				}
			}
		}
		
		public static bool IsValidReference(string referenceName)
		{
			var invalidReferences = File.ReadAllLines(invalidAssembliesFilePath);

			if (File.Exists(customInvalidAssembliesPath))
			{
				var customInvalidRefs = File.ReadAllLines(customInvalidAssembliesPath);
				invalidReferences = invalidReferences.Concat(customInvalidRefs).ToArray();
			}
			
			foreach (var invalidRef in invalidReferences)
			{
				if (invalidRef.Contains("*"))
				{
					if (referenceName.StartsWith(invalidRef.Replace("*", "")))
					{
						return false;
					}
				}

				if (referenceName.Equals(invalidRef))
				{
					return false;
				}
			}

			return true;
		}

		static string GenerateProjectReferenceEntry(Assembly reference, string csProjDir)
		{
			// the project will be generated in a folder next to the project path
			var path = Path.Combine("..", reference.name, reference.name + ".csproj");
			return PROJECT_TEMPLATE.Replace(KEY_INCLUDE, path);
		}

		static string GenerateCompileSourceEntry(string source, string csProjDir)
		{
			if (!csProjDir.EndsWith(Path.DirectorySeparatorChar))
			{
				csProjDir += Path.DirectorySeparatorChar;
			}
			source = PackageUtil.GetRelativePath(csProjDir, source);
			return SOURCE_TEMPLATE.Replace(KEY_INCLUDE, source);
		}

		static string GenerateDllReferenceEntry(string dllPath, string csProjDir)
		{
			dllPath = PackageUtil.GetRelativePath(csProjDir, dllPath);
			var name = Path.GetFileNameWithoutExtension(dllPath);
			return DLL_TEMPLATE.Replace(KEY_INCLUDE, name)
							   .Replace(KEY_HINT, dllPath);
		}
	}
}
