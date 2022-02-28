using Beamable.Server.Editor.CodeGen;
using System;
using System.IO;
using UnityEditor.Compilation;
using UnityEngine;

namespace Beamable.Server.Editor
{
	public static class BuildUtils
	{
		// static BuildUtils()
		// {
		// 	CompilationPipeline.
		// }

		public static void PrepareBuildContext(MicroserviceDescriptor descriptor, bool includeDebugTools, bool watch)
		{
			try
			{
				FileUtils.CleanBuildDirectory(descriptor);

				var dependencies = DependencyResolver.GetDependencies(descriptor);
				FileUtils.CopyAssemblies(descriptor, dependencies);
				FileUtils.CopyDlls(descriptor, dependencies);
				FileUtils.CopySingleFiles(descriptor, dependencies);

				// var dependencies = UpdateBuildContextWithSource(descriptor);

				var programFilePath = Path.Combine(descriptor.BuildPath, "Program.cs");
				var csProjFilePath = Path.Combine(descriptor.BuildPath, $"{descriptor.ImageName}.csproj");
				var dockerfilePath = Path.Combine(descriptor.BuildPath, "Dockerfile");
				var robotFilePath = Path.Combine(descriptor.BuildPath, "Beamable__Change_Token_Class.cs");
				(new ProgramCodeGenerator(descriptor)).GenerateCSharpCode(programFilePath);
				(new DockerfileGenerator(descriptor, includeDebugTools, watch)).Generate(dockerfilePath);
				(new ProjectGenerator(descriptor, dependencies)).Generate(csProjFilePath);
				(new RobotTokenGenerator(descriptor)).GenerateFile(robotFilePath);

			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		public static void UpdateBuildContextWithSource(MicroserviceDescriptor descriptor)
		{
			// TODO: The full copy overwrite doesn't work for dotnet watch.
			var dependencies = DependencyResolver.GetDependencies(descriptor);
			FileUtils.CopyAssemblies(descriptor, dependencies);
			FileUtils.CopyDlls(descriptor, dependencies);
			FileUtils.CopySingleFiles(descriptor, dependencies);
			var robotFilePath = Path.Combine(descriptor.BuildPath, "Beamable__Change_Token_Class.cs");
			(new RobotTokenGenerator(descriptor)).GenerateFile(robotFilePath);
		}
	}
}
