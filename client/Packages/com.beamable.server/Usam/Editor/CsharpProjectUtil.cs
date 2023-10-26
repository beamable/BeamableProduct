using System;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace Beamable.Server.Editor.Usam
{
	public static class CsharpProjectUtil
	{
		
		private const string KEY_COMPILED_FILES = "[COMPILED_FILES]";
		private const string KEY_OTHER_PROJECTS = "[PROJECT_REFS]";
		private const string KEY_INCLUDE = "[INCLUDE]";
		private const string KEY_FOLDER = "FOLDER";
		private const string KEY_BEAMABLE_VERSION = "[BEAM_VERSION]";

		private static readonly string TEMPLATE_OUTPUT_DIR =
			Path.Combine("Library", "BeamableEditor", "GeneratedProjects", KEY_FOLDER);
		private static readonly string SOURCE_TEMPLATE = $"<Compile Include=\"{KEY_INCLUDE}\" />";
		private static readonly string PROJECT_TEMPLATE = $"<ProjectReference Include=\"{KEY_INCLUDE}\" />";
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
    </ItemGroup>

    <!-- Source files -->
    <ItemGroup>
	{KEY_COMPILED_FILES}
	</ItemGroup>

    <!-- Other projects -->
    <ItemGroup>
	{KEY_OTHER_PROJECTS}
	</ItemGroup>

</Project>
";

		
		
		
		public static string GenerateCsharpProject(Assembly assembly, string csProjDir)
		{
			var sourceFiles = string.Join(Environment.NewLine,
			            assembly.sourceFiles.Select(x => GenerateCompileSourceEntry(x, csProjDir)));

			var projectReferences = string.Join(Environment.NewLine,
			                                    assembly.assemblyReferences.Select(
				                                    x => GenerateProjectReferenceEntry(x, csProjDir)));
			
			var file = TEMPLATE
					.Replace(KEY_COMPILED_FILES, sourceFiles)
					.Replace(KEY_OTHER_PROJECTS, projectReferences)
				;

			return file;
		}

		static string GenerateProjectReferenceEntry(Assembly reference, string csProjDir)
		{
			// the project will be generated in a folder next to the project path
			var path = Path.Combine(csProjDir, "..", reference.name, reference.name + ".csproj");
			return PROJECT_TEMPLATE.Replace(KEY_INCLUDE, path);
		}
		
		static string GenerateCompileSourceEntry(string source, string csProjDir)
		{
			source = Path.GetRelativePath(csProjDir, source);
			return SOURCE_TEMPLATE.Replace(KEY_INCLUDE, source);
		}
	}
}
