using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Content;
using Beamable.Serialization.SmallerJSON;
using System;
using UnityEngine;

namespace Beamable.Editor.Alias
{
	public interface IAliasService
	{
		Promise<AliasResolve> Resolve(string cidOrAlias);
	}

	public class AliasResolve
	{
		public OptionalString Alias = new OptionalString();
		public OptionalString Cid = new OptionalString();
		public bool HasCid => Cid.HasValue;
		public bool HasAlias => Alias.HasValue;

	}

	public class AliasService : IAliasService
	{
		private readonly IHttpRequester _httpRequester;

		public AliasService(IHttpRequester httpRequester)
		{
			_httpRequester = httpRequester;
		}

		public async Promise<AliasResolve> Resolve(string cidOrAlias)
		{
			if (AliasHelper.IsCid(cidOrAlias))
			{
				return new AliasResolve {Alias = new OptionalString(), Cid = new OptionalString(cidOrAlias)};
			}

			var resolve = await MapAliasToCid(cidOrAlias);

			if (!resolve.available) // the resolve notion from the server is backwards as of Feb 25th. "available=true" means that the alias has been taken by a customer.
			{
				throw new AliasDoesNotExistException(cidOrAlias);
			}

			return new AliasResolve
			{
				Alias = new OptionalString(resolve.alias), Cid = new OptionalString(resolve.cid.ToString())
			};
		}

		async Promise<AliasResolveResponse> MapAliasToCid(string alias)
		{
			AliasHelper.ValidateAlias(alias);

			var url = $"/basic/realms/customer/alias/available?alias={alias}";
			var res = await _httpRequester.ManualRequest<AliasResolveResponse>(Method.GET, url);
			return res;
		}

		[Serializable]
		public class AliasResolveResponse
		{
			public string alias;
			public bool available;
			public long cid;
		}

		public class AliasDoesNotExistException : Exception
		{
			public AliasDoesNotExistException(string alias) : base($"Alias does not exist. alias=[{alias}]")
			{

			}
		}
	}




}
