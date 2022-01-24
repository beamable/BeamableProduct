using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Beamable.Common;
using Beamable.Server.Editor.CodeGen;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Beamable.Server.Editor.DockerCommands
{
	public class BuildImageCommand : DockerCommandReturnable<Unit>
	{
		private const string BUILD_PREF = "{0}BuildAtLeastOnce";
		private MicroserviceDescriptor _descriptor;
		public bool IncludeDebugTools { get; }
		public string ImageName { get; }
		public string BuildPath { get; }
		public Promise<Unit> ReadyForExecution { get; }

		public static bool WasEverBuildLocally(IDescriptor descriptor)
		{
			return EditorPrefs.GetBool(string.Format(BUILD_PREF, descriptor.Name), false);
		}

		static void SetAsBuild(IDescriptor descriptor)
		{
			EditorPrefs.SetBool(string.Format(BUILD_PREF, descriptor.Name), true);
		}

		public BuildImageCommand(MicroserviceDescriptor descriptor, bool includeDebugTools)
		{
			_descriptor = descriptor;
			IncludeDebugTools = includeDebugTools;
			ImageName = descriptor.ImageName;
			BuildPath = descriptor.BuildPath;
			UnityLogLabel = "[BUILD]";
			ReadyForExecution = new Promise<Unit>();
			// copy the cs files from the source path to the build path
			// build the Program file, and place it in the temp dir.
			try
			{
				CleanBuildDirectory(descriptor);

				var dependencies = DependencyResolver.GetDependencies(descriptor);
				CopyAssemblies(descriptor, dependencies);
				CopyDlls(descriptor, dependencies);
				CopySingleFiles(descriptor, dependencies);

				var programFilePath = Path.Combine(descriptor.BuildPath, "Program.cs");
				var csProjFilePath = Path.Combine(descriptor.BuildPath, $"{descriptor.ImageName}.csproj");
				var dockerfilePath = Path.Combine(descriptor.BuildPath, "Dockerfile");
				(new ProgramCodeGenerator(descriptor)).GenerateCSharpCode(programFilePath);
				(new DockerfileGenerator(descriptor, IncludeDebugTools)).Generate(dockerfilePath);
				(new ProjectGenerator(descriptor, dependencies)).Generate(csProjFilePath);

				// TODO: Check that no UnityEngine references exist.
				// TODO: Check that there are no invalid types in the serialization process.
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}

		private void CopyDlls(MicroserviceDescriptor descriptor, MicroserviceDependencies dependencies)
		{
			string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

			foreach (var dll in dependencies.DllsToCopy)
			{
				var sourceDirectory = Path.GetDirectoryName(dll.assetPath);
				var fullSource = Path.Combine(rootPath, sourceDirectory);
				Debug.Log("Copying dll from " + fullSource);

				// TODO: better folder namespacing?
				CopyFolderToBuildDirectory(fullSource, "libdll", descriptor);
			}
		}

		private void CopyAssemblies(MicroserviceDescriptor descriptor, MicroserviceDependencies dependencies)
		{
			string rootPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);

			// copy over the assembly definition folders...
			if (dependencies.Assemblies.Invalid.Any())
			{
				throw new Exception(
					$"Invalid dependencies discovered for microservice. {string.Join(",", dependencies.Assemblies.Invalid.Select(x => x.Name))}");
			}

			foreach (var assemblyDependency in dependencies.Assemblies.ToCopy)
			{
				var sourceDirectory = Path.GetDirectoryName(assemblyDependency.Location);
				var fullSource = Path.Combine(rootPath, sourceDirectory);
				Debug.Log("Copying assembly from " + fullSource);

				// TODO: better folder namespacing?
				CopyFolderToBuildDirectory(fullSource, assemblyDependency.Name, descriptor);
			}
		}

		private void CleanBuildDirectory(MicroserviceDescriptor descriptor)
		{
			// remove everything in the hidden folder...
			if (Directory.Exists(descriptor.BuildPath))
			{
				Directory.Delete(descriptor.BuildPath, true);
			}

			Directory.CreateDirectory(descriptor.BuildPath);
		}

		private void CopySingleFiles(MicroserviceDescriptor descriptor, MicroserviceDependencies dependencies)
		{
			// copy over the single files...
			foreach (var dep in dependencies.FilesToCopy)
			{
				var targetRelative = dep.Agnostic.SourcePath.Substring(Application.dataPath.Length - "Assets/".Length);
				var targetFull = descriptor.BuildPath + targetRelative;

				Debug.Log("Copying source code to " + targetFull);
				var targetDir = Path.GetDirectoryName(targetFull);
				Directory.CreateDirectory(targetDir);

				// to avoid any file issues, we load the file into memory
				var src = File.ReadAllText(dep.Agnostic.SourcePath);
				File.WriteAllText(targetFull, src);
			}
		}

		private void CopyFolderToBuildDirectory(string sourceFolderPath,
		                                        string subFolder,
		                                        MicroserviceDescriptor descriptor)
		{
			var directoryQueue = new Queue<string>();
			directoryQueue.Enqueue(sourceFolderPath);

			while (directoryQueue.Count > 0)
			{
				var path = directoryQueue.Dequeue();

				var files = Directory
					.GetFiles(path);
				foreach (var file in files)
				{
					var subPath = file.Substring(sourceFolderPath.Length + 1);
					var destinationFile = Path.Combine(descriptor.BuildPath, subFolder, subPath);

#if UNITY_EDITOR_WIN
					var fullPath = Path.GetFullPath(destinationFile);
					if (fullPath.Length >= 255)
					{
						Debug.LogError(
							$"There could be problems during building {descriptor.Name}- path is too long. " +
							"Consider moving project to another folder so path would be shorter.");
					}
#endif

					Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
					File.Copy(file, destinationFile, true);
				}

				var subDirs = Directory.GetDirectories(path);
				foreach (var subDir in subDirs)
				{
					var dirName = Path.GetFileName(subDir);
					if (new[] {"~", "obj", "bin"}.Contains(dirName) || dirName.StartsWith("."))
						continue; // skip hidden or dumb folders...

					directoryQueue.Enqueue(subDir);
				}
			}
		}

		protected override void ModifyStartInfo(ProcessStartInfo processStartInfo)
		{
			base.ModifyStartInfo(processStartInfo);
			processStartInfo.EnvironmentVariables["DOCKER_BUILDKIT"] =
				MicroserviceConfiguration.Instance.EnableDockerBuildkit ? "1" : "0";
			processStartInfo.EnvironmentVariables["DOCKER_SCAN_SUGGEST"] = "false";
		}

		public override string GetCommandString()
		{
			return $"{DockerCmd} build -t {ImageName} \"{BuildPath}\"";
		}

		protected override void HandleStandardOut(string data)
		{
			if (!MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
			{
				base.HandleStandardOut(data);
			}

			OnStandardOut?.Invoke(data);
		}

		protected override void HandleStandardErr(string data)
		{
			if (!MicroserviceLogHelper.HandleLog(_descriptor, UnityLogLabel, data))
			{
				base.HandleStandardErr(data);
			}

			OnStandardErr?.Invoke(data);
		}

		protected override void Resolve()
		{
			if (string.IsNullOrEmpty(StandardErrorBuffer))
			{
				Promise.CompleteSuccess(PromiseBase.Unit);
				SetAsBuild(_descriptor);
			}
			else
			{
				Promise.CompleteError(new Exception($"Build failed err=[{StandardErrorBuffer}]"));
			}
		}
	}
}
