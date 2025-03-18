using Beamable.Common;
using Beamable.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Beamable.Player.CloudSaving
{
	public class CloudDataUpdateBuilder
	{
		private HashSet<string> _filesToForget = new();
		private Dictionary<string, string> _filesToRename = new();
		private HashSet<string> _filesToArchive = new();

		/// <summary>
		/// Gets a list of files that are marked to be removed from the cloud save manifest but kept locally.
		/// </summary>
		public List<string> FilesToForget => _filesToForget.ToList();

		/// <summary>
		/// Gets a dictionary mapping files that are scheduled to be renamed, where the key is the original file name and the value is the new file name.
		/// </summary>
		public Dictionary<string, string> FilesToRename => _filesToRename;

		/// <summary>
		/// Gets a list of files that are scheduled to be archived instead of deleted.
		/// Archived files are moved to a separate folder and are no longer part of the cloud save system.
		/// </summary>
		public List<string> FilesToArchive => _filesToArchive.ToList();

		/// <summary>
		/// Remove the file from the Manifest, stopping it from being saved to the cloud while keeping it locally.
		/// <para>
		/// This method removes the file entry from the cloud save manifest but does not archive the file.
		/// If <see cref="PlayerCloudSavingConfiguration.UseAutoCloud"/> is enabled, the file will be re-added automatically during the next synchronization.
		/// So it is not indicated to <see cref="ArchiveSaveData"/> instead.
		/// </para>
		/// </summary>
		/// <param name="fileName">The name of the file to be removed from the cloud save manifest.</param>
		public void ForgetSaveData(string fileName)
		{
			fileName = BeamUnityFileUtils.SanitizeFileName(fileName);
			if (!_filesToForget.Add(fileName))
			{
				Debug.LogWarning($"{fileName} is already added to forget");
			}
		}

		/// <summary>
		/// Renames a save file locally and updates the cloud save manifest to reflect the new name.
		/// <para>
		/// This method ensures that all references to the save file remain unchanged in the cloud and locally.
		/// If the original file is no longer present, this method still completes successfully.
		/// </para>
		/// </summary>
		/// <param name="fileName">The current name of the save file.</param>
		/// <param name="newFileName">The new name for the save file.</param>
		public void RenameSaveData(string fileName, string newFileName)
		{
			fileName = BeamUnityFileUtils.SanitizeFileName(fileName);
			newFileName = BeamUnityFileUtils.SanitizeFileName(newFileName);
			if (!_filesToRename.TryAdd(fileName, newFileName))
			{
				Debug.LogWarning($"{fileName} is already added to be renamed");
			}
		}

		/// <summary>
		/// Archives a save file instead of deleting it, as some platforms restrict local file deletion.
		/// <para>
		/// This method removes the file from the cloud save manifest and moves it to a separate folder that is not used for cloud saving.
		/// </para>
		/// </summary>
		/// <param name="fileName">The name of the file to be archived.</param>
		public void ArchiveSaveData(string fileName)
		{
			fileName = BeamUnityFileUtils.SanitizeFileName(fileName);
			if (!_filesToArchive.Add(fileName))
			{
				Debug.LogWarning($"{fileName} is already added to archive");
			}
		}

	}
}
