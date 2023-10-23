using Beamable.Common;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Beamable.Server.Editor.Usam
{
	public class CodeService : ILoadWithContext
	{
		private readonly BeamCommands _cli;
		
		
		public Promise OnReady { get; }

		public List<ServiceInfo> Services = new List<ServiceInfo>();
		
		private static readonly List<string> IgnoreFolderSuffixes = new List<string> {"~", "obj", "bin"};

		public CodeService(BeamCommands cli)
		{
			_cli = cli;
			OnReady = Init();
		}
		
		public async Promise Init()
		{
			Debug.Log("Running init");
			await SetManifest(_cli);
			await RefreshServices();
			Debug.Log("Done");
		}

		public async Promise RefreshServices()
		{
			var ps = _cli.ProjectList();
			ps.OnStreamListCommandResult(cb =>
			{
				Services.Clear();
				Services.AddRange(cb.data.localServices);
			});
			await ps.Run();
		}


		public static async Promise SetManifest(BeamCommands cli)
		{
			var files = GetBeamServices();
			var args = new ServicesSetLocalManifestArgs();
			args.localHttpNames = new string[files.Count];
			args.localHttpContexts = new string[files.Count];
			args.localHttpDockerfiles = new string[files.Count];
			// TODO: add some validation to check that these files actually make sense
			for (var i = 0; i < files.Count; i++)
			{
				args.localHttpNames[i] = files[i].name;
				args.localHttpContexts[i] = files[i].assetRelativePath; // TODO: this isn't always true. It is probably actually the case that we want to the copy the Unity project, and set the Dockerfile route
				args.localHttpDockerfiles[i] = files[i].relativeDockerFile;
			}

			var command = cli.ServicesSetLocalManifest(args);
			await command.Run();
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
			if (!Directory.Exists(directoryPath))
			{
				return;
			}

			try
			{
				foreach (var file in Directory.GetFiles(directoryPath))
				{
					if (Path.GetExtension(file) == targetExtension)
					{
						foundFiles.Add(file);
					}
				}

				foreach (var subdirectory in Directory.GetDirectories(directoryPath))
				{
					var folderName = Path.GetFileName(subdirectory);

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
					ScanDirectoryRecursive(subdirectory, targetExtension, excludeFolders, foundFiles);

				}
			}
			catch (UnauthorizedAccessException ex)
			{
				Debug.LogError($"Beam Error accessing {directoryPath}: {ex.Message}");
			}
		}
		
		
	}
}
