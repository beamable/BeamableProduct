using System.Linq;
using System.Numerics;
using Beamable.Server;

namespace microserviceTests.microservice
{
   [Microservice("named", EnableEagerContentLoading = false)]
   public class NamedSerializationMicroservice : Microservice
   {
      public static MicroserviceFactory<NamedSerializationMicroservice> Factory => () => new NamedSerializationMicroservice();

      [ClientCallable]
      public int Add(int a, int b)
      {
         return a + b;
      }

      [ClientCallable]
      public bool IsTrue([Parameter("notX")]bool x)
      {
         return x;
      }

      [ClientCallable]
      public int Sum(int[] arr)
      {
         return arr?.Sum() ?? 0;
      }

      [ClientCallable]
      public int ComplexInput(Vector2 xy, ComplexDoodad doodad)
      {
         var vectorParts = (int) (xy.X + xy.Y);
         var doodadPart = doodad.Sum();
         return vectorParts + doodadPart;
      }

      public class ComplexDoodad
      {
         public int X;
         public string Foo;
         public ComplexDoodad Recurse;

         public int Sum()
         {
            var subPart = Recurse?.Sum() ?? 0;
            return X + subPart;
         }
      }
   }
}
