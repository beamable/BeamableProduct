using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Common.Content;
using Beamable.Content;
using Beamable.Editor.BeamCli.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants.Features.Content;

namespace Beamable.Editor.ContentService
{
	public class ContentBaker
	{
		[MenuItem(Constants.MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Bake Content")]
		public static void BakeContent_Window()
		{
			Debug.Log("Baking content...");
			var task = BakeContent(false);
			EditorUtility.DisplayProgressBar("Baking Content", "Baking content!", 0);

			void OnUpdate()
			{
				EditorUtility.DisplayProgressBar("Baking Content", "Baking content!", 0.5f);
				if (!task.IsCompleted) return;

				EditorUtility.ClearProgressBar();
				EditorApplication.update -= OnUpdate;
				if (task.IsFaulted)
				{
					Debug.LogException(task.Exception.GetBaseException());
				}
				else if (!task.IsCanceled)
				{
					Debug.Log("Finished Baking content!");
				}

				if (Application.isBatchMode)
				{
					EditorApplication.Exit(task.IsFaulted || task.IsCanceled ? 1 : 0);
				}
			}

			EditorApplication.update += OnUpdate;
		}

		public static async Task BakeContent(bool skipCheck)
		{
			var api = BeamEditorContext.Default;
			await api.InitializePromise;
			var configuration = ContentConfiguration.Instance;
			var manifestId = configuration.RuntimeManifestID;

			// ContentPs synchronizes published changes before reporting local status. Always await it,
			// including automatic builds, and use the runtime manifest rather than the editor selection.
			bool hasLocalChanges = false;
			var status = api.Cli.ContentPs(new ContentPsArgs {manifestIds = new[] {manifestId}});
			status.OnStreamContentPsCommandEvent(report =>
			{
				hasLocalChanges |= report.data.RelevantManifestsAgainstLatest
					.Any(manifest => manifest.Entries.Any(entry => entry.StatusEnum != ContentStatus.UpToDate));
			});
			await RunCommand(status, "content ps");

			if (!skipCheck && hasLocalChanges && !EditorUtility.DisplayDialog("Local changes",
				    "You have local changes in your content. " +
				    "Do you want to proceed with baking using only the unchanged data?", "Yes", "No"))
			{
				return;
			}

			var bake = api.Cli.ContentBake(new ContentBakeArgs
			{
				source = "local",
				format = "unity",
				manifestId = manifestId,
				// Generated CLI arguments need quotes around paths that may contain spaces.
				outputDir = "\"" + BEAMABLE_RESOURCES_PATH + "\"",
				localChanges = "skip",
				compression = configuration.EnableBakedContentCompression ? "gzip" : "none"
			});
			bake.OnStreamContentBakeResult(report =>
				Debug.Log($"[Bake Content] Baked {report.data.Count} content objects to '{report.data.ContentPath}'"));
			await RunCommand(bake, "content bake");
			AssetDatabase.Refresh();
		}

		private static async Task RunCommand(BeamCommandWrapper command, string commandName)
		{
			// BeamWebCommand dispatches CLI errors without rejecting its completion promise.
			// Its completion waits for dispatched callbacks, so inspect errors after awaiting it.
			var errors = new List<ErrorOutput>();
			command.OnError(report => errors.Add(report.data));
			await command.Run();
			if (errors.Count > 0)
			{
				throw new CliInvocationException(commandName, errors);
			}
		}
	}
}
