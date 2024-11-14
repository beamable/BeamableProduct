// this file was copied from nuget package Beamable.Common@3.0.0-PREVIEW.RC6
// https://www.nuget.org/packages/Beamable.Common/3.0.0-PREVIEW.RC6

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
