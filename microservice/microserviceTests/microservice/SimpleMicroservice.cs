using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Beamable.Common;
using Beamable.Common.Inventory;
using Beamable.Server;
using UnityEngine;

namespace microserviceTests.microservice
{
   [Microservice("simple", UseLegacySerialization = true)]
   public class SimpleMicroservice : Microservice
   {
      public static MicroserviceFactory<SimpleMicroservice> Factory => () => new SimpleMicroservice();

      [ClientCallable]
      public int Sum(int[] numbers)
      {
         if (numbers == null) return 0;

         var sum = 0;
         for (var i = 0; i < numbers.Length; i++)
         {
            sum += numbers[i];
         }

         return sum;
      }

      [ClientCallable]
      public int TwoArrays(int[] arr1, int[] arr2)
      {
         return Sum(arr1) + Sum(arr2);
      }

      [ClientCallable]
      public int Add(int a, int b)
      {
         return a + b;
      }

      [ClientCallable(requiredScopes: new []{"someScope"})]
      public bool AdminOnly()
      {
         return true;
      }

      [ClientCallable]
      public async Task<int> Delay(int ms)
      {
         await Task.Delay(ms);
         return ms;
      }
      
      [ClientCallable]
      public async Promise<int> PromiseTestMethod()
      {
         return await Promise<int>.Successful(1);
      }
      
      [ClientCallable]
      public string MethodWithJSON_AsParameter(string jsonString)
      {
         return jsonString;
      }
      
      [ClientCallable]
      public string MethodWithRegularString_AsParameter(string str)
      {
         return str;
      }
      
      [ClientCallable]
      public Vector2Int MethodWithVector2Int_AsParameter(Vector2Int vec)
      {
         return vec;
      }

      // TODO: Add a test for an empty arg array, or a null

      [ClientCallable]
      public async Task<string> InventoryTest(ItemRef itemRef)
      {
         var items = await Services.Inventory.GetItems(itemRef);
         var x = items.FirstOrDefault();

         return x.ItemContent.Id;
      }

      [ClientCallable]
      public async Task<string> GetUserEmail(int dbid)
      {
         return (await Services.Auth.GetUser(dbid)).email;
      }

      [ClientCallable]
      public async Task<string> GetContent(string id)
      {
         var content = await Services.Content.GetContent(id);
         return "Echo: " + content.Id;
      }
   }
}