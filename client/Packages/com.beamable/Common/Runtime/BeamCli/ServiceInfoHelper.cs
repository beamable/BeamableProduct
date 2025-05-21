// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

﻿using Beamable.Common.BeamCli.Contracts;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Beamable.Common.BeamCli
{
	public static class ServiceInfoHelper
	{
		public static void OpenCode(this ServiceInfo info)
		{
			var path = new DirectoryInfo(Path.GetFullPath(info.projectPath)).Parent;
			var solutionFiles = Directory.EnumerateFiles(path!.FullName, "*.sln", SearchOption.AllDirectories).ToList();
			switch (solutionFiles.Count)
			{
				case 0:
					Debug.LogError("Could not find any");
					break;
				case 1:
					Debug.Log(solutionFiles[0]);
					System.Diagnostics.Process.Start(solutionFiles[0]);
					break;
				default:
					Debug.LogError($"Found multiple solution files: {string.Join(",", solutionFiles)}");
					break;
			}
		}
	}
}
