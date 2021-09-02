using System.Collections.Generic;
using Beamable.Service;
using Beamable.Spew;

namespace Beamable.Serialization
{
   [EditorServiceResolver(typeof(EditorSingletonServiceResolver<StreamFactoryService>))]
   public class StreamFactoryService
   {
      private List<JsonSerializable.ISerializableFactory> loadFactories = new List<JsonSerializable.ISerializableFactory>();
      private List<JsonSerializable.IDeleteListener> deleteListeners = new List<JsonSerializable.IDeleteListener>();

      public void AddGlobalISerializableFactory(JsonSerializable.ISerializableFactory factory)
      {
         loadFactories.Add(factory);
         ServerStateLogger.LogFormat("Registered Factory {0}, now at {1} factories.", factory, loadFactories.Count);
      }

      public void RemoveGlobalISerializableFactory(JsonSerializable.ISerializableFactory factory)
      {
         loadFactories.Remove(factory);
      }

      public void AddGlobalDeleteListener(JsonSerializable.IDeleteListener listener)
      {
         deleteListeners.Add(listener);
      }

      public void RemoveGlobalDeleteListener(JsonSerializable.IDeleteListener listener)
      {
         deleteListeners.Remove(listener);
      }

      public JsonSerializable.LoadStream CreateLoadStream(IDictionary<string, object> data, JsonSerializable.ListMode mode)
      {
         var ls = JsonSerializable.LoadStream.Spawn();
         ls.Init(data, mode);
         for (int i = 0; i < loadFactories.Count; i++)
         {
            ls.RegisterISerializableFactory(loadFactories[i]);
         }
         return ls;
      }

      public JsonSerializable.DeleteStream CreateDeleteStream(IDictionary<string, object> data)
      {
         var ds = JsonSerializable.DeleteStream.Spawn();
         ds.Init(data);
         for (int i = 0; i < deleteListeners.Count; i++)
         {
            ds.RegisterDeleteListener(deleteListeners[i]);
         }
         return ds;
      }
   }
}
