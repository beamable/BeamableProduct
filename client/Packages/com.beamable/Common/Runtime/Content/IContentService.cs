// this file was copied from nuget package Beamable.Common@4.3.0-PREVIEW.RC2
// https://www.nuget.org/packages/Beamable.Common/4.3.0-PREVIEW.RC2

using System;

namespace Beamable.Common.Content
{
	[Obsolete]
	public interface IContentService
	{
		[Obsolete]

		Promise<TContent> Resolve<TContent>(IContentRef<TContent> reference) where TContent : IContentObject, new();
	}

	[Obsolete]
	public static class ContentServiceResolver
	{

	}
}
