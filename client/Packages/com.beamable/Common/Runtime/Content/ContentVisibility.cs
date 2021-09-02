namespace Beamable.Common.Content
{
   public enum ContentVisibility
   {
      Public, Private
   }

   public static class ContentVisibilityExtensions
   {
      public static ContentVisibility FromString(string str)
      {
         switch (str?.ToLower())
         {
            case ContentConstants.PUBLIC: return ContentVisibility.Public;
            case ContentConstants.PRIVATE: return ContentVisibility.Private;
            default: return ContentVisibility.Public;
         }
      }
   }
}