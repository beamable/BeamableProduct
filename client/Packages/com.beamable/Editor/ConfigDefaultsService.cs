using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Config;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

namespace Beamable.Editor
{
	public class ConfigDefaultsService
	{
		private readonly AliasService _aliasService;

		private OptionalString _alias = new OptionalString();
		private OptionalString _cid = new OptionalString();
		private OptionalString _pid = new OptionalString();

		/// <summary>
		/// A readonly optional string for the CID in the config-defaults file.
		/// </summary>
		public ReadonlyOptionalString Cid => new ReadonlyOptionalString(_cid);

		/// <summary>
		/// A readonly optional string for the PID in the config-defaults file.
		/// </summary>
		public ReadonlyOptionalString Pid => new ReadonlyOptionalString(_pid);

		/// <summary>
		/// A readonly optional string for the Alias in the config-defaults file.
		/// </summary>
		public ReadonlyOptionalString Alias => new ReadonlyOptionalString(_alias);

		public ConfigDefaultsService(AliasService aliasService)
		{
			_aliasService = aliasService;
		}


		public void SaveConfig(string alias, string cid, string pid)
		{

			// var configService = ServiceScope.GetService<ConfigDefaultsService>();
			AliasHelper.ValidateAlias(alias);
			AliasHelper.ValidateCid(cid);
			_cid.Set(cid);
			_pid.Set(pid);
			_alias.Set(alias);
			var config = new ConfigData()
			{
				cid = cid,
				alias = alias,
				pid = pid,
			};

			var path = ConfigDatabaseProvider.GetFullPath();
			var asJson = JsonUtility.ToJson(config, true);

			var writeConfig = true;
			if (File.Exists(path))
			{
				var existingJson = File.ReadAllText(path);
				if (string.Equals(existingJson, asJson))
				{
					writeConfig = false;
				}
			}

			if (writeConfig)
			{
				string directoryName = Path.GetDirectoryName(path);
				if (!string.IsNullOrWhiteSpace(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}

				if (File.Exists(path))
				{
					var fileInfo = new FileInfo(path);
					fileInfo.IsReadOnly = false;
				}

				if (Provider.enabled)
				{
					var vcTask = Provider.Checkout(path, CheckoutMode.Asset);
					vcTask.Wait();
					if (!vcTask.success)
					{
						Debug.LogWarning($"Unable to checkout: {path}");
					}
				}

				File.WriteAllText(path, asJson);
			}
		}

		/// <summary>
		/// This will read the config-defaults file and store the values in the
		/// <see cref="Alias"/>, <see cref="Cid"/>, and <see cref="Pid"/> fields.
		/// This will also resolve the alias and cid such that the CID is a cid, and the alias is an alias.
		/// </summary>
		public async Promise LoadFromDisk()
		{
			bool hasFile = ConfigDatabaseProvider.HasConfigFile();

			if (!hasFile)
			{
				return;
			}

			var data = ConfigDatabaseProvider.GetConfigData();
			_alias = OptionalString.FromString(data.alias);
			_cid = OptionalString.FromString(data.cid);
			_pid = OptionalString.FromString(data.pid);

			// check that the alias is valid
			try
			{
				if (_alias.HasValue)
				{
					var aliasResolve = await _aliasService.Resolve(_alias.Value);
					aliasResolve.Alias.DoIfExists(_alias.Set);
					aliasResolve.Cid.DoIfExists(_cid.Set);
				}

				_alias.DoIfExists(AliasHelper.ValidateAlias);
			}
			catch
			{
				// if the alias isn't valid, erase it from memory!
				_alias.Clear();
			}

			// check that the cid is valid
			try
			{
				if (_cid.HasValue)
				{
					var cidResolve = await _aliasService.Resolve(_cid.Value);
					cidResolve.Alias.DoIfExists(_alias.Set);
					cidResolve.Cid.DoIfExists(_cid.Set);
				}

				_cid.DoIfExists(AliasHelper.ValidateCid);
			}
			catch
			{
				// if the cid isn't valid, erase it from memory
				_cid.Clear();
			}
		}

	}
}
