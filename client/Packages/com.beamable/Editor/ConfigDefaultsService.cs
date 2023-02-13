using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Beamable.Common.Content;
using Beamable.Common.Dependencies;
using Beamable.Config;
using System;

namespace Beamable.Editor
{
	public class ConfigDefaultException : Exception
	{
		public ConfigDefaultException(string message) : base(message)
		{
			
		}
	}
	
	public class ConfigDefaultsService
	{
		private readonly AliasService _aliasService;

		private OptionalString _alias = new OptionalString();
		private OptionalString _cid = new OptionalString();
		private OptionalString _pid = new OptionalString();


		public ReadonlyOptionalString Cid => new ReadonlyOptionalString(_cid); // TODO cache?
		public ReadonlyOptionalString Pid => new ReadonlyOptionalString(_pid); // TODO cache?
		
		public ConfigDefaultsService(AliasService aliasService)
		{
			_aliasService = aliasService;
		}

		public async Promise LoadFromDisk()
		{
			var hasFile = ConfigDatabase.HasConfigFile(ConfigDatabase.GetConfigFileName());

			if (!hasFile)
			{
				return;
			}
			
			_alias = LoadConfigStringFromDisk(Constants.Features.Config.ALIAS_KEY);
			_cid = LoadConfigStringFromDisk(Constants.Features.Config.CID_KEY);
			_pid = LoadConfigStringFromDisk(Constants.Features.Config.PID_KEY);


			// check that the alias is valid
			try
			{
				if (_alias.HasValue)
				{
					var aliasResolve = await _aliasService.Resolve(_alias.Value);
					aliasResolve.Alias.DoIfExists(_alias.Set);
					aliasResolve.Cid.DoIfExists(_cid.Set);
					// aliasResolve.Alias.DoIfNotExists(_alias.Clear); // if the resolved alias didn't include anything, then the alias isn't valid.
					// aliasResolve.Cid.DoIfExists(cid =>
					// {
					// 	// if the alias mapped to a cid
					// 	// and the current cid doesn't exist, or isn't valid...
					// 	if (!_cid.HasValue || !AliasHelper.IsCid(_cid.Value))
					// 	{
					// 		_cid.Set(cid);
					// 	}
					// });
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

		/// <summary>
		/// Given a key, return an optional value loaded from the config-defaults file.
		/// If the key exists, but has an empty value, the returned optional is blank.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private static OptionalString LoadConfigStringFromDisk(string key)
		{
			if (ConfigDatabase.TryGetString(key, out var value) && !string.IsNullOrWhiteSpace(value))
			{
				return new OptionalString(value);
			}
			else
			{
				return new OptionalString();
			}
		}
		
		
	}
}
