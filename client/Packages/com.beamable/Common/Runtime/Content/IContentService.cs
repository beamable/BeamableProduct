namespace Beamable.Common.Content
{
   public interface IContentService
   {
      Promise<TContent> Resolve<TContent>(IContentRef<TContent> reference) where TContent : IContentObject, new();
   }

   public static class ContentServiceResolver
   {

   }
}