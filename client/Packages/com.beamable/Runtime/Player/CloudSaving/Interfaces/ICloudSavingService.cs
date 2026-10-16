using Beamable.Api.CloudSaving;
using Beamable.Common;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beamable.Player.CloudSaving
{
	/// <summary>Manages a player's local save files and synchronization with cloud storage.</summary>
	/// <remarks>Obtain this service from <see cref="BeamContext.CloudSaving"/>. Await <see cref="Init"/> and
	/// inspect the returned status. Saving locally and uploading to the cloud are separate operations.
	/// Use <see cref="ForceUploadLocalData"/> when upload completion must be awaited.</remarks>
	public interface ICloudSavingService
	{
		/// <summary>Handles the conflicts discovered during synchronization.</summary>
		/// <param name="conflictResolver">The current conflicts and the operation used to resolve each one.</param>
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
		/// Event triggered when an error occurs in the <see cref="PlayerCloudSaving"/>.
		/// </summary>
		public Action<CloudSavingError> OnCloudSavingError { get; set; }

		/// <summary>Gets the current initialization and conflict state of this save service.</summary>
		/// <value>The service's current <see cref="CloudSaveStatus"/>.</value>
		/// <remarks>Save and load operations accept Initialized and ConflictedData. Forced upload and download
		/// require Initialized. Check this status again after resolving conflicts.</remarks>
		public CloudSaveStatus ServiceStatus { get; }

		/// <summary>Initializes local saves, retrieves cloud data, and attempts conflict resolution.</summary>
		/// <param name="pollingIntervalSeconds">The synchronization polling interval in seconds; the default is 10.</param>
		/// <returns>The resulting status, normally Initialized or ConflictedData; I/O and request failures can reject the promise.</returns>
		/// <remarks>Only starts from Inactive. Other states are returned unchanged, so completion does not
		/// always mean readiness. A competing save service can prevent initialization and leave the status Inactive.
		/// Polling starts after initialization completes. Configure a conflict resolver before calling this method.</remarks>
		/// <example>
		/// <para>Example: initialize, save and read a local string, then explicitly await its cloud upload.</para>
		/// <code>
		/// using System;
		/// using System.Threading.Tasks;
		/// using Beamable;
		/// using Beamable.Player.CloudSaving;
		/// using UnityEngine;
		///
		/// public static class SaveAndUploadExample
		/// {
		///     public static async Task Run()
		///     {
		///         try
		///         {
		///             var context = await BeamContext.Default.Instance;
		///             var saves = context.CloudSaving;
		///             var status = await saves.Init();
		///             if (status != CloudSaveStatus.Initialized)
		///             {
		///                 Debug.LogWarning($"Resolve initialization or conflicts first: {status}");
		///                 return;
		///             }
		///             await saves.SaveData("progress.txt", "level=2");
		///             Debug.Log(await saves.LoadDataString("progress.txt"));
		///             // Local-save completion alone does not confirm a cloud upload.
		///             await saves.ForceUploadLocalData();
		///         }
		///         catch (Exception exception)
		///         {
		///             Debug.LogException(exception);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		public Promise<CloudSaveStatus> Init(int pollingIntervalSeconds = 10);

		/// <summary>Restarts synchronization for an initialized save service.</summary>
		/// <param name="pollingIntervalSeconds">The synchronization polling interval in seconds; the default is 10.</param>
		/// <returns>The resulting initialization status. If the service is not Initialized, returns its current status unchanged.</returns>
		/// <remarks>This stops the active service and calls <see cref="Init"/> again. It does not clear all local files.
		/// Resolve conflicts before calling it; failures during initialization can reject the promise.</remarks>
		public Promise<CloudSaveStatus> ReInit(int pollingIntervalSeconds = 10);

		/// <summary>Uploads pending local changes and waits for synchronization to finish.</summary>
		/// <returns>A promise that completes after pending changes are processed, or rejects on failure.</returns>
		/// <remarks>Requires <see cref="CloudSaveStatus.Initialized"/>. Other states produce a rejected promise.
		/// Waits for an active update before processing changes. Local writes completed through
		/// <see cref="SaveData(string,string)"/> are not, by themselves, proof of a completed cloud upload.</remarks>
		/// <seealso cref="Init"/>
		public Promise<Unit> ForceUploadLocalData();

		/// <summary>Downloads cloud save data to the local device.</summary>
		/// <returns>A promise that completes after download and local manifest update, or rejects on failure.</returns>
		/// <remarks>Requires Initialized; other statuses reject the promise. This operation can replace local data.
		/// Use <see cref="GetDataConflictDetails"/> and <see cref="ResolveDataConflict"/> for pending conflicts first.</remarks>
		public Promise<Unit> ForceDownloadCloudData();

		/// <summary>Gets the currently unresolved file conflicts.</summary>
		/// <returns>A read-only view of the current conflicts, empty when none remain.</returns>
		/// <remarks>The view changes as conflicts are resolved. Copy it before iterating and invoking
		/// <see cref="ResolveDataConflict"/>, which removes entries from the underlying list.</remarks>
		/// <seealso cref="SetConflictResolverOverride"/>
		public IReadOnlyList<DataConflictDetail> GetDataConflictDetails();

		/// <summary>Attempts to keep the selected local or cloud version of one conflicting file.</summary>
		/// <param name="conflict">A current conflict obtained from <see cref="GetDataConflictDetails"/>.</param>
		/// <param name="resolveType">Whether to keep the local or cloud version; the other version is replaced.</param>
		/// <remarks>Unknown conflicts are ignored with a warning. Resolution failures are logged and leave the
		/// conflict unresolved; this void method does not report success. Recheck conflicts and
		/// <see cref="ServiceStatus"/> afterward. Resolving the final conflict can finish initialization.
		/// Local save/load operations accept ConflictedData, while forced synchronization requires Initialized.</remarks>
		/// <seealso cref="SetConflictResolverOverride"/>
		public void ResolveDataConflict(DataConflictDetail conflict, ConflictResolveType resolveType);

		/// <summary>Writes a string to a local save file and updates the local manifest.</summary>
		/// <param name="fileName">The file name, sanitized by the service before use.</param>
		/// <param name="content">The string content to write; the caller controls serialization.</param>
		/// <returns>A promise that completes after the local file and manifest are saved, or rejects on failure.</returns>
		/// <remarks>Accepts Initialized or ConflictedData; other states reject the promise. Completion does not
		/// confirm cloud upload. Resolve outstanding conflicts and await <see cref="ForceUploadLocalData"/>
		/// when upload completion is required.</remarks>
		/// <seealso cref="Init"/>
		public Promise<Unit> SaveData(string fileName, string content);

		/// <summary>Writes bytes to a local save file and updates the local manifest.</summary>
		/// <param name="fileName">The file name, sanitized by the service before use.</param>
		/// <param name="content">The bytes to write; the caller controls serialization.</param>
		/// <returns>A promise that completes after the local file and manifest are saved, or rejects on failure.</returns>
		/// <remarks>Accepts Initialized or ConflictedData. This does not wait for upload; use
		/// <see cref="ForceUploadLocalData"/> after resolving conflicts to await cloud synchronization.</remarks>
		/// <seealso cref="Init"/>
		public Promise<Unit> SaveData(string fileName, byte[] content);

		/// <summary>Writes an object to a local JSON save file and updates the local manifest.</summary>
		/// <typeparam name="T">The serializable type of the saved value.</typeparam>
		/// <param name="fileName">The file name, sanitized by the service before use.</param>
		/// <param name="contentData">The value to serialize.</param>
		/// <returns>A promise that completes after the local file and manifest are saved, or rejects on failure.</returns>
		/// <remarks>Accepts Initialized or ConflictedData. Uses
		/// <see cref="PlayerCloudSavingConfiguration.CustomSerializer"/> when configured, otherwise Unity JsonUtility.
		/// Strings and byte arrays are written directly. This does not wait for cloud upload.</remarks>
		/// <seealso cref="ForceUploadLocalData"/>
		/// <seealso cref="Init"/>
		public Promise<Unit> SaveData<T>(string fileName, T contentData);

		/// <summary>Reads a local save file as a string.</summary>
		/// <param name="fileName">The file name, sanitized by the service before use.</param>
		/// <returns>A promise containing the file's text, or an empty string if the file does not exist. Other read failures reject it.</returns>
		/// <remarks>Accepts Initialized or ConflictedData. This reads local data and does not fetch the latest
		/// cloud version. Use <see cref="ForceDownloadCloudData"/> when an explicit download is required.</remarks>
		/// <seealso cref="Init"/>
		public Promise<string> LoadDataString(string fileName);

		/// <summary>
		/// Loads a file and returns its content as a byte array.
		/// </summary>
		/// <param name="fileName">The name of the file to load.</param>
		/// <returns>A <see cref="Promise"/> containing the file content as a byte array.</returns>
		public Promise<byte[]> LoadDataByte(string fileName);

		/// <summary>
		/// Loads a file and deserializes its content into an object of type <typeparamref name="T"/>.
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
		
		/// <summary>Replaces the conflict-resolution callback used during synchronization.</summary>
		/// <param name="resolverOverride">The callback to use, or null to restore the default resolver.</param>
		/// <remarks>Set before <see cref="Init"/> to handle initial conflicts. The callback is synchronous and
		/// receives the conflicts discovered by that synchronization attempt. Setting it does not immediately
		/// resolve existing conflicts. Choose which version to keep explicitly; resolution can replace data.</remarks>
		/// <example>
		/// <para>Example: leave conflicts pending during initialization, then apply a local/cloud choice supplied by the player. This example owns the resolver override for the operation.</para>
		/// <code>
		/// using System;
		/// using System.Linq;
		/// using System.Threading.Tasks;
		/// using Beamable;
		/// using Beamable.Player.CloudSaving;
		/// using UnityEngine;
		///
		/// public static class ResolveSaveConflictsExample
		/// {
		///     public static async Task Run(ConflictResolveType playerChoice)
		///     {
		///         try
		///         {
		///             var context = await BeamContext.Default.Instance;
		///             var saves = context.CloudSaving;
		///             // Install before the first Init to prevent automatic resolution.
		///             saves.SetConflictResolverOverride(resolver =&gt; { });
		///             try
		///             {
		///                 var status = await saves.Init();
		///                 if (status != CloudSaveStatus.ConflictedData)
		///                 {
		///                     Debug.Log($"Save status: {status}");
		///                     return;
		///                 }
		///                 foreach (var conflict in saves.GetDataConflictDetails().ToArray())
		///                     saves.ResolveDataConflict(conflict, playerChoice);
		///                 if (saves.ServiceStatus != CloudSaveStatus.Initialized)
		///                 {
		///                     Debug.LogWarning("Some conflicts remain unresolved.");
		///                     return;
		///                 }
		///                 await saves.ForceUploadLocalData();
		///             }
		///             finally
		///             {
		///                 saves.SetConflictResolverOverride(null);
		///             }
		///         }
		///         catch (Exception exception)
		///         {
		///             Debug.LogException(exception);
		///         }
		///     }
		/// }
		/// </code>
		/// </example>
		public void SetConflictResolverOverride(ConflictResolver resolverOverride);

	}
}
