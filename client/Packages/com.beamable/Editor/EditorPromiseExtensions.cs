using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Platform.SDK;
using UnityEditor;

namespace Beamable.Editor
{
   public static class EditorPromiseExtensions
   {
      public static Promise<T> EditorWait<T>(this Promise<T> self, float seconds)
      {
         var result = new Promise<T>();
         async Task Wait()
         {
            await Task.Delay((int) (seconds * 1000));
            var __ = self.Then(val =>
            {
               EditorApplication.delayCall += () => result.CompleteSuccess(val);
            }).Error(e => { EditorApplication.delayCall += () => result.CompleteError(e); });
         }


         var _ = Wait();

         return result;
      }
   }
}