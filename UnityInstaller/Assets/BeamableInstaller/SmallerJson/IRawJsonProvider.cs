namespace Beamable.Installer.SmallerJSON
{
   public interface IRawJsonProvider
   {
      string ToJson();
   }

   public class RawJsonProvider : IRawJsonProvider
   {
      public string Json;

      public string ToJson()
      {
         return Json;
      }
   }
}