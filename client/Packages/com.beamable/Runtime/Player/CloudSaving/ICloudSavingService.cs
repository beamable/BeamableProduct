using Beamable.Api.CloudSaving;
using Beamable.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Player.CloudSaving
{
	public interface ICloudSavingService
	{
		/// <summary>
		/// The path to the device's folder that contains all the save files.
		/// The data folder is inside the <see cref="Application.persistentDataPath"/>
		/// This is used only for Debug or Inspection, to save a File you should use <see cref="SaveData{T}"/> or <see cref="SaveStringData"/> and to load you should use <see cref="LoadData{T}"/> or <see cref="LoadStringData"/>
		/// </summary>
		public string LocalDataFullPath { get; }
		
		/// <summary>
		/// An event with a <see cref="ManifestResponse"/> parameter.
		/// This event triggers anytime the player's cloud files are updated by another device, or from the Portal.
		/// </summary>
		public Action<ManifestResponse> OnManifestUpdated { get; set; }
		
		/// <summary>
		/// An event with a <see cref="CloudSavingError"/> parameter.
		/// This event triggers anytime there is an error in the <see cref="CloudSavingService"/>
		/// </summary>
		public Action<CloudSavingError> OnCloudSavingError { get; set; }
		
		/// <summary>
		/// This fields stores the <see cref="CloudSaveStatus"/> as status from the CloudSavingService.  
		/// </summary>
		public CloudSaveStatus ServiceStatus { get; }

		/// <summary>
		/// The <see cref="PlayerCloudSaving"/> must initialize before handling the cloud save.
		/// This method will start the Initialization process. The <see cref="ServiceStatus"/> property will return <see cref="CloudSaveStatus.Initializing"/> when this method is running.
		/// </summary>
		/// <param name="pollingIntervalSeconds">
		/// /// When a file is <i>written</i> using the <see cref="SaveData{T}"/> or <see cref="SaveStringData"/>, it will be backed up on the Beamable server.
		/// The <see cref="pollingIntervalSeconds"/> controls how often Beamable checks for new or updated files on the local device.
		/// </param>
		/// <returns>A <see cref="Promise"/> representing when the initialization has completed. </returns>
		public Promise<CloudSaveStatus> Init(int pollingIntervalSeconds = 10);
		
		/// <summary>
		/// This method will clear all local user data, and re-fetch everything from the Beamable server.
		/// The <see cref="Init"/> method will be called as part of this execution.
		/// </summary>
		/// <param name="pollingIntervalSeconds">
		/// /// When a file is <i>written</i> using the <see cref="SaveData{T}"/> or <see cref="SaveStringData"/>, it will be backed up on the Beamable server.
		/// The <see cref="pollingIntervalSeconds"/> controls how often Beamable checks for new or updated files on the local device.
		/// </param>
		/// <returns>A <see cref="Promise"/> representing when the re-initialization has completed. </returns>
		public Promise<CloudSaveStatus> ReInit(int pollingIntervalSeconds = 10);

		/// <summary>
		/// This method will force the upload of local save data to cloud save
		/// </summary>
		/// <returns>A <see cref="Promise"/> representing if the upload was successful</returns>
		public Promise<bool> ForceUploadLocalData();

		/// <summary>
		/// This method will force the download of cloud save data to local device data
		/// </summary>
		/// <returns>A <see cref="Promise"/> representing if the download was successful</returns>
		public Promise<bool> ForceDownloadCloudData();
		
		/// <summary>
		/// When Init or ReInit returns with <see cref="CloudSaveStatus.ConflictedData"/> it means that Local Data and Cloud Data are conflicted
		/// This method return all conflicted data files and their details
		/// </summary>
		/// <returns>A <see cref="Promise"/> with the conflict details of all conflicted items, if none it will return a empty list.</returns>
		public Promise<IReadOnlyList<DataConflictDetail>> GetDataConflictDetails(); 
		
		/// <summary>
		/// When there is a conflicted file between Cloud and Local data, you need to Fix the conflict before Saving new files, uploading data, or downloading data.
		/// This method resolve the conflict by choosing which save to use, if it's better to use Local or Cloud.
		/// </summary>
		/// <param name="fileName">The name of the conflicted file</param>
		/// <param name="useLocalData">If the service should resolve using local data or not. If false, it will use the Cloud data instead</param>
		/// <returns>A <see cref="Promise"/> representing when the conflict resolve has completed. </returns>
		public Promise<Unit> ResolveDataConflict(string fileName, bool useLocalData);

		/// <summary>
		/// To save files that needs to be handled by the cloud data, you need to save them locally first.
		/// This method saves the file using a string as content. You need to Serialize and Deserialize it by yourself.
		/// </summary>
		/// <param name="fileName">Name of the file to be saved locally</param>
		/// <param name="content">The string content of the file</param>
		/// <returns>A <see cref="Promise"/> representing when the file save has completed. </returns>
		public Promise<Unit> SaveStringData(string fileName, string content);
		
		/// <summary>
		/// To save files that needs to be handled by the cloud data, you need to save them locally first.
		/// This method saves the file using byte as content. You need to Serialize and Deserialize it by yourself.
		/// </summary>
		/// <param name="fileName">Name of the file to be saved locally</param>
		/// <param name="content">The byte array that represents the content of the file</param>
		/// <returns>A <see cref="Promise"/> representing when the file save has completed. </returns>
		public Promise<Unit> SaveByteData(string fileName, byte[] content);
		
		/// <summary>
		/// To save files that needs to be handled by the cloud data, you need to save them locally first.
		/// This method saves the file using the JsonSerializer to convert the object into a JSON string as content.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="contentData">The object to be serialized into a JSON</param>
		/// <returns>A <see cref="Promise"/> representing when the file save has completed. </returns>
		public Promise<Unit> SaveData<T>(string fileName, T contentData);

		/// <summary>
		/// To recover and use files from CloudSaving you need to load them correctly.
		/// This method loads the File and returns it's content as a string.
		/// </summary>
		/// <param name="fileName">Name of the file to be loaded locally</param>
		/// <returns>A <see cref="Promise"/> with the file content when the file is loaded.</returns>
		public Promise<string> LoadStringData(string fileName);
		
		/// <summary>
		/// To recover and use files from CloudSaving you need to load them correctly.
		/// This method loads the File and returns it's content as a byte array.
		/// </summary>
		/// <param name="fileName">Name of the file to be loaded locally</param>
		/// <returns>A <see cref="Promise"/> with the file content as a byte array when the file is loaded.</returns>
		public Promise<byte[]> LoadByteData(string fileName);
		
		/// <summary>
		/// To recover and use files from CloudSaving you need to load them correctly.
		/// This method loads the File, parse it's content as <see cref="T"/> and returns it.
		/// </summary>
		/// <param name="fileName">Name of the file to be loaded locally</param>
		/// <returns>A <see cref="Promise"/> with the <see cref="T"/> object from the file's content when the file is loaded.</returns>
		public Promise<T> LoadData<T>(string fileName);
		
		/// <summary>
		/// It isn't indicated to delete files locally as some platforms have restrictions on deleting local files. To simulate the same behaviour you can Archive it instead
		/// This methods will archive the File from your manifest and move the file to another folder which isn't used for the Cloud Saving.
		/// </summary>
		/// <param name="fileName">The name of the file to be archived. The same name used on Save and Load methods</param>
		/// <returns>A <see cref="Promise"/> representing when the archive process is finished. </returns>
		public Promise<Unit> ArchiveSavedData(string fileName);

		/// <summary>
		/// If you need to rename a save file you can easily use this method for this
		/// This methods will rename the save file locally and update the Cloud Save Manifest Locally and on Cloud to ensure that all data about the save are kept unchanged.
		/// If there isn't a file with the name anymore this method will still return a Successful promise.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>A <see cref="Promise"/> representing when the file renaming has completed. </returns>
		public Promise<bool> RenameSavedData(string fileName, string newFileName);
		
		/// <summary>
		/// If you want to stop Saving a file to the cloud and wants to keep the data locally you can use this method for this.
		/// This methods will remove the file entry from the manifest while not archiving the file
		/// If you are using <see cref="PlayerCloudSavingConfiguration.UseAutoCloud"/>, this method will not forget the file as in the next check it will be automatically added.
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns>A <see cref="Promise"/> representing when the file renaming has completed. </returns>
		public Promise<Unit> ForgetData(string fileName);

	}
}
