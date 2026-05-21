using Beamable.Editor.BeamCli.Commands;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants;

namespace Beamable.Editor
{
	public static class McpMenuItems
	{
		
		[MenuItem(MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Install MCP Server")]
		private static async void InstallMcpServer()
		{
			var api = BeamEditorContext.Default;
			await api.InitializePromise;

			var command = api.Cli.McpSetup(new McpSetupArgs());
			string resultPath = null;

			command.OnStreamMcpSetupCommandResult(cb =>
			{
				resultPath = cb.data.configPath;
			});

			try
			{
				await command.Run();
				if (!string.IsNullOrEmpty(resultPath))
				{
					Debug.Log($"[Beamable] MCP Server config written to: {resultPath}");
					EditorUtility.DisplayDialog(
						"MCP Server Installed",
						$"MCP config written to:\n{resultPath}\n\nAI clients can now discover Beamable CLI tools via this config.",
						"OK");
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogError($"[Beamable] Failed to install MCP Server: {ex.Message}");
				EditorUtility.DisplayDialog(
					"MCP Server Install Failed",
					$"Could not install MCP Server config.\n\n{ex.Message}",
					"OK");
			}
		}
	}
}
