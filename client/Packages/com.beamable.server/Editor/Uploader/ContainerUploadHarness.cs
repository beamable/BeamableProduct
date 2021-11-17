using Beamable.Editor;
using Beamable.Server.Editor.DockerCommands;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Beamable.Server.Editor.Uploader
{
	/// <summary>
	/// Container uploader sub-component of Beamable.
	/// </summary>
	public class ContainerUploadHarness
	{
		private readonly CommandRunnerWindow _context;

		public event Action<float, long, long> onProgress;

		public ContainerUploadHarness(CommandRunnerWindow context)
		{
			_context = context;
		}

		/// <summary>
		/// Log a message to the progress panel.
		/// </summary>
		public void Log(string message)
		{
			// TODO add back in a progress system
			//         ProgressPanel.LogMessage(message);
			Debug.Log($"Container Upload msg=[{message}]");
		}

		/// <summary>
		/// Receive a progress report and display it.
		/// </summary>
		public void ReportUploadProgress(string name, long amount, long total)
		{
			var progress = total == 0 ? 1 : (float)amount / total;
			Debug.Log($"PROGRESS HAPPENED. name=[{name}] amount=[{amount}] total=[{total}]");
			//ProgressPanel.ReportLayerProgress(name, progress);
			onProgress?.Invoke(progress, amount, total);
		}

		public async Task<string> GetImageId(MicroserviceDescriptor descriptor)
		{
			var command = new GetImageIdCommand(descriptor);
			var imageId = await command.Start(null);

			return imageId;
		}

		public async Task SaveImage(MicroserviceDescriptor descriptor, string outputPath, string imageId = null)
		{
			if (imageId == null)
			{
				imageId = await GetImageId(descriptor);
			}

			var saveImageCommand = new SaveImageCommand(descriptor, imageId, outputPath);

			await saveImageCommand.Start(_context);
		}

		/// <summary>
		/// Upload the specified container to the private Docker registry.
		/// </summary>
		public async Task UploadContainer(MicroserviceDescriptor descriptor,
										  Action onSuccess,
										  Action onFailure,
										  string imageId = null)
		{
			// TODO: Either check disk space prior to extraction, or offer a streaming-only solution? ~ACM 2019-12-18
			var filename = FileUtil.GetUniqueTempPathInProject() + ".tar";
			var folder = FileUtil.GetUniqueTempPathInProject();

			try
			{
				if (imageId == null)
				{
					imageId = await GetImageId(descriptor);
				}

				await SaveImage(descriptor, filename, imageId);
				using (var file = File.OpenRead(filename))
				{
					var tar = TarArchive.CreateInputTarArchive(file, Encoding.Default);
					tar.ExtractContents(folder);
				}

				var beamable = await EditorAPI.Instance;
				var uploader = new ContainerUploader(beamable, this, descriptor, imageId);
				await uploader.Upload(folder);
				Debug.Log("Finished upload");

				onSuccess?.Invoke();
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
				onFailure?.Invoke();
				throw ex;
			}
			finally
			{
				Directory.Delete(folder, true);
				File.Delete(filename);
			}
		}
	}
}
