using Beamable.Common;
using Beamable.Editor.BeamCli;
using Beamable.Editor.BeamCli.Commands;
using UnityEditor;
using UnityEngine;
using static Beamable.Common.Constants;

namespace Beamable.Editor
{
	/// <summary>
	/// Whether the editor's CLI server also serves MCP to AI agents (`server serve --enable-mcp`). A per-user choice,
	/// since it lets local agents run CLI commands with this user's credentials.
	/// </summary>
	public static class McpServerSettings
	{
		private const string PREF_KEY = "Beamable.Cli.EnableMcpServer";

		public static bool Enabled
		{
			get => EditorPrefs.GetBool(PREF_KEY, false);
			set => EditorPrefs.SetBool(PREF_KEY, value);
		}
	}

	public static class McpMenuItems
	{
		private const string ENABLE_HTTP_MENU = MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Enable MCP for AI Agents";

		[MenuItem(ENABLE_HTTP_MENU, true)]
		private static bool ValidateToggleMcpForAgents()
		{
			Menu.SetChecked(ENABLE_HTTP_MENU, McpServerSettings.Enabled);
			return true;
		}

		/// <summary>
		/// Restarts the editor's CLI server with or without the MCP endpoint, and when turning it on, points the
		/// project's .mcp.json at it so agents opened in this project discover it.
		/// </summary>
		[MenuItem(ENABLE_HTTP_MENU)]
		private static async void ToggleMcpForAgents()
		{
			var enable = !McpServerSettings.Enabled;
			McpServerSettings.Enabled = enable;

			var api = BeamEditorContext.Default;
			await api.InitializePromise;
			var factory = api.ServiceScope.GetService<BeamWebCommandFactory>();
			factory.KillServer();
			await factory.EnsureServerIsRunning();

			if (!enable)
			{
				Debug.Log("[Beamable] The CLI server no longer serves MCP. Agents configured with its URL will not connect.");
				return;
			}

			var command = api.Cli.McpSetup(new McpSetupArgs { transport = "http" });
			string configPath = null;
			string error = null;
			command.OnStreamMcpSetupCommandResult(cb => configPath = cb.data.configPath);
			// The CLI reports a failed setup as an error event; the command itself still completes.
			command.OnError(cb => error = cb.data.message);
			try
			{
				await command.Run();
			}
			catch (System.Exception ex)
			{
				error = ex.Message;
			}

			if (string.IsNullOrEmpty(configPath))
			{
				error = string.IsNullOrEmpty(error) ? "The CLI did not report where it wrote the config." : error;
				Debug.LogError($"[Beamable] Failed to configure MCP for AI agents: {error}");
				EditorUtility.DisplayDialog("MCP Setup Failed", $"Could not point AI agents at the editor's CLI server.\n\n{error}", "OK");
				return;
			}

			Debug.Log($"[Beamable] AI agents can use the Beamable CLI over MCP. Config written to: {configPath}");
			EditorUtility.DisplayDialog(
				"MCP Enabled for AI Agents",
				$"The editor's CLI server now serves MCP, and {configPath} points AI agents at it.\n\n" +
				"Agents connect while this editor is open. `beam mcp status` tells them whether it is.",
				"OK");
		}
		
		[MenuItem(MenuItems.Windows.Paths.MENU_ITEM_PATH_WINDOW_BEAMABLE_UTILITIES + "/Install MCP Server")]
		private static async void InstallMcpServer()
		{
			var api = BeamEditorContext.Default;
			await api.InitializePromise;

			var command = api.Cli.McpSetup(new McpSetupArgs());
			string resultPath = null;

			string error = null;
			command.OnStreamMcpSetupCommandResult(cb =>
			{
				resultPath = cb.data.configPath;
			});
			command.OnError(cb => error = cb.data.message);

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
				else
				{
					throw new System.Exception(error ?? "The CLI did not report where it wrote the config.");
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
