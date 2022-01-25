using Beamable.Common;
using Beamable.Common.Content;

namespace Beamable
{
	public static class BeamableContentExtensions
	{
		public static Promise<IContentObject> Resolve(this BeamContext ctx, IContentRef contentRef, string manifestId=null)
		{
			return ctx.Content.GetContent(contentRef, manifestId);
		}

		public static Promise<T> Resolve<T>(this BeamContext ctx, IContentRef<T> contentRef, string manifestId=null)
			where T : ContentObject, new()
		{
			return ctx.Content.GetContent<T>(contentRef, manifestId);
		}

		public static Promise<IContentObject> Resolve2(this IContentRef contentRef, string manifestId=null, BeamContext ctx=null)
		{
			ctx = ctx ?? BeamContext.Default;
			return ctx.Resolve(contentRef, manifestId);
		}

		public static Promise<T> Resolve2<T>(this IContentRef<T> contentRef, string manifestId=null, BeamContext ctx=null)
			where T : ContentObject, new()
		{
			ctx = ctx ?? BeamContext.Default;
			return ctx.Resolve(contentRef, manifestId);
		}
	}
}
