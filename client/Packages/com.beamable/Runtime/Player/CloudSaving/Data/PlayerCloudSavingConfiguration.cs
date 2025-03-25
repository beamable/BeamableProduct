using Beamable.Common;
using System;
using UnityEngine;

namespace Beamable.Player.CloudSaving
{
	public class PlayerCloudSavingConfiguration
	{
		/// <summary>
		/// Enables or disables the AutoCloud feature.
		/// <para>
		/// AutoCloud is an automated process that saves all files located in <see cref="ICloudSavingService.LocalDataFullPath"/>.
		/// Only files in this folder are considered; subfolders are ignored.
		/// When disabled, files must be manually saved using <see cref="ICloudSavingService.SaveData{T}"/>, <see cref="ICloudSavingService.SaveData(string,string)"/> or <see cref="ICloudSavingService.SaveData(string,byte[])"/>
		/// </para>
		/// </summary>
		public bool UseAutoCloud = false;

		/// <summary>
		/// Overrides the default conflict resolution strategy.
		/// <para>
		/// If a data conflict occurs during the <see cref="ICloudSavingService.Init"/> process, this function is invoked automatically to resolve it.
		/// If no custom resolver is provided, the system will use <see cref="PlayerCloudSaving.DefaultResolver"/>.
		/// </para>
		/// </summary>
		public ICloudSavingService.ConflictResolver HandleConflicts;

		/// <summary>
		/// Handles errors that occur during file downloads.
		/// <para>
		/// If an exception is thrown while downloading a file, this function will be invoked with the exception as a parameter.
		/// The function should return a <see cref="Promise{Unit}"/> indicating the resolution of the error handling process.
		/// If no custom ErrorHandler is provided, the system will use <see cref="PlayerCloudSaving.DefaultDownloadFileErrorRecover"/>
		/// </para>
		/// </summary>
		public Func<Exception, Promise<Unit>> HandleDownloadFileError;

		/// <summary>
		/// Allows overriding the default serialization behavior for <see cref="ICloudSavingService.SaveData{T}"/>.
		/// <para>
		/// If set, this function will be used to serialize objects before saving them to a file.
		/// If not set, the data will be serialized to JSON using <see cref="JsonUtility.ToJson(object)"/>.
		/// </para>
		/// </summary>
		public Func<object, string> CustomSerializer;

		/// <summary>
		/// Allows overriding the default deserialization behavior for <see cref="ICloudSavingService.LoadData{T}"/>.
		/// <para>
		/// If set, this function will be used to deserialize objects when loading from a file.
		/// If not set, the data will be deserialized from JSON using <see cref="JsonUtility.FromJson{T}(string)"/>.
		/// </para>
		/// </summary>
		public Func<string, object> CustomDeserializer;
	}
}
