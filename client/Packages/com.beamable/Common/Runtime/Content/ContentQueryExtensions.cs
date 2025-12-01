// this file was copied from nuget package Beamable.Common@6.2.1
// https://www.nuget.org/packages/Beamable.Common/6.2.1

using System.Collections.Generic;
using System.Linq;

namespace Beamable.Common.Content
{
	public static class ContentQueryExtensions
	{

		public static bool Accept(this ContentQuery query, ClientContentInfo info)
		{
			if (query.TypeConstraints != null)
			{
				var type = ContentTypeReflectionCache.Instance.GetTypeFromId(info.contentId);
				if (!query.AcceptType(type))
				{
					return false;
				}
			}

			if (query.IdContainsConstraint != null)
			{
				var idMatch = info.contentId.Split('.').Last().Contains(query.IdContainsConstraint);
				if (!idMatch)
				{
					return false;
				}
			}

			if (query.TagConstraints != null)
			{
				if (!query.AcceptTags(new HashSet<string>(info.tags)))
				{
					return false;
				}
			}

			return true;
		}
	}
}
