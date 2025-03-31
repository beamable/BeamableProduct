using Beamable.Api.CloudSaving;
using Beamable.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Player.CloudSaving
{
	public interface ICloudSavingService
	{
		public delegate void ConflictResolver(IConflictResolver conflictResolver);

		/// <summary>
		/// The path to the device's folder that contains all the save files.
		/// <para>
		/// The data folder is located inside <see cref="Application.persistentDataPath"/>.
		/// </para>
		/// <para>
		/// This is intended for debugging and inspection only. To save a file, use <see cref="SaveData{T}"/>, <see cref="SaveData(string,string)"/> or <see cref="SaveData(string,byte[])"/>.
		/// To load a file, use <see cref="LoadData{T}"/> or <see cref="LoadDataString"/>.
		/// </para>
		/// </summary>
		public string LocalDataFullPath { get; }

		/// <summary>
		/// Event triggered when the player's cloud save files are updated by another device or from the Portal.
		/// </summary>
		public Action<CloudSavingManifest> OnManifestUpdated { get; set; }

		/// <summary>
		/// Event triggered when an error occurs in the <see cref="CloudSavingService"/>.
		/// </summary>
		public Action<CloudSavingError> OnCloudSavingError { get; set; }

		/// <summary>
		/// Stores the current status of the <see cref="CloudSavingService"/>.
		/// </summary>
		public CloudSaveStatus ServiceStatus { get; }

		/// <summary>
		/// Initializes the <see cref="PlayerCloudSaving"/> before handling cloud saves.
		/// <para>
		/// This method starts the initialization process. While running, the <see cref="ServiceStatus"/> property will return <see cref="CloudSaveStatus.Initializing"/>.
		/// </para>
		/// </summary>
		/// <param name="pollingIntervalSeconds">
		/// Defines how often Beamable checks for new or updated local files after they are written using <see cref="SaveData{T}"/> or <see cref="SaveData(string,string)"/>.
		/// </param>
		/// <returns>A <see cref="Promise"/> that completes when initialization is finished.</returns>
		public Promise<CloudSaveStatus> Init(int pollingIntervalSeconds = 10);

		/// <summary>
		/// Clears all local user data and re-fetches everything from the Beamable server.
		/// <para>
		/// This method automatically calls <see cref="Init"/> as part of its execution.
		/// </para>
		/// </summary>
		/// <param name="pollingIntervalSeconds">
		/// Defines how often Beamable checks for new or updated local files after they are written using <see cref="SaveData{T}"/> or <see cref="SaveData(string,string)"/>.
		/// </param>
		/// <returns>A <see cref="Promise"/> that completes when re-initialization is finished.</returns>
		public Promise<CloudSaveStatus> ReInit(int pollingIntervalSeconds = 10);

		/// <summary>
		/// Forces the upload of local save data to the cloud.
		/// </summary>
		/// <returns>A <see cref="Promise"/> that completes when the upload is finished.</returns>
		public Promise<Unit> ForceUploadLocalData();

		/// <summary>
		/// Forces the download of cloud save data to the local device.
		/// </summary>
		/// <returns>A <see cref="Promise"/> that completes when the download is finished.</returns>
		public Promise<Unit> ForceDownloadCloudData();

		/// <summary>
		/// Retrieves details of any conflicting files when <see cref="Init"/> or <see cref="ReInit"/> return <see cref="CloudSaveStatus.ConflictedData"/>.
		/// </summary>
		/// <returns>A list of <see cref="DataConflictDetail"/> objects representing the conflict details. Returns an empty list if there are no conflicts.</returns>
		public IReadOnlyList<DataConflictDetail> GetDataConflictDetails();

		/// <summary>
		/// Resolves a file conflict between local and cloud data.
		/// <para>
		/// This method must be called before saving, uploading, or downloading data when a conflict exists.
		/// </para>
		/// </summary>
		/// <param name="conflict">The <see cref="DataConflictDetail"/> representing the conflicting file.</param>
		/// <param name="resolveType">The resolution strategy (e.g., prefer local or cloud data).</param>
		public void ResolveDataConflict(DataConflictDetail conflict, ConflictResolveType resolveType);

		/// <summary>
		/// Saves a file locally using a string as content.
		/// <para>
		/// You are responsible for serializing and deserializing the content.
		/// </para>
		/// </summary>
		/// <param name="fileName">The name of the file to save.</param>
		/// <param name="content">The string content of the file.</param>
		/// <returns>A <see cref="Promise"/> that completes when the file is saved.</returns>
		public Promise<Unit> SaveData(string fileName, string content);

		/// <summary>
		/// Saves a file locally using a byte array as content.
		/// <para>
		/// You are responsible for serializing and deserializing the content.
		/// </para>
		/// </summary>
		/// <param name="fileName">The name of the file to save.</param>
		/// <param name="content">The byte array representing the file content.</param>
		/// <returns>A <see cref="Promise"/> that completes when the file is saved.</returns>
		public Promise<Unit> SaveData(string fileName, byte[] content);

		/// <summary>
		/// Saves a file locally with automatic JSON serialization.
		/// <para>
		/// If no custom serializer is provided in <see cref="PlayerCloudSavingConfiguration.CustomSerializer"/>, the default serializer (JsonUtility) is used.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The type of the object being saved.</typeparam>
		/// <param name="fileName">The name of the file to save.</param>
		/// <param name="contentData">The object to serialize into JSON.</param>
		/// <returns>A <see cref="Promise"/> that completes when the file is saved.</returns>
		public Promise<Unit> SaveData<T>(string fileName, T contentData);

		/// <summary>
		/// Loads a file and returns its content as a string.
		/// </summary>
		/// <param name="fileName">The name of the file to load.</param>
		/// <returns>A <see cref="Promise"/> containing the file content as a string.</returns>
		public Promise<string> LoadDataString(string fileName);

		/// <summary>
		/// Loads a file and returns its content as a byte array.
		/// </summary>
		/// <param name="fileName">The name of the file to load.</param>
		/// <returns>A <see cref="Promise"/> containing the file content as a byte array.</returns>
		public Promise<byte[]> LoadDataByte(string fileName);

		/// <summary>
		/// Loads a file and deserializes its content into an object of type <see cref="T"/>.
		/// <para>
		/// If no custom deserializer is provided in <see cref="PlayerCloudSavingConfiguration.CustomDeserializer"/>, the default deserializer (JsonUtility) is used.
		/// </para>
		/// </summary>
		/// <typeparam name="T">The type of the object being deserialized.</typeparam>
		/// <param name="fileName">The name of the file to load.</param>
		/// <returns>A <see cref="Promise"/> containing the deserialized object.</returns>
		public Promise<T> LoadData<T>(string fileName);

		/// <summary>
		/// Updates cloud save data using the provided update builder.
		/// </summary>
		/// <param name="builder">An <see cref="Action"/> that configures the update process.</param>
		/// <returns>A <see cref="Promise"/> that completes when the update is finished.</returns>
		public Promise<Unit> Update(Action<CloudDataUpdateBuilder> builder);
		
		/// <summary>
		/// Updates the Default Conflict Resolver with a <see cref="ConflictResolver"/> override delegate 
		/// </summary>
		/// <param name="resolverOverride">The <see cref="ConflictResolver"/> delegate to override the default conflict resolver</param>
		public void SetConflictResolverOverride(ConflictResolver resolverOverride);

	}
}
