using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Semantics;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class CodeService : ILoadWithContext
	{
		private readonly BeamCommands _cli;


		public Promise OnReady { get; private set; }
		public bool IsDockerRunning { get; private set; }
		public List<ServiceInfo> Services { get; private set; } = new List<ServiceInfo>();

		private static readonly List<string> IgnoreFolderSuffixes = new List<string> { "~", "obj", "bin" };
		private List<BeamServiceSignpost> _services;

		public CodeService(BeamCommands cli)
		{
			_cli = cli;
			OnReady = Init();
		}


		public async Promise Init()
		{
			Debug.Log("Running init");
			_services = GetBeamServices();
			Debug.Log("have services");
			// TODO: we need validation. What happens if the .beamservice files point to non-existent files
			SetSolution(_services);
			Debug.Log("setsolution");

			await SetManifest(_cli, _services);
			Debug.Log("setmanifest");

			await RefreshServices();
			await UpdateServicesVersions();
			Debug.Log("Done");
		}

		public async Promise UpdateServicesVersions()
		{
			var version = new BeamVersionResults();
			var versionCommand = _cli.Version(new VersionArgs()
			{
				showVersion = true,
				showLocation = true,
				showTemplates = true,
				showType = true
			}).OnStreamVersionResults(result =>
			{
				version = result.data;
			});
			await versionCommand.Run().Error(Debug.LogException);

			if (string.IsNullOrEmpty(version?.version))
			{
				Debug.Log("Could not detect current version, skipping");
				return;
			}
			var versions = _cli.ProjectVersion(new ProjectVersionArgs
			{
				requestedVersion = version?.version
			});
			versions.OnStreamProjectVersionCommandResult(result =>
			{
				EditorApplication.delayCall += () =>
				{
					Debug.Log("Versions updated");
				};
			});
			await versions.Run().Error(Debug.LogException);
		}

		public async Promise RefreshServices()
		{
			var ps2 = _cli.ServicesPs(new ServicesPsArgs() { json = false, remote = true });
			BeamServiceListResult result = null;
			ps2.OnStreamServiceListResult(cb =>
			{
				result = cb.data;
				IsDockerRunning = cb.data.IsDockerRunning;
			});
			await ps2.Run().Error(Debug.LogException);

			var ps = _cli.ProjectList();
			ps.OnStreamListCommandResult(cb =>
			{
				Services.Clear();
				Services.AddRange(cb.data.localServices.Where(i => !string.IsNullOrEmpty(i.dockerfilePath)));
			});
			await ps.Run().Error(Debug.LogException);
		}

		/// <summary>
		/// Regenerates the files: Program.cs, Dockerfile and .csproj. Then copy these files
		/// to the desired Standalone Microservice.
		/// </summary>
		/// <param name="signPost">The signpost asset that references to the project in which wants to regenerate the files.</param>
		public async Promise RegenerateProjectFiles(BeamServiceSignpost signPost)
		{
			var tempPath = $"Temp/{signPost.name}";
			var projName = new ServiceName(signPost.name);
			var projPath = signPost.relativeDockerFile.Replace("/Dockerfile", "");

			var args = new ProjectRegenerateArgs() { name = projName, output = tempPath, copyPath = projPath };
			var command = _cli.ProjectRegenerate(args);
			await command.Run();
		}

		/// <summary>
		/// Update the sln file to add references to known beam services.
		/// This may cause a script reload if the sln file needs to regenerate
		/// </summary>
		/// <param name="services"></param>
		public static void SetSolution(List<BeamServiceSignpost> services)
		{
			// find the local sln file
			var slnPath = FindFirstSolutionFile();
			if (string.IsNullOrEmpty(slnPath) || !File.Exists(slnPath))
			{
				Debug.Log("Beam. No script file, so reloading scripts");
				UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
				return; // once scripts reload, the current invocation of scripts end.
			}

			var contents = File.ReadAllText(slnPath);

			var generatedContent = SolutionPostProcessor.OnGeneratedSlnSolution(slnPath, contents);
			var areDifferent = generatedContent != contents; // TODO: is there a better way to check if the solution file needs to be regenerated? This feels like it could become a bottleneck.
			if (areDifferent)
			{
				// force the sln file to be re-generated, by deleting it. // TODO: we'll need to "unlock" the file in certain VCS
				File.Delete(slnPath);
				UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
			}
		}

		private static string FindFirstSolutionFile()
		{
			var files = Directory.GetFiles(".");
			foreach (var file in files)
			{
				if (Path.GetExtension(file) == ".sln")
				{
					return file;
				}
			}

			return null;
		}

		public static async Promise SetManifest(BeamCommands cli, List<BeamServiceSignpost> files)
		{
			var args = new ServicesSetLocalManifestArgs();
			args.localHttpNames = new string[files.Count];
			args.localHttpContexts = new string[files.Count];
			args.localHttpDockerFiles = new string[files.Count];
			// TODO: add some validation to check that these files actually make sense
			for (var i = 0; i < files.Count; i++)
			{
				args.localHttpNames[i] = files[i].name;
				args.localHttpContexts[i] = files[i].assetRelativePath;
				args.localHttpDockerFiles[i] = files[i].relativeDockerFile;
			}


			var command = cli.ServicesSetLocalManifest(args);
			await command.Run().Error(ex =>
			{
				Debug.LogError(ex);
			});
		}


		public static List<BeamServiceSignpost> GetBeamServices()
		{
			var files = GetSignpostFiles(".beamservice");
			var data = GetSignpostData<BeamServiceSignpost>(files);
			return data;
		}

		public static List<T> GetSignpostData<T>(IEnumerable<string> files)
		{
			var output = new List<T>();
			foreach (var file in files)
			{
				var json = File.ReadAllText(file);
				var data = JsonUtility.FromJson<T>(json);
				output.Add(data);
			}
			return output;
		}

		private static IEnumerable<string> GetSignpostFiles(string extension)
		{
			var files = new HashSet<string>();

			ScanDirectoryRecursive("Assets", extension, IgnoreFolderSuffixes, files);
			ScanDirectoryRecursive("Packages", extension, IgnoreFolderSuffixes, files);
			return files;
		}

		private static void ScanDirectoryRecursive(string directoryPath, string targetExtension, List<string> excludeFolders, HashSet<string> foundFiles)
		{
			// TODO: ChatGPT wrote this, but actually, it should use a queue<string> to do a non-stack-recursive BFS over the file system
			if (!Directory.Exists(directoryPath))
			{
				return;
			}


			var directories = new Queue<string>();
			directories.Enqueue(directoryPath);

			while (directories.Count > 0)
			{
				try
				{
					var dir = directories.Dequeue();
					var folderName = Path.GetFileName(dir);

					var exclude = false;
					foreach (var excludeSuffix in excludeFolders)
					{
						if (folderName.EndsWith(excludeSuffix))
						{
							exclude = true;
							break;
						}
					}

					if (exclude) continue;

					foreach (var file in Directory.GetFiles(dir))
					{
						if (Path.GetExtension(file) == targetExtension)
						{
							foundFiles.Add(file);
						}
					}

					foreach (var subDir in Directory.GetDirectories(dir))
					{
						directories.Enqueue(subDir);
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					Debug.LogError($"Beam Error accessing {directoryPath}: {ex.Message}");
				}
			}
		}


	}
}