using Beamable.Editor.Modules.EditorConfig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
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

		private static readonly string TEMPLATE_OUTPUT_DIR =
			Path.Combine("Library", "BeamableEditor", "GeneratedProjects", KEY_FOLDER);
		private static readonly string SOURCE_TEMPLATE = $"<Compile Include=\"{KEY_INCLUDE}\" />";
		private static readonly string PROJECT_TEMPLATE = $"<ProjectReference Include=\"{KEY_INCLUDE}\" />";
		private static readonly string DLL_TEMPLATE = $"<Reference Include=\"{KEY_INCLUDE}\">\n\t\t\t<HintPath>{KEY_HINT}</HintPath>\n\t\t</Reference>";
		private static readonly string TEMPLATE = $@"
<Project Sdk=""Microsoft.NET.Sdk"">
    <!-- Provide compiler symbol -->
    <PropertyGroup>
        <DefineConstants>$(DefineConstants);BEAMABLE_MICROSERVICE</DefineConstants>
    </PropertyGroup>

    <!-- Project settings -->
    <PropertyGroup>
        <TargetFramework>netstandard2.1</TargetFramework>
        <ImplicitlyExpandNETStandardFacades>false</ImplicitlyExpandNETStandardFacades>
        <ImplicitlyExpandDesignTimeFacades>false</ImplicitlyExpandDesignTimeFacades>
        <EnableDefaultItems>false</EnableDefaultItems>
    </PropertyGroup>

    <!-- Nuget references -->
    <ItemGroup>
        <PackageReference Include=""Beamable.UnityEngine"" Version=""{KEY_BEAMABLE_VERSION}""/>
        <PackageReference Include=""Beamable.Common"" Version=""{KEY_BEAMABLE_VERSION}""/>
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


		/// <summary>
		/// AssemblyUtil.Reload(); must be called before this
		/// </summary>
		public static void GenerateAllReferencedAssemblies()
		{
			foreach (var assembly in AssemblyUtil.ReferencedAssemblies)
			{
				var path = GenerateCsharpProjectPath(assembly);
				var content = GenerateCsharpProject(assembly, path);
				var fileName = GenerateCsharpProjectFilename(assembly);
				Directory.CreateDirectory(path);
				File.WriteAllText(fileName, content);
			}
		}

		public static string GenerateCsharpProjectPath(Assembly assembly)
		{
			return TEMPLATE_OUTPUT_DIR.Replace(KEY_FOLDER, assembly.name);
		}

		public static string GenerateCsharpProjectFilename(Assembly assembly)
		{
			return Path.Combine(GenerateCsharpProjectPath(assembly), assembly.name + ".csproj");
		}
		
		public static string GenerateCsharpProject(Assembly assembly, string csProjDir)
		{
			var sourceFiles = string.Join(Environment.NewLine + "\t\t",
			            assembly.sourceFiles.Select(x => GenerateCompileSourceEntry(x, csProjDir)));

			var assemblyReferences = GetValidAssemblyReferences(assembly);
			var projectReferences = string.Join(Environment.NewLine+ "\t\t",
			                                    assemblyReferences.Select(
				                                    x => GenerateProjectReferenceEntry(x, csProjDir)));

			var sdkVersion = BeamableEnvironment.SdkVersion;
			
			

			var dlls = GetValidDllReferences(assembly);
			var dllReferences = string.Join(Environment.NewLine+ "\t\t",
			                                dlls.Select(x => GenerateDllReferenceEntry(x, csProjDir)));
			
			var file = TEMPLATE
					.Replace(KEY_COMPILED_FILES, sourceFiles)
					.Replace(KEY_OTHER_PROJECTS, projectReferences)
					.Replace(KEY_DLL_REFERENCES, dllReferences)
					.Replace(KEY_BEAMABLE_VERSION, sdkVersion.ToString())
				;
			
			return file;
		}
		
		static IEnumerable<string> GetValidDllReferences(Assembly assembly)
		{
			var projectRoot = UnityEngine.Application.dataPath.Substring(0, UnityEngine.Application.dataPath.Length - "/Assets".Length);
			
			foreach (var dll in assembly.compiledAssemblyReferences)
			{
				if (!dll.StartsWith(projectRoot)) continue;
				yield return dll.Substring(projectRoot.Length + 1);
			}

			yield break;
		}

		static IEnumerable<Assembly> GetValidAssemblyReferences(Assembly assembly)
		{
			var invalidPrefixes = new string[] {"Unity.", "UnityEditor.", "UnityEngine."};
			foreach (var reference in assembly.assemblyReferences)
			{
				var name = reference.name;
				var valid = true;
				foreach (var prefix in invalidPrefixes)
				{
					if (name.StartsWith(prefix))
					{
						valid = false;
						break;
					}
				}

				if (valid)
				{
					yield return reference;
				}
			}
		}

		static string GenerateProjectReferenceEntry(Assembly reference, string csProjDir)
		{
			// the project will be generated in a folder next to the project path
			var path = Path.Combine("..", reference.name, reference.name + ".csproj");
			return PROJECT_TEMPLATE.Replace(KEY_INCLUDE, path);
		}
		
		static string GenerateCompileSourceEntry(string source, string csProjDir)
		{
			source = Path.GetRelativePath(csProjDir, source);
			return SOURCE_TEMPLATE.Replace(KEY_INCLUDE, source);
		}

		static string GenerateDllReferenceEntry(string dllPath, string csProjDir)
		{
			dllPath = Path.GetRelativePath(csProjDir, dllPath);
			var name = Path.GetFileNameWithoutExtension(dllPath);
			return DLL_TEMPLATE.Replace(KEY_INCLUDE, name)
			                   .Replace(KEY_HINT, dllPath);
		}
	}
}
