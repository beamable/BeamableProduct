using Beamable.Common;
using Beamable.Server.Editor.CodeGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

namespace Beamable.Server.Editor.DockerCommands
{
	public class BuildImageCommand : DockerCommandReturnable<Unit>
	{
		private const string BUILD_PREF = "{0}BuildAtLeastOnce";
		private MicroserviceDescriptor _descriptor;
		private readonly bool _pull;
		public bool IncludeDebugTools { get; }
		public string ImageName { get; set; }
		public string BuildPath { get; set; }
		public Promise<Unit> ReadyForExecution { get; private set; }

		private Exception _constructorEx;

		public static bool WasEverBuildLocally(IDescriptor descriptor)
		{
			return EditorPrefs.GetBool(string.Format(BUILD_PREF, descriptor.Name), false);
		}

		static void SetAsBuild(IDescriptor descriptor, bool build = true)
		{
			EditorPrefs.SetBool(string.Format(BUILD_PREF, descriptor.Name), build);
		}

		public BuildImageCommand(MicroserviceDescriptor descriptor, bool includeDebugTools, bool watch, bool pull=true)
		{
			_descriptor = descriptor;
			_pull = pull;
			IncludeDebugTools = includeDebugTools;
			ImageName = descriptor.ImageName;
			BuildPath = descriptor.BuildPath;
			UnityLogLabel = "[BUILD]";
			ReadyForExecution = new Promise<Unit>();
			// copy the cs files from the source path to the build path
			// build the Program file, and place it in the temp dir.
			BuildUtils.PrepareBuildContext(descriptor, includeDebugTools, watch);
		}


		protected override void ModifyStartInfo(ProcessStartInfo processStartInfo)
		{
			base.ModifyStartInfo(processStartInfo);
			processStartInfo.EnvironmentVariables["DOCKER_BUILDKIT"] = MicroserviceConfiguration.Instance.EnableDockerBuildkit ? "1" : "0";
			processStartInfo.EnvironmentVariables["DOCKER_SCAN_SUGGEST"] = "false";
		}

		public override string GetCommandString()
		{
			var pullStr = _pull ? "--pull" : "";
			#if BEAMABLE_DEVELOPER
			pullStr = ""; // we cannot force the pull against the local image.
			#endif

			var platformStr = "--platform linux/amd64";
			#if BEAMABLE_DISABLE_AMD_MICROSERVICE_BUILDS
			platformStr = "";
			#endif

			return $"{DockerCmd} build {pullStr} {platformStr} --label \"beamable-service-name={_descriptor.Name}\" -t {ImageName} \"{BuildPath}\" ";
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
			bool success = string.IsNullOrEmpty(StandardErrorBuffer);
			SetAsBuild(_descriptor, success);
			if (success)
			{
				Promise.CompleteSuccess(PromiseBase.Unit);
			}
			else
			{
				Promise.CompleteError(new Exception($"Build failed err=[{StandardErrorBuffer}]"));
			}
		}
	}
}
