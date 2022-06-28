using Beamable.Common;
using Beamable.Common.Api;
using System.Collections.Generic;

namespace Beamable.Server.Api
{
	public class EphemeralUserDataCache<T> : UserDataCache<T>
	{
		private readonly string _name;
		private readonly CacheResolver _resolver;

		public EphemeralUserDataCache(string name, CacheResolver resolver)
		{
			_name = name;
			_resolver = resolver;
		}

		public override Promise<T> Get(long gamerTag)
		{
			return _resolver(new List<long> { gamerTag }).Map(res => res[gamerTag]);
		}

		public override Promise<Dictionary<long, T>> GetBatch(List<long> gamerTags)
		{
			return _resolver(gamerTags);
		}

		public override void Set(long gamerTag, T data)
		{
			// no op. In an ephemeral cache, data is never stored.
		}

		public override void Remove(long gamerTag)
		{
			// no op. In an ephemeral cache, data is never stored.
		}
	}
}
