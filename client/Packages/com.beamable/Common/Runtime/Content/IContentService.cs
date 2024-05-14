// this file was copied from nuget package Beamable.Common@0.0.0-PREVIEW.NIGHTLY-202405121132
// https://www.nuget.org/packages/Beamable.Common/0.0.0-PREVIEW.NIGHTLY-202405121132

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
