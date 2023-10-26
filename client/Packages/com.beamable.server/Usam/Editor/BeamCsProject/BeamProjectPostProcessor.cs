using NUnit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable.Server.Editor.Usam.BeamCsProject
{
	public class BeamProjectPostProcessor : AssetPostprocessor
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

		public static string MakeFileRelative(string assetPath)
		{

			return Path.GetRelativePath(TEMPLATE_OUTPUT_DIR, assetPath);
		}
		
		
		public static string GenerateBeamProject(string unityCsProjPath, string unityCsProjContent)
		{
			var sourceFiles = ExtractCompiledSourceFiles(unityCsProjContent);
			var compiledSection = string.Join(Environment.NewLine,
			                                  sourceFiles.Select(x => SOURCE_TEMPLATE.Replace(KEY_INCLUDE, MakeFileRelative(x))));

			var projectRefs = ExtractProjectReferences(unityCsProjContent);
			var projectRefSection = string.Join(Environment.NewLine,
			                                    projectRefs.Select(
				                                    x => PROJECT_TEMPLATE.Replace(KEY_INCLUDE, MakeFileRelative(x))));

			var source = TEMPLATE
			             .Replace(KEY_COMPILED_FILES, compiledSection)
			             .Replace(KEY_OTHER_PROJECTS, projectRefSection)
			             .Replace(KEY_BEAMABLE_VERSION, BeamableEnvironment.SdkVersion.ToString());
				;
			
			return source;
		}

		[MenuItem("CSPROJ/TEST")]
		public static void Test()
		{
			GetAssemblyDependencyGraph();
			// var path = "/Users/chrishanna/Documents/Github/BeamableProduct/client/Tunacan.csproj";
			// var content = File.ReadAllText(path);
			//
			// var output = GenerateBeamProject(path, content);
			// Debug.Log(output);
		}
		
		static void GetAssemblyDependencyGraph()
		{
			// Get all assembly definitions in the project
			Assembly[] assemblies = CompilationPipeline.GetAssemblies();

			// CompilationPipeline.
			// Create a dictionary to store the assembly dependencies
			Dictionary<string, List<string>> dependencyGraph = new Dictionary<string, List<string>>();

			// Iterate through each assembly and its references
			foreach (Assembly assembly in assemblies)
			{
				string assemblyName = assembly.name;
				// assembly.sourceFiles
				string[] references = assembly.assemblyReferences.Select(x => x.name).ToArray();

				// Add the assembly and its references to the graph
				dependencyGraph[assemblyName] = new List<string>(references);
			}

			// Print the dependency graph to the console
			foreach (var entry in dependencyGraph)
			{
				string assemblyName = entry.Key;
				List<string> references = entry.Value;
				string referencesStr = string.Join(", ", references.ToArray());
				Debug.Log($"{assemblyName} -> {referencesStr}");
			}
		}

		public static string[] ExtractCompiledSourceFiles(string content)
		{
			var regexPattern = new Regex("<Compile Include=\"(.+?)\"");
			var matches = regexPattern.Matches(content);
			var results = new string[matches.Count];
			for (var i = 0; i < matches.Count; i++)
			{
				results[i] = matches[i].Groups[1].Value;
			}
			return results;
		}

		public static string[] ExtractProjectReferences(string content)
		{
			var regexPattern = new Regex("<ProjectReference Include=\"(.+?)\"");
			var matches = regexPattern.Matches(content);
			var results = new string[matches.Count];
			for (var i = 0; i < matches.Count; i++)
			{
				results[i] = matches[i].Groups[1].Value;
			}
			return results;
		}

		private static Assembly[] _assemblies;
		private static Dictionary<string, Assembly> _nameToAssembly;
		private static Dictionary<Assembly, Assembly[]> _assemblyGraph;

		private static HashSet<string> _referencedAssemblies = new HashSet<string>();
		
		private static bool OnPreGeneratingCSProjectFiles()
		{
			_assemblies = CompilationPipeline.GetAssemblies();
			Debug.Log("Generating CS PROJ files");
			
			
			// CompilationPipeline.
			// Create a dictionary to store the assembly dependencies
			_assemblyGraph = new Dictionary<Assembly, Assembly[]>();
			_nameToAssembly = new Dictionary<string, Assembly>();
			_referencedAssemblies.Clear();
			// Iterate through each assembly and its references
			foreach (Assembly assembly in _assemblies)
			{
				string assemblyName = assembly.name;
				_nameToAssembly[assemblyName] = assembly;
				_assemblyGraph[assembly] = assembly.assemblyReferences;
			}
			
			var beamServices = CodeService.GetBeamServices();
			foreach (var service in beamServices)
			{
				foreach (var reference in service.assemblyReferences)
				{
					_referencedAssemblies.Add(reference);
				}
			}


			foreach (var reference in _referencedAssemblies)
			{
				Debug.Log("REFERENCING " + reference);
				if (!_nameToAssembly.TryGetValue(reference, out var assembly))
				{
					continue;
				}

				foreach (var src in assembly.sourceFiles)
				{
					Debug.Log("  SRC-" + src);
				}

				foreach (var asm in assembly.assemblyReferences)
				{
					Debug.Log("  ASM-" + asm.name);
				}
			}
			
			return false; // if we don't return false, then this methods PREVENTS Unity from generating csproj files what-so-ever.
		}

		private static string OnGeneratedCSProject2(string path, string content)
		{
			// Debug.Log("GENERATING " + path);
			/*
			 *
			 *<Reference Include="Newtonsoft.Json">
			   <HintPath>/Users/chrishanna/Documents/Github/BeamableProduct/client/Library/PackageCache/com.unity.nuget.newtonsoft-json@3.0.2/Runtime/Newtonsoft.Json.dll</HintPath>
			   </Reference>
			 *
			 *     <ProjectReference Include="Truckster.csproj">
			   <Project>{1ff9d068-4c38-dcf5-8813-aa9c1d8147e8}</Project>
			   <Name>Truckster</Name>
			   </ProjectReference>
			   
			    <Compile Include="Assets/Tunacan/TunacanLand.cs" />
			   <Compile Include="Assets/Tunacan/Sub/Deepblue.cs" />
			 */
			
			var name = Path.GetFileNameWithoutExtension(path);
			if (_referencedAssemblies.Contains(name))
			{
				
			}
			
			if (path.ToLower().Contains("tunacan"))
			{
				// Debug.Log("FOUND TUNA CAN " + path);
				// Debug.Log(content);
				// var name = Path.GetFileNameWithoutExtension(path);

				// if (_nameToAssembly.TryGetValue(name, out var assembly))
				// {
				// 	Debug.Log("FOUND TUNA ASSEMBLY!");
				// 	foreach (var x in _assemblyGraph[assembly])
				// 	{
				// 		Debug.Log(" depends on " + x.name);
				// 	}
				// }
				//
				
				// var outputDir = TEMPLATE_OUTPUT_DIR.Replace(KEY_FOLDER, name);
				// var outputName = Path.Combine(outputDir, name + ".csproj");
				// var gen = GenerateBeamProject(path, content);
				//
				// Directory.CreateDirectory(outputDir);
				// File.WriteAllText(outputName, gen);
				// Debug.Log(outputName);
				// Debug.Log(gen);
			}
			return content;
		}
	}
}
