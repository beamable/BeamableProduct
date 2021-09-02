using System.Collections.Generic;
using Beamable.Pooling;
using UnityEngine;

namespace Beamable.Serialization
{
   public class TestJsonSerializer : MonoBehaviour
   {

      void OnGUI()
      {
         if (GUILayout.Button("Test List"))
         {
            for (int i = 0; i < 10; ++i)
            {
               TestList();
            }
         }
         if (GUILayout.Button("Test Intrusive List"))
         {
            for (int i = 0; i < 10; ++i)
            {
               TestILL();
            }
         }
      }

      public class Item : ClassPool<Item>, JsonSerializable.ISerializable
      {
         public int mInt;
         public string mString;

         public void Serialize(JsonSerializable.IStreamSerializer s)
         {
            s.Serialize("mInt", ref mInt);
            s.Serialize("mString", ref mString);
         }
      }

      public class ListContainer : JsonSerializable.ISerializable
      {
         public List<Item> items = new List<Item>();

         public void Serialize(JsonSerializable.IStreamSerializer s)
         {
            s.SerializeList("items", ref items);
         }
      }

      ListContainer lc = new ListContainer();
      void TestList()
      {

         lc.items = new List<Item>();
         for (int i = 0; i < 1000; ++i)
         {
            var t = new Item();
            t.mInt = UnityEngine.Random.Range(0, 1000);
            t.mString = "blah";
            lc.items.Add(t);
         }

         var dct = JsonSerializable.Serialize(lc);
         lc.items.Clear();
         JsonSerializable.Deserialize(lc, dct);
         lc.items.Clear();
      }

      public class ILLContainer : JsonSerializable.ISerializable
      {
         public LinkedList<Item> items = new LinkedList<Item>();

         public void Serialize(JsonSerializable.IStreamSerializer s)
         {
            s.SerializeILL<Item>("items", ref items);
         }
      }

      ILLContainer illCon = new ILLContainer();
      void TestILL()
      {

         for (int i = 0; i < 1000; ++i)
         {
            var t = Item.Spawn();
            t.mInt = UnityEngine.Random.Range(0, 1000);
            t.mString = "blah";
            illCon.items.AddFirst(t.poolNode);
         }

         var dct = JsonSerializable.Serialize(illCon);

         var node = illCon.items.First;
         while (node != null)
         {
            var o = node.Value;
            node = node.Next;
            illCon.items.Remove(o.poolNode);
            o.Recycle();
         }

         JsonSerializable.Deserialize(illCon, dct);

         node = illCon.items.First;
         while (node != null)
         {
            var o = node.Value;
            node = node.Next;
            illCon.items.Remove(o.poolNode);
            o.Recycle();
         }
      }
   }
}
